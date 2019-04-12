using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using NHibernate.Hql.Ast.ANTLR.Util;
using NHibernate.Type;

namespace NHibernate.Hql.Ast.ANTLR.Tree
{
	/// <summary>
	/// Represents the list of expressions in a SELECT clause.
	/// Author: josh
	/// Ported by: Steve Strong
	/// </summary>
	[CLSCompliant(false)]
	public class SelectClause : SelectExpressionList
	{
		private const string JoinFetchWithoutOwnerExceptionMsg = "Query specified join fetching, but the owner of the fetched association was not present in the select list [{0}]";
		private bool _prepared;
		private bool _scalarSelect;
		private List<FromElement> _collectionFromElements;
		private IType[] _queryReturnTypes;
		// An 2d array of column names, the first dimension is parallel with the
		// return types array. The second dimension is the list of column names for each
		// type.
		private string[][] _scalarColumnNames;
		private readonly List<FromElement> _fromElementsForLoad = new List<FromElement>();
		private ConstructorNode _constructorNode;
		private string[] _aliases;
		private int[] _columnNamesStartPositions;

		public static bool VERSION2_SQL;

		public SelectClause(IToken token)
			: base(token)
		{
		}


		/// <summary>
		/// Prepares a derived (i.e., not explicitly defined in the query) select clause.
		/// </summary>
		/// <param name="fromClause">The from clause to which this select clause is linked.</param>
		public void InitializeDerivedSelectClause(FromClause fromClause)
		{
			if (_prepared)
			{
				throw new InvalidOperationException("SelectClause was already prepared!");
			}

			//Used to be tested by the TCK but the test is no longer here
			//		if ( getSessionFactoryHelper().isStrictJPAQLComplianceEnabled() && !getWalker().isSubQuery() ) {
			//			// NOTE : the isSubQuery() bit is a temporary hack...
			//			throw new QuerySyntaxException( "JPA-QL compliance requires select clause" );
			//		}
			var fromElements = fromClause.GetProjectionListTyped();
			var queryReturnTypeList = new List<IType>(fromElements.Count);
			foreach (FromElement fromElement in fromElements)
			{
				IType type = fromElement.SelectType;

				AddCollectionFromElement(fromElement);

				if (type != null)
				{
					bool collectionOfElements = fromElement.IsCollectionOfValuesOrComponents;
					if (!collectionOfElements)
					{
						if (!fromElement.IsFetch)
						{
							// Add the type to the list of returned sqlResultTypes.
							queryReturnTypeList.Add(type);
						}
						// We will generate the select expression later in order to avoid having different
						// columns order when _scalarSelect is true
						_fromElementsForLoad.Add(fromElement);
					}
				}
			}

			_queryReturnTypes = queryReturnTypeList.ToArray();

			// Get all the select expressions (that we just generated) and render the select.
			if (Walker.IsShallowQuery)
			{
				RenderScalarSelects(CollectSelectExpressions(), fromClause);
				InitializeScalarColumnNames();
			}
			else
			{
				RenderNonScalarSelects(CollectSelectExpressions(false, e => !e.IsScalar), fromClause, _fromElementsForLoad);
			}

			FinishInitialization();
		}

