using System;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Engine;
using NHibernate.Linq.Clauses;
using NHibernate.Linq.Visitors;
using NHibernate.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace NHibernate.Linq.ReWriters
{
	internal interface IIsEntityDecider
	{
		bool IsEntity(MemberExpression expression, out bool isIdentifier);
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
			_memberExpressionJoinDetector = new MemberExpressionJoinDetector(this, joiner, _sessionFactory);
			_whereJoinDetector = new WhereJoinDetector(this, joiner, _sessionFactory);
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

		// Since v5.3
		[Obsolete("This method has no usages and will be removed in a future version")]
		public bool IsEntity(System.Type type)
		{
			return _sessionFactory.GetImplementors(type.FullName).Any();
		}

		// Since v5.3
		[Obsolete("This method has no usages and will be removed in a future version")]
		public bool IsIdentifier(System.Type type, string propertyName)
		{
			var metadata = _sessionFactory.GetClassMetadata(type);
			return metadata != null && propertyName.Equals(metadata.IdentifierPropertyName);
		}

		bool IIsEntityDecider.IsEntity(MemberExpression expression, out bool isIdentifier)
		{
			isIdentifier =
				ExpressionsHelper.TryGetMappedType(_sessionFactory, expression, out var mappedType, out var entityPersister, out _, out var memberPath) 
				&& entityPersister?.IdentifierPropertyName == memberPath;

			return mappedType?.IsEntityType == true;
		}
	}
}
