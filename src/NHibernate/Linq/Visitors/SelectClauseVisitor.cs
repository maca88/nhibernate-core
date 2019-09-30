using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Hql.Ast;
using NHibernate.Linq.Expressions;
using NHibernate.Util;
using Remotion.Linq.Parsing;

namespace NHibernate.Linq.Visitors
{
	public class SelectClauseVisitor : RelinqExpressionVisitor
	{
		private readonly HqlTreeBuilder _hqlTreeBuilder = new HqlTreeBuilder();
		private HashSet<Expression> _hqlNodes;
		private readonly ParameterExpression _inputParameter;
		private readonly VisitorParameters _parameters;
		private int _iColumn;
		private List<HqlExpression> _hqlTreeNodes = new List<HqlExpression>();
		private readonly HqlGeneratorExpressionVisitor _hqlVisitor;
		private bool _evaluateInHql;
		private readonly List<Expression> _chainExpressions = new List<Expression>();
		private readonly ReplaceExpressionVisitor _replaceExpressionVisitor = new ReplaceExpressionVisitor();

		public SelectClauseVisitor(System.Type inputType, VisitorParameters parameters)
		{
			_inputParameter = Expression.Parameter(inputType, "input");
			_parameters = parameters;
			_hqlVisitor = new HqlGeneratorExpressionVisitor(_parameters);
		}

		public LambdaExpression ProjectionExpression { get; private set; }

		public IEnumerable<HqlExpression> GetHqlNodes()
		{
			return _hqlTreeNodes;
		}

		public void VisitSelector(Expression expression)
		{
			var distinct = expression as NhDistinctExpression;
			if (distinct != null)
			{
				expression = distinct.Expression;
			}

			// Find the sub trees that can be expressed purely in HQL
			var nominator = new SelectClauseHqlNominator(_parameters);
			nominator.Nominate(expression);
			_hqlNodes = nominator.HqlCandidates;

			// Strip the nominator wrapper from the select expression
			expression = UnwrapNominatedExpression(expression);

			// Linq2SQL ignores calls to local methods. Linq2EF seems to not support
			// calls to local methods at all. For NHibernate we support local methods,
			// but prevent their use together with server-side distinct, since it may
			// end up being wrong.
			if (distinct != null && nominator.ContainsUntranslatedMethodCalls)
				throw new NotSupportedException("Cannot use distinct on result that depends on methods for which no SQL equivalent exist.");

			// Now visit the tree
			var projection = Transform(Visit(expression));
			if ((projection != expression && !_hqlNodes.Contains(expression)) || _hqlTreeNodes.Count == 0)
			{
				ProjectionExpression = Expression.Lambda(projection, _inputParameter);
			}

			// When having only constants in the select clause we need to add one column in order to have a valid sql statement
			if (_hqlTreeNodes.Count == 0)
			{
				_hqlTreeNodes.Add(_hqlVisitor.Visit(Expression.Constant(1)).AsExpression());
			}

			// Handle any boolean results in the output nodes
			_hqlTreeNodes = _hqlTreeNodes.ConvertAll(node => node.ToArithmeticExpression());

			if (distinct != null)
			{
				var treeNodes = new List<HqlTreeNode>(_hqlTreeNodes.Count + 1) {_hqlTreeBuilder.Distinct()};
				treeNodes.AddRange(_hqlTreeNodes);
				_hqlTreeNodes = new List<HqlExpression>(1) {_hqlTreeBuilder.ExpressionSubTreeHolder(treeNodes)};
			}
		}

		#region Overrides

		public override Expression Visit(Expression expression)
		{
			if (expression == null)
			{
				return null;
			}

			expression = UnwrapNominatedExpression(expression);
			if (_hqlNodes.Contains(expression))
			{
				// Pure HQL evaluation
				_hqlTreeNodes.Add(_hqlVisitor.Visit(expression).AsExpression());
				_evaluateInHql = true;
				return Convert(Expression.ArrayIndex(_inputParameter, Expression.Constant(_iColumn++)), expression.Type);
			}

			// Can't handle this node with HQL.  Just recurse down, and emit the expression
			expression = base.Visit(expression);

			if (!_evaluateInHql)
			{
				return expression;
			}

			switch (expression.NodeType)
			{
				case ExpressionType.Call:
				case ExpressionType.MemberAccess:
					_chainExpressions.Add(expression);
					break;
			}

			return expression;
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			return node.Update(VisitAndTransform(node.Left), VisitAndConvert(node.Conversion, nameof(VisitBinary)), VisitAndTransform(node.Right));
		}

		protected override Expression VisitConditional(ConditionalExpression node)
		{
			return node.Update(VisitAndTransform(node.Test), VisitAndTransform(node.IfTrue), VisitAndTransform(node.IfFalse));
		}

