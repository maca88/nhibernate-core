﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using NHibernate.Cache;
using NHibernate.Engine;
using NHibernate.Hql;
using NHibernate.Param;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Transform;
using NHibernate.Type;
using IQueryable = NHibernate.Persister.Entity.IQueryable;

namespace NHibernate.Loader.Custom
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class CustomLoader : Loader
	{

		public Task<IList> ListAsync(ISessionImplementor session, QueryParameters queryParameters, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<IList>(cancellationToken);
			}
			return ListAsync(session, queryParameters, querySpaces, cancellationToken);
		}

		// Not ported: scroll

		// Since v5.3
		[Obsolete("Use overload with QueryParameters parameter instead.")]
		protected override Task<object> GetResultColumnOrRowAsync(object[] row, IResultTransformer resultTransformer, DbDataReader rs,
		                                               ISessionImplementor session, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return rowProcessor.BuildResultRowAsync(row, rs, resultTransformer != null, session, cancellationToken);
		}

		protected override Task<object> GetResultColumnOrRowAsync(object[] row, QueryParameters queryParameters, DbDataReader rs,
		                                               ISessionImplementor session, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return rowProcessor.BuildResultRowAsync(row, rs, queryParameters.ResultTransformer != null, session, cancellationToken);
		}

		protected override Task<object[]> GetResultRowAsync(object[] row, DbDataReader rs, ISessionImplementor session, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object[]>(cancellationToken);
			}
			return rowProcessor.BuildResultRowAsync(row, rs, session, cancellationToken);
		}

		public partial class ResultRowProcessor
		{

			/// <summary> Build a logical result row. </summary>
			/// <param name="data">
			/// Entity data defined as "root returns" and already handled by the normal Loader mechanism.
			/// </param>
			/// <param name="resultSet">The ADO result set (positioned at the row currently being processed). </param>
			/// <param name="hasTransformer">Does this query have an associated <see cref="IResultTransformer"/>. </param>
			/// <param name="session">The session from which the query request originated.</param>
			/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
			/// <returns> The logical result row </returns>
			/// <remarks>
			/// At this point, Loader has already processed all non-scalar result data.  We
			/// just need to account for scalar result data here...
			/// </remarks>
			public async Task<object> BuildResultRowAsync(object[] data, DbDataReader resultSet, bool hasTransformer, ISessionImplementor session, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				object[] resultRow;
				// NH Different behavior (patched in NH-1612 to solve Hibernate issue HHH-2831).
				if (!hasScalars && (hasTransformer || data.Length == 0))
				{
					resultRow = data;
				}
				else
				{
					resultRow = await (ExtractResultRowAsync(data, resultSet, session, cancellationToken)).ConfigureAwait(false);
				}

				return (hasTransformer) ? resultRow : (resultRow.Length == 1) ? resultRow[0] : resultRow;
			}

			public Task<object[]> BuildResultRowAsync(object[] data, DbDataReader resultSet, ISessionImplementor session, CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled<object[]>(cancellationToken);
				}
				// NH Different behavior (patched in NH-1612 to solve Hibernate issue HHH-2831).
				return !hasScalars && data.Length == 0 ? Task.FromResult<object[]>(data ): ExtractResultRowAsync(data, resultSet, session, cancellationToken);
			}

			private async Task<object[]> ExtractResultRowAsync(object[] data, DbDataReader resultSet, ISessionImplementor session, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				// build an array with indices equal to the total number
				// of actual returns in the result Hibernate will return
				// for this query (scalars + non-scalars)
				var resultRow = new object[columnProcessors.Length];
				for (var i = 0; i < columnProcessors.Length; i++)
				{
					resultRow[i] = await (columnProcessors[i].ExtractAsync(data, resultSet, session, cancellationToken)).ConfigureAwait(false);
				}
				return resultRow;
			}
		}

		public partial interface IResultColumnProcessor
		{
			Task<object> ExtractAsync(object[] data, DbDataReader resultSet, ISessionImplementor session, CancellationToken cancellationToken);
		}

		public partial class NonScalarResultColumnProcessor : IResultColumnProcessor
		{

			public Task<object> ExtractAsync(object[] data, DbDataReader resultSet, ISessionImplementor session, CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled<object>(cancellationToken);
				}
				try
				{
					return Task.FromResult<object>(Extract(data, resultSet, session));
				}
				catch (Exception ex)
				{
					return Task.FromException<object>(ex);
				}
			}
		}

		public partial class ScalarResultColumnProcessor : IResultColumnProcessor
		{

			public Task<object> ExtractAsync(object[] data, DbDataReader resultSet, ISessionImplementor session, CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return Task.FromCanceled<object>(cancellationToken);
				}
				return type.NullSafeGetAsync(resultSet, alias, session, null, cancellationToken);
			}
		}
	}
}
