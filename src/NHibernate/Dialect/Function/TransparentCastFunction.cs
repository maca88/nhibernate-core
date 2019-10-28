using System;
using System.Collections;
using NHibernate.Engine;
using NHibernate.SqlCommand;

namespace NHibernate.Dialect.Function
{
	/// <summary>
	/// A HQL only cast for helping HQL knowing the type. Does not generates any actual cast in SQL code.
	/// </summary>
	[Serializable]
	public class TransparentCastFunction : CastFunction
	{
		protected override bool CastingIsRequired(string sqlType)
		{
			return false;
		}

		// Avoid checking cast type name as it may not exist
		public override SqlString Render(IList args, ISessionFactoryImplementor factory)
		{
			return new SqlString("(", args[0], ")");
		}
	}
}
