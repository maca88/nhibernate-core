using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NHibernate.Test.Linq.ByMethod
{
	[TestFixture]
	public class JoinTests : LinqTestCase
	{
		[Test]
		public void MultipleLinqJoinsWithSameProjectionNames()
		{
			var orders = db.Orders
						   .Join(db.Orders, x => x.OrderId, x => x.OrderId - 1, (order, order1) => new { order, order1 })
						   .Select(x => new { First = x.order, Second = x.order1 })
						   .Join(db.Orders, x => x.First.OrderId, x => x.OrderId - 2, (order, order1) => new { order, order1 })
						   .Select(x => new { FirstId = x.order.First.OrderId, SecondId = x.order.Second.OrderId, ThirdId = x.order1.OrderId })
						   .ToList();

			Assert.That(orders.Count, Is.EqualTo(828));
			Assert.IsTrue(orders.All(x => x.FirstId == x.SecondId - 1 && x.SecondId == x.ThirdId - 1));
		}

		[Test]
		public void Works()
		{
			var qHql = session.CreateQuery(
				"select o.OrderId, o2.OrderId " +
				"from Order o, Order o2 " +
				"where (o.OrderId = o2.OrderId + 1) or (o.OrderId = o2.OrderId-1)"
			).List();
			var q = (from o in db.Orders
			         from o2 in db.Orders
			         where (o.OrderId == o2.OrderId + 1) || (o.OrderId == o2.OrderId - 1)
			         select new { o.OrderId, OrderId2 = o2.OrderId }).ToList();

			var q2Hql = session.CreateQuery(
				"select o.OrderId, p.Name, u.RegisteredAt " +
				"from Order o " +
				"inner join Product p on o.OrderId = p.ProductId " +
				"inner join User u on o.OrderId = u.Id"
			).List();
			var q2 = (from o in db.Orders
			          join p in db.Products on o.OrderId equals p.ProductId
			          join a in db.Users on o.OrderId equals a.Id
			          select new { o.OrderId, p.Name, a.RegisteredAt }
				).ToList();

			var result = (from e in db.Employees
			              from s in e.Subordinates.DefaultIfEmpty()
			              from ss in s.Subordinates.DefaultIfEmpty()
			              select new { e.EmployeeId, Subordinate = (int?) s.EmployeeId, SubSubordinate = (int?) ss.EmployeeId }).ToList();
		}

		private void Subqueries()
		{
			var q221Hql = session.CreateQuery(
				"select o.OrderId" +
				"from (from Order o1 take 1) o"
			).List();

			var q2Hql = session.CreateQuery(
				"select o.OrderId" +
				"from Order o " +
				"left join (from Order o1 take 1) o2 on o.OrderId = o2.OrderId"
			).List();

			var q22Hql = session.CreateQuery(
				"select o.OrderId, p.Name" +
				"from Order o " +
				"inner join Product p on o.OrderId = (select o1.Id from Order o1 take 1)"
			).List();
		}

		[Test]
		public void MultipleLinqJoinsWithSameProjectionNames2()
		{
			var q22 = (from u in db.Users
					   from u2 in db.Users
					   where u.Id != u2.Id
					   select new { u.Id, ID2 = u2.Id }
				).ToList();

			var q2 = (from o in db.Orders
			          join p in db.Products on o.OrderId equals p.ProductId into groupJoin
			          from p in groupJoin.Where(g => g.Name != null).DefaultIfEmpty()
			          select new {ProductId = p == null ? (int?) null : p.ProductId, o.OrderId}
				).ToList();

			var q3 = (from o in db.Orders
			          join p in db.Products on o.OrderId equals p.ProductId into groupJoin
			          from p in groupJoin.DefaultIfEmpty()
			          join a in db.Animals on o.OrderId equals a.Id into groupJoin2
			          from a in groupJoin2.DefaultIfEmpty()
			          select new
			          {
				          ProductId = p == null ? (int?) null : p.ProductId,
				          o.OrderId,
				          AnimalId = a == null ? (int?) null : a.Id
			          }
				).ToList();

			/*
			var result = (from o in db.Orders
			                    from ol in o.OrderLines.DefaultIfEmpty()
			     
						  select new { OrderId = o.OrderId, OrderLineId = ol.Id }).ToList();*/

			//var test = session.CreateQuery("select o from Order o left join Product p with o.OrderId = p.ProductId").List();
			/*
			var orders = db.Orders
			               .Join(db.Products, x => x.OrderId, x => x.ProductId, (order, product) => order)
			               .DefaultIfEmpty()
			               //.Join(db.Animals, x => x.OrderId, x => x.Id, (order, animal) => order)
			               .ToList();*/


			//Assert.IsTrue(orders.All(x => x.FirstId == x.SecondId - 1 && x.SecondId == x.ThirdId - 1));


			/*
			  from l1 in ss.Set<Level1>()
                    join l1_Optional in ss.Set<Level2>() on (int?)l1.Id equals l1_Optional.Level1_Optional_Id into grouping
                    from l1_Optional in grouping.DefaultIfEmpty()
                    from l2 in ss.Set<Level2>()
                    join l2_Required_Reverse in ss.Set<Level1>() on l2.Level1_Required_Id equals l2_Required_Reverse.Id
                    select new { l1_Optional, l2_Required_Reverse },
			 */
		}
	}
}
