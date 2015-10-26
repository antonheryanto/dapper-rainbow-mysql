using System;
using PetaTest;
using Dapper;
using System.Reflection;
using System.Linq;

namespace Test
{
	[TestFixture]
	public class DapperWrapperTests
	{
		public class User
		{
			[PrimaryKey]
			public int Id { get; set;}
			public string Name { get; set; }
			public string Email { get; set; }
		}

		public class Address
		{
			[PrimaryKey]
			public string Street { get; set; }
			private string Hidden { get; set; }
		}

		[Test]
		public void CanGetPrimaryKey ()
		{
			var objWrapped = new DapperWrapper (new User ().GetType());
			Assert.IsTrue (objWrapped.getPrimaryKey() == "Id");

			var objWrapper2 = new DapperWrapper (new Address ().GetType());
			Assert.IsTrue (objWrapper2.getPrimaryKey () == "Street");
		}

		[Test]
		public void CanGetPrimaryKeyType(){
			var objWrapper = new DapperWrapper (new User ().GetType());
			Assert.IsTrue (objWrapper.getPrimaryKeyType () == typeof(int));

			var objWrapper2 = new DapperWrapper (new Address ().GetType());
			Assert.IsTrue (objWrapper2.getPrimaryKeyType () == typeof(string));
		}

		[Test]
		public void CanGetColumnProperties(){
			var objWrapper = new DapperWrapper (new Address ().GetType());
			var columns = objWrapper.getTableColumns ();
			Assert.Equals (columns.Count, 1);

			var objWrapper2 = new DapperWrapper (new User ().GetType());
			var columns2 = objWrapper2.getTableColumns();
			Assert.Equals(columns2.Count, 3);
		}
	}
}