		protected override Expression VisitDynamic(DynamicExpression node)
		{
			return node.Update(VisitAndTransform((IArgumentProvider) node) ?? (IEnumerable<Expression>) node.Arguments);
		}

		protected override Expression VisitInvocation(InvocationExpression node)
		{
			var args = VisitAndTransform((IArgumentProvider) node) ?? (IEnumerable<Expression>) node.Arguments;
			return node.Update(node.Expression, args);
		}

		// Override the original implementation to visit arguments first
		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var args = VisitAndTransform((IArgumentProvider) node) ?? (IEnumerable<Expression>) node.Arguments;
			var obj = Visit(node.Object);
			return node.Update(obj, args);
		}

		protected override Expression VisitNewArray(NewArrayExpression node)
		{
			return node.Update(VisitAndTransform(node.Expressions) ?? (IEnumerable<Expression>) node.Expressions);
		}

		protected override Expression VisitNew(NewExpression node)
		{
			return node.Update(VisitAndTransform(node.Arguments) ?? (IEnumerable<Expression>) node.Arguments);
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node)
		{
			return node.Update(VisitAndTransform(node.Expression));
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			return Transform(base.VisitUnary(node));
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			return node.Update(VisitConvertAndTransform(node.NewExpression, nameof(VisitMemberInit)), Visit(node.Bindings, VisitMemberBinding));
		}

		protected override Expression VisitListInit(ListInitExpression node)
		{
			return node.Update(VisitConvertAndTransform(node.NewExpression, nameof(VisitListInit)), Visit(node.Initializers, VisitElementInit));
		}

		protected override ElementInit VisitElementInit(ElementInit node)
		{
			return node.Update(VisitAndTransform(node.Arguments) ?? (IEnumerable<Expression>) node.Arguments);
		}

		protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
		{
			return node.Update(VisitAndTransform(node.Expression));
		}

		#endregion

		public T VisitConvertAndTransform<T>(T node, string callerName) where T : Expression
		{
			return (T) Transform(VisitAndConvert(node, callerName));
		}

		private Expression VisitAndTransform(Expression expression)
		{
			return Transform(Visit(expression));
		}

		private Expression[] VisitAndTransform(ReadOnlyCollection<Expression> nodes)
		{
			Expression[] newNodes = null;
			for (int i = 0, n = nodes.Count; i < n; i++)
			{
				var curNode = nodes[i];
				var node = VisitAndTransform(curNode);
				if (newNodes != null)
				{
					newNodes[i] = node;
				}
				else if (!ReferenceEquals(node, curNode))
				{
					newNodes = new Expression[n];
					for (var j = 0; j < i; j++)
					{
						newNodes[j] = nodes[j];
					}

					newNodes[i] = node;
				}
			}

			return newNodes;
		}

		private Expression[] VisitAndTransform(IArgumentProvider nodes)
		{
			Expression[] newNodes = null;
			for (int i = 0, n = nodes.ArgumentCount; i < n; i++)
			{
				var curNode = nodes.GetArgument(i);
				var node = VisitAndTransform(curNode);
				if (newNodes != null)
				{
					newNodes[i] = node;
				}
				else if (!ReferenceEquals(node, curNode))
				{
					newNodes = new Expression[n];
					for (var j = 0; j < i; j++)
					{
						newNodes[j] = nodes.GetArgument(j);
					}

					newNodes[i] = node;
				}
			}

			return newNodes;
		}

		private Expression UnwrapNominatedExpression(Expression expression)
		{
			if (expression is NhNominatedExpression nominatedExpression)
			{
				return nominatedExpression.Expression;
			}

			return expression;
		}

