using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Test.FilterTest
{
	public class OrderStatus
	{
		public virtual string Code { get; set; }

		public virtual string Name { get; protected set; }

		public virtual ISet<OrderStatusLanguage> Names { get; set; } = new HashSet<OrderStatusLanguage>();
	}

	public class OrderStatusLanguage
	{
		public virtual int Id { get; set; }

		public virtual OrderStatus OrderStatus { get; set; }

		public virtual string LanguageCode { get; set; }

		public virtual string Name { get; set; }
	}
}
