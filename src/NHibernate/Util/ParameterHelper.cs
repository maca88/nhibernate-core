using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Hql;
using NHibernate.Hql.Ast.ANTLR.Tree;
using NHibernate.Proxy;
using NHibernate.Type;

namespace NHibernate.Util
{
	internal static class ParameterHelper
	{
		/// <summary>
		/// Guesses the <see cref="IType"/> from the <c>param</c>'s value.
		/// </summary>
		/// <param name="param">The object to guess the <see cref="IType"/> of.</param>
		/// <param name="sessionFactory">The session factory to search for entity persister.</param>
		/// <param name="isCollection">The output parameter that represents whether the <paramref name="param"/> is a collection.</param>
		/// <returns>An <see cref="IType"/> for the object.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <c>param</c> is null because the <see cref="IType"/>
		/// can't be guess from a null value.
		/// </exception>
		public static IType TryGuessType(object param, ISessionFactoryImplementor sessionFactory, out bool isCollection)
		{
			if (param == null)
			{
				throw new ArgumentNullException("param", "The IType can not be guessed for a null value.");
			}

			if (param is IEnumerable enumerable && !(param is string))
			{
				var firstValue = enumerable.Cast<object>().FirstOrDefault();
				isCollection = true;
				return firstValue == null
					? TryGuessType(enumerable.GetCollectionElementType(), sessionFactory)
					: TryGuessType(firstValue, sessionFactory, out _);
			}

			isCollection = false;
			var clazz = NHibernateProxyHelper.GetClassWithoutInitializingProxy(param);
			return TryGuessType(clazz, sessionFactory);
		}

		/// <summary>
		/// Guesses the <see cref="IType"/> from the <c>param</c>'s value.
		/// </summary>
		/// <param name="param">The object to guess the <see cref="IType"/> of.</param>
		/// <param name="sessionFactory">The session factory to search for entity persister.</param>
		/// <returns>An <see cref="IType"/> for the object.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <c>param</c> is null because the <see cref="IType"/>
		/// can't be guess from a null value.
		/// </exception>
		public static IType GuessType(object param, ISessionFactoryImplementor sessionFactory)
		{
			if (param == null)
			{
				throw new ArgumentNullException("param", "The IType can not be guessed for a null value.");
			}

			System.Type clazz = NHibernateProxyHelper.GetClassWithoutInitializingProxy(param);
			return GuessType(clazz, sessionFactory);
		}

		/// <summary>
		/// Guesses the <see cref="IType"/> from the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="clazz">The <see cref="System.Type"/> to guess the <see cref="IType"/> of.</param>
		/// <param name="sessionFactory">The session factory to search for entity persister.</param>
		/// <param name="isCollection">The output parameter that represents whether the <paramref name="clazz"/> is a collection.</param>
		/// <returns>An <see cref="IType"/> for the <see cref="System.Type"/>.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <c>clazz</c> is null because the <see cref="IType"/>
		/// can't be guess from a null type.
		/// </exception>
		public static IType TryGuessType(System.Type clazz, ISessionFactoryImplementor sessionFactory, out bool isCollection)
		{
			if (clazz == null)
			{
				throw new ArgumentNullException("clazz", "The IType can not be guessed for a null value.");
			}

			if (clazz.IsCollectionType())
			{
				isCollection = true;
				return TryGuessType(ReflectHelper.GetCollectionElementType(clazz), sessionFactory, out _);
			}

			isCollection = false;
			return TryGuessType(clazz, sessionFactory);
		}

		/// <summary>
		/// Guesses the <see cref="IType"/> from the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="clazz">The <see cref="System.Type"/> to guess the <see cref="IType"/> of.</param>
		/// <param name="sessionFactory">The session factory to search for entity persister.</param>
		/// <param name="isCollection">The output parameter that represents whether the <paramref name="clazz"/> is a collection.</param>
		/// <returns>An <see cref="IType"/> for the <see cref="System.Type"/>.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <c>clazz</c> is null because the <see cref="IType"/>
		/// can't be guess from a null type.
		/// </exception>
		public static IType GuessType(System.Type clazz, ISessionFactoryImplementor sessionFactory, out bool isCollection)
		{
			if (clazz == null)
			{
				throw new ArgumentNullException("clazz", "The IType can not be guessed for a null value.");
			}

			if (typeof(IEnumerable).IsAssignableFrom(clazz) && typeof(string) != clazz)
			{
				isCollection = true;
				return GuessType(ReflectHelper.GetCollectionElementType(clazz), sessionFactory);
			}

			isCollection = false;
			return GuessType(clazz, sessionFactory);
		}

