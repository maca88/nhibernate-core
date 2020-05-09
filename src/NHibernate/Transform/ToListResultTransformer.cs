using System;
using System.Collections;
using System.Collections.Generic;

namespace NHibernate.Transform
{
	/// <summary> 
	/// Transforms each result row from a tuple into a <see cref="IList"/>, such that what
	/// you end up with is a <see cref="IList"/> of <see cref="IList"/>.
	/// </summary>
	[Serializable]
	public class ToListResultTransformer : IResultTransformer, IResultTransformerExtended
	{
		internal static readonly ToListResultTransformer Instance = new ToListResultTransformer();

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public object TransformTuple(object[] tuple, string[] aliases)
		{
			return new List<object>(tuple);
		}

		/// <inheritdoc />
		public object TransformTuple(object[] tuple, string[] aliases, object[] parameterValues)
		{
			return new List<object>(tuple);
		}

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public IList TransformList(IList list)
		{
			return list;
		}

		/// <inheritdoc />
		public IList TransformList(IList list, object[] parameterValues)
		{
			return list;
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
