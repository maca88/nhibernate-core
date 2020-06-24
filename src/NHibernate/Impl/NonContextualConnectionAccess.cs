using System;
using System.Data.Common;
using NHibernate.Connection;
using NHibernate.Engine;

namespace NHibernate.Impl
{
	[Serializable]
	partial class NonContextualConnectionAccess : IConnectionAccess
	{
		private readonly ISessionFactoryImplementor _sessionFactory;

		public NonContextualConnectionAccess(ISessionFactoryImplementor connectionProvider)
		{
			_sessionFactory = connectionProvider;
		}

		public DbConnection GetConnection()
		{
			return _sessionFactory.ConnectionProvider.GetConnection();
		}

		public void CloseConnection(DbConnection conn)
		{
			_sessionFactory.ConnectionProvider.CloseConnection(conn);
		}
	}
}
