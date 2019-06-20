﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NHibernate.Test.Tools.hbm2ddl.SchemaValidator
{
	using System.Threading.Tasks;
	[TestFixture]
	public class SchemaValidateFixtureAsync
	{
		private const string _resourcesPrefix = "NHibernate.Test.Tools.hbm2ddl.SchemaValidator.";
		private const string _version1Resource = _resourcesPrefix + "1_Version.hbm.xml";
		private const string _version2Resource = _resourcesPrefix + "2_Version.hbm.xml";
		private const string _version3Resource = _resourcesPrefix + "3_Version.hbm.xml";
		private Configuration _configuration1;
		private SchemaExport _export1;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_configuration1 = BuildConfiguration(_version1Resource);
		}

		[SetUp]
		public void SetUp()
		{
			_export1 = new SchemaExport(_configuration1);
			_export1.Create(true, true);
		}

		[TearDown]
		public void TearDown()
		{
			_export1.Drop(true, true);
		}

		[Test]
		public async Task ShouldVerifySameTableAsync()
		{
			var validator = new Tool.hbm2ddl.SchemaValidator((_configuration1));
			await (validator.ValidateAsync());
		}

#if NETFX
#endif

		[Test]
		public void ShouldNotVerifyModifiedTableAsync()
		{
			var cfgV2 = BuildConfiguration(_version2Resource);
			var validatorV2 = new Tool.hbm2ddl.SchemaValidator(cfgV2);

			Assert.That(
				() => validatorV2.ValidateAsync(),
				Throws.TypeOf<SchemaValidationException>()
				      .And.Message.EqualTo("Schema validation failed: see list of validation errors")
				      .And.Property("ValidationErrors").Some.Contains("Missing column: Name in ").IgnoreCase.And.Contains("Version").IgnoreCase);
		}

		[Test]
		public void ShouldNotVerifyMultiModifiedTableAsync()
		{
			var cfg = BuildConfiguration(_version3Resource);

			var validator = new Tool.hbm2ddl.SchemaValidator(cfg);

			var error = Assert.ThrowsAsync<SchemaValidationException>(() => validator.ValidateAsync());
			Assert.That(error,
				Has.Message.EqualTo("Schema validation failed: see list of validation errors")
					.And.Property("ValidationErrors").Some.Contains("Missing column: Name in ").IgnoreCase.And.Contains("Version").IgnoreCase);
			Assert.That(error,
				Has.Property("ValidationErrors").Some.Contains("Missing column: Title in ").IgnoreCase.And.Contains("Version").IgnoreCase);
			Assert.That(error,
				Has.Property("ValidationErrors").Some.Contains("Missing sequence or table: ").IgnoreCase.And.Contains("id_table").IgnoreCase);
		}

		private static Configuration BuildConfiguration(string resource)
		{
			var cfg = TestConfigurationHelper.GetDefaultConfiguration();
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
				cfg.AddInputStream(stream);
			return cfg;
		}
	}
}
