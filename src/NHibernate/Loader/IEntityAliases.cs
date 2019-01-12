using NHibernate.Persister.Entity;

namespace NHibernate.Loader
{
	/// <summary>
	/// Metadata describing the SQL result set column indexes
	/// for a particular entity
	/// </summary>
	public interface IEntityColumnIndexes
	{
		/// <summary>
		/// The result set column indexes for the primary key columns
		/// </summary>
		int[] KeyIndexes { get; }

		/// <summary>
		/// The result set column index for the discriminator columns
		/// </summary>
		int? DiscriminatorIndex { get; }

		/// <summary>
		/// The result set column indexes for the version columns
		/// </summary>
		int[] VersionIndexes { get; }

		/// <summary>
		/// The result set column index for the Oracle row id
		/// </summary>
		int? RowIdIndex { get; }

		/// <summary>
		/// The result set column indexes for the property columns
		/// </summary>
		int[][] PropertiesIndexes { get; }

		/// <summary>
		/// The result set column indexes for the property columns of a subclass
		/// </summary>
		int[][] GetPropertiesIndexes(ILoadable persister);
	}

	/// <summary>
	/// Metadata describing the SQL result set column aliases
	/// for a particular entity
	/// </summary>
	public interface IEntityAliases
	{
		/// <summary>
		/// The result set column aliases for the primary key columns
		/// </summary>
		string[] SuffixedKeyAliases { get; }

		/// <summary>
		/// The result set column aliases for the discriminator columns
		/// </summary>
		string SuffixedDiscriminatorAlias { get; }

		/// <summary>
		/// The result set column aliases for the version columns
		/// </summary>
		string[] SuffixedVersionAliases { get; }

		/// <summary>
		/// The result set column alias for the Oracle row id
		/// </summary>
		string RowIdAlias { get; }

		/// <summary>
		/// The result set column aliases for the property columns
		/// </summary>
		string[][] SuffixedPropertyAliases { get; }

		/// <summary>
		/// The result set column aliases for the property columns of a subclass
		/// </summary>
		string[][] GetSuffixedPropertyAliases(ILoadable persister);
	}
}
