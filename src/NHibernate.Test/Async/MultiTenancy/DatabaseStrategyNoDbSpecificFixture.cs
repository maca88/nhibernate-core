﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Data.SqlClient;
using System.Linq;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Engine;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NHibernate.MultiTenancy;
using NUnit.Framework;

namespace NHibernate.Test.MultiTenancy
{
	using System.Threading.Tasks;
	[TestFixture]
	public class DatabaseStrategyNoDbSpecificFixtureAsync : TestCaseMappingByCode
	{
		private Guid _id;

		protected override void Configure(Configuration configuration)
		{
			configuration.Properties[Cfg.Environment.MultiTenant] = MultiTenancyStrategy.Database.ToString();
			configuration.Properties[Cfg.Environment.GenerateStatistics] = true.ToString();
			base.Configure(configuration);
		}

		[Test]
		public async Task SecondLevelCacheReusedForSameTenantAsync()
		{
			using (var sesTen1 = OpenTenantSession("tenant1"))
			{
				var entity = await (sesTen1.GetAsync<Entity>(_id));
			}

			Sfi.Statistics.Clear();
			using (var sesTen2 = OpenTenantSession("tenant1"))
			{
				var entity = await (sesTen2.GetAsync<Entity>(_id));
			}

			Assert.That(Sfi.Statistics.PrepareStatementCount, Is.EqualTo(0));
			Assert.That(Sfi.Statistics.SecondLevelCacheHitCount, Is.EqualTo(1));
		}

		[Test]
		public async Task SecondLevelCacheSeparationPerTenantAsync()
		{
			using (var sesTen1 = OpenTenantSession("tenant1"))
			{
				var entity = await (sesTen1.GetAsync<Entity>(_id));
			}

			Sfi.Statistics.Clear();
			using (var sesTen2 = OpenTenantSession("tenant2"))
			{
				var entity = await (sesTen2.GetAsync<Entity>(_id));
			}

			Assert.That(Sfi.Statistics.PrepareStatementCount, Is.EqualTo(1));
			Assert.That(Sfi.Statistics.SecondLevelCacheHitCount, Is.EqualTo(0));
		}

		[Test]
		public async Task QueryCacheReusedForSameTenantAsync()
		{
			using (var sesTen1 = OpenTenantSession("tenant1"))
			{
				var entity = await (sesTen1.Query<Entity>().WithOptions(x => x.SetCacheable(true)).Where(e => e.Id == _id).SingleOrDefaultAsync());
			}

			Sfi.Statistics.Clear();
			using (var sesTen2 = OpenTenantSession("tenant1"))
			{
				var entity = await (sesTen2.Query<Entity>().WithOptions(x => x.SetCacheable(true)).Where(e => e.Id == _id).SingleOrDefaultAsync());
			}

			Assert.That(Sfi.Statistics.PrepareStatementCount, Is.EqualTo(0));
			Assert.That(Sfi.Statistics.QueryCacheHitCount, Is.EqualTo(1));
		}

		[Test]
		public async Task QueryCacheSeparationPerTenantAsync()
		{
			using (var sesTen1 = OpenTenantSession("tenant1"))
			{
				var entity = await (sesTen1.Query<Entity>().WithOptions(x => x.SetCacheable(true)).Where(e => e.Id == _id).SingleOrDefaultAsync());
			}

			Sfi.Statistics.Clear();
			using (var sesTen2 = OpenTenantSession("tenant2"))
			{
				var entity = await (sesTen2.Query<Entity>().WithOptions(x => x.SetCacheable(true)).Where(e => e.Id == _id).SingleOrDefaultAsync());
			}

			Assert.That(Sfi.Statistics.PrepareStatementCount, Is.EqualTo(1));
			Assert.That(Sfi.Statistics.QueryCacheHitCount, Is.EqualTo(0));
		}

		private ISession OpenTenantSession(string tenantId)
		{
			return Sfi.WithOptions().TenantConfiguration(GetTenantConfig(tenantId)).OpenSession();
		}

		private TenantConfiguration GetTenantConfig(string tenantId)
		{
			return new TenantConfiguration(new TestTenantConnectionProvider(Sfi, tenantId));
		}

		#region Test Setup

		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();

			mapper.Class<Entity>(
				rc =>
				{
					rc.Cache(m => m.Usage(CacheUsage.NonstrictReadWrite));
					rc.Id(x => x.Id, m => m.Generator(Generators.GuidComb));
					rc.Property(x => x.Name);
				});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override ISession OpenSession()
		{
			return OpenTenantSession("defaultTenant");
		}

		protected override void OnTearDown()
		{
			using (ISession session = OpenSession())
			using (ITransaction transaction = session.BeginTransaction())
			{
				session.Delete("from System.Object");

				session.Flush();
				transaction.Commit();
			}
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var e1 = new Entity {Name = "Bob"};
				session.Save(e1);

				var e2 = new Entity {Name = "Sally"};
				session.Save(e2);

				session.Flush();
				transaction.Commit();
				_id = e1.Id;
			}
		}

		#endregion Test Setup
	}
}
