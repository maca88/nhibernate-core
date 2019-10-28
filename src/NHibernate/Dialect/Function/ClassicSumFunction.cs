using System;

namespace NHibernate.Dialect.Function
{
	/// <summary>
	/// Classic SUM sqlfunction that return types as it was done in Hibernate 3.1
	/// </summary>
	[Serializable]
	// Since v5.3
	[Obsolete("This class has no more usages in NHibernate and will be removed in a future version.")]
	public class ClassicSumFunction : ClassicAggregateFunction
	{
		public ClassicSumFunction() : base("sum", false)
		{
		}
	}
}
