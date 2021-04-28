using System;
using System.Collections.Generic;

namespace NHibernate.Test.CacheTest
{
	public class Organization
	{
		public virtual int Id { get; protected set; }

		public virtual string Name { get; set; }

		public virtual ISet<User> Users { get; set; } = new HashSet<User>();
	}

	public class User
	{
		public virtual int Id { get; protected set; }

		public virtual string Name { get; set; }

		public virtual Organization Organization { get; set; }
	}
}
