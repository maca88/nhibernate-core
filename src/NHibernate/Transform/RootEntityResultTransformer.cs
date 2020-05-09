using System;
using System.Collections;
using NHibernate.Util;

namespace NHibernate.Transform
{
	[Serializable]
	public class RootEntityResultTransformer : IResultTransformer, ITupleSubsetResultTransformer, IResultTransformerExtended
	{
		internal static readonly RootEntityResultTransformer Instance = new RootEntityResultTransformer();

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public object TransformTuple(object[] tuple, string[] aliases)
		{
			return tuple[tuple.Length - 1];
		}

		/// <inheritdoc />
		public object TransformTuple(object[] tuple, string[] aliases, object[] parameterValues)
		{
			return tuple[tuple.Length - 1];
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

		public bool IsTransformedValueATupleElement(String[] aliases, int tupleLength)
		{
			return true;
		}

		public bool[] IncludeInTransform(String[] aliases, int tupleLength)
		{
			bool[] includeInTransform;
			if (tupleLength == 1)
			{
				includeInTransform = ArrayHelper.True;
			}
			else
			{
				includeInTransform = new bool[tupleLength];
				includeInTransform[tupleLength - 1] = true;
			}
			return includeInTransform;
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