		/// <summary>
		/// Prepares an explicitly defined select clause.
		/// </summary>
		/// <param name="fromClause">The from clause linked to this select clause.</param>
		/// <exception cref="SemanticException"></exception>
		public void InitializeExplicitSelectClause(FromClause fromClause)
		{
			if (_prepared)
			{
				throw new InvalidOperationException("SelectClause was already prepared!");
			}

			//explicit = true;	// This is an explict Select.
			//ArrayList sqlResultTypeList = new ArrayList();
			List<IType> queryReturnTypeList = new List<IType>();

			// First, collect all of the select expressions.
			// NOTE: This must be done *before* invoking setScalarColumnText() because setScalarColumnText()
			// changes the AST!!!
			var selectExpressions = CollectSelectExpressions();
			var nonScalarExpressions = !Walker.IsShallowQuery
				? CollectSelectExpressions(true, e => !e.IsScalar)
				: null;

			for (int i = 0; i < selectExpressions.Length; i++)
			{
				ISelectExpression expr = selectExpressions[i];

				if (expr.IsConstructor)
				{
					_constructorNode = (ConstructorNode)expr;
					IList<IType> constructorArgumentTypeList = _constructorNode.ConstructorArgumentTypeList;
					//sqlResultTypeList.addAll( constructorArgumentTypeList );
					queryReturnTypeList.AddRange(constructorArgumentTypeList);
					_scalarSelect = true;

					for (int j = 1; j < _constructorNode.ChildCount; j++)
					{
						ISelectExpression se = _constructorNode.GetChild(j) as ISelectExpression;

						if (se != null && IsReturnableEntity(se))
						{
							_fromElementsForLoad.Add(se.FromElement);
						}
					}
				}
				else
				{
					IType type = expr.DataType;
					if (type == null && !(expr is ParameterNode))
					{
						throw new QueryException("No data type for node: " + expr.GetType().Name + " " + new ASTPrinter().ShowAsString((IASTNode)expr, ""));
					}
					//sqlResultTypeList.add( type );

					// If the data type is not an association type, it could not have been in the FROM clause.
					if (expr.IsScalar)
					{
						_scalarSelect = true;
					}

					if (IsReturnableEntity(expr))
					{
						_fromElementsForLoad.Add(expr.FromElement);
					}

					// Always add the type to the return type list.
					queryReturnTypeList.Add(type);
				}
			}

			_queryReturnTypes = queryReturnTypeList.ToArray();

			//init the aliases, after initing the constructornode
			InitAliases(selectExpressions);

			if (_scalarSelect || Walker.IsShallowQuery)
			{
				// If there are any scalars (non-entities) selected, render the select column aliases.
				RenderScalarSelects(selectExpressions, fromClause);
				InitializeScalarColumnNames();
			}

			if (!Walker.IsShallowQuery)
			{
				var fetchedFromElements = new List<FromElement>();
				// add the fetched entities
				var fromElements = fromClause.GetProjectionListTyped();
				foreach (FromElement fromElement in fromElements)
				{
					if (fromElement.IsFetch)
					{
						var origin = GetOrigin(fromElement);

						// Only perform the fetch if its owner is included in the select 
						if (!_fromElementsForLoad.Contains(origin))
						{
							// NH-2846: Before 2012-01-18, we threw this exception. However, some
							// components using LINQ (e.g. paging) like to automatically append e.g. Count(). It
							// can then be difficult to avoid having a bogus fetch statement, so just ignore those.
							// An alternative solution may be to have the linq provider filter out the fetch instead.
							// throw new QueryException(string.Format(JoinFetchWithoutOwnerExceptionMsg, fromElement.GetDisplayText()));

							//throw away the fromElement. It's clearly redundant.
							fromElement.Parent.RemoveChild(fromElement);
						}
						else
						{

							IType type = fromElement.SelectType;
							AddCollectionFromElement(fromElement);

							if (type != null)
							{
								bool collectionOfElements = fromElement.IsCollectionOfValuesOrComponents;
								if (!collectionOfElements)
								{
									// Add the type to the list of returned sqlResultTypes.
									fromElement.IncludeSubclasses = true;
									_fromElementsForLoad.Add(fromElement);
									//sqlResultTypeList.add( type );
									// We will generate the select expression later in order to avoid having different
									// columns order when _scalarSelect is true
									fetchedFromElements.Add(fromElement);
								}
							}
						}
					}
				}

				// generate id select fragment and then property select fragment for
				// each expression, just like generateSelectFragments().
				RenderNonScalarSelects(nonScalarExpressions, fromClause, fetchedFromElements);
			}

			FinishInitialization();
		}

		private static FromElement GetOrigin(FromElement fromElement)
		{
			var realOrigin = fromElement.RealOrigin;
			if (realOrigin != null)
				return realOrigin;

			// work around that crazy issue where the tree contains
			// "empty" FromElements (no text); afaict, this is caused
			// by FromElementFactory.createCollectionJoin()
			var origin = fromElement.Origin;
			if (origin == null)
				throw new QueryException("Unable to determine origin of join fetch [" + fromElement.GetDisplayText() + "]");

			return origin;
		}

