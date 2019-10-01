using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Hql.Ast;
using NHibernate.Linq.Functions;
using NHibernate.Linq.Expressions;
using NHibernate.Persister.Entity;
using NHibernate.Type;
using Remotion.Linq.Clauses.Expressions;
using NHibernate.Util;

namespace NHibernate.Linq.Visitors
{
	/// <summary>
	/// Analyze the select clause to determine what parts can be translated
	/// fully to HQL, and some other properties of the clause.
	/// </summary>
	class SelectClauseHqlNominator
	{
		private readonly ILinqToHqlGeneratorsRegistry _functionRegistry;
		private readonly VisitorParameters _parameters;

		/// <summary>
		/// The expression parts that can be converted to pure HQL.
		/// </summary>
		public HashSet<Expression> HqlCandidates { get; private set; }

		/// <summary>
		/// If true after an expression have been analyzed, the
		/// expression as a whole contain at least one method call which
		/// cannot be converted to a registered function, i.e. it must
		/// be executed client side.
		/// </summary>
		public bool ContainsUntranslatedMethodCalls { get; private set; }

		public SelectClauseHqlNominator(VisitorParameters parameters)
		{
			_parameters = parameters;
			_functionRegistry = parameters.SessionFactory.Settings.LinqToHqlGeneratorsRegistry;
		}

		internal void Nominate(Expression expression)
		{
			HqlCandidates = new HashSet<Expression>();
			ContainsUntranslatedMethodCalls = false;
			CanBeEvaluatedInHql(expression);
		}

		private bool CanBeEvaluatedInHql(Expression expression)
		{
			// Do client side evaluation for constants
			if (expression == null || expression.NodeType == ExpressionType.Constant)
			{
				return true;
			}

			bool canBeEvaluated;
			switch (expression.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Divide:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Power:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.And:
				case ExpressionType.Or:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.LeftShift:
				case ExpressionType.RightShift:
				case ExpressionType.AndAlso:
				case ExpressionType.OrElse:
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Coalesce:
				case ExpressionType.ArrayIndex:
					canBeEvaluated = CanBeEvaluatedInHql((BinaryExpression) expression);
					break;
				case ExpressionType.Conditional:
					canBeEvaluated = CanBeEvaluatedInHql((ConditionalExpression) expression);
					break;
				case ExpressionType.Call:
					canBeEvaluated = CanBeEvaluatedInHql((MethodCallExpression) expression);
					break;
				case ExpressionType.ArrayLength:
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.Quote:
				case ExpressionType.TypeAs:
				case ExpressionType.UnaryPlus:
					canBeEvaluated = CanBeEvaluatedInHql(((UnaryExpression) expression).Operand);
					break;
				case ExpressionType.MemberAccess:
					canBeEvaluated = CanBeEvaluatedInHql((MemberExpression) expression);
					break;
				case ExpressionType.Extension:
					if (expression is NhNominatedExpression nominatedExpression)
					{
						expression = nominatedExpression.Expression;
					}

					canBeEvaluated = true; // Sub queries cannot be executed client side
					break;
				case ExpressionType.MemberInit:
					canBeEvaluated = CanBeEvaluatedInHql((MemberInitExpression) expression);
					break;
				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
					canBeEvaluated = CanBeEvaluatedInHql((NewArrayExpression) expression);
					break;
				case ExpressionType.ListInit:
					canBeEvaluated = CanBeEvaluatedInHql((ListInitExpression) expression);
					break;
				case ExpressionType.New:
					canBeEvaluated = CanBeEvaluatedInHql((NewExpression) expression);
					break;
				case ExpressionType.Dynamic:
					canBeEvaluated = CanBeEvaluatedInHql((DynamicExpression) expression);
					break;
				case ExpressionType.Invoke:
					canBeEvaluated = CanBeEvaluatedInHql((InvocationExpression) expression);
					break;
				case ExpressionType.TypeIs:
					canBeEvaluated = CanBeEvaluatedInHql(((TypeBinaryExpression) expression).Expression);
					break;
				default:
					canBeEvaluated = true;
					break;
			}

			if (canBeEvaluated)
			{
				HqlCandidates.Add(expression);
			}

			return canBeEvaluated;
		}

		private bool CanBeEvaluatedInHql(MethodCallExpression methodExpression)
		{
			var canBeEvaluated = methodExpression.Object == null || // Is static or extension method
			                     (methodExpression.Object.NodeType != ExpressionType.Constant && // Does not belong to a parameter
			                     CanBeEvaluatedInHql(methodExpression.Object));
			foreach (var argumentExpression in methodExpression.Arguments)
			{
				// If one of the agruments cannot be converted to hql we have to execute the method on the client side
				canBeEvaluated &= CanBeEvaluatedInHql(argumentExpression);
			}

			canBeEvaluated &= _functionRegistry.TryGetGenerator(methodExpression.Method, out _);
			ContainsUntranslatedMethodCalls |= !canBeEvaluated;
			return canBeEvaluated;
		}

