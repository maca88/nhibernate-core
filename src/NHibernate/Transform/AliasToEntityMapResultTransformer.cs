using System;
using System.Collections;

namespace NHibernate.Transform
{
	[Serializable]
	public class AliasToEntityMapResultTransformer : AliasedTupleSubsetResultTransformer
	{
		internal static readonly AliasToEntityMapResultTransformer Instance = new AliasToEntityMapResultTransformer();

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public override object TransformTuple(object[] tuple, string[] aliases)
		{
			return TransformTuple(tuple, aliases, null);
		}

		/// <inheritdoc />
		public override object TransformTuple(object[] tuple, string[] aliases, object[] parameterValues)
		{
			IDictionary result = new Hashtable();
			for (int i = 0; i < tuple.Length; i++)
			{
				string alias = aliases[i];
				if (alias != null)
				{
					// TODO: Incredibly dodgy!! what if the user defines an alias ending in "_"
					result[alias] = tuple[i];
				}
			}

			return result;
		}

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public override IList TransformList(IList collection)
		{
			return collection;
		}

		/// <inheritdoc />
		public override IList TransformList(IList collection, object[] parameterValues)
		{
			return collection;
		}

		public override bool IsTransformedValueATupleElement(string[] aliases, int tupleLength)
		{
			return false;
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
