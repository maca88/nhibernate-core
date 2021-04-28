using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Impl;
using NHibernate.Linq;
using NHibernate.Multi;
using NHibernate.Persister.Entity;
using NHibernate.Test.CacheTest.Caches;
using NUnit.Framework;
using Environment = NHibernate.Cfg.Environment;

namespace NHibernate.Test.CacheTest
{
	public class DistributedUpdateTimestampsCache : UpdateTimestampsCache
	{
		protected readonly ISet<string> CacheableSpaces;

		public DistributedUpdateTimestampsCache(CacheBase updateTimestamps, ISet<string> cacheableSpaces) : base(updateTimestamps)
		{
			CacheableSpaces = cacheableSpaces;
		}

		public override void Invalidate(IReadOnlyCollection<string> spaces)
		{
			spaces = spaces.Where(x => CacheableSpaces.Contains(x)).ToList();

			if (spaces.Any())
			{
				base.Invalidate(spaces);
			}
		}

		public override void PreInvalidate(IReadOnlyCollection<string> spaces)
		{
			spaces = spaces.Where(x => CacheableSpaces.Contains(x)).ToList();

			if (spaces.Any())
			{
				base.PreInvalidate(spaces);
			}
		}

		public override Task InvalidateAsync(IReadOnlyCollection<string> spaces, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}

			spaces = spaces.Where(x => CacheableSpaces.Contains(x)).ToList();

			if (!spaces.Any())
			{
				return Task.CompletedTask;
			}

			return base.InvalidateAsync(spaces, cancellationToken);
		}

		public override Task PreInvalidateAsync(IReadOnlyCollection<string> spaces, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}

			spaces = spaces.Where(x => CacheableSpaces.Contains(x)).ToList();

			if (!spaces.Any())
			{
				return Task.CompletedTask;
			}

			return base.PreInvalidateAsync(spaces, cancellationToken);
		}
	}

	[TestFixture]
	public class UserOrganizationFixture : TestCase
	{
		protected override string[] Mappings => new[]
		{
			"CacheTest.UserOrganization.hbm.xml"
		};

		protected override string MappingsAssembly => "NHibernate.Test";

		protected override string CacheConcurrencyStrategy => null;

		protected override void Configure(Configuration configuration)
		{
			configuration.SetProperty(Environment.UseSecondLevelCache, "true");
			configuration.SetProperty(Environment.UseQueryCache, "true");
			configuration.SetProperty(Environment.GenerateStatistics, "true");
			configuration.SetProperty(Environment.CacheProvider, typeof(BatchableCacheProvider).AssemblyQualifiedName);
		}

		protected override void OnSetUp()
		{
			var cacheablePersisters = Sfi.GetAllClassMetadata()
				.Where(x => x.Value.GetType() == typeof(SingleTableEntityPersister))
				.Select(x => x.Value)
				.Cast<SingleTableEntityPersister>()
				.Where(x => x.HasCache)
				.ToList();

			var sessionFactoryImpl = (SessionFactoryImpl) ((DebugSessionFactory) Sfi).ActualFactory;
			var standardQueryCache = (StandardQueryCache) sessionFactoryImpl.QueryCache;
			var updateTimestamps = (CacheBase) typeof(UpdateTimestampsCache).GetField("_updateTimestamps", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sessionFactoryImpl.UpdateTimestampsCache);
			var updateTimestampsCache = new DistributedUpdateTimestampsCache(updateTimestamps, cacheablePersisters.SelectMany(x => x.PropertySpaces).ToHashSet());

			// Set fields
			typeof(SessionFactoryImpl).GetField("updateTimestampsCache", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sessionFactoryImpl, updateTimestampsCache);
			typeof(StandardQueryCache).GetField("_updateTimestampsCache", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(standardQueryCache, updateTimestampsCache);

			using (var s = Sfi.OpenSession())
			using (var tx = s.BeginTransaction())
			{
				var totalItems = 6;
				for (var i = 1; i <= totalItems; i++)
				{
					var parent = new Organization
					{
						Name = $"Name{i}"
					};
					for (var j = 1; j <= totalItems; j++)
					{
						var child = new User
						{
							Organization = parent
						};
						parent.Users.Add(child);
					}
					s.Save(parent);
				}

				tx.Commit();
			}
		}

		protected override void OnTearDown()
		{
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				s.CreateQuery("delete from User").ExecuteUpdate();
				s.CreateQuery("delete from Organization").ExecuteUpdate();
				tx.Commit();
			}

			Sfi.EvictQueries();
		}

		[Test]
		public void Test()
		{
			dynamic results;

			using (var s = Sfi.OpenSession())
			using (var tx = s.BeginTransaction())
			{
				results = s.Query<User>()
					.WithOptions(o => o.SetCacheable(true))
					.Where(o => o.Organization.Name == "Name1")
					.Select(o => new
					{
						o.Id,
						o.Name,
						OrganizationName = o.Organization.Name
					})
					.ToList();

				tx.Commit();
			}

			Assert.That(results, Has.Count.EqualTo(6));

			Thread.Sleep(1000);

			using (var s = Sfi.OpenSession())
			using (var tx = s.BeginTransaction())
			{
				var organization = s.Query<Organization>().First(o => o.Name == "Name1");
				organization.Name = "Test";

				tx.Commit();
			}

			using (var s = Sfi.OpenSession())
			using (var tx = s.BeginTransaction())
			{
				results = s.Query<User>()
					.WithOptions(o => o.SetCacheable(true))
					.Where(o => o.Organization.Name == "Name1")
					.Select(o => new
					{
						o.Id,
						o.Name,
						OrganizationName = o.Organization.Name
					})
					.ToList();

				tx.Commit();
			}

			Assert.That(results, Has.Count.EqualTo(0));
		}

		private BatchableCache GetDefaultQueryCache()
		{
			var queryCache = Sfi.GetQueryCache(null);
			var field = typeof(StandardQueryCache).GetField(
				"_cache",
				BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.That(field, Is.Not.Null, "Unable to find _cache field");
			var cache = (BatchableCache) field.GetValue(queryCache);
			Assert.That(cache, Is.Not.Null, "_cache is null");

			return cache;
		}
	}
}
