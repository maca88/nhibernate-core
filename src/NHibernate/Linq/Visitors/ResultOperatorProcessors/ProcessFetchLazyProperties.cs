using Remotion.Linq.Clauses.ResultOperators;

namespace NHibernate.Linq.Visitors.ResultOperatorProcessors
{
	internal class ProcessFetchLazyProperties : IResultOperatorProcessor<FetchLazyPropertiesResultOperator>
	{
		public void Process(FetchLazyPropertiesResultOperator resultOperator, QueryModelVisitor queryModelVisitor, IntermediateHqlTree tree)
		{
			tree.AddFromLastChildClause(tree.TreeBuilder.Fetch());
		}
	}

	internal class ProcessDefaultIfEmpty : IResultOperatorProcessor<DefaultIfEmptyResultOperator>
	{
		public void Process(DefaultIfEmptyResultOperator resultOperator, QueryModelVisitor queryModelVisitor, IntermediateHqlTree tree)
		{
			// We don't need to do anything here, the logic is done inside QueryModelVisitor
		}
	}
}