		/// <summary>
		/// Adds null checks (simulates ?. operator) for client side evaluations in order to prevent NRE and makes it consistent with the server
		/// side evaluation.
		/// e.g. input[0].Property -> { var value = input[0]; value = input[0] == null ? null : input[0].Property; return value; }
		/// </summary>
		/// <param name="root">The root expression.</param>
		/// <returns>The transformed expression.</returns>
		private Expression Transform(Expression root)
		{
			if (_chainExpressions.Count == 0)
			{
				_evaluateInHql = false;
				return root;
			}

			var valueVariable = Expression.Variable(typeof(object), "value");
			var assignments = new List<Expression>();
			var currentType = _chainExpressions.First().Type;
			foreach (var expression in _chainExpressions)
			{
				Expression convertedValue;
				System.Type nextType;
				switch (expression)
				{
					case MemberExpression memberExpression:
						if (assignments.Count == 0)
						{
							assignments.Add(
								Expression.Assign(
									valueVariable,
									Expression.Convert(memberExpression.Expression, typeof(object))));
							currentType = memberExpression.Expression.Type;
						}

						nextType = memberExpression.Type.GetNullableType();
						convertedValue = Expression.Convert(valueVariable, currentType);
						assignments.Add(
							Expression.Assign(
								valueVariable,
								Expression.Condition(
									Expression.Equal(convertedValue, Expression.Default(currentType)),
									Expression.Convert(Expression.Default(nextType), typeof(object)),
									Expression.Convert(Expression.MakeMemberAccess(
										                   memberExpression.Expression.Type != currentType
															   ? Expression.Convert(convertedValue, memberExpression.Expression.Type)
											                   : convertedValue,
										                   memberExpression.Member), typeof(object))
								)));
						currentType = nextType;
						break;
					case MethodCallExpression methodCallExpression:
						if (methodCallExpression.Object == null)
						{
							continue; // This will never happen as method arguments are transformed after they are visited
						}

						if (assignments.Count == 0)
						{
							assignments.Add(
								Expression.Assign(
									valueVariable, Expression.Convert(methodCallExpression.Object, typeof(object))));
							currentType = methodCallExpression.Object.Type;
						}

						nextType = methodCallExpression.Type.GetNullableType();
						convertedValue = Expression.Convert(valueVariable, currentType);
						assignments.Add(
							Expression.Assign(
								valueVariable,
								Expression.Condition(
									Expression.Equal(convertedValue, Expression.Default(currentType)),
									Expression.Convert(Expression.Default(nextType), typeof(object)),
									Expression.Convert(
										Expression.Call(
											methodCallExpression.Object.Type != currentType
												? Expression.Convert(convertedValue, methodCallExpression.Object.Type)
												: convertedValue,
											methodCallExpression.Method,
											methodCallExpression.Arguments),
										typeof(object))
								)));
						currentType = nextType;
						break;
				}
			}

			assignments.Add(valueVariable);
			var block = Expression.Block(new[] { valueVariable }, assignments);
			var convertedBlock = Expression.Convert(block, currentType);
			var toReplace = _chainExpressions.Last();
			var convert = root.NodeType == ExpressionType.Convert ? root : null;
			// We have to match the end type if the current block type is not equal to the one that we have to repalce
			if (currentType != toReplace.Type)
			{
				root = convert?.NodeType == ExpressionType.Convert 
					? _replaceExpressionVisitor.Replace(root, convert, Expression.Convert(block, convert.Type))
					// Unwrap the nullable type by casting it
					: _replaceExpressionVisitor.Replace(root, toReplace, Expression.Convert(convertedBlock, toReplace.Type));
			}
			else
			{
				root = _replaceExpressionVisitor.Replace(root, toReplace, convertedBlock);
			}

			_chainExpressions.Clear();
			_evaluateInHql = false;

			return root;
		}

		private static readonly MethodInfo ConvertChangeType =
			ReflectHelper.GetMethod(() => System.Convert.ChangeType(default(object), default(System.Type)));

		private static Expression Convert(Expression expression, System.Type type)
		{
			//#1121
			if (type.IsEnum)
			{
				expression = Expression.Call(
					ConvertChangeType,
					expression,
					Expression.Constant(Enum.GetUnderlyingType(type)));
			}

			return Expression.Convert(expression, type);
		}

		private class ReplaceExpressionVisitor : ExpressionVisitor
		{
			private readonly Dictionary<Expression, Expression> _replacements = new Dictionary<Expression, Expression>();

			public Expression Replace(Expression rootExpression, Expression oldExpression, Expression newExpression)
			{
				_replacements.Add(oldExpression, newExpression);
				rootExpression = Visit(rootExpression);
				if (_replacements.Count > 0)
				{
					throw new InvalidOperationException("The old expression is not present int the root expression.");
				}

				return rootExpression;
			}

			public override Expression Visit(Expression node)
			{
				if (_replacements.Count == 0)
				{
					return node;
				}

				if (_replacements.TryGetValue(node, out var newNode))
				{
					_replacements.Remove(node);
					return newNode;
				}

				return base.Visit(node);
			}
		}
	}

	// Since v5
	[Obsolete]
	public static class BooleanToCaseConvertor
	{
		[Obsolete]
		public static IEnumerable<HqlExpression> Convert(IEnumerable<HqlExpression> hqlTreeNodes)
		{
			return hqlTreeNodes.Select(node => node.ToArithmeticExpression());
		}

		[Obsolete]
		public static HqlExpression ConvertBooleanToCase(HqlExpression node)
		{
			return node.ToArithmeticExpression();
		}
	}
}
