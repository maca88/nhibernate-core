using System;
using System.Collections;
using System.Reflection;

namespace NHibernate.Transform
{
	[Serializable]
	public class AliasToBeanConstructorResultTransformer : IResultTransformer, IResultTransformerExtended
	{
		private readonly ConstructorInfo constructor;

		public AliasToBeanConstructorResultTransformer(ConstructorInfo constructor)
		{
			if (constructor == null)
			{
				throw new ArgumentNullException("constructor");
			}
			this.constructor = constructor;
		}

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public object TransformTuple(object[] tuple, string[] aliases)
		{
			return TransformTuple(tuple, aliases, null);
		}

		/// <inheritdoc />
		public object TransformTuple(object[] tuple, string[] aliases, object[] parameterValues)
		{
			try
			{
				return constructor.Invoke(tuple);
			}
			catch (Exception e)
			{
				throw new QueryException(
					"could not instantiate: " +
					constructor.DeclaringType.FullName,
					e);
			}
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

		public bool Equals(AliasToBeanConstructorResultTransformer other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			return Equals(other.constructor, constructor);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as AliasToBeanConstructorResultTransformer);
		}

		public override int GetHashCode()
		{
			return constructor.GetHashCode();
		}
	}
}
