using System;
using System.Collections.Specialized;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Linq.Clauses;
using NHibernate.Linq.Visitors;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace NHibernate.Linq.ReWriters
{
	internal interface IIsEntityDecider
	{
		bool IsEntity(System.Type type);
		bool IsIdentifier(System.Type type, string propertyName);
	}

	public class AddJoinsReWriter : NhQueryModelVisitorBase, IIsEntityDecider
	{
		private readonly ISessionFactoryImplementor _sessionFactory;
		private readonly MemberExpressionJoinDetector _memberExpressionJoinDetector;
		private readonly WhereJoinDetector _whereJoinDetector;

		private AddJoinsReWriter(ISessionFactoryImplementor sessionFactory, QueryModel queryModel)
		{
			_sessionFactory = sessionFactory;
			var joiner = new Joiner(queryModel);
			_memberExpressionJoinDetector = new MemberExpressionJoinDetector(this, joiner);
			_whereJoinDetector = new WhereJoinDetector(this, joiner);
		}

		public static void ReWrite(QueryModel queryModel, VisitorParameters parameters)
		{
			var visitor = new AddJoinsReWriter(parameters.SessionFactory, queryModel);
			visitor.VisitQueryModel(queryModel);
		}

		public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
		{
			_memberExpressionJoinDetector.Transform(selectClause);
		}

		public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
		{
			_memberExpressionJoinDetector.Transform(ordering);
		}

		public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
		{
			_memberExpressionJoinDetector.Transform(resultOperator);
		}

		public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
		{
			_whereJoinDetector.Transform(whereClause);
		}

		public override void VisitNhHavingClause(NhHavingClause havingClause, QueryModel queryModel, int index)
		{
			_whereJoinDetector.Transform(havingClause);
		}

		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
		{
			// When there are association navigations inside an on clause (e.g. c.ContactTitle equals o.Customer.ContactTitle),
			// we have to move the condition to the where statement, otherwise the query will be invalid.
			// Link newly created joins with the current join clause in order to later detect which join type to use.
			queryModel.BodyClauses.CollectionChanged += OnCollectionChange;
			_whereJoinDetector.Transform(joinClause);
			queryModel.BodyClauses.CollectionChanged -= OnCollectionChange;

			void OnCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
			{
				foreach (var nhJoinClause in e.NewItems.OfType<NhJoinClause>())
				{
					nhJoinClause.RelatedBodyClause = joinClause;
				}
			}
		}

		public bool IsEntity(System.Type type)
		{
			return _sessionFactory.GetImplementors(type.FullName).Any();
		}

		public bool IsIdentifier(System.Type type, string propertyName)
		{
			var metadata = _sessionFactory.GetClassMetadata(type);
			return metadata != null && propertyName.Equals(metadata.IdentifierPropertyName);
		}
	}
}
