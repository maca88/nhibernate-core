using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.Type;

namespace NHibernate.Dialect.Function
{
	/// <summary>
	/// Classic AVG sqlfunction that return types as it was done in Hibernate 3.1
	/// </summary>
	// Since v5.3
	[Obsolete("This class has no more usages in NHibernate and will be removed in a future version.")]
	[Serializable]
	public class ClassicAvgFunction : ClassicAggregateFunction
	{
		public ClassicAvgFunction() : base("avg", false)
		{
		}

		// Since v5.3
		[Obsolete("Use GetReturnType method instead.")]
		public override IType ReturnType(IType columnType, IMapping mapping)
		{
			return GetReturnType(new IType[] {columnType}, mapping, true);
		}

		/// <inheritdoc />
		public override IType GetReturnType(IEnumerable<IType> argumentTypes, IMapping mapping, bool throwOnError)
		{
			if (!TryGetArgumentType(argumentTypes, mapping, throwOnError, out var argumentType, out var sqlType))
			{
				return null;
			}

			if (sqlType.DbType == DbType.Int16 || sqlType.DbType == DbType.Int32 || sqlType.DbType == DbType.Int64)
			{
				return NHibernateUtil.Single;
			}
			else
			{
				return argumentType;
			}
		}
	}
}
