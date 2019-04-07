using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.DomainModel.Northwind.Entities;
using NHibernate.Exceptions;
using NHibernate.Proxy;
using NUnit.Framework;

namespace NHibernate.Test.Linq
{
	[TestFixture]
	public class SelectionTests : LinqTestCase
	{
		[Test]
		public void CanGetCountOnQueryWithAnonymousType()
		{
			var query = from user in db.Users
						select new { user.Name, RoleName = user.Role.Name };

			int totalCount = query.Count();

			Assert.AreEqual(3, totalCount);
		}

		[Test]
		public void CanGetFirstWithAnonymousType()
		{
			var query = from user in db.Users
						select new { user.Name, RoleName = user.Role.Name };

			var firstUser = query.First();

			Assert.IsNotNull(firstUser);
		}

		[Test]
		public void CanAggregateWithAnonymousType()
		{
			var query = from user in db.Users
						select new { user.Name, RoleName = user.Role.Name };

			var userInfo = query.Aggregate((u1, u2) => u1);

			Assert.IsNotNull(userInfo);
		}

		[Test]
		public void CanSelectUsingMemberInitExpression()
		{
			var query = from user in db.Users
						select new UserDto(user.Id, user.Name) { InvalidLoginAttempts = user.InvalidLoginAttempts };

			var list = query.ToList();
			Assert.AreEqual(3, list.Count);
		}

		[Test]
		public void CanSelectNestedAnonymousType()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							Enums = new
							{
								user.Enum1,
								user.Enum2
							},
							RoleName = user.Role.Name
						};

			var list = query.ToList();
			Assert.AreEqual(3, list.Count);

