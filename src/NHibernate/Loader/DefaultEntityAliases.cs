using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Persister.Entity;

namespace NHibernate.Loader
{
	internal class EntityColumnIndexes : IEntityColumnIndexes
	{
		private readonly string _suffix;
		private readonly IDictionary<string, int> _aliasIndexes;
		private readonly IDictionary<string, int[]> _userProvidedAliases;
		private readonly IDictionary<string, int[][]> _subclassesPropertiesIndexes;

		public EntityColumnIndexes(
			ILoadable persister,
			string suffix,
			IReadOnlyList<string> orderedAliases)
		: this(null, persister, suffix, orderedAliases)
		{

		}

		public EntityColumnIndexes(
			IDictionary<string, string[]> userProvidedAliases,
			ILoadable persister,
			string suffix,
			IReadOnlyList<string> orderedAliases)
		{
			_suffix = suffix;
			_aliasIndexes = new Dictionary<string, int>(orderedAliases.Count, StringComparer.Ordinal);
			_subclassesPropertiesIndexes = new Dictionary<string, int[][]>();

			for (var i = 0; i < orderedAliases.Count; i++)
			{
				_aliasIndexes.Add(orderedAliases[i], i);
			}

			KeyIndexes = DetermineKeyIndexes(persister);
			DiscriminatorIndex = DetermineDiscriminatorIndex(persister);
			PropertiesIndexes = DeterminePropertiesIndexes(persister);
			VersionIndexes = persister.IsVersioned ? PropertiesIndexes[persister.VersionProperty] : null;
			RowIdIndex = DetermineRowIdIndex(persister);

			if (userProvidedAliases == null)
			{
				return;
			}

			_userProvidedAliases = new Dictionary<string, int[]>();
			foreach (var pair in userProvidedAliases)
			{
				_userProvidedAliases.Add(pair.Key, SafeGetAliasIndexes(pair.Value));
			}
		}

		public int[] KeyIndexes { get; }

		public int? DiscriminatorIndex { get; }

		public int[] VersionIndexes { get; }

		public int? RowIdIndex { get; }

		public int[][] PropertiesIndexes { get; }

		public int[][] GetPropertiesIndexes(ILoadable persister)
		{
			if (_subclassesPropertiesIndexes.TryGetValue(persister.EntityName, out var propertiesIndexes))
			{
				return propertiesIndexes;
			}

			var propertyNames = persister.PropertyNames;
			propertiesIndexes = new int[propertyNames.Length][];

			if (_userProvidedAliases == null)
			{
				for (var i = 0; i < propertyNames.Length; i++)
				{
					propertiesIndexes[i] = SafeGetAliasIndexes(GetPropertyAliases(persister, i));
				}

				_subclassesPropertiesIndexes.Add(persister.EntityName, propertiesIndexes);

				return propertiesIndexes;
			}

			for (var i = 0; i < propertyNames.Length; i++)
			{
				propertiesIndexes[i] =
					SafeGetUserProvidedAliases(propertyNames[i]) ??
					SafeGetAliasIndexes(GetPropertyAliases(persister, i));
			}

			_subclassesPropertiesIndexes.Add(persister.EntityName, propertiesIndexes);

			return propertiesIndexes;
		}

		protected virtual string GetDiscriminatorAlias(ILoadable persister, string suffix)
		{
			return persister.GetDiscriminatorAlias(suffix);
		}

		protected virtual string[] GetIdentifierAliases(ILoadable persister, string suffix)
		{
			return persister.GetIdentifierAliases(suffix);
		}

		protected virtual string[] GetPropertyAliases(ILoadable persister, int j)
		{
			return persister.GetPropertyAliases(_suffix, j);
		}

		private int[] DetermineKeyIndexes(ILoadable persister)
		{
			if (_userProvidedAliases != null)
			{
				var result = SafeGetUserProvidedAliases(persister.IdentifierPropertyName) ??
				             SafeGetUserProvidedAliases(EntityPersister.EntityID);

				if (result != null)
				{
					return result;
				}
			}

			return GetAliasIndexes(GetIdentifierAliases(persister, _suffix));
		}

		private int[][] DeterminePropertiesIndexes(ILoadable persister)
		{
			return GetPropertiesIndexes(persister);
		}

		private int? DetermineDiscriminatorIndex(ILoadable persister)
		{
			if (_userProvidedAliases != null)
			{
				var columns = SafeGetUserProvidedAliases(AbstractEntityPersister.EntityClass);
				if (columns != null)
				{
					return columns[0];
				}
			}

			var alias = GetDiscriminatorAlias(persister, _suffix);
			return alias == null ? (int?) null : GetAliasIndex(alias);
		}

		private int? DetermineRowIdIndex(ILoadable persister)
		{
			if (persister.HasRowId)
			{
				return GetAliasIndex(Loadable.RowIdAlias + _suffix);
			}

			return null;
		}

		private int[] GetAliasIndexes(string[] aliases)
		{
			var indexes = new int[aliases.Length];
			for (var i = 0; i < indexes.Length; i++)
			{
				indexes[i] = GetAliasIndex(aliases[i]);
			}

			return indexes;
		}