		/// <summary>
		/// FromElements which need to be accounted for in the load phase (either for return or for fetch).
		/// </summary>
		public IList<FromElement> FromElementsForLoad
		{
			get { return _fromElementsForLoad; }
		}

		public bool IsScalarSelect
		{
			get { return _scalarSelect; }
		}

		public bool IsDistinct
		{
			get { return ChildCount > 0 && GetChild(0).Type == HqlSqlWalker.DISTINCT; }
		}

		/// <summary>
		/// The column alias names being used in the generated SQL.
		/// </summary>
		public string[][] ColumnNames
		{
			get { return _scalarColumnNames; }
		}

		/// <summary>
		/// The constructor to use for dynamic instantiation queries.
		/// </summary>
		public ConstructorInfo Constructor
		{
			get { return _constructorNode == null ? null : _constructorNode.Constructor; }
		}

		public bool IsMap
		{
			get { return _constructorNode != null && _constructorNode.IsMap; }
		}

		public bool IsList
		{
			get { return _constructorNode != null && _constructorNode.IsList; }
		}

		/// <summary>
		/// The HQL aliases, or generated aliases
		/// </summary>
		public string[] QueryReturnAliases
		{
			get { return _aliases; }
		}

		public IList<FromElement> CollectionFromElements
		{
			get { return _collectionFromElements; }
		}

		/// <summary>
		/// The types actually being returned from this query at the "object level".
		/// </summary>
		public IType[] QueryReturnTypes
		{
			get { return _queryReturnTypes; }
		}

		protected internal override IASTNode GetFirstSelectExpression()
		{
			foreach (IASTNode child in this)
			{
				if (!(child.Type == HqlSqlWalker.DISTINCT || child.Type == HqlSqlWalker.ALL))
				{
					return child;
				}
			}

			return null;
		}

		private static bool IsReturnableEntity(ISelectExpression selectExpression)
		{
			FromElement fromElement = selectExpression.FromElement;
			bool isFetchOrValueCollection = fromElement != null &&
					(fromElement.IsFetch || fromElement.IsCollectionOfValuesOrComponents);

			if (isFetchOrValueCollection)
			{
				return false;
			}
			else
			{
				return selectExpression.IsReturnableEntity;
			}
		}

		private void InitAliases(ISelectExpression[] selectExpressions)
		{
			if (_constructorNode == null)
			{
				_aliases = new String[selectExpressions.Length];
				for (int i = 0; i < selectExpressions.Length; i++)
				{
					string alias = selectExpressions[i].Alias;
					_aliases[i] = alias ?? i.ToString();
				}
			}
			else
			{
				_aliases = _constructorNode.GetAliases();
			}
		}

		private void RenderNonScalarSelects(
			ISelectExpression[] nonScalarExpressions,
			FromClause currentFromClause,
			IList<FromElement> fetchedFromElements)
		{
			var nonscalarSize = nonScalarExpressions.Length + fetchedFromElements.Count;
			var appender = new ASTAppender(ASTFactory, this);

			var j = 0;
			foreach (var e in nonScalarExpressions)
			{
				var fromElement = e.FromElement;
				if (fromElement != null)
				{
					RenderNonScalarIdentifiers(fromElement, nonscalarSize, j, e, appender);
					j++;
				}
			}

			var combinedExpressions = new List<ISelectExpression>(nonScalarExpressions);
			// Append fetched elements
			foreach (var fetchedFromElement in fetchedFromElements)
			{
				var fragment = fetchedFromElement.GetIdentifierSelectFragment(nonscalarSize, j);
				fetchedFromElement.EntitySuffix = fragment.GetSuffix();
				var generatedExpr = (SelectExpressionImpl) appender.Append(HqlSqlWalker.SELECT_EXPR, fragment.ToSqlStringFragment(false), false);
				generatedExpr.FromElement = fetchedFromElement;
				combinedExpressions.Add(generatedExpr);
				j++;
			}

			if (currentFromClause.IsSubQuery)
			{
				return;
			}
			
			// Generate the property select tokens.
			j = 0;
			foreach (var e in combinedExpressions)
			{
				var fromElement = e.FromElement;
				if (fromElement != null)
				{
					RenderNonScalarProperties(appender, fromElement, nonscalarSize, j);
					j++;
				}
			}
		}

