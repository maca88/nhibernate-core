using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Linq;
using NUnit.Framework;

namespace NHibernate.Test.FilterTest
{
	[TestFixture]
	public class FormulaGlobalFilterFixture : TestCase
	{
		protected override string[] Mappings => new[]
		{
			"FilterTest.FormulaGlobalFilter.hbm.xml"
		};

		protected override string MappingsAssembly => "NHibernate.Test";

		protected override string CacheConcurrencyStrategy => null;

		protected override void Configure(Configuration configuration)
		{
			configuration.SetProperty(Environment.UseSecondLevelCache, "true");
			configuration.SetProperty(Environment.UseQueryCache, "true");
		}

		protected override void OnSetUp()
		{
			using (var s = Sfi.OpenSession())
			using (var tx = s.BeginTransaction())
			{
				var status = CreateOrderStatus("SAV", "Saved", "Shranjen", "Salvato");
				s.Save(status);
				status = CreateOrderStatus("CAN", "Canceled", "Preklican", "Cancellato");
				s.Save(status);
				status = CreateOrderStatus("DIS", "Dispatched", "Poslan", "Inviato");
				s.Save(status);
				status = CreateOrderStatus("COM", "Completed", "Zaključen", "Completato");
				s.Save(status);
				tx.Commit();
			}
		}

		private static OrderStatus CreateOrderStatus(string code, string enName, string slName, string itName)
		{
			var status = new OrderStatus
			{
				Code = code
			};
			status.Names.Add(
				new OrderStatusLanguage
				{
					Name = enName,
					LanguageCode = "en",
					OrderStatus = status
				});
			status.Names.Add(
				new OrderStatusLanguage
				{
					Name = slName,
					LanguageCode = "sl",
					OrderStatus = status
				});
			status.Names.Add(
				new OrderStatusLanguage
				{
					Name = itName,
					LanguageCode = "it",
					OrderStatus = status
				});

			return status;
		}

		protected override void OnTearDown()
		{
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				s.EnableFilter("Language")
				 .SetParameter("Current", "en")
				 .SetParameter("Fallback", "en");

				s.CreateQuery("delete from OrderStatusLanguage").ExecuteUpdate();
				s.CreateQuery("delete from OrderStatus").ExecuteUpdate();
				tx.Commit();
			}
		}

		[Test]
		public void TestCaching()
		{
			using (var s = OpenSession())
			using (var tx = s.BeginTransaction())
			{
				s.EnableFilter("Language")
				 .SetParameter("Current", "it")
				 .SetParameter("Fallback", "en");

				var orderStatus = s.CreateQuery("from OrderStatus s fetch all properties where s.Code = :code")
				                   .SetParameter("code", "SAV")
				                   .UniqueResult<OrderStatus>();

				var statuses = s.Query<OrderStatus>()
				                .WithOptions(o => o.SetCacheMode(CacheMode.Ignore))
				                .ToList();

				


				tx.Commit();
			}
		}
	}
}
