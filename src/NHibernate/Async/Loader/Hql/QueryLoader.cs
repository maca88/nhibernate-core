﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Impl;
using NHibernate.Param;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Type;
using NHibernate.Util;
using IQueryable = NHibernate.Persister.Entity.IQueryable;

namespace NHibernate.Loader.Hql
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class QueryLoader : BasicLoader
	{

		public Task<IList> ListAsync(ISessionImplementor session, QueryParameters queryParameters, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<IList>(cancellationToken);
			}
			try
			{
				CheckQuery(queryParameters);
				return ListAsync(session, queryParameters, _queryTranslator.QuerySpaces, cancellationToken);
			}
			catch (Exception ex)
			{
				return Task.FromException<IList>(ex);
			}
		}

		protected override async Task<object> GetResultColumnOrRowAsync(object[] row, IResultTransformer resultTransformer, DbDataReader rs,
													   ISessionImplementor session, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Object[] resultRow = await (GetResultRowAsync(row, rs, session, cancellationToken)).ConfigureAwait(false);
			bool hasTransform = HasSelectNew || resultTransformer != null;
			return (!hasTransform && resultRow.Length == 1
				        ? resultRow[0]
				        : resultRow
			       );
		}

		protected override async Task<object[]> GetResultRowAsync(object[] row, DbDataReader rs, ISessionImplementor session, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			object[] resultRow;

			if (_hasScalars)
			{
				string[][] scalarColumns = _scalarColumnNames;
				int queryCols = ResultTypes.Length;
				resultRow = new object[queryCols];
				for (int i = 0; i < queryCols; i++)
				{
					resultRow[i] = await (ResultTypes[i].NullSafeGetAsync(rs, scalarColumns[i], session, null, cancellationToken)).ConfigureAwait(false);
				}
			}
			else
			{
				resultRow = ToResultRow(row);
			}

			return resultRow;
		}

		internal async Task<IEnumerable> GetEnumerableAsync(QueryParameters queryParameters, IEventSource session, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			CheckQuery(queryParameters);
			bool statsEnabled = session.Factory.Statistics.IsStatisticsEnabled;

			var stopWath = new Stopwatch();
			if (statsEnabled)
			{
				stopWath.Start();
			}

			var cmd = await (PrepareQueryCommandAsync(queryParameters, false, session, cancellationToken)).ConfigureAwait(false);

			// This DbDataReader is disposed of in EnumerableImpl.Dispose
			var rs = await (GetResultSetAsync(cmd, queryParameters, session, null, cancellationToken)).ConfigureAwait(false);

			var resultTransformer = _selectNewTransformer ?? queryParameters.ResultTransformer;
			IEnumerable result = 
				new EnumerableImpl(rs, cmd, session, queryParameters.IsReadOnly(session), _queryTranslator.ReturnTypes, _queryTranslator.GetColumnNames(), queryParameters.RowSelection, resultTransformer, _queryReturnAliases);

			if (statsEnabled)
			{
				stopWath.Stop();
				session.Factory.StatisticsImplementor.QueryExecuted("HQL: " + _queryTranslator.QueryString, 0, stopWath.Elapsed);
				// NH: Different behavior (H3.2 use QueryLoader in AST parser) we need statistic for orginal query too.
				// probably we have a bug some where else for statistic RowCount
				session.Factory.StatisticsImplementor.QueryExecuted(QueryIdentifier, 0, stopWath.Elapsed);
			}
			return result;
		}
	}
}