		private void RenderNonScalarIdentifiers(
			FromElement fromElement, int nonscalarSize, int j, ISelectExpression expr, ASTAppender appender)
		{
			if (fromElement.FromClause.IsSubQuery)
			{
				return;
			}

			var fragment = fromElement.GetIdentifierSelectFragment(nonscalarSize, j);
			fromElement.EntitySuffix = fragment.GetSuffix();
			if (!_scalarSelect)
			{
				//TODO: is this a bit ugly?
				expr.Text = fragment.ToSqlStringFragment(false);
			}
			else
			{
				appender.Append(HqlSqlWalker.SQL_TOKEN, fragment.ToSqlStringFragment(false), false);
			}
		}

		private void RenderNonScalarProperties(ASTAppender appender, FromElement fromElement, int nonscalarSize, int k)
		{
			var fragment = fromElement.GetPropertiesSelect(nonscalarSize, k);
			appender.Append(HqlSqlWalker.SQL_TOKEN, fragment.ToSqlStringFragment(false), false);

			if (fromElement.QueryableCollection != null && fromElement.IsFetch)
			{
				fragment = fromElement.GetCollectionSelectFragment(nonscalarSize, k);
				appender.Append(HqlSqlWalker.SQL_TOKEN, fragment.ToSqlStringFragment(false), false);
			}

			// Look through the FromElement's children to find any collections of values that should be fetched...
			ASTIterator iter = new ASTIterator(fromElement);
			foreach (FromElement child in iter)
			{
				if (child.IsCollectionOfValuesOrComponents && child.IsFetch)
				{
					// Need a better way to define the suffixes here...
					fragment = child.GetValueCollectionSelectFragment(nonscalarSize, nonscalarSize + k);
					appender.Append(HqlSqlWalker.SQL_TOKEN, fragment.ToSqlStringFragment(false), false);
				}
			}
		}

		private void RenderScalarSelects(IList<ISelectExpression> se, FromClause currentFromClause)
		{
			if (currentFromClause.IsSubQuery)
			{
				return;
			}

			List<int> deprecateExpressions = null; // 6.0 TODO: Remove 
			_scalarColumnNames = new string[se.Count][];
			for (var i = 0; i < se.Count; i++)
			{
				var expr = se[i];
				_scalarColumnNames[i] = expr.SetScalarColumn(i, NameGenerator.ScalarName); // Create SQL_TOKEN nodes for the columns.
				// 6.0 TODO: Remove 
				if (_scalarColumnNames[i] == null)
				{
					if (deprecateExpressions == null)
					{
						deprecateExpressions = new List<int>();
					}

					deprecateExpressions.Add(i);
				}
			}

			// 6.0 TODO: Remove 
			if (deprecateExpressions != null)
			{
#pragma warning disable 618
				var columnNames = SessionFactoryHelper.GenerateColumnNames(_queryReturnTypes);
#pragma warning restore 618
				foreach (var index in deprecateExpressions)
				{
					_scalarColumnNames[index] = columnNames[index];
				}
			}
		}

		private void AddCollectionFromElement(FromElement fromElement)
		{
			if (fromElement.IsFetch)
			{
				if (fromElement.CollectionJoin || fromElement.QueryableCollection != null)
				{
					String suffix;
					if (_collectionFromElements == null)
					{
						_collectionFromElements = new List<FromElement>();
						suffix = VERSION2_SQL ? "__" : "0__";
					}
					else
					{
						suffix = _collectionFromElements.Count + "__";
					}
					_collectionFromElements.Add(fromElement);
					fromElement.CollectionSuffix = suffix;
				}
			}
		}

		private void FinishInitialization()
		{
			_prepared = true;
		}

		private void InitializeScalarColumnNames()
		{
			_columnNamesStartPositions = new int[_scalarColumnNames.Length];
			var startPosition = 1;
			for (var i = 0; i < _scalarColumnNames.Length; i++)
			{
				_columnNamesStartPositions[i] = startPosition;
				startPosition += _scalarColumnNames[i].Length;
			}
		}

		public int GetColumnNamesStartPosition(int i)
		{
			return _columnNamesStartPositions[i];
		}
	}
}