		private int[] SafeGetAliasIndexes(string[] aliases)
		{
			var indexes = new int[aliases.Length];
			for (var i = 0; i < indexes.Length; i++)
			{
				var value = SafeGetAliasIndex(aliases[i]);
				if (!value.HasValue)
				{
					return null;
				}

				indexes[i] = value.Value;
			}

			return indexes;
		}

		private int? SafeGetAliasIndex(string alias)
		{
			if (!_aliasIndexes.TryGetValue(alias, out var index))
			{
				return null;
			}

			return index;
		}

		private int GetAliasIndex(string alias)
		{
			if (!_aliasIndexes.TryGetValue(alias, out var index))
			{
				throw new InvalidOperationException($"Alias {alias} is not present in the generated SQL");
			}

			return index;
		}

		private int[] SafeGetUserProvidedAliases(string propertyPath)
		{
			return propertyPath == null || !_userProvidedAliases.TryGetValue(propertyPath, out var indexes)
				? null 
				: indexes;
		}
	}

	/// <summary>
	/// EntityAliases which handles the logic of selecting user provided aliases (via return-property),
	/// before using the default aliases.
	/// </summary>
	public class DefaultEntityAliases : IEntityAliases
	{
		private readonly string _suffix;
		private readonly IDictionary<string, string[]> _userProvidedAliases;
		private string _rowIdAlias;

		public DefaultEntityAliases(ILoadable persister, string suffix)
			: this(null, persister, suffix)
		{
		}

		/// <summary>
		/// Calculate and cache select-clause aliases.
		/// </summary>
		public DefaultEntityAliases(IDictionary<string, string[]> userProvidedAliases, ILoadable persister, string suffix)
		{
			_suffix = suffix;
			_userProvidedAliases = userProvidedAliases?.Count > 0 ? userProvidedAliases : null;

			SuffixedKeyAliases = DetermineKeyAliases(persister);
			SuffixedPropertyAliases = DeterminePropertyAliases(persister);
			SuffixedDiscriminatorAlias = DetermineDiscriminatorAlias(persister);

			SuffixedVersionAliases = persister.IsVersioned ? SuffixedPropertyAliases[persister.VersionProperty] : null;
			//rowIdAlias is generated on demand in property
		}

		/// <summary>
		/// Returns aliases for subclass persister
		/// </summary>
		public string[][] GetSuffixedPropertyAliases(ILoadable persister)
		{
			if (_userProvidedAliases == null)
				return GetAllPropertyAliases(persister);

			var propertyNames = persister.PropertyNames;
			var suffixedPropertyAliases = new string[propertyNames.Length][];
			for (var i = 0; i < propertyNames.Length; i++)
			{
				suffixedPropertyAliases[i] =
					SafeGetUserProvidedAliases(propertyNames[i]) ??
					GetPropertyAliases(persister, i);
			}

			return suffixedPropertyAliases;
		}

		public string[] SuffixedVersionAliases { get; }

		public string[][] SuffixedPropertyAliases { get; }

		public string SuffixedDiscriminatorAlias { get; }

		public string[] SuffixedKeyAliases { get; }

		// TODO: not visible to the user!
		public string RowIdAlias => _rowIdAlias ?? (_rowIdAlias = Loadable.RowIdAlias + _suffix);

		/// <summary>
		/// Returns default aliases for all the properties
		/// </summary>
		private string[][] GetAllPropertyAliases(ILoadable persister)
		{
			var propertyNames = persister.PropertyNames;
			var suffixedPropertyAliases = new string[propertyNames.Length][];
			for (var i = 0; i < propertyNames.Length; i++)
			{
				suffixedPropertyAliases[i] = GetPropertyAliases(persister, i);
			}

			return suffixedPropertyAliases;
		}

		protected virtual string GetDiscriminatorAlias(ILoadable persister, string suffix)
		{
			return persister.GetDiscriminatorAlias(suffix);
		}

		protected virtual string[] GetIdentifierAliases(ILoadable persister, string suffix)
		{
			return persister.GetIdentifierAliases(suffix);
		}

		protected virtual string[] GetPropertyAliases(ILoadable persister, int j)
		{
			return persister.GetPropertyAliases(_suffix, j);
		}

		private string[] DetermineKeyAliases(ILoadable persister)
		{
			if (_userProvidedAliases != null)
			{
				var result = SafeGetUserProvidedAliases(persister.IdentifierPropertyName) ??
				             GetUserProvidedAliases(EntityPersister.EntityID);

				if (result != null)
					return result;
			}

			return GetIdentifierAliases(persister, _suffix);
		}
		
		private string[][] DeterminePropertyAliases(ILoadable persister)
		{
			return GetSuffixedPropertyAliases(persister);
		}
		
		private string DetermineDiscriminatorAlias(ILoadable persister)
		{
			if (_userProvidedAliases != null)
			{
				var columns = GetUserProvidedAliases(AbstractEntityPersister.EntityClass);
				if (columns != null) 
					return columns[0];
			}

			return GetDiscriminatorAlias(persister, _suffix);
		}
		
		private string[] SafeGetUserProvidedAliases(string propertyPath)
		{
			if (propertyPath == null)
				return null;

			return GetUserProvidedAliases(propertyPath);
		}

		private string[] GetUserProvidedAliases(string propertyPath)
		{
			_userProvidedAliases.TryGetValue(propertyPath, out var result);
			return result;
		}
	}
}
