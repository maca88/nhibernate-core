using System;
using System.Collections;
using NHibernate.Util;

namespace NHibernate.Transform
{
	[Serializable]
	public class PassThroughResultTransformer : IResultTransformer, ITupleSubsetResultTransformer, IResultTransformerExtended
	{
		internal static readonly PassThroughResultTransformer Instance = new PassThroughResultTransformer();

		#region IResultTransformer Members

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public object TransformTuple(object[] tuple, string[] aliases)
		{
			return tuple.Length == 1 ? tuple[0] : tuple;
		}

		/// <inheritdoc />
		public object TransformTuple(object[] tuple, string[] aliases, object[] parameterValues)
		{
			return tuple.Length == 1 ? tuple[0] : tuple;
		}

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public IList TransformList(IList collection)
		{
			return collection;
		}

		/// <inheritdoc />
		public IList TransformList(IList collection, object[] parameterValues)
		{
			return collection;
		}

		#endregion

		public bool IsTransformedValueATupleElement(string[] aliases, int tupleLength)
		{
			return tupleLength == 1;
		}

		public bool[] IncludeInTransform(string[] aliases, int tupleLength)
		{
			bool[] includeInTransformedResult = new bool[tupleLength];
			ArrayHelper.Fill(includeInTransformedResult, true);
			return includeInTransformedResult;
		}

		internal IList UntransformToTuples(IList results, bool isSingleResult)
		{
			// untransform only if necessary; if transformed, do it in place;
			if (isSingleResult)
			{
				for (int i = 0; i < results.Count; i++)
				{
					Object[] tuple = UntransformToTuple(results[i], isSingleResult);
					results[i]= tuple;
				}
			}
			return results;
		}

		internal object[] UntransformToTuple(object transformed, bool isSingleResult)
		{
			return isSingleResult ? new[] {transformed} : (object[]) transformed;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, this))
				return true;
			if (obj == null)
				return false;
			return obj.GetType() == GetType();
		}

		public override int GetHashCode()
		{
			return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Instance);
		}
	}
}
