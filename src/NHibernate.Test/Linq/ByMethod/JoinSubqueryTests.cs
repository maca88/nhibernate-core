﻿using System.Collections;
using System.Linq;
using NHibernate.DomainModel.Northwind.Entities;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NHibernate.Test.Linq.ByMethod
{
	[TestFixture]
	public class JoinSubqueryTests : LinqTestCase
	{
		#region HqlEntitySubQuery

		[Test]
		public void HqlEntitySubQuery()
		{
			var result = session.CreateQuery("from Order o inner join (from Order where OrderId = 10248) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertEntitySubQuery(result);

			result = session.CreateQuery("from Order o inner join (from Order o2 where o2.OrderId = 10248) o3 on (o.OrderId - 1) = o3.OrderId").List();
			AssertEntitySubQuery(result);

			result = session.CreateQuery("from Order o inner join (select o2 from Order o2 where o2.OrderId = 10248) o3 on (o.OrderId - 1) = o3.OrderId").List();
			AssertEntitySubQuery(result);

			result = session.CreateQuery("select o, o3 from Order o inner join (select o2 from Order o2 where o2.OrderId = 10248) o3 on (o.OrderId - 1) = o3.OrderId").List();
			AssertEntitySubQuery(result);

			result = session.CreateQuery("select o, o3 from Order o inner join (from Order o2 where o2.OrderId = 10248) o3 on (o.OrderId - 1) = o3.OrderId").List();
			AssertEntitySubQuery(result);
		}

		private void AssertEntitySubQuery(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(2));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
		}

		#endregion

		#region ScalarSubQuery

		[Test]
		public void HqlScalarSubQuery()
		{
			var result = session.CreateQuery(@"
	select o.Customer.CustomerId, o.ShippedTo, o2
	from Order o inner join (
		select OrderId, Customer.CustomerId, ShippedTo from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertScalarSubQuery(result);

			result = session.CreateQuery(@"
	select o.Customer.CustomerId, o.ShippedTo, o2.OrderId, o2.CustomerId, o2.ShippedTo
	from Order o inner join (
		select OrderId, Customer.CustomerId, ShippedTo from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertScalarSubQuery(result);
		}

		private void AssertScalarSubQuery(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(5));
			Assert.That(array[0], Is.EqualTo("TOMSP"));
			Assert.That(array[1], Is.EqualTo("Toms Spezialitäten"));
			Assert.That(array[2], Is.EqualTo(10248));
			Assert.That(array[3], Is.EqualTo("VINET"));
			Assert.That(array[4], Is.EqualTo("Vins et alcools Chevalier"));
		}

		#endregion

		#region IdSubQuery

		[Test]
		public void HqlIdSubQuery()
		{
			var result = session.CreateQuery(@"
	select o
	from Order o inner join (
		select id from Order where OrderId = 10248
	) o2 on o.id = o2.id").List();
			AssertIdSubQuery<Order>(result);

			result = session.CreateQuery(@"
	select o
	from CompositeOrder o inner join (
		select id from CompositeOrder where OrderId = 10248
	) o2 on o.id = o2.id").List();
			AssertIdSubQuery<CompositeOrder>(result);
		}

		private void AssertIdSubQuery<T>(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<T>());
			Assert.That(item, Has.Property("OrderId").EqualTo(10248));
		}

		#endregion

		#region SubclassSubQuery

		[Test]
		public void HqlSubclassSubQuery()
		{
			var result = session.CreateQuery(@"
	from Animal a inner join (
		from Animal where Father is null
	) a2 on a.Father = a2").List();
			AssertSubclassSubQuery(result);
		}

		private void AssertSubclassSubQuery(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(2));
			Assert.That(array[0], Is.TypeOf<Cat>());
			Assert.That(array[1], Is.TypeOf<Dog>());
		}

		#endregion

		#region MixedSubQuery

		[Test]
		public void HqlMixedSubQuery()
		{
			var result = session.CreateQuery(@"
	select o, o2, o.Customer.CustomerId, o.ShippedTo
	from Order o inner join (
		select OrderId, Customer, ShippedTo from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertMixedSubQuery(result);

			result = session.CreateQuery(@"
	select o, o2.OrderId, o2.cu, o2.ShippedTo, o.Customer.CustomerId, o.ShippedTo
	from Order o inner join (
		select OrderId, Customer as cu, ShippedTo from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertMixedSubQuery(result);
		}

		private void AssertMixedSubQuery(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(6));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.EqualTo(10248));
			Assert.That(array[2], Is.TypeOf<Customer>().And.Property("CustomerId").EqualTo("VINET"));
			Assert.That(array[3], Is.EqualTo("Vins et alcools Chevalier"));
			Assert.That(array[4], Is.EqualTo("TOMSP"));
			Assert.That(array[5], Is.EqualTo("Toms Spezialitäten"));
		}

		#endregion

		#region MixedSubQuery

		[Test]
		public void HqlSubQueryComponentPropertySelection()
		{
			var result = session.CreateQuery(@"
	select o2.cu.Address.Street, o2.emp.Address.Street
	from Order o inner join (
		select OrderId, Customer as cu, Employee as emp from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertSubQueryComponentPropertySelection(result);
		}

		private void AssertSubQueryComponentPropertySelection(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(2));
			Assert.That(array[0], Is.EqualTo("59 rue de l'Abbaye"));
			Assert.That(array[1], Is.EqualTo("14 Garrett Hill"));
		}

		#endregion

		#region IdSubQueryWithPagingAndOrderBy

		[Test]
		public void HqlIdSubQueryWithPagingAndOrderBy()
		{
			var result = session.CreateQuery(@"
	select o
	from Order o inner join (
		select id from Order order by OrderId skip 2 take 2
	) o2 on o.id = o2.id").List();
			AssertIdSubQueryWithPagingAndOrderBy<Order>(result);

			result = session.CreateQuery(@"
	select o
	from CompositeOrder o inner join (
		select id from CompositeOrder order by OrderId skip 2 take 2
	) o2 on o.id = o2.id").List();
			AssertIdSubQueryWithPagingAndOrderBy<CompositeOrder>(result);
		}

		private void AssertIdSubQueryWithPagingAndOrderBy<T>(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(2));
			var item = result[0];
			Assert.That(item, Is.TypeOf<T>());
			Assert.That(item, Has.Property("OrderId").EqualTo(10250));
			item = result[1];
			Assert.That(item, Is.TypeOf<T>());
			Assert.That(item, Has.Property("OrderId").EqualTo(10251));
		}

		#endregion

		#region GroupBySubQueryWithAliases

		[Test]
		public void HqlGroupBySubQueryWithAliases()
		{
			var result = session.CreateQuery(@"
	select o, o2
	from Order o inner join (
		select Customer.CustomerId, count(*) as total, max(OrderId) as orderId from Order group by Customer.CustomerId
	) o2 on o.id = o2.orderId
	where o2.total > 30").List();
			AssertGroupBySubQueryWithAliases(result);

			result = session.CreateQuery(@"
	select o, o2.CustomerId, o2.total, o2.orderId
	from Order o inner join (
		select Customer.CustomerId, count(*) as total, max(OrderId) as orderId from Order group by Customer.CustomerId
	) o2 on o.id = o2.orderId
	where o2.total > 30").List();
			AssertGroupBySubQueryWithAliases(result);
		}

		private void AssertGroupBySubQueryWithAliases(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(4));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(11064));
			Assert.That(array[1], Is.EqualTo("SAVEA"));
			Assert.That(array[2], Is.EqualTo(31));
			Assert.That(array[3], Is.EqualTo(11064));
		}

		#endregion

		#region SubQueryWithEntityAlias

		[Test]
		public void HqlSubQueryWithEntityAlias()
		{
			var result = session.CreateQuery(@"
	select o, o3.order.OrderId, o3.customer.CustomerId, o3.ShippedTo
	from Order o inner join (
		select o2 as order, o2.Customer as customer, o2.ShippedTo from Order o2 where o2.OrderId = 10248
	) o3 on (o.OrderId - 1) = o3.order.OrderId").List();
			AssertSubQueryWithEntityAlias(result);
		}

		private void AssertSubQueryWithEntityAlias(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(4));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.EqualTo(10248));
			Assert.That(array[2], Is.EqualTo("VINET"));
			Assert.That(array[3], Is.EqualTo("Vins et alcools Chevalier"));
		}

		#endregion

		#region MultipleEntitySubQueries

		[Test]
		public void HqlMultipleEntitySubQueries()
		{
			var result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	left join (
		from Order where OrderId = 10250
	) o3 on (o3.OrderId - 2) = o2.OrderId").List();
			AssertMultipleEntitySubQueries(result);

			result = session.CreateQuery(@"
	select o, o2, o3
	from Order o
	inner join (
		from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	left join (
		from Order where OrderId = 10250
	) o3 on (o3.OrderId - 2) = o2.OrderId").List();
			AssertMultipleEntitySubQueries(result);
		}

		private void AssertMultipleEntitySubQueries(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(3));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			Assert.That(array[2], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10250));
		}

		#endregion

		#region MultipleScalarSubQueries

		[Test]
		public void HqlMultipleScalarSubQueries()
		{
			var result = session.CreateQuery(@"
	from Order o
	inner join (
		select OrderId, Customer.CustomerId, ShippedTo from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	left join (
		select OrderId, Customer.CustomerId, ShippedTo from Order where OrderId = 10250
	) o3 on (o3.OrderId - 2) = o2.OrderId").List();
			AssertMultipleScalarSubQueries(result);

			result = session.CreateQuery(@"
	select o, o2, o3
	from Order o
	inner join (
		select OrderId, Customer.CustomerId, ShippedTo from Order where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	left join (
		select OrderId, Customer.CustomerId, ShippedTo from Order where OrderId = 10250
	) o3 on (o3.OrderId - 2) = o2.OrderId").List();
			AssertMultipleScalarSubQueries(result);
		}

		private void AssertMultipleScalarSubQueries(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(7));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.EqualTo(10248));
			Assert.That(array[2], Is.EqualTo("VINET"));
			Assert.That(array[3], Is.EqualTo("Vins et alcools Chevalier"));
			Assert.That(array[4], Is.EqualTo(10250));
			Assert.That(array[5], Is.EqualTo("HANAR"));
			Assert.That(array[6], Is.EqualTo("Hanari Carnes"));
		}

		#endregion

		#region NestedEntitySubQueries

		[Test]
		public void HqlNestedEntitySubQueries()
		{
			var result = session.CreateQuery(@"
	from Order o 
	left join (
		select e as emp, e2 as sup
		from Employee e
		left join (from Employee) e2 on e.Superior = e2
	) o3 on o.Employee = o3.emp
	order by o.id
	take 1").List();
			AssertNestedEntitySubQueries(result);

			result = session.CreateQuery(@"
	select o, o3
	from Order o 
	left join (
		select e as emp, e2 as sup
		from Employee e
		left join (from Employee) e2 on e.Superior = e2
	) o3 on o.Employee = o3.emp
	order by o.id
	take 1").List();
			AssertNestedEntitySubQueries(result);

			result = session.CreateQuery(@"
	select o, o3.emp, o3.sup
	from Order o 
	left join (
		select e as emp, e2 as sup
		from Employee e
		left join (from Employee) e2 on e.Superior = e2
	) o3 on o.Employee = o3.emp
	order by o.id
	take 1").List();
			AssertNestedEntitySubQueries(result);
		}

		private void AssertNestedEntitySubQueries(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(3));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			Assert.That(array[1], Is.TypeOf<Employee>().And.Property("EmployeeId").EqualTo(5));
			Assert.That(array[2], Is.Null);
		}

		#endregion

		#region NestedScalarSubQueries

		[Test]
		public void HqlNestedScalarSubQueries()
		{
			var result = session.CreateQuery(@"
	select o.OrderId, o.ShippedTo, o.Customer.CustomerId, o.ShippingAddress.City, o3 as ord3
	from Order o
	left join (
		select o1.OrderId, o1.ShippedTo, o1.Customer.CustomerId, o1.ShippingAddress.City, o2 as ord2
		from Order o1
		left join (
			select OrderId, ShippedTo, Customer.CustomerId, ShippingAddress.City
			from Order
		) o2 on o1.OrderId = o2.OrderId-1
	) o3 on o.OrderId = o3.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedScalarSubQueries(result);

			result = session.CreateQuery(@"
	select o.OrderId, o.ShippedTo, o.Customer.CustomerId, o.ShippingAddress.City, o3 as ord3
	from Order o
	left join (
		select
			o1.OrderId, o1.ShippedTo, o1.Customer.CustomerId, o1.ShippingAddress.City,
			o2.OrderId as oid, o2.ShippedTo as sto, o2.CustomerId as cuid, o2.City as cty
		from Order o1
		left join (
			select OrderId, ShippedTo, Customer.CustomerId, ShippingAddress.City
			from Order
		) o2 on o1.OrderId = o2.OrderId-1
	) o3 on o.OrderId = o3.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedScalarSubQueries(result);

			result = session.CreateQuery(@"
	select
		o.OrderId, o.ShippedTo, o.Customer.CustomerId, o.ShippingAddress.City,
		o3.OrderId, o3.ShippedTo, o3.CustomerId, o3.City, o3.oid, o3.sto, o3.cuid, o3.cty
	from Order o 
	left join (
		select
			o1.OrderId, o1.ShippedTo, o1.Customer.CustomerId, o1.ShippingAddress.City,
			o2.OrderId as oid, o2.ShippedTo as sto, o2.CustomerId as cuid, o2.City as cty
		from Order o1
		left join (
			select OrderId, ShippedTo, Customer.CustomerId, ShippingAddress.City
			from Order
		) o2 on o1.OrderId = o2.OrderId-1
	) o3 on o.OrderId = o3.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedScalarSubQueries(result);
		}

		private void AssertNestedScalarSubQueries(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(12));
			Assert.That(array[0], Is.EqualTo(10248));
			Assert.That(array[1], Is.EqualTo("Vins et alcools Chevalier"));
			Assert.That(array[2], Is.EqualTo("VINET"));
			Assert.That(array[3], Is.EqualTo("Reims"));
			Assert.That(array[4], Is.EqualTo(10249));
			Assert.That(array[5], Is.EqualTo("Toms Spezialitäten"));
			Assert.That(array[6], Is.EqualTo("TOMSP"));
			Assert.That(array[7], Is.EqualTo("Münster"));
			Assert.That(array[8], Is.EqualTo(10250));
			Assert.That(array[9], Is.EqualTo("Hanari Carnes"));
			Assert.That(array[10], Is.EqualTo("HANAR"));
			Assert.That(array[11], Is.EqualTo("Rio de Janeiro"));
		}

		#endregion

		#region NestedMixedSubQueries

		[Test]
		public void HqlNestedMixedSubQueries()
		{
			var result = session.CreateQuery(@"
	select o, o.ShippedTo, o.Customer, o.ShippingAddress.City, o3
	from Order o
	left join (
		select o1 as ord2, o1.ShippedTo as sto2, o1.Customer as cu2, o1.Customer.Address.City as cty2, o2
		from Order o1
		left join (
			select o0 as ord1, o0.ShippedTo as sto1, o0.Customer as cu1, o0.Customer.Address.City as cty1
			from Order o0
		) o2 on o1.OrderId = o2.ord1.OrderId-1
	) o3 on o.OrderId = o3.ord2.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedMixedSubQueries(result);

			result = session.CreateQuery(@"
	select 
		o, o.ShippedTo, o.Customer, o.ShippingAddress.City,
		o3.ord2, o3.sto2, o3.cu2, o3.cty2, o3.sub1
	from Order o
	left join (
		select o1 as ord2, o1.ShippedTo as sto2, o1.Customer as cu2, o1.Customer.Address.City as cty2, o2 as sub1
		from Order o1
		left join (
			select o0 as ord1, o0.ShippedTo as sto1, o0.Customer as cu1, o0.Customer.Address.City as cty1
			from Order o0
		) o2 on o1.OrderId = o2.ord1.OrderId-1
	) o3 on o.OrderId = o3.ord2.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedMixedSubQueries(result);

			result = session.CreateQuery(@"
	select 
		o, o.ShippedTo, o.Customer, o.ShippingAddress.City,
		o3.ord2, o3.sto2, o3.cu2, o3.cty2,
		o3.sub1.ord1, o3.sub1.sto1, o3.sub1.cu1, o3.sub1.cty1
	from Order o
	left join (
		select o1 as ord2, o1.ShippedTo as sto2, o1.Customer as cu2, o1.Customer.Address.City as cty2, o2 as sub1
		from Order o1
		left join (
			select o0 as ord1, o0.ShippedTo as sto1, o0.Customer as cu1, o0.Customer.Address.City as cty1
			from Order o0
		) o2 on o1.OrderId = o2.ord1.OrderId-1
	) o3 on o.OrderId = o3.ord2.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedMixedSubQueries(result);

			result = session.CreateQuery(@"
	select 
		o4.ord3, o4.ord3.ShippedTo, o4.cu3, o4.ord3.ShippingAddress.City,
		o4.sub2.ord2, o4.sub2.ord2.ShippedTo, o4.sub2.cu2, o4.sub2.cu2.Address.City,
		o4.sub2.sub1.ord1, o4.sub2.sub1.ord1.ShippedTo, o4.sub2.sub1.cu1, o4.sub2.sub1.cu1.Address.City
	from Order o5
	inner join (
		select 
			o as ord3, o.Customer as cu3, o3 as sub2
		from Order o
		left join (
			select o1 as ord2, o1.Customer as cu2, o2 as sub1
			from Order o1
			left join (
				select o0 as ord1, o0.Customer as cu1
				from Order o0
			) o2 on o1.OrderId = o2.ord1.OrderId-1
		) o3 on o.OrderId = o3.ord2.OrderId-1
		where o.OrderId = 10248
	) o4 on o5.OrderId = o4.ord3.OrderId
	").List();
			AssertNestedMixedSubQueries(result);

			result = session.CreateQuery(@"
	select
		o, o.ShippedTo, o.Customer, o.ShippingAddress.City,
		o3.ord2, o3.sto2, o3.cu2, o3.cty2,
		o3.ord1, o3.sto1, o3.cu1, o3.cty1
	from Order o
	left join (
		select
			o1 as ord2, o1.ShippedTo as sto2, o1.Customer as cu2, o1.Customer.Address.City as cty2,
			o2.ord1, o2.ord1.ShippedTo as sto1, o2.cu1, o2.cu1.Address.City as cty1
		from Order o1
		left join (
			select o0 as ord1, o0.Customer as cu1
			from Order o0
		) o2 on o1.OrderId = o2.ord1.OrderId-1
	) o3 on o.OrderId = o3.ord2.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedMixedSubQueries(result);

			result = session.CreateQuery(@"
	select
		o, o.ShippedTo, o.Customer, o.ShippingAddress.City,
		o3.ord2, o3.sto2, o3.cu2, o3.cty2,
		o3.ord1, o3.sto1, o3.cu1, o3.cty1
	from Order o
	left join (
		select
			o1 as ord2, o1.ShippedTo as sto2, o1.Customer as cu2, o1.Customer.Address.City as cty2,
			o2 as ord1, o2.ShippedTo as sto1, o2.Customer as cu1, o2.Customer.Address.City as cty1
		from Order o1
		left join (from Order) o2 on o1.OrderId = o2.OrderId-1
	) o3 on o.OrderId = o3.ord2.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedMixedSubQueries(result);

			result = session.CreateQuery(@"
	select
		o, o.ShippedTo, o.Customer, o.ShippingAddress.City,
		o3.ord2, o3.ord2.ShippedTo, o3.ord2.Customer, o3.ord2.Customer.Address.City,
		o3.ord1, o3.ord1.ShippedTo, o3.ord1.Customer, o3.ord1.Customer.Address.City
	from Order o
	left join (
		select o1 as ord2, o2 as ord1
		from Order o1
		left join (from Order) o2 on o1.OrderId = o2.OrderId-1
	) o3 on o.OrderId = o3.ord2.OrderId-1
	where o.OrderId = 10248").List();
			AssertNestedMixedSubQueries(result);
		}

		private void AssertNestedMixedSubQueries(IList result)
		{
			Assert.That(result, Has.Count.EqualTo(1));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(12));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			Assert.That(array[1], Is.EqualTo("Vins et alcools Chevalier"));
			Assert.That(array[2], Is.TypeOf<Customer>().And.Property("CustomerId").EqualTo("VINET"));
			Assert.That(array[3], Is.EqualTo("Reims"));
			Assert.That(array[4], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[5], Is.EqualTo("Toms Spezialitäten"));
			Assert.That(array[6], Is.TypeOf<Customer>().And.Property("CustomerId").EqualTo("TOMSP"));
			Assert.That(array[7], Is.EqualTo("Münster"));
			Assert.That(array[8], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10250));
			Assert.That(array[9], Is.EqualTo("Hanari Carnes"));
			Assert.That(array[10], Is.TypeOf<Customer>().And.Property("CustomerId").EqualTo("HANAR"));
			Assert.That(array[11], Is.EqualTo("Rio de Janeiro"));
		}

		#endregion

		#region EntitySubQueryWithEntityFetch

		[Test]
		public void HqlEntitySubQueryWithEntityFetch()
		{
			session.Clear();
			var result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		inner join fetch o1.Customer
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertEntitySubQueryWithEntityFetch(result, new[] {false, true});

			session.Clear();
			result = session.CreateQuery(@"
	select o, o2.ord
	from Order o
	inner join (
		select o1 as ord, o1.Customer.CustomerId
		from Order o1
		inner join fetch o1.Customer
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.ord.OrderId").List();
			AssertEntitySubQueryWithEntityFetch(result, new[] {false, true});

			session.Clear();
			result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	inner join fetch o.Customer").List();
			AssertEntitySubQueryWithEntityFetch(result, new[] {true, false});

			session.Clear();
			result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		inner join fetch o1.Customer
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	inner join fetch o.Customer").List();
			AssertEntitySubQueryWithEntityFetch(result, new[] {true, true});
		}

		private void AssertEntitySubQueryWithEntityFetch(IList result, bool[] fetches)
		{
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(2));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			var order = (Order) array[0];
			Assert.That(NHibernateUtil.IsInitialized(order.Customer), fetches[0] ? Is.True : (IResolveConstraint) Is.False);
			order = (Order) array[1];
			Assert.That(NHibernateUtil.IsInitialized(order.Customer), fetches[1] ? Is.True : (IResolveConstraint) Is.False);
		}

		#endregion

		#region EntitySubQueryWithCollectionFetch

		[Test]
		public void HqlEntitySubQueryWithCollectionFetch()
		{
			session.Clear();
			var result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		inner join fetch o1.OrderLines
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertEntitySubQueryWithCollectionFetch(result, new[] {false, true});

			session.Clear();
			result = session.CreateQuery(@"
	select o, o2.ord
	from Order o
	inner join (
		select o1 as ord, o1.ShippedTo
		from Order o1
		inner join fetch o1.OrderLines
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.ord.OrderId").List();
			AssertEntitySubQueryWithCollectionFetch(result, new[] {false, true});

			session.Clear();
			result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	inner join fetch o.OrderLines").List();
			AssertEntitySubQueryWithCollectionFetch(result, new[] {true, false});

			session.Clear();
			result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		inner join fetch o1.OrderLines
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	inner join fetch o.OrderLines").List();
			AssertEntitySubQueryWithCollectionFetch(result, new[] {true, true});
		}

		private void AssertEntitySubQueryWithCollectionFetch(IList result, bool[] fetches)
		{
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(2));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			var order = (Order)array[0];
			Assert.That(NHibernateUtil.IsInitialized(order.OrderLines), fetches[0] ? Is.True : (IResolveConstraint) Is.False);
			order = (Order) array[1];
			Assert.That(NHibernateUtil.IsInitialized(order.OrderLines), fetches[1] ? Is.True : (IResolveConstraint) Is.False);
		}

		#endregion

		#region EntitySubQueryWithCollectionOfValuesFetch

		[Test]
		public void HqlEntitySubQueryWithCollectionOfValuesFetch()
		{
			session.Clear();
			var result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		inner join fetch o1.ProductIds
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId").List();
			AssertEntitySubQueryWithCollectionOfValuesFetch(result, new[] {false, true});

			session.Clear();
			result = session.CreateQuery(@"
	select o, o2.ord
	from Order o
	inner join (
		select o1 as ord, o1.ShippedTo
		from Order o1
		inner join fetch o1.ProductIds
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.ord.OrderId").List();
			AssertEntitySubQueryWithCollectionOfValuesFetch(result, new[] {false, true});

			session.Clear();
			result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	inner join fetch o.ProductIds").List();
			AssertEntitySubQueryWithCollectionOfValuesFetch(result, new[] {true, false});

			session.Clear();
			result = session.CreateQuery(@"
	from Order o
	inner join (
		from Order o1
		inner join fetch o1.ProductIds
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId
	inner join fetch o.ProductIds").List();
			AssertEntitySubQueryWithCollectionOfValuesFetch(result, new[] {true, true});
		}

		private void AssertEntitySubQueryWithCollectionOfValuesFetch(IList result, bool[] fetches)
		{
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(2));
			Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
			Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			var order = (Order)array[0];
			Assert.That(NHibernateUtil.IsInitialized(order.ProductIds), fetches[0] ? Is.True : (IResolveConstraint) Is.False);
			order = (Order) array[1];
			Assert.That(NHibernateUtil.IsInitialized(order.ProductIds), fetches[1] ? Is.True : (IResolveConstraint) Is.False);
		}

		#endregion

		#region HqlDuplicateEntitySelectionSubQuery

		[Test]
		public void HqlDuplicateEntitySelectionSubQuery()
		{
			AssertDuplicateEntitySelectionSubQuery(@"
	from Order o
	inner join (
		select o1 as ord1, o1 as ord2
		from Order o1
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.ord1.OrderId");

			AssertDuplicateEntitySelectionSubQuery(@"
	select o, o2.ord1, o2.ord2
	from Order o
	inner join (
		select o1 as ord1, o1 as ord2
		from Order o1
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.ord1.OrderId");

			AssertDuplicateEntitySelectionSubQuery(@"
	select o, o2, o2
	from Order o
	inner join (
		select o1
		from Order o1
		where o1.OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId");

			AssertDuplicateEntitySelectionSubQuery(@"
	select o, o2, o2
	from Order o
	inner join (
		from Order
		where OrderId = 10248
	) o2 on (o.OrderId - 1) = o2.OrderId");
		}

		private void AssertDuplicateEntitySelectionSubQuery(string query)
		{
			IList result;
			using (var logSpy = new SqlLogSpy())
			{
				result = session.CreateQuery(query).List();
				AssertDuplicateEntitySelectionSubQuery(logSpy.GetWholeLog(), result, false);
			}

			using (var logSpy = new SqlLogSpy())
			{
				result = session.CreateQuery(query).Enumerable().OfType<object[]>().ToList();
				AssertDuplicateEntitySelectionSubQuery(logSpy.GetWholeLog(), result, true);
			}
		}

		private void AssertDuplicateEntitySelectionSubQuery(string sql, IList result, bool shallow)
		{
			var selectSql = sql.Substring(0, sql.IndexOf("from"));
			var item = result[0];
			Assert.That(item, Is.TypeOf<object[]>());
			var array = (object[]) item;
			Assert.That(array, Has.Length.EqualTo(3));
			if (shallow)
			{
				Assert.That(GetTotalOccurrences(selectSql, ","), Is.EqualTo(1));
				Assert.That(array[0], Is.AssignableFrom<Order>().And.Property("OrderId").EqualTo(10249));
				Assert.That(array[1], Is.AssignableFrom<Order>().And.Property("OrderId").EqualTo(10248));
				Assert.That(array[1], Is.AssignableFrom<Order>().And.Property("OrderId").EqualTo(10248));
			}
			else
			{
				Assert.That(GetTotalOccurrences(selectSql, ","), Is.EqualTo(27));
				Assert.That(array[0], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10249));
				Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
				Assert.That(array[1], Is.TypeOf<Order>().And.Property("OrderId").EqualTo(10248));
			}
		}

		#endregion
	}
}
