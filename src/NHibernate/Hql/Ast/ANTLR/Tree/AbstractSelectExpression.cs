using System;
using Antlr.Runtime;

namespace NHibernate.Hql.Ast.ANTLR.Tree
{
	[CLSCompliant(false)]
	public abstract class AbstractSelectExpression : HqlSqlWalkerNode, ISelectExpression 
	{
		private string _alias;
		private int _scalarColumnIndex = -1;

		protected AbstractSelectExpression(IToken token) : base(token)
		{
		}

		public string Alias
		{
			get { return _alias; }
			set { _alias = value; }
		}

		//Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public void SetScalarColumn(int i)
		{
			_scalarColumnIndex = i;
			SetScalarColumnText(i);
		}

		public string[] SetScalarColumn(int i, Func<int, int, string> aliasCreator)
		{
			_scalarColumnIndex = i;
			return SetScalarColumnText(i, aliasCreator);
		}

		public int ScalarColumnIndex
		{
			get { return _scalarColumnIndex; }
		}

		public bool IsConstructor
		{
			get { return false; }
		}

		public virtual bool IsReturnableEntity
		{
			get { return false; }
		}

		public virtual FromElement FromElement 
		{
			get { return null; }
			set {}
		}

		public virtual bool IsScalar
		{
			get
			{
				// Default implementation:
				// If this node has a data type, and that data type is not an association, then this is scalar.
				return DataType != null && !DataType.IsAssociationType; // Moved here from SelectClause [jsd]
			}
		}

		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public abstract void SetScalarColumnText(int i);

		public virtual string[] SetScalarColumnText(int i, Func<int, int, string> aliasCreator)
		{
#pragma warning disable 618
			SetScalarColumnText(i);
#pragma warning restore 618
			return new string[0];
		}
	}
}
