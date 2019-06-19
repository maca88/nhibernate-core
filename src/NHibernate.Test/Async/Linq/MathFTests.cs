﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


#if NETCOREAPP2_0
using System;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.DomainModel.Northwind.Entities;
using NUnit.Framework;
using NHibernate.Linq;

namespace NHibernate.Test.Linq
{
	using System.Threading.Tasks;
	using System.Threading;
	[TestFixture]
	public class MathFTestsAsync : LinqTestCase
	{
		private IQueryable<OrderLine> _orderLines;

		protected override void OnSetUp()
		{
			base.OnSetUp();
			_orderLines = db.OrderLines
			                .OrderBy(ol => ol.Id)
			                .Take(10).ToList().AsQueryable();
		}

		[Test]
		public async Task SignAllPositiveTestAsync()
		{
			AssumeFunctionSupported("sign");
			var signs = await ((from o in db.OrderLines
			             select MathF.Sign((float) o.UnitPrice)).ToListAsync());

			Assert.That(signs.All(x => x == 1), Is.True);
		}

		[Test]
		public async Task SignAllNegativeTestAsync()
		{
			AssumeFunctionSupported("sign");
			var signs = await ((from o in db.OrderLines
			             select MathF.Sign(0f - (float) o.UnitPrice)).ToListAsync());

			Assert.That(signs.All(x => x == -1), Is.True);
		}

		[Test]
		public async Task SinTestAsync()
		{
			AssumeFunctionSupported("sin");
			await (TestAsync(o => MathF.Round(MathF.Sin((float) o.UnitPrice), 5)));
		}

		[Test]
		public async Task CosTestAsync()
		{
			AssumeFunctionSupported("cos");
			await (TestAsync(o => MathF.Round(MathF.Cos((float)o.UnitPrice), 5)));
		}

		[Test]
		public async Task TanTestAsync()
		{
			AssumeFunctionSupported("tan");
			await (TestAsync(o => MathF.Round(MathF.Tan((float)o.Discount), 5)));
		}

		[Test]
		public async Task SinhTestAsync()
		{
			AssumeFunctionSupported("sinh");
			await (TestAsync(o => MathF.Round(MathF.Sinh((float)o.Discount), 5)));
		}

		[Test]
		public async Task CoshTestAsync()
		{
			AssumeFunctionSupported("cosh");
			await (TestAsync(o => MathF.Round(MathF.Cosh((float)o.Discount), 5)));
		}

		[Test]
		public async Task TanhTestAsync()
		{
			AssumeFunctionSupported("tanh");
			await (TestAsync(o => MathF.Round(MathF.Tanh((float)o.Discount), 5)));
		}

		[Test]
		public async Task AsinTestAsync()
		{
			AssumeFunctionSupported("asin");
			await (TestAsync(o => MathF.Round(MathF.Asin((float)o.Discount), 5)));
		}

		[Test]
		public async Task AcosTestAsync()
		{
			AssumeFunctionSupported("acos");
			await (TestAsync(o => MathF.Round(MathF.Acos((float)o.Discount), 5)));
		}

		[Test]
		public async Task AtanTestAsync()
		{
			AssumeFunctionSupported("atan");
			await (TestAsync(o => MathF.Round(MathF.Atan((float)o.UnitPrice), 5)));
		}

		[Test]
		public async Task Atan2TestAsync()
		{
			AssumeFunctionSupported("atan2");
			await (TestAsync(o => MathF.Round(MathF.Atan2((float)o.Discount, 0.5f), 5)));
		}

		[Test]
		public async Task PowTestAsync()
		{
			AssumeFunctionSupported("power");
			await (TestAsync(o => MathF.Round(MathF.Pow((float)o.Discount, 0.5f), 5)));
		}

		private async Task TestAsync(Expression<Func<OrderLine, float>> selector, CancellationToken cancellationToken = default(CancellationToken))
		{
			var expected = await (_orderLines
			               .Select(selector)
			               .ToListAsync(cancellationToken));

			var actual = await (db.OrderLines
			               .OrderBy(ol => ol.Id)
			               .Select(selector)
			               .Take(10)
			               .ToListAsync(cancellationToken));

			Assert.AreEqual(expected.Count, actual.Count);
			for (var i = 0; i < expected.Count; i++)
				Assert.AreEqual(expected[i], actual[i], 0.000001);
		}
	}
}
#endif