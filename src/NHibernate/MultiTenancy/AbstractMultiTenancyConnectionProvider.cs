using System;
using System.Data.Common;
using NHibernate.Connection;
using NHibernate.Engine;

namespace NHibernate.MultiTenancy
{
	/// <summary>
	/// Base implementation for multi-tenancy strategy.
	/// </summary>
	[Serializable]
	public abstract partial class AbstractMultiTenancyConnectionProvider : IMultiTenancyConnectionProvider
	{
		/// <inheritdoc />
		public IConnectionAccess GetConnectionAccess(TenantConfiguration configuration, ISessionFactoryImplementor sessionFactory)
		{
			var tenantConnectionString = GetTenantConnectionString(configuration, sessionFactory);
			if (string.IsNullOrEmpty(tenantConnectionString))
			{
				throw new HibernateException($"Tenant '{configuration.TenantIdentifier}' connection string is empty.");
			}

			return new ContextualConnectionAccess(tenantConnectionString, sessionFactory);
		}

		protected abstract string GetTenantConnectionString(TenantConfiguration configuration, ISessionFactoryImplementor sessionFactory);

		[Serializable]
		partial class ContextualConnectionAccess : IConnectionAccess
		{
			private readonly ISessionFactoryImplementor _sessionFactory;

			public ContextualConnectionAccess(string connectionString, ISessionFactoryImplementor sessionFactory)
			{
				ConnectionString = connectionString;
				_sessionFactory = sessionFactory;
			}

			public string ConnectionString { get; }

			public DbConnection GetConnection()
			{
				return _sessionFactory.ConnectionProvider.GetConnection(ConnectionString);
			}

			public void CloseConnection(DbConnection conn)
			{
				_sessionFactory.ConnectionProvider.CloseConnection(conn);
			}
		}
	}
}
