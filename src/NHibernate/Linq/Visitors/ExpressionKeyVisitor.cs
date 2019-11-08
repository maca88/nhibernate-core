using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using NHibernate.Engine;
using NHibernate.Param;
using NHibernate.Type;
using NHibernate.Util;
using Remotion.Linq.Parsing;

namespace NHibernate.Linq.Visitors
{
	/// <summary>
	/// Performs the equivalent of a ToString() on an expression. Swaps out constants for 
	/// parameters so that, for example:
	///		from c in Customers where c.City = "London"
	/// generate the same key as 
	///		from c in Customers where c.City = "Madrid"
	/// </summary>
	public class ExpressionKeyVisitor : RelinqExpressionVisitor
	{
		private static readonly ICollection<MethodBase> _pagingMethods = new HashSet<MethodBase>
		{
			ReflectionCache.EnumerableMethods.SkipDefinition,
			ReflectionCache.EnumerableMethods.TakeDefinition,
			ReflectionCache.QueryableMethods.SkipDefinition,
			ReflectionCache.QueryableMethods.TakeDefinition
		};

		private readonly IDictionary<ConstantExpression, NamedParameter> _constantToParameterMap;
		private readonly HashSet<Expression> _ignoreParameters = new HashSet<Expression>();
		readonly StringBuilder _string = new StringBuilder();
		private readonly ISessionFactoryImplementor _sessionFactory;
		private bool _collectParameters;

		// 6.0 TODO: Remove constructor
		private ExpressionKeyVisitor(
			IDictionary<ConstantExpression, NamedParameter> constantToParameterMap,
			ISessionFactoryImplementor sessionFactory)
		{
			_constantToParameterMap = constantToParameterMap;
			_sessionFactory = sessionFactory;
		}

		private ExpressionKeyVisitor(ISessionFactoryImplementor sessionFactory)
		{
			_constantToParameterMap = new Dictionary<ConstantExpression, NamedParameter>();
			_sessionFactory = sessionFactory;
			_collectParameters = true;
		}

		// Since v5.3
		[Obsolete("Use the overload with ISessionFactoryImplementor parameter")]
		public static string Visit(Expression expression, IDictionary<ConstantExpression, NamedParameter> parameters)
		{
			var visitor = new ExpressionKeyVisitor(parameters, null);

			visitor.Visit(expression);

			return visitor.ToString();
		}

		public static string Visit(
			Expression expression,
			ISessionFactoryImplementor sessionFactory,
			out IDictionary<ConstantExpression, NamedParameter> parameters)
		{
			var visitor = new ExpressionKeyVisitor(sessionFactory);
			visitor.Visit(expression);
			parameters = visitor._constantToParameterMap;

			return visitor.ToString();
		}

		public override string ToString()
		{
			return _string.ToString();
		}

		protected override Expression VisitBinary(BinaryExpression expression)
		{
			if (expression.Method != null)
			{
				_string.Append(expression.Method.DeclaringType.Name);
				_string.Append(".");
				VisitMethod(expression.Method);
			}
			else
			{
				_string.Append(expression.NodeType);
			}

			_string.Append("(");

			Visit(expression.Left);
			_string.Append(", ");
			Visit(expression.Right);

			_string.Append(")");

			return expression;
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			Visit(expression.Test);
			_string.Append(" ? ");
			Visit(expression.IfTrue);
			_string.Append(" : ");
			Visit(expression.IfFalse);

			return expression;
		}

		protected override Expression VisitConstant(ConstantExpression expression)
		{
			NamedParameter param;

			if (_collectParameters && 
				!_ignoreParameters.Contains(expression) &&
				!_constantToParameterMap.ContainsKey(expression) && 
				!typeof(IQueryable).IsAssignableFrom(expression.Type) && 
				!IsNullObject(expression))
			{
				// The parameter type will be determined later in HqlGeneratorExpressionVisitor as the expression is not fully parsed.
				var value = expression.Value;
				var parameterName = "p" + (_constantToParameterMap.Count + 1);
				param = IsCollectionType(expression)
					? new NamedListParameter(parameterName, value, null)
					: new NamedParameter(parameterName, value, null);
				_constantToParameterMap.Add(expression, param);
			}

			// 6.0 TODO: Remove if and throw
			if (_constantToParameterMap == null)
				throw new InvalidOperationException("Cannot visit a constant without a constant to parameter map.");
			if (_constantToParameterMap.TryGetValue(expression, out param))
			{
				VisitParameter(param);
				return expression;
			}

			if (expression.Value == null)
			{
				_string.Append("NULL");
			}
			else
			{
				var value = expression.Value as IEnumerable;
				if (value != null && !(value is string) && !(value is IQueryable))
				{
					_string.Append("{");
					_string.Append(String.Join(",", value.Cast<object>()));
					_string.Append("}");
				}
				else
				{
					_string.Append(GetConstantValue(expression.Value));
				}
			}

			return expression;
		}

		private void VisitParameter(NamedParameter param)
		{
			// Nulls generate different query plans.  X = variable generates a different query depending on if variable is null or not.
			if (param.Value == null)
			{
				_string.Append("NULL");
				return;
			}

			if (param.IsCollection && !((IEnumerable) param.Value).Cast<object>().Any())
			{
				_string.Append("EmptyList");
			}
			else
			{
				_string.Append(param.Name);
			}

			// Add the constant type as a different query may be created based on the type
			_string.Append("(");
			_string.Append(param.Value.GetType());
			_string.Append(")");
		}

