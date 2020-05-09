using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Transform;

namespace NHibernate.Linq
{
	[Serializable]
	public class ResultTransformer : IResultTransformer, IEquatable<ResultTransformer>, IResultTransformerExtended
	{
		private readonly Func<object[], object> _itemTransformation; // TODO 6.0: Remove
		private readonly Func<IEnumerable<object>, object> _listTransformation; // TODO 6.0: Remove
		private readonly Func<object[], object[], object> _itemTransformationParams; // TODO 6.0: Rename to _itemTransformation
		private readonly Func<IEnumerable<object>, object[], object> _listTransformationParams; // TODO 6.0: Rename to _listTransformation

		// Since v5.3
		[Obsolete("Use overload with Func<object[], object[], object> parameter instead.")]
		public ResultTransformer(Func<object[], object> itemTransformation, Func<IEnumerable<object>, object> listTransformation)
		{
			_itemTransformation = itemTransformation;
			_listTransformation = listTransformation;
		}

		public ResultTransformer(
			Func<object[], object[], object> itemTransformation,
			Func<IEnumerable<object>, object[], object> listTransformation)
		{
			_itemTransformationParams = itemTransformation;
			_listTransformationParams = listTransformation;
		}

		#region IResultTransformer Members

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public object TransformTuple(object[] tuple, string[] aliases)
		{
			return _itemTransformation == null ? tuple : _itemTransformation(tuple);
		}

		/// <inheritdoc />
		public object TransformTuple(object[] tuple, string[] aliases, object[] parameterValues)
		{
			return _itemTransformationParams == null ? tuple : _itemTransformationParams(tuple, parameterValues);
		}

		// Since v5.3
		[Obsolete("Use overload with parameterValues parameter instead.")]
		public IList TransformList(IList collection)
		{
			if (_listTransformation == null)
			{
				return collection;
			}

			var toTransform = GetToTransform(collection);
			var transformResult = _listTransformation(toTransform);

			var resultList = transformResult as IList;
			return resultList ?? new List<object> { transformResult };
		}

		public IList TransformList(IList collection, object[] parameterValues)
		{
			if (_listTransformationParams == null)
			{
				return collection;
			}

			var toTransform = GetToTransform(collection);
			var transformResult = _listTransformationParams(toTransform, parameterValues);

			var resultList = transformResult as IList;
			return resultList ?? new List<object> { transformResult };
		}

		static IEnumerable<object> GetToTransform(IList collection)
		{
			if (collection.Count > 0)
			{
				var objects = collection[0] as object[];
				if (objects != null && objects.Length == 1)
				{
					return collection.Cast<object[]>().Select(o => o[0]);
				}
			}
			return collection.Cast<object>();
		}

		#endregion

		public bool Equals(ResultTransformer other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			return Equals(other._listTransformation, _listTransformation) &&
				Equals(other._itemTransformation, _itemTransformation) &&
				Equals(other._listTransformationParams, _listTransformationParams) &&
				Equals(other._itemTransformationParams, _itemTransformationParams);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as ResultTransformer);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int lt = (_listTransformation != null ? _listTransformation.GetHashCode() : 0);
				int it = (_itemTransformation != null ? _itemTransformation.GetHashCode() : 0);
				int lt2 = (_listTransformationParams != null ? _listTransformationParams.GetHashCode() : 0);
				int it2 = (_itemTransformationParams != null ? _itemTransformationParams.GetHashCode() : 0);
				return (lt*397) ^ (it*17) ^ (lt2 * 397) ^ (it2 * 17);
			}
		}
	}
}
