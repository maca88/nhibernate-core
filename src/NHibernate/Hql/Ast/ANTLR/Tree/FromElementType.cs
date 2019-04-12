using System;
using System.Collections.Generic;
using System.Text;

using NHibernate.Engine;
using NHibernate.Param;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Hql.Ast.ANTLR.Tree
{
	/// <summary>
	/// Delegate that handles the type and join sequence information for a FromElement.
	/// Author: josh
	/// Ported by: Steve Strong
	/// </summary>
	[CLSCompliant(false)]
	public class FromElementType
	{
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(FromElementType));

		private readonly FromElement _fromElement;
		private readonly IEntityPersister _persister;
		private readonly EntityType _entityType;
		private IQueryableCollection _queryableCollection;
		private CollectionPropertyMapping _collectionPropertyMapping;
		private JoinSequence _joinSequence;
		private IParameterSpecification _indexCollectionSelectorParamSpec;
		private string _collectionSuffix;

		public FromElementType(FromElement fromElement, IEntityPersister persister, EntityType entityType)
		{
			_fromElement = fromElement;
			_persister = persister;
			_entityType = entityType;

			var queryable = persister as IQueryable;
			if (queryable != null)
				fromElement.Text = queryable.TableName + " " + fromElement.TableAlias;
		}

		protected FromElementType(FromElement fromElement)
		{
			_fromElement = fromElement;
		}

		public IEntityPersister EntityPersister
		{
			get { return _persister; }
		}

		private string TableAlias
		{
			get { return _fromElement.TableAlias; }
		}

		private string CollectionTableAlias
		{
			get { return _fromElement.CollectionTableAlias; }
		}

		public virtual IType DataType
		{
			get
			{
				if (_persister == null)
				{
					if (_queryableCollection == null)
					{
						return null;
					}
					return _queryableCollection.Type;
				}
				else
				{
					return _entityType;
				}
			}
		}

		public IType SelectType
		{
			get
			{
				if (_entityType == null)
				{
					return null;
				}

				bool shallow = _fromElement.FromClause.Walker.IsShallowQuery;
				return TypeFactory.ManyToOne(_entityType.GetAssociatedEntityName(), shallow);
			}
		}

		public string CollectionSuffix
		{
			get { return _collectionSuffix; }
			set { _collectionSuffix = value; }
		}

		public string EntitySuffix { get; set; }

		public IParameterSpecification IndexCollectionSelectorParamSpec
		{
			get { return _indexCollectionSelectorParamSpec; }
			set { _indexCollectionSelectorParamSpec = value; }
		}

		public JoinSequence JoinSequence
		{
			get
			{
				if (_joinSequence != null)
				{
					return _joinSequence;
				}

				// Class names in the FROM clause result in a JoinSequence (the old FromParser does this).
				var joinable = _persister as IJoinable;
				if (joinable != null)
				{
					return _fromElement.SessionFactoryHelper.CreateJoinSequence().SetRoot(joinable, TableAlias);
				}
				
				return null; // TODO: Should this really return null?  If not, figure out something better to do here.
			}
			set { _joinSequence = value; }
		}

		/// <summary>
		/// Returns the identifier select SQL fragment.
		/// </summary>
		/// <param name="size">The total number of returned types.</param>
		/// <param name="k">The sequence of the current returned type.</param>
		/// <returns>the identifier select SQL fragment.</returns>
		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public string RenderIdentifierSelect(int size, int k)
		{
			return GetIdentifierSelectFragment(size, k)?.ToSqlStringFragment(false) ?? string.Empty;
		}

		public SelectFragment GetIdentifierSelectFragment(int size, int k)
		{
			CheckInitialized();

			// Render the identifier select fragment using the table alias.
			if (_fromElement.FromClause.IsSubQuery)
			{
				var queryable = Queryable;
				if (queryable == null)
					return null;

				return new SelectFragment(queryable.Factory.Dialect)
				       .AddColumns(_fromElement.TableAlias, queryable.IdentifierColumnNames);
			}
			else
			{
				var queryable = Queryable;
				if (queryable == null)
					throw new QueryException("not an entity");

				return queryable.GetIdentifierSelectFragment(TableAlias, GetSuffix(size, k));
			}
		}

		/// <summary>
		/// Render the identifier select, but in a 'scalar' context (i.e. generate the column alias).
		/// </summary>
		/// <param name="i">the sequence of the returned type</param>
		/// <returns>the identifier select with the column alias.</returns>
		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public virtual string RenderScalarIdentifierSelect(int i)
		{
			return GetScalarIdentifierSelectFragment(i, NameGenerator.ScalarName).ToSqlStringFragment(false);
		}

		/// <summary>
		/// Render the identifier select fragment, but in a 'scalar' context (i.e. generate the column alias).
		/// </summary>
		/// <param name="i">The sequence of the returned type</param>
		/// <param name="aliasCreator">A function to generate aliases.</param>
		/// <returns>The identifier select fragment.</returns>
		public virtual SelectFragment GetScalarIdentifierSelectFragment(int i, Func<int,int, string> aliasCreator)
		{
			CheckInitialized();
			var cols = GetPropertyMapping(Persister.Entity.EntityPersister.EntityID).ToColumns(TableAlias, Persister.Entity.EntityPersister.EntityID);
			var factory = Queryable?.Factory ?? QueryableCollection?.Factory ?? throw new QueryException("not an entity or collection");
			var fragment = new SelectFragment(factory.Dialect);
			// For property references generate <tablealias>.<columnname> as <projectionalias>
			for (var j = 0; j < cols.Length; j++)
			{
				fragment.AddColumn(null, cols[j], aliasCreator(i, j));
			}

			return fragment;
		}


		/// <summary>
		/// Returns the property select SQL fragment.
		/// </summary>
		/// <param name="size">The total number of returned types.</param>
		/// <param name="k">The sequence of the current returned type.</param>
		/// <param name="allProperties"></param>
		/// <returns>the property select SQL fragment.</returns>
		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public string RenderPropertySelect(int size, int k, bool allProperties)
		{
			return GetPropertiesSelectFragment(size, k, null, allProperties).ToSqlStringFragment();
		}

		/// <summary>
		/// Returns the property select SQL fragment.
		/// </summary>
		/// <param name="size">The total number of returned types.</param>
		/// <param name="k">The sequence of the current returned type.</param>
		/// <param name="allProperties"></param>
		/// <returns>the property select SQL fragment.</returns>
		public SelectFragment GetPropertiesSelectFragment(int size, int k, bool allProperties)
		{
			return GetPropertiesSelectFragment(size, k, null, allProperties);
		}

		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public string RenderPropertySelect(int size, int k, string[] fetchLazyProperties)
		{
			return GetPropertiesSelectFragment(size, k, fetchLazyProperties, false).ToSqlStringFragment();
		}

		/// <summary>
		/// Returns the property select SQL fragment.
		/// </summary>
		/// <param name="size">The total number of returned types.</param>
		/// <param name="k">The sequence of the current returned type.</param>
		/// <param name="fetchLazyProperties"></param>
		/// <returns>the property select SQL fragment.</returns>
		public SelectFragment GetPropertiesSelectFragment(int size, int k, string[] fetchLazyProperties)
		{
			return GetPropertiesSelectFragment(size, k, fetchLazyProperties, false);
		}

		/// <summary>
		/// Returns the properties select fragment.
		/// </summary>
		/// <param name="size">The total number of returned types.</param>
		/// <param name="k">The sequence of the current returned type.</param>
		/// <param name="fetchLazyProperties"></param>
		/// <param name="allProperties">Whether to include all lazy properties.</param>
		/// <returns>The properties select fragment.</returns>
		private SelectFragment GetPropertiesSelectFragment(int size, int k, string[] fetchLazyProperties, bool allProperties)
		{
			CheckInitialized();

			// Use the old method when fetchProperties is null to prevent any breaking changes
			// 6.0 TODO: simplify condition by removing the fetchProperties part
			return fetchLazyProperties == null || allProperties
				? Queryable?.GetPropertiesSelectFragment(TableAlias, GetSuffix(size, k), allProperties)
				: Queryable?.GetPropertiesSelectFragment(TableAlias, GetSuffix(size, k), fetchLazyProperties);
		}

		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public string RenderCollectionSelectFragment(int size, int k)
		{
			return GetCollectionSelectFragment(size, k)?.ToSqlStringFragment(false) ?? string.Empty;
		}

		public SelectFragment GetCollectionSelectFragment(int size, int k)
		{
			if (_queryableCollection == null)
				return null;

			if (_collectionSuffix == null)
				_collectionSuffix = GenerateSuffix(size, k);

			return _queryableCollection.GetSelectFragment(CollectionTableAlias, _collectionSuffix);
		}

		// Since 5.3
		[Obsolete("This method has no more usage in NHibernate and will be removed in a future version.")]
		public string RenderValueCollectionSelectFragment(int size, int k)
		{
			return GetValueCollectionSelectFragment(size, k)?.ToSqlStringFragment(false) ?? string.Empty;
		}

		public SelectFragment GetValueCollectionSelectFragment(int size, int k)
		{
			if (_queryableCollection == null)
				return null;

			if (_collectionSuffix == null)
				_collectionSuffix = GenerateSuffix(size, k);

			return _queryableCollection.GetSelectFragment(TableAlias, _collectionSuffix);
		}

		public bool IsEntity
		{
			get { return _persister != null; }
		}

		public bool IsCollectionOfValuesOrComponents
		{
			get
			{
				if (_persister != null)
					return false;

				if (_queryableCollection == null)
					return false;
				
				return !_queryableCollection.ElementType.IsEntityType;
			}
		}

		public virtual IPropertyMapping GetPropertyMapping(string propertyName)
		{
			CheckInitialized();

			if (_queryableCollection == null)
			{		
				// Not a collection?
				return (IPropertyMapping)_persister;	// Return the entity property mapping.
			}

			// If the property is a special collection property name, return a CollectionPropertyMapping.
			if (CollectionProperties.IsCollectionProperty(propertyName))
			{
				if (_collectionPropertyMapping == null)
				{
					_collectionPropertyMapping = new CollectionPropertyMapping(_queryableCollection);
				}
				return _collectionPropertyMapping;
			}
			if (_queryableCollection.ElementType.IsAnyType)
			{
				// collection of <many-to-any/> mappings...
				// used to circumvent the component-collection check below...
				return _queryableCollection;
			}

			if (_queryableCollection.ElementType.IsComponentType)
			{
				// Collection of components.
				if (propertyName == Persister.Entity.EntityPersister.EntityID)
				{
					return (IPropertyMapping)_queryableCollection.OwnerEntityPersister;
				}
			}

			return _queryableCollection;
		}

		/// <summary>
		/// Returns the type of a property, given it's name (the last part) and the full path.
		/// </summary>
		/// <param name="propertyName">The last part of the full path to the property.</param>
		/// <param name="propertyPath">The full property path.</param>
		/// <returns>The type</returns>
		public virtual IType GetPropertyType(string propertyName, string propertyPath)
		{
			CheckInitialized();

			IType type = null;

			// If this is an entity and the property is the identifier property, then use getIdentifierType().
			//      Note that the propertyName.equals( propertyPath ) checks whether we have a component
			//      key reference, where the component class property name is the same as the
			//      entity id property name; if the two are not equal, this is the case and
			//      we'd need to "fall through" to using the property mapping.
			if (_persister != null && (propertyName == propertyPath) && propertyName == _persister.IdentifierPropertyName)
			{
				type = _persister.IdentifierType;
			}
			else
			{	// Otherwise, use the property mapping.
				IPropertyMapping mapping = GetPropertyMapping(propertyName);
				type = mapping.ToType(propertyPath);
			}
			if (type == null)
			{
				throw new MappingException("Property " + propertyName + " does not exist in " +
						((_queryableCollection == null) ? "class" : "collection") + " "
						+ ((_queryableCollection == null) ? _fromElement.ClassName : _queryableCollection.Role));
			}
			return type;
		}

		public string[] ToColumns(string tableAlias, string path, bool inSelect)
		{
			return ToColumns(tableAlias, path, inSelect, false);
		}

		/// <summary>
		/// Returns the Hibernate queryable implementation for the HQL class.
		/// </summary>
		public IQueryable Queryable
		{
			get { return _persister as IQueryable; }
		}

		public virtual IQueryableCollection QueryableCollection
		{
			get { return _queryableCollection; }
			set
			{
				if (_queryableCollection != null)
				{
					throw new InvalidOperationException("QueryableCollection is already defined for " + this + "!");
				}
				_queryableCollection = value;
				if (!_queryableCollection.IsOneToMany)
				{
					// For many-to-many joins, use the tablename from the queryable collection for the default text.
					_fromElement.Text = _queryableCollection.TableName + " " + TableAlias;
				}
			}
		}

		public string[] ToColumns(string tableAlias, string path, bool inSelect, bool forceAlias)
		{
			CheckInitialized();
			IPropertyMapping propertyMapping = GetPropertyMapping(path);

			// If this from element is a collection and the path is a collection property (maxIndex, etc.) then
			// generate a sub-query.
			if (!inSelect && _queryableCollection != null && CollectionProperties.IsCollectionProperty(path))
			{
				IDictionary<string, IFilter> enabledFilters = _fromElement.Walker.EnabledFilters;

				string subquery = CollectionSubqueryFactory.CreateCollectionSubquery(
						_joinSequence,
						enabledFilters,
						propertyMapping.ToColumns(tableAlias, path)
				);
				if (Log.IsDebugEnabled())
				{
					Log.Debug("toColumns({0},{1}) : subquery = {2}", tableAlias, path, subquery);
				}
				return new [] { "(" + subquery + ")" };
			}
			else
			{
				if (forceAlias)
				{
					return propertyMapping.ToColumns(tableAlias, path);
				}
				else if (_fromElement.Walker.StatementType == HqlSqlWalker.SELECT)
				{
					return propertyMapping.ToColumns(tableAlias, path);
				}
				else if (_fromElement.Walker.CurrentClauseType == HqlSqlWalker.SELECT)
				{
					return propertyMapping.ToColumns(tableAlias, path);
				}
				else if (_fromElement.Walker.IsSubQuery)
				{
					// for a subquery, the alias to use depends on a few things (we
					// already know this is not an overall SELECT):
					//      1) if this FROM_ELEMENT represents a correlation to the
					//          outer-most query
					//              A) if the outer query represents a multi-table
					//                  persister, we need to use the given alias
					//                  in anticipation of one of the multi-table
					//                  executors being used (as this subquery will
					//                  actually be used in the "id select" phase
					//                  of that multi-table executor)
					//              B) otherwise, we need to use the persister's
					//                  table name as the column qualification
					//      2) otherwise (not correlated), use the given alias
					if (IsCorrelation)
					{
						if (IsMultiTable)
						{
							return propertyMapping.ToColumns(tableAlias, path);
						}
						else
						{
							return propertyMapping.ToColumns(ExtractTableName(), path);
						}
					}
					else
					{
						return propertyMapping.ToColumns(tableAlias, path);
					}
				}
				else
				{
					string[] columns = propertyMapping.ToColumns(path);
					Log.Info("Using non-qualified column reference [{0} -> ({1})]", path, ArrayHelper.ToString(columns));
					return columns;
				}
			}
		}

		private static string GetSuffix(int size, int sequence)
		{
			return GenerateSuffix(size, sequence);
		}

		private static string GenerateSuffix(int size, int k)
		{
			String suffix = size == 1 ? "" : k.ToString() + '_';
			return suffix;
		}

		private void CheckInitialized()
		{
			_fromElement.CheckInitialized();
		}

		private bool IsCorrelation
		{
			get
			{
				FromClause top = _fromElement.Walker.GetFinalFromClause();
				return _fromElement.FromClause != _fromElement.Walker.CurrentFromClause &&
					   _fromElement.FromClause == top;
			}
		}

		private bool IsMultiTable
		{
			get
			{
				// should be safe to only ever expect EntityPersister references here
				return _fromElement.Queryable != null &&
					   _fromElement.Queryable.IsMultiTable;
			}
		}

		private string ExtractTableName()
		{
			// should be safe to only ever expect EntityPersister references here
			return _fromElement.Queryable.TableName;
		}
	}
}
