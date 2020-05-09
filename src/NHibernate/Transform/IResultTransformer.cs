using System;
using System.Collections;

namespace NHibernate.Transform
{
	/// <summary>
	/// Implementors define a strategy for transforming criteria query
	/// results into the actual application-visible query result list.
	/// </summary>
	/// <seealso cref="NHibernate.ICriteria.SetResultTransformer(IResultTransformer)" />
	public interface IResultTransformer
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="tuple"></param>
		/// <param name="aliases"></param>
		/// <returns></returns>
		// Since v5.3
		[Obsolete("Use TransformTuple extension method with parameterValues parameter instead.")]
		object TransformTuple(object[] tuple, string[] aliases);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
		// Since v5.3
		[Obsolete("Use TransformList extension method with parameterValues parameter instead.")]
		IList TransformList(IList collection);
	}

	// TODO 6.0: Move to IResultTransformer
	internal interface IResultTransformerExtended : IResultTransformer
	{
		/// <summary>
		/// Transforms the retrieved row from the query to an object.
		/// </summary>
		/// <param name="values">The values for the row to transform.</param>
		/// <param name="aliases">Column aliases of <paramref name="values"/>.</param>
		/// <param name="parameterValues">The parameters values that are used for the query.</param>
		/// <returns>The transformed row.</returns>
		object TransformTuple(object[] values, string[] aliases, object[] parameterValues);

		/// <summary>
		/// Transforms the retrieved query collection.
		/// </summary>
		/// <param name="collection">The query collection.</param>
		/// <param name="parameterValues">The parameters values that are used for the query.</param>
		/// <returns>The transformed collection.</returns>
		IList TransformList(IList collection, object[] parameterValues);
	}

	// TODO 6.0: Remove
	public static class ResultTransformerExtensions
	{
		/// <summary>
		/// Transforms the retrieved row from the query to an object.
		/// </summary>
		/// <param name="transformer">The transformer.</param>
		/// <param name="values">The values for the row to transform.</param>
		/// <param name="aliases">Column aliases of <paramref name="values"/>.</param>
		/// <param name="parameterValues">The parameters values that are used for the query.</param>
		/// <returns>The transformed row.</returns>
		public static object TransformTuple(
			this IResultTransformer transformer,
			object[] values,
			string[] aliases,
			object[] parameterValues)
		{
			if (transformer is IResultTransformerExtended transformerExtended)
			{
				return transformerExtended.TransformTuple(values, aliases, parameterValues);
			}

#pragma warning disable 618
			return transformer.TransformTuple(values, aliases);
#pragma warning restore 618
		}

		/// <summary>
		/// Transforms the retrieved query collection.
		/// </summary>
		/// <param name="transformer">The transformer.</param>
		/// <param name="collection">The query collection.</param>
		/// <param name="parameterValues">The parameters values that are used for the query.</param>
		/// <returns>The transformed collection.</returns>
		public static IList TransformList(
			this IResultTransformer transformer,
			IList collection,
			object[] parameterValues)
		{
			if (transformer is IResultTransformerExtended transformerExtended)
			{
				return transformerExtended.TransformList(collection, parameterValues);
			}

#pragma warning disable 618
			return transformer.TransformList(collection);
#pragma warning restore 618
		}
	}
}