		private object GetConstantValue(object value)
		{
			// When MappedAs is used we have to put all sql types information in the key in order to
			// distinct when different precisions/sizes are used.
			if (_sessionFactory != null && value is IType type)
			{
				return string.Concat(
					type.Name,
					"[",
					string.Join(",", type.SqlTypes(_sessionFactory).Select(o => o.ToString())),
					"]");
			}

			return value;
		}

		private T AppendCommas<T>(T expression) where T : Expression
		{
			Visit(expression);
			_string.Append(", ");

			return expression;
		}

		protected override Expression VisitLambda<T>(Expression<T> expression)
		{
			_string.Append('(');

			Visit(expression.Parameters, AppendCommas);
			_string.Append(") => (");
			Visit(expression.Body);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitMember(MemberExpression expression)
		{
			base.VisitMember(expression);

			_string.Append('.');
			_string.Append(expression.Member.Name);

			return expression;
		}

		protected override Expression VisitMethodCall(MethodCallExpression expression)
		{
			var method = expression.Method.IsGenericMethod
				? expression.Method.GetGenericMethodDefinition()
				: expression.Method;

			var prevCollectParameters = _collectParameters;
			if (_collectParameters)
			{
				if (VisitorUtil.IsMappedAs(method) ||
					(_pagingMethods.Contains(method) && !_sessionFactory.Dialect.SupportsVariableLimit))
				{
					// Prevent adding the second argument as a parameter
					_ignoreParameters.Add(expression.Arguments[1]);
				}

				// Prevent adding constants when using dyanmic components (e.g. p.Properties["Name"])
				if (VisitorUtil.IsDynamicComponentDictionaryGetter(expression, _sessionFactory))
				{
					_collectParameters = false;
				}
			}

			Visit(expression.Object);
			_string.Append('.');
			VisitMethod(expression.Method);
			_string.Append('(');
			ExpressionVisitor.Visit(expression.Arguments, AppendCommas);
			_string.Append(')');

			_collectParameters = prevCollectParameters;

			return expression;
		}

		protected override Expression VisitNew(NewExpression expression)
		{
			_string.Append("new ");
			_string.Append(expression.Constructor.DeclaringType.AssemblyQualifiedName);
			_string.Append('(');
			Visit(expression.Arguments, AppendCommas);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitParameter(ParameterExpression expression)
		{
			_string.Append(expression.Name);

			return expression;
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression expression)
		{
			_string.Append("IsType(");
			Visit(expression.Expression);
			_string.Append(", ");
			_string.Append(expression.TypeOperand.AssemblyQualifiedName);
			_string.Append(")");

			return expression;
		}

		protected override Expression VisitUnary(UnaryExpression expression)
		{
			_string.Append(expression.NodeType);
			_string.Append('(');
			Visit(expression.Operand);
			_string.Append(')');

			return expression;
		}

		protected override Expression VisitQuerySourceReference(Remotion.Linq.Clauses.Expressions.QuerySourceReferenceExpression expression)
		{
			_string.Append(expression.ReferencedQuerySource.ItemName);
			return expression;
		}

		protected override Expression VisitDynamic(DynamicExpression expression)
		{
			FormatBinder(expression.Binder);
			Visit(expression.Arguments, AppendCommas);
			return expression;
		}

		private void VisitMethod(MethodInfo methodInfo)
		{
			_string.Append(methodInfo.Name);
			if (methodInfo.IsGenericMethod)
			{
				_string.Append('[');
				_string.Append(string.Join(",", methodInfo.GetGenericArguments().Select(a => a.AssemblyQualifiedName)));
				_string.Append(']');
			}
		}

		private void FormatBinder(CallSiteBinder binder)
		{
			switch (binder)
			{
				case ConvertBinder b:
					_string.Append("Convert ").Append(b.Type);
					break;
				case CreateInstanceBinder _:
					_string.Append("Create");
					break;
				case DeleteIndexBinder _:
					_string.Append("DeleteIndex");
					break;
				case DeleteMemberBinder b:
					_string.Append("DeleteMember ").Append(b.Name);
					break;
				case BinaryOperationBinder b:
					_string.Append(b.Operation);
					break;
				case GetIndexBinder _:
					_string.Append("GetIndex");
					break;
				case GetMemberBinder b:
					_string.Append("GetMember ").Append(b.Name);
					break;
				case InvokeBinder _:
					_string.Append("Invoke");
					break;
				case InvokeMemberBinder b:
					_string.Append("InvokeMember ").Append(b.Name);
					break;
				case SetIndexBinder _:
					_string.Append("SetIndex");
					break;
				case SetMemberBinder b:
					_string.Append("SetMember ").Append(b.Name);
					break;
				case UnaryOperationBinder b:
					_string.Append(b.Operation);
					break;
				case DynamicMetaObjectBinder _:
					_string.Append("DynamicMetaObject");
					break;
			}
		}

		private static bool IsCollectionType(ConstantExpression expression)
		{
			if (expression.Value != null)
			{
				return expression.Value is IEnumerable && !(expression.Value is string);
			}

			return expression.Type.IsCollectionType();
		}

		private static bool IsNullObject(ConstantExpression expression)
		{
			return expression.Type == typeof(Object) && expression.Value == null;
		}
	}
}