		/// <summary>
		/// Guesses the <see cref="IType"/> from the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="clazz">The <see cref="System.Type"/> to guess the <see cref="IType"/> of.</param>
		/// <param name="sessionFactory">The session factory to search for entity persister.</param>
		/// <returns>An <see cref="IType"/> for the <see cref="System.Type"/>.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <c>clazz</c> is null because the <see cref="IType"/>
		/// can't be guess from a null type.
		/// </exception>
		public static IType GuessType(System.Type clazz, ISessionFactoryImplementor sessionFactory)
		{
			return TryGuessType(clazz, sessionFactory) ??
					throw new HibernateException("Could not determine a type for class: " + clazz.AssemblyQualifiedName);
		}

		/// <summary>
		/// Guesses the <see cref="IType"/> from the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="clazz">The <see cref="System.Type"/> to guess the <see cref="IType"/> of.</param>
		/// <param name="sessionFactory">The session factory to search for entity persister.</param>
		/// <returns>An <see cref="IType"/> for the <see cref="System.Type"/>.</returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the <c>clazz</c> is null because the <see cref="IType"/>
		/// can't be guess from a null type.
		/// </exception>
		public static IType TryGuessType(System.Type clazz, ISessionFactoryImplementor sessionFactory)
		{
			if (clazz == null)
			{
				throw new ArgumentNullException("clazz", "The IType can not be guessed for a null value.");
			}

			var type = TypeFactory.HeuristicType(clazz);
			if (type == null || type is SerializableType)
			{
				if (sessionFactory.TryGetEntityPersister(clazz.FullName) != null)
				{
					return NHibernateUtil.Entity(clazz);
				}
			}

			return type;
		}

		///// <summary>
		///// Get the parameter type from the ast tree.
		///// </summary>
		///// <param name="argumentNodes">The parameter type information.</param>
		///// <param name="sessionFactory">The session factory from where to retrive entity properties.</param>
		///// <returns>The <see cref="IType"/> of the parameter.</returns>
		//public static IType GetParameterType(IEnumerable<IASTNode> argumentNodes, ISessionFactoryImplementor sessionFactory)
		//{
		//	var arguments = argumentNodes.ToList();
		//	if (arguments.Count == 0)
		//	{
		//		throw new QueryException("Parameter requires at least one argument");
		//	}

		//	var typeName = arguments[0].Text;
		//	ParameterType parameterType;
		//	if (arguments.Count == 1)
		//	{
		//		parameterType = ParameterType.Basic;
		//	}
		//	else if (!Enum.TryParse(arguments[1].Text, out parameterType))
		//	{
		//		throw new QueryException($"Invalid parameterType argument: {arguments[1].Text}");
		//	}

		//	var additionalData = arguments.Count == 3
		//		? arguments[2].Text
		//		: null;

		//	return GetParameterType(typeName, parameterType, additionalData, sessionFactory) ??
		//			throw new QueryException($"Invalid type argument: {arguments[0].Text}");
		//}

		//private static IType GetParameterType(
		//	string typeName,
		//	ParameterType parameterType,
		//	string additionalData,
		//	ISessionFactoryImplementor sessionFactory)
		//{
		//	switch (parameterType)
		//	{
		//		case ParameterType.Basic:
		//			return TypeFactory.Basic(typeName);
		//		case ParameterType.Entity:
		//			return sessionFactory.GetEntityPersister(typeName).EntityMetamodel.EntityType;
		//		case ParameterType.EntityProperty:
		//			return sessionFactory.GetEntityPersister(typeName).EntityMetamodel.GetPropertyType(additionalData);
		//		case ParameterType.Enum:
		//			return NHibernateUtil.Enum(ReflectHelper.ClassForName(typeName));
		//		//case ParameterType.Enum:
		//		//	return (IType) Activator.CreateInstance(ReflectHelper.ClassForName(typeName));
		//		case ParameterType.Custom:
		//			return NHibernateUtil.Custom(ReflectHelper.ClassForName(typeName));
		//		default:
		//			return null;
		//	}
		//}
	}
}