		private bool CanBeEvaluatedInHql(MemberExpression memberExpression)
		{
			var canBeEvaluated = CanBeEvaluatedInHql(memberExpression.Expression);
			// Check for a mapped property e.g. Count
			if (!canBeEvaluated || _functionRegistry.TryGetGenerator(memberExpression.Member, out _))
			{
				return canBeEvaluated;
			}

			// Check whether the member is mapped
			var entityName = ExpressionsHelper.TryGetEntityName(_parameters.SessionFactory, memberExpression, out var memberPath);
			if (entityName == null)
			{
				return false; // Not mapped
			}

			var persister = _parameters.SessionFactory.GetEntityPersister(entityName);
			return persister.EntityMetamodel.GetIdentifierPropertyType(memberPath) != null ||
					persister.EntityMetamodel.GetPropertyIndexOrNull(memberPath).HasValue;
		}

		private bool CanBeEvaluatedInHql(ConditionalExpression conditionalExpression)
		{
			var canBeEvaluated = CanBeEvaluatedInHql(conditionalExpression.Test);
			// In Oracle, when a query that selects a parameter is executed multiple times with different parameter types,
			// will fail to get the value from the data reader. e.g. select case when <condition> then @p0 else @p1 end.
			// In order to prevent that, we have to execute only the condition on the server side and do the rest on the client side.
			if (canBeEvaluated &&
			    conditionalExpression.IfTrue.NodeType == ExpressionType.Constant &&
			    conditionalExpression.IfFalse.NodeType == ExpressionType.Constant)
			{
				return false;
			}

			canBeEvaluated &= (CanBeEvaluatedInHql(conditionalExpression.IfTrue) && HqlIdent.SupportsType(conditionalExpression.IfTrue.Type)) &
			                  (CanBeEvaluatedInHql(conditionalExpression.IfFalse) && HqlIdent.SupportsType(conditionalExpression.IfFalse.Type));

			return canBeEvaluated;
		}

		private bool CanBeEvaluatedInHql(BinaryExpression binaryExpression)
		{
			var canBeEvaluated = CanBeEvaluatedInHql(binaryExpression.Left) &
			                     CanBeEvaluatedInHql(binaryExpression.Right);

			// Subtract datetimes on the client side as the result varies when executed on the server side.
			// In Sql Server when using datetime2 subtract is not possbile.
			// In Oracle a number is returned that represents the difference between the two in days.
			if (new[]
			    {
				    ExpressionType.Subtract,
				    ExpressionType.SubtractChecked
			    }.Contains(binaryExpression.NodeType) &&
			    ContainsAnyOfTypes(new[] {binaryExpression.Left, binaryExpression.Right},
			                       typeof(DateTime?), typeof(DateTime),
			                       typeof(DateTimeOffset?), typeof(DateTimeOffset),
			                       typeof(TimeSpan?), typeof(TimeSpan)))
			{
				return false;
			}

			// Concatenation of strings can be only done on the server side when the left and right side types match.
			if (binaryExpression.NodeType == ExpressionType.Add &&
			    (binaryExpression.Left.Type == typeof(string) || binaryExpression.Right.Type == typeof(string)))
			{
				canBeEvaluated &= binaryExpression.Left.Type == binaryExpression.Right.Type;
			}

			return canBeEvaluated;
		}

		private bool CanBeEvaluatedInHql(MemberInitExpression memberInitExpression)
		{
			CanBeEvaluatedInHql(memberInitExpression.NewExpression);
			VisitMemberBindings(memberInitExpression.Bindings);
			return false;
		}

		private bool CanBeEvaluatedInHql(DynamicExpression dynamicExpression)
		{
			foreach (var argument in dynamicExpression.Arguments)
			{
				CanBeEvaluatedInHql(argument);
			}

			return false;
		}

		private bool CanBeEvaluatedInHql(ListInitExpression listInitExpression)
		{
			CanBeEvaluatedInHql(listInitExpression.NewExpression);
			foreach (var initializer in listInitExpression.Initializers)
			{
				foreach (var listInitArgument in initializer.Arguments)
				{
					CanBeEvaluatedInHql(listInitArgument);
				}
			}

			return false;
		}

		private bool CanBeEvaluatedInHql(NewArrayExpression newArrayExpression)
		{
			foreach (var arrayExpression in newArrayExpression.Expressions)
			{
				CanBeEvaluatedInHql(arrayExpression);
			}

			return false;
		}

		private bool CanBeEvaluatedInHql(InvocationExpression invocationExpression)
		{
			foreach (var argument in invocationExpression.Arguments)
			{
				CanBeEvaluatedInHql(argument);
			}

			return false;
		}

		private bool CanBeEvaluatedInHql(NewExpression newExpression)
		{
			foreach (var argument in newExpression.Arguments)
			{
				CanBeEvaluatedInHql(argument);
			}

			return false;
		}

		private void VisitMemberBindings(IEnumerable<MemberBinding> bindings)
		{
			foreach (var binding in bindings)
			{
				switch (binding)
				{
					case MemberAssignment assignment:
						CanBeEvaluatedInHql(assignment.Expression);
						break;
					case MemberListBinding listBinding:
						foreach (var argument in listBinding.Initializers.SelectMany(o => o.Arguments))
						{
							CanBeEvaluatedInHql(argument);
						}

						break;
					case MemberMemberBinding memberBinding:
						VisitMemberBindings(memberBinding.Bindings);
						break;
				}
			}
		}

		private static bool ContainsAnyOfTypes(IEnumerable<Expression> expressions, params System.Type[] types)
		{
			return expressions.Any(o => types.Contains(o.Type));
		}
	}
}