			//assert role names -- to ensure that the correct values were used to invoke constructor
			Assert.IsTrue(list.All(u => u.RoleName == "Admin" || u.RoleName == "User" || String.IsNullOrEmpty(u.RoleName)));
		}

		[Test]
		public void CanSelectNestedAnonymousTypeWithMultipleReferences()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							Enums = new
							{
								user.Enum1,
								user.Enum2
							},
							RoleName = user.Role.Name,
							RoleIsActive = (bool?) user.Role.IsActive
						};

			var list = query.ToList();
			Assert.AreEqual(3, list.Count);

			//assert role names -- to ensure that the correct values were used to invoke constructor
			Assert.IsTrue(list.All(u => u.RoleName == "Admin" || u.RoleName == "User" || String.IsNullOrEmpty(u.RoleName)));
		}

		[Test]
		public void CanSelectNestedAnonymousTypeWithComponentReference()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							Enums = new
							{
								user.Enum1,
								user.Enum2
							},
							RoleName = user.Role.Name,
							ComponentProperty = user.Component.Property1
						};

			var list = query.ToList();
			Assert.AreEqual(3, list.Count);

			//assert role names -- to ensure that the correct values were used to invoke constructor
			Assert.IsTrue(list.All(u => u.RoleName == "Admin" || u.RoleName == "User" || String.IsNullOrEmpty(u.RoleName)));
		}

		[Test]
		public void CanSelectNestedMemberInitExpression()
		{
			var query = from user in db.Users
						select new UserDto(user.Id, user.Name)
						{
							InvalidLoginAttempts = user.InvalidLoginAttempts,
							Dto2 = new UserDto2
									   {
								RegisteredAt = user.RegisteredAt,
								Enum = user.Enum2
							},
							RoleName = user.Role.Name
						};

			var list = query.ToList();
			Assert.AreEqual(3, list.Count);

			//assert role names -- to ensure that the correct values were used to invoke constructor
			Assert.IsTrue(list.All(u => u.RoleName == "Admin" || u.RoleName == "User" || String.IsNullOrEmpty(u.RoleName)));
		}

		[Test]
		public void CanSelectNestedMemberInitWithinNewExpression()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							user.InvalidLoginAttempts,
							Dto = new UserDto2
									  {
								RegisteredAt = user.RegisteredAt,
								Enum = user.Enum2
							},
							RoleName = user.Role.Name
						};

			var list = query.ToList();
			Assert.AreEqual(3, list.Count);

			//assert role names -- to ensure that the correct values were used to invoke constructor
			Assert.IsTrue(list.All(u => u.RoleName == "Admin" || u.RoleName == "User" || String.IsNullOrEmpty(u.RoleName)));
		}

		[Test]
		public void CanSelectSingleProperty()
		{
			var query = from user in db.Users
						where user.Name == "ayende"
						select user.RegisteredAt;

			DateTime date = query.Single();
			Assert.AreEqual(new DateTime(2010, 06, 17), date);
		}

		[Test]
		public void CanSelectWithProxyInterface()
		{
			var query = (from user in db.IUsers
						 where user.Name == "ayende"
						 select user).ToArray();

			Assert.AreEqual(1, query.Length);
			Assert.AreEqual("ayende", query.First().Name);
		}

		[Test]
		public void CanSelectBinaryExpressions()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							IsSmall = (user.Enum1 == EnumStoredAsString.Small)
						};

			var list = query.ToList();

			foreach (var user in list)
			{
				if (user.Name == "rahien")
				{
					Assert.IsTrue(user.IsSmall);
				}
				else
				{
					Assert.IsFalse(user.IsSmall);
				}
			}
		}

		[Test]
		public void CanSelectWithMultipleBinaryExpressions()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							IsAyende = (user.Enum1 == EnumStoredAsString.Medium
								&& user.Enum2 == EnumStoredAsInt32.High)
						};

			var list = query.ToList();

			foreach (var user in list)
			{
				if (user.Name == "ayende")
				{
					Assert.IsTrue(user.IsAyende);
				}
				else
				{
					Assert.IsFalse(user.IsAyende);
				}
			}
		}

		[Test]
		public void CanSelectWithMultipleBinaryExpressionsWithOr()
		{
			var query = from user in db.Users
						select new
						{
							user.Name,
							IsAyende = (user.Name == "ayende"
								|| user.Name == "rahien")
						};

			var list = query.ToList();

			foreach (var user in list)
			{
				if (user.Name == "ayende" || user.Name == "rahien")
				{
					Assert.IsTrue(user.IsAyende);
				}
				else
				{
					Assert.IsFalse(user.IsAyende);
				}
			}
		}

		[Test]
		public void CanSelectWithAnySubQuery()
		{
			var query = from timesheet in db.Timesheets
						select new
						{
							timesheet.Id,
							HasEntries = timesheet.Entries.Any()
						};

			var list = query.ToList();

			Assert.AreEqual(2, list.Count(t => t.HasEntries));
			Assert.AreEqual(1, list.Count(t => !t.HasEntries));
		}

		[Test]
		public void CanSelectWithAggregateSubQuery()
		{
			if (!Dialect.SupportsScalarSubSelects)
				Assert.Ignore(Dialect.GetType().Name + " does not support scalar sub-queries");

			var timesheets = (from timesheet in db.Timesheets orderby timesheet.Id
							  select new
							  {
								  timesheet.Id,
								  EntryCount = timesheet.Entries.Count
							  }).ToArray();

			Assert.AreEqual(3, timesheets.Length);
			Assert.AreEqual(0, timesheets[0].EntryCount);
			Assert.AreEqual(2, timesheets[1].EntryCount);
			Assert.AreEqual(4, timesheets[2].EntryCount);
		}

		[Test]
		public void CanSelectConditional()
		{
			using (var sqlLog = new SqlLogSpy())
			{
				var q = db.Orders.Where(o => o.Customer.CustomerId == "test")
						   .Select(o => o.ShippedTo.Contains("test") ? o.ShippedTo : o.Customer.CompanyName)
						   .OrderBy(o => o)
						   .Distinct()
						   .ToList();

				Assert.That(FindAllOccurrences(sqlLog.GetWholeLog(), "case"), Is.EqualTo(2));
			}

			using (var sqlLog = new SqlLogSpy())
			{
				var q = db.Orders.Where(o => o.Customer.CustomerId == "test")
						   .Select(o => o.OrderDate.HasValue ? o.OrderDate : o.ShippingDate)
						   .FirstOrDefault();

				Assert.That(FindAllOccurrences(sqlLog.GetWholeLog(), "case"), Is.EqualTo(1));
			}

			using (var sqlLog = new SqlLogSpy())
			{
				var q = db.Orders.Where(o => o.Customer.CustomerId == "test")
						   .Select(o => new
						   {
							   Value = o.OrderDate.HasValue
								   ? o.Customer.CompanyName
								   : (o.ShippingDate.HasValue
									? o.Shipper.CompanyName + "Shipper"
									: o.ShippedTo)
						   })
						   .FirstOrDefault();

				var log = sqlLog.GetWholeLog();
				Assert.That(FindAllOccurrences(log, "as col"), Is.EqualTo(1));
			}

			using (var sqlLog = new SqlLogSpy())
			{
				var q = db.Orders.Where(o => o.Customer.CustomerId == "test")
				          .Select(o => new
				          {
					          Value = o.OrderDate.HasValue
						          ? o.Customer.CompanyName
						          : (o.ShippingDate.HasValue
							          ? o.Shipper.CompanyName + "Shipper"
									  : null)
				          })
				          .FirstOrDefault();

				var log = sqlLog.GetWholeLog();
				Assert.That(FindAllOccurrences(log, "as col"), Is.EqualTo(1));
			}

			using (var sqlLog = new SqlLogSpy())
			{
				var q = db.Orders.Where(o => o.Customer.CustomerId == "test")
						  .Select(o => new
						  {
							  Value = o.OrderDate.HasValue
								  ? o.Customer.CompanyName
								  : (o.ShippingDate.HasValue
									  ? o.Shipper.CompanyName + "Shipper"
									  : "default")
						  })
						  .FirstOrDefault();

				var log = sqlLog.GetWholeLog();
				Assert.That(FindAllOccurrences(log, "as col"), Is.EqualTo(1));
			}

			var defaultValue = "default";
			using (var sqlLog = new SqlLogSpy())
			{
				var q = db.Orders.Where(o => o.Customer.CustomerId == "test")
						  .Select(o => new
						  {
							  Value = o.OrderDate.HasValue
								  ? o.Customer.CompanyName
								  : (o.ShippingDate.HasValue
									  ? o.Shipper.CompanyName + "Shipper"
									  : defaultValue)
						  })
						  .FirstOrDefault();

				var log = sqlLog.GetWholeLog();
				Assert.That(FindAllOccurrences(log, "as col"), Is.EqualTo(1));
			}
		}

		[Test]
		public void CanSelectConditionalSubQuery()
		{
			if (!Dialect.SupportsScalarSubSelects)
				Assert.Ignore(Dialect.GetType().Name + " does not support scalar sub-queries");

			var list = db.Customers
						   .Select(c => new
						   {
							   Date = db.Orders.Where(o => o.Customer.CustomerId == c.CustomerId)
										.Select(o => o.OrderDate.HasValue ? o.OrderDate : o.ShippingDate)
										.Max()
						   })
						   .ToList();
			Assert.That(list, Has.Count.GreaterThan(0));

			var list2 = db.Orders
			              .Select(
				              o => new
				              {
					              UnitPrice = o.Freight.HasValue
						              ? o.OrderLines.Where(l => l.Discount == 1)
						                 .Select(l => l.Product.UnitPrice.HasValue ? l.Product.UnitPrice : l.UnitPrice)
						                 .Max()
						              : o.OrderLines.Where(l => l.Discount == 0)
						                 .Select(l => l.Product.UnitPrice.HasValue ? l.Product.UnitPrice : l.UnitPrice)
						                 .Max()
				              })
			              .ToList();
			Assert.That(list2, Has.Count.GreaterThan(0));

			var list3 = db.Orders
						  .Select(o => new
						  {
							  Date = o.OrderLines.Any(l => o.OrderDate.HasValue)
								  ? db.Employees
									  .Select(e => e.BirthDate.HasValue ? e.BirthDate : e.HireDate)
									  .Max()
								  : o.Employee.Superior != null ? o.Employee.Superior.BirthDate : o.Employee.BirthDate
						  })
						  .ToList();
			Assert.That(list3, Has.Count.GreaterThan(0));
		}

		[Test, KnownBug("NH-3045")]
		public void CanSelectFirstElementFromChildCollection()
		{
			using (var log = new SqlLogSpy())
			{
				var orders = db.Customers
					.Select(customer => customer.Orders.OrderByDescending(x => x.OrderDate).First())
					.ToList();

				Assert.That(orders, Has.Count.GreaterThan(0));

				var text = log.GetWholeLog();
				var count = text.Split(new[] { "SELECT" }, StringSplitOptions.None).Length - 1;
				Assert.That(count, Is.EqualTo(1));
			}
		}

		[Test]
		public void CanSelectWrappedType()
		{
			//NH-2151
			var query = from user in db.Users
						select new Wrapper<User> { item = user, message = user.Name + " " + user.Role };

			Assert.IsTrue(query.ToArray().Length > 0);
		}

		[Test]
		public void CanProjectWithCast()
		{
			// NH-2463
			// ReSharper disable RedundantCast

			var names1 = db.Users.Select(p => new { p1 = p.Name }).ToList();
			Assert.AreEqual(3, names1.Count);

			var names2 = db.Users.Select(p => new { p1 = ((User) p).Name }).ToList();
			Assert.AreEqual(3, names2.Count);

			var names3 = db.Users.Select(p => new { p1 = (p as User).Name }).ToList();
			Assert.AreEqual(3, names3.Count);

			var names4 = db.Users.Select(p => new { p1 = ((IUser) p).Name }).ToList();
			Assert.AreEqual(3, names4.Count);

			var names5 = db.Users.Select(p => new { p1 = (p as IUser).Name }).ToList();
			Assert.AreEqual(3, names5.Count);
			// ReSharper restore RedundantCast
		}

		[Test]
		public void CanSelectAfterOrderByAndTake()
		{
			// NH-3320
			var names = db.Users.OrderBy(p => p.Name).Take(3).Select(p => p.Name).ToList();
			Assert.AreEqual(3, names.Count);
		}

		[Test]
		public void CanSelectManyWithCast()
		{
			// NH-2688
			// ReSharper disable RedundantCast
			var orders1 = db.Customers.Where(c => c.CustomerId == "VINET").SelectMany(o => o.Orders).ToList();
			Assert.AreEqual(5, orders1.Count);

			//$exception	{"c.Orders is not mapped [.SelectMany[NHibernate.DomainModel.Northwind.Entities.Customer,NHibernate.DomainModel.Northwind.Entities.Order](.Where[NHibernate.DomainModel.Northwind.Entities.Customer](NHibernate.Linq.NhQueryable`1[NHibernate.DomainModel.Northwind.Entities.Customer], Quote((c, ) => (String.op_Equality(c.CustomerId, p1))), ), Quote((o, ) => (Convert(o.Orders))), )]"}	System.Exception {NHibernate.Hql.Ast.ANTLR.QuerySyntaxException} 
			// Block OData navigation to detail request requests like 
			// http://localhost:2711/TestWcfDataService.svc/TestEntities(guid&#39;0dd52f6c-1943-4013-a88e-3b63a1fbe11b&#39;)/Details1 
			var orders2 = db.Customers.Where(c => c.CustomerId == "VINET").SelectMany(o => (ISet<Order>) o.Orders).ToList();
			Assert.AreEqual(5, orders2.Count);

			//$exception	{"([100001].Orders As ISet`1)"}	System.Exception {System.NotSupportedException} 
			var orders3 = db.Customers.Where(c => c.CustomerId == "VINET").SelectMany(o => (o.Orders as ISet<Order>)).ToList();
			Assert.AreEqual(5, orders3.Count);

			var orders4 = db.Customers.Where(c => c.CustomerId == "VINET").SelectMany(o => (IEnumerable<Order>) o.Orders).ToList();
			Assert.AreEqual(5, orders4.Count);

			var orders5 = db.Customers.Where(c => c.CustomerId == "VINET").SelectMany(o => (o.Orders as IEnumerable<Order>)).ToList();
			Assert.AreEqual(5, orders5.Count);
			// ReSharper restore RedundantCast
		}

		[Test]
		public void CanSelectCollection()
		{
			var orders = db.Customers.Where(c => c.CustomerId == "VINET").Select(o => o.Orders).ToList();
			Assert.AreEqual(5, orders[0].Count);
		}

		[Test]
		public void CanSelectConditionalKnownTypes()
		{
			if (!Dialect.SupportsScalarSubSelects)
				Assert.Ignore(Dialect.GetType().Name + " does not support scalar sub-queries");

			var moreThanTwoOrderLinesBool = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? true : false, Param = true }).ToList();
			Assert.That(moreThanTwoOrderLinesBool.Count(x => x.HasMoreThanTwo == true), Is.EqualTo(410));

			var moreThanTwoOrderLinesNBool = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? true : (bool?)null, Param = (bool?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNBool.Count(x => x.HasMoreThanTwo == true), Is.EqualTo(410));

			var moreThanTwoOrderLinesShort = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? (short)1 : (short)0, Param = (short)0 }).ToList();
			Assert.That(moreThanTwoOrderLinesShort.Count(x => x.HasMoreThanTwo == 1), Is.EqualTo(410));

			var moreThanTwoOrderLinesNShort = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? (short?)1 : (short?)null, Param = (short?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNShort.Count(x => x.HasMoreThanTwo == 1), Is.EqualTo(410));

			var moreThanTwoOrderLinesInt = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1 : 0, Param = 1 }).ToList();
			Assert.That(moreThanTwoOrderLinesInt.Count(x => x.HasMoreThanTwo == 1), Is.EqualTo(410));

			var moreThanTwoOrderLinesNInt = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1 : (int?)null, Param = (int?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNInt.Count(x => x.HasMoreThanTwo == 1), Is.EqualTo(410));

			var moreThanTwoOrderLinesDecimal = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1m : 0m, Param = 1m }).ToList();
			Assert.That(moreThanTwoOrderLinesDecimal.Count(x => x.HasMoreThanTwo == 1m), Is.EqualTo(410));

			var moreThanTwoOrderLinesNDecimal = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1m : (decimal?)null, Param = (decimal?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNDecimal.Count(x => x.HasMoreThanTwo == 1m), Is.EqualTo(410));

			var moreThanTwoOrderLinesSingle = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1f : 0f, Param = 1f }).ToList();
			Assert.That(moreThanTwoOrderLinesSingle.Count(x => x.HasMoreThanTwo == 1f), Is.EqualTo(410));

			var moreThanTwoOrderLinesNSingle = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1f : (float?)null, Param = (float?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNSingle.Count(x => x.HasMoreThanTwo == 1f), Is.EqualTo(410));

			var moreThanTwoOrderLinesDouble = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1d : 0d, Param = 1d }).ToList();
			Assert.That(moreThanTwoOrderLinesDouble.Count(x => x.HasMoreThanTwo == 1d), Is.EqualTo(410));

			var moreThanTwoOrderLinesNDouble = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? 1d : (double?)null, Param = (double?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNDouble.Count(x => x.HasMoreThanTwo == 1d), Is.EqualTo(410));
			
			var moreThanTwoOrderLinesString = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? "yes" : "no", Param = "no" }).ToList();
			Assert.That(moreThanTwoOrderLinesString.Count(x => x.HasMoreThanTwo == "yes"), Is.EqualTo(410));

			var now = DateTime.Now.Date;
			var moreThanTwoOrderLinesDateTime = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? o.OrderDate.Value : now, Param = now }).ToList();
			Assert.That(moreThanTwoOrderLinesDateTime.Count(x => x.HasMoreThanTwo != now), Is.EqualTo(410));

			var moreThanTwoOrderLinesNDateTime = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? o.OrderDate : null, Param = (DateTime?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNDateTime.Count(x => x.HasMoreThanTwo != null), Is.EqualTo(410));

			var moreThanTwoOrderLinesGuid = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? o.Shipper.Reference : Guid.Empty, Param = Guid.Empty }).ToList();
			Assert.That(moreThanTwoOrderLinesGuid.Count(x => x.HasMoreThanTwo != Guid.Empty), Is.EqualTo(410));

			var moreThanTwoOrderLinesNGuid = db.Orders.Select(o => new { Id = o.OrderId, HasMoreThanTwo = o.OrderLines.Count() > 2 ? o.Shipper.Reference : (Guid?)null, Param = (Guid?)null }).ToList();
			Assert.That(moreThanTwoOrderLinesNGuid.Count(x => x.HasMoreThanTwo != null), Is.EqualTo(410));
		}

		[Test]
		public void CanSelectConditionalEntity()
		{
			var fatherInsteadOfChild = db.Animals.Select(a => a.Father.SerialNumber == "5678" ? a.Father : a).ToList();
			Assert.That(fatherInsteadOfChild, Has.Exactly(2).With.Property("SerialNumber").EqualTo("5678"));
		}

		[Test]
		public void CanSelectConditionalEntityWithCast()
		{
			var fatherInsteadOfChild = db.Mammals.Select(a => a.Father.SerialNumber == "5678" ? (object)a.Father : (object)a).ToList();
			Assert.That(fatherInsteadOfChild, Has.Exactly(2).With.Property("SerialNumber").EqualTo("5678"));
		}

		[Test]
		public void CanSelectConditionalEntityValue()
		{
			var fatherInsteadOfChild = db.Animals.Select(a => a.Father.SerialNumber == "5678" ? a.Father.SerialNumber : a.SerialNumber).ToList();
			Assert.That(fatherInsteadOfChild, Has.Exactly(2).EqualTo("5678"));
		}

		[Test]
		public void CanSelectConditionalEntityValueWithEntityComparison()
		{
			var father = db.Animals.Single(a => a.SerialNumber == "5678");
			var fatherInsteadOfChild = db.Animals.Select(a => a.Father == father ? a.Father.SerialNumber : a.SerialNumber).ToList();
			Assert.That(fatherInsteadOfChild, Has.Exactly(2).EqualTo("5678"));
		}

		[Test]
		public void CanSelectConditionalEntityValueWithEntityComparisonComplex()
		{
			var animal = db.Animals.Select(
				               a => new
				               {
								   Parent = a.Father != null || a.Mother != null ? (a.Father ?? a.Mother) : null,
								   ParentSerialNumber = a.Father != null || a.Mother != null ? (a.Father ?? a.Mother).SerialNumber : null,
								   Parent2 = a.Mother ?? a.Father,
								   a.Father,
								   a.Mother
				               })
			               .FirstOrDefault(o => o.ParentSerialNumber == "5678");

			Assert.That(animal, Is.Not.Null);
			Assert.That(animal.Father, Is.Not.Null);
			Assert.That(animal.Mother, Is.Not.Null);
			Assert.That(animal.Parent, Is.Not.Null);
			Assert.That(animal.Parent2, Is.Not.Null);
			Assert.That(NHibernateUtil.IsInitialized(animal.Parent), Is.True);
			Assert.That(NHibernateUtil.IsInitialized(animal.Parent2), Is.True);
			Assert.That(NHibernateUtil.IsInitialized(animal.Father), Is.True);
			Assert.That(NHibernateUtil.IsInitialized(animal.Mother), Is.True);
		}

		[Test]
		public void CanSelectConditionalEntityValueWithEntityCast()
		{
			var list = db.Animals.Select(
				               a => new
				               {
					               BodyWeight = (double?) (a is Cat 
						               ? (a.Father ?? a.Mother).BodyWeight
										: (a is Dog
											? (a.Mother ?? a.Father).BodyWeight
											: (a.Father.Father.BodyWeight)
						               ))
				               })
			               .ToList();
			Assert.That(list, Has.Exactly(1).With.Property("BodyWeight").Not.Null);
		}

		[Test]
		public void CanSelectConstant()
		{
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = a.Id + 1f + 5d }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = a.Id + 1m + 5 }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = 1 }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = "test" }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = 1 + 5 }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Id = a.Id, Test = 1 }));
			AssertOneSelectColumn(db.Animals.Select(a => new { a.Id, Test = "test" }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = 1, Test2 = "test" }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = a.Id, Test2 = new { Value = "test" }, Test3 = 1 }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = new UserDto(1, "test") }));
			AssertOneSelectColumn(db.Animals.Select(a => new { Test = new UserDto(1, "test"), a.Id }));
			AssertOneSelectColumn(db.Animals.Select(a => new UserDto(1, "test")));
			AssertOneSelectColumn(db.Animals.Select(a => new UserDto(1, "test") {RoleName = a.Description}));
			AssertOneSelectColumn(db.Animals.Select(a => new UserDto(a.Id, "test")));
			AssertOneSelectColumn(db.Animals.Select(a => 1));
			AssertOneSelectColumn(db.Animals.Select(a => "test"));
		}

		[Test]
		public void CanSelectWithIsOperator()
		{
			Assert.DoesNotThrow(() => db.Animals.Select(a => a is Dog).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => a.FatherSerialNumber is string).ToList());
		}


		[Test]
		public void CanSelectComponentProperty()
		{
			AssertOneSelectColumn(db.Users.Select(u => u.Component.Property1));
			AssertOneSelectColumn(db.Users.Select(u => u.Component.OtherComponent.OtherProperty1));
		}

		[Test]
		public void CanSelectNonMappedComponentProperty()
		{
			Assert.DoesNotThrow(() => db.Users.Select(u => u.Component.Property3).ToList());
			Assert.DoesNotThrow(() => db.Users.Select(u => u.Component.OtherComponent.OtherProperty2).ToList());
			var list = db.Users.Select(u => new
			{
				u.Component.OtherComponent.OtherProperty1,
				u.Component.OtherComponent.OtherProperty2,
				u.Component.Property1,
				u.Component.Property2,
				u.Component.Property3
			}).ToList();
			Assert.That(list.Select(o => o.OtherProperty2), Is.EquivalentTo(list.Select(o => o.OtherProperty1)));
			Assert.That(
				list.Select(o => (o.Property1 ?? o.Property2) == null ? null : $"{o.Property1}{o.Property2}"),
				Is.EquivalentTo(list.Select(o => o.Property3)));
		}

		[Test]
		public void CanSelectWithAnInvocation()
		{
			Func<string, string> func = s => s + "postfix";
			Assert.DoesNotThrow(() => db.Animals.Select(a => func(a.SerialNumber)).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => func(a.FatherSerialNumber)).ToList());
		}

		[Test]
		public void CanSelectEnumerable()
		{
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new[] { a.Id } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new[] { a.Id, 1 } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new[] { 1 } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new[] { a, a.Father, a.Mother } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new
			{
				Enumerable = new[]
			{
				new UserDto(a.Id, a.FatherSerialNumber) {RoleName = a.FatherSerialNumber},
				new UserDto(1, a.FatherSerialNumber) {RoleName = a.FatherSerialNumber, InvalidLoginAttempts = 1},
				null,
				new UserDto(1, "test") {RoleName = "test", InvalidLoginAttempts = 1}
			}
			}).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new[] { a.SerialNumber, a.FatherSerialNumber, null } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new int[][] { new[] { a.Id }, new[] { 1 }, new[] { a.Id, 1 } } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new List<int> { a.Id, 1 } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new List<int>(5) { a.Id, 1 } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new List<int>(a.Id) { 1 } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new List<string>(a.Id) { a.SerialNumber, a.FatherSerialNumber, null } }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new
			{
				Enumerable = new List<UserDto>(a.Id)
				{
					new UserDto(a.Id, a.FatherSerialNumber) {RoleName = a.FatherSerialNumber},
					new UserDto(1, a.FatherSerialNumber) {RoleName = a.FatherSerialNumber, InvalidLoginAttempts = 1},
					null,
					new UserDto(1, "test") {RoleName = "test", InvalidLoginAttempts = 1}
				}
			}).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new[] { a.SerialNumber, a.FatherSerialNumber, null }[a.Id - a.Id].Length }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new { Enumerable = new List<string> { a.SerialNumber, a.FatherSerialNumber, null }[a.Id - a.Id].Length }).ToList());
			Assert.DoesNotThrow(() => db.Animals.Select(a => new
			{
				Enumerable = new Dictionary<string, string>
			{
				{ a.SerialNumber, a.FatherSerialNumber },
				{ "1", a.Father.SerialNumber },
				{ "2", null }
			}[a.SerialNumber]
			}).ToList());
		}

		[Test]
		public void CanSelectConditionalSubClassPropertyValue()
		{
			var animal = db.Animals.Select(
				               a => new
				               {
					               Pregnant = a is Mammal ? ((Mammal) a).Pregnant : false
				               })
			               .Where(o => o.Pregnant)
			               .ToList();

			Assert.That(animal, Has.Count.EqualTo(1));
		}

		[Test]
		public void CanSelectConditionalEntityValueWithEntityComparisonRepeat()
		{
			// Check again in the same ISessionFactory to ensure caching doesn't cause failures
			CanSelectConditionalEntityValueWithEntityComparison();
		}

		[Test]
		public void CanSelectConditionalObject()
		{
			var fatherIsKnown = db.Animals.Select(a => new { a.SerialNumber, Superior = a.Father.SerialNumber, FatherIsKnown = a.Father.SerialNumber == "5678" ? (object)true : (object)false }).ToList();
			Assert.That(fatherIsKnown, Has.Exactly(1).With.Property("FatherIsKnown").True);
		}

		[Test]
		public void TestClientSideEvaluation()
		{
			var list = db.Animals.Select(a => new
			{
				ClientSide = string.IsNullOrEmpty(a.FatherSerialNumber) ? 1 : 0,
				ClientSide2 = string.IsNullOrEmpty(a.Father.SerialNumber) ? 1 : 0
			}).ToList();
			Assert.That(list.Select(o => o.ClientSide), Is.EquivalentTo(list.Select(o => o.ClientSide2)));

			var list2 = db.Animals.Select(a => new
			{
				ClientSide = a.Father.IsProxy(),
				ClientSide2 = a.FatherSerialNumber.IsProxy()
			}).ToList();
			Assert.That(list2.Select(o => o.ClientSide), Is.EquivalentTo(list2.Select(o => o.ClientSide2)));

			var list3 = db.Orders.Where(o => o.OrderDate.HasValue).Select(o => new
			{
				ClientSide = o.OrderDate.Value.TimeOfDay.Days,
				ClientSide2 = o.OrderDate.Value
			}).ToList();
			Assert.That(list3.Select(o => o.ClientSide), Is.EquivalentTo(list3.Select(o => o.ClientSide2.TimeOfDay.Days)));

			var list4 = db.Orders.Where(o => o.OrderDate.HasValue).Select(o => new
			{
				o.OrderId,
				ClientSide = o.OrderDate.Value.TimeOfDay.CompareTo(new TimeSpan(o.OrderId)),
				ClientSide2 = o.OrderDate.Value
			}).ToList();
			Assert.That(list4.Select(o => o.ClientSide), Is.EquivalentTo(list4.Select(o => o.ClientSide2.TimeOfDay.CompareTo(new TimeSpan(o.OrderId)))));
		}

		[Test]
		public void TestServerAndClientSideEvaluationComparison()
		{
			var list = db.Animals.Select(
				a => new
				{
					ServerSide = (int?) a.Father.SerialNumber.Length,
					ClientSide = (int?) a.FatherSerialNumber.Length
				}).ToList();
			Assert.That(list.Select(o => o.ServerSide), Is.EquivalentTo(list.Select(o => o.ServerSide)));

			var list1 = db.Animals
						 .Where(a => a.Father.SerialNumber != null)
						 .Select(
							 a => new
							 {
								 ServerSide = a.Father.SerialNumber.Length,
								 ClientSide = a.FatherSerialNumber.Length
							 })
						 .ToList();
			Assert.That(list1.Select(o => o.ClientSide), Is.EquivalentTo(list1.Select(o => o.ServerSide)));

			var clientSide = db.Animals.Select(a => a.FatherSerialNumber.Length.ToString()).ToList();
			var serverSide = db.Animals.Select(a => a.FatherSerialNumber.Length.ToString()).ToList();
			Assert.That(clientSide, Is.EquivalentTo(serverSide));

			Assert.Throws<GenericADOException>(
				() =>
				{
					db.Animals.Select(
						a => new
						{
							ServerSide = a.Father.SerialNumber.Length
						}).ToList();
				});

			Assert.Throws<GenericADOException>(
				() =>
				{
					db.Animals.Select(
						a => new
						{
							ClientSide = a.FatherSerialNumber.Length
						}).ToList();
				});

			var list2 = db.Animals.Select(
				a => new
				{
					ServerSide = a.Father.SerialNumber.Length.ToString(),
					ClientSide = a.FatherSerialNumber.Length.ToString()
				}).ToList();
			Assert.That(list2.Select(o => o.ClientSide), Is.EquivalentTo(list2.Select(o => o.ServerSide)));

			var list3 = db.Animals.Select(
				a => new
				{
					ServerSide = (int?) a.Father.SerialNumber.Substring(0, ((int?) a.Father.SerialNumber.Length - 1) ?? 0).Length,
					ClientSide = (int?) a.FatherSerialNumber.Substring(0, ((int?) a.FatherSerialNumber.Length - 1) ?? 0).Length
				}).ToList();
			Assert.That(list3.Select(o => o.ClientSide), Is.EquivalentTo(list3.Select(o => o.ServerSide)));

			var list4 = db.Animals.Select(a => new
			{
				ServerSide = a.Father.SerialNumber,
				ClientSide = a.FatherSerialNumber,
				Test = (object) null
			}).ToList();
			Assert.That(list4.Select(o => o.ClientSide), Is.EquivalentTo(list4.Select(o => o.ServerSide)));

			var list5 = db.Animals.Select(a => new
			{
				ServerSide = a.Father.SerialNumber == null,
				ClientSide = a.FatherSerialNumber == null
			}).ToList();
			Assert.That(list5.Select(o => o.ClientSide), Is.EquivalentTo(list5.Select(o => o.ServerSide)));

			var list6 = db.Animals
						  .Where(a => a.Father.SerialNumber != null)
						  .Select(
							  a => new
							  {
								  ServerSide = -a.Father.SerialNumber.Length,
								  ClientSide = -a.FatherSerialNumber.Length
							  }).ToList();
			Assert.That(list6.Select(o => o.ClientSide), Is.EquivalentTo(list6.Select(o => o.ServerSide)));

			var list7 = db.Animals
						  .Select(
							  a => new
							  {
								  ServerSide = a.Father != null ? a.Father.SerialNumber : null,
								  ClientSide = a.HasFather ? a.FatherSerialNumber : null
							  }).ToList();
			Assert.That(list7.Select(o => o.ClientSide), Is.EquivalentTo(list7.Select(o => o.ServerSide)));

			var list8 = db.Animals
			              .Where(a => a is Dog)
			              .Select(
				              a => new
				              {
					              ServerSide = (long?) (int?) ((Dog) a).Father.SerialNumber.Length,
					              ClientSide = (long?) (int?) ((Dog) a).FatherSerialNumber.Length
				              }).ToList();
			Assert.That(list8.Select(o => o.ClientSide), Is.EquivalentTo(list8.Select(o => o.ServerSide)));
		}

		public class Wrapper<T>
		{
			public T item;
			public string message;
		}

		private static void AssertOneSelectColumn(IQueryable query)
		{
			using (var sqlLog = new SqlLogSpy())
			{
				// Execute query
				foreach (var item in query) { }
				Assert.That(FindAllOccurrences(sqlLog.GetWholeLog(), "as col"), Is.EqualTo(1));
			}
		}

		private static int FindAllOccurrences(string source, string substring)
		{
			if (source == null)
			{
				return 0;
			}
			int n = 0, count = 0;
			while ((n = source.IndexOf(substring, n, StringComparison.InvariantCulture)) != -1)
			{
				n += substring.Length;
				++count;
			}
			return count;
		}
	}
}
