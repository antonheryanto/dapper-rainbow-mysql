using System;
using PetaTest;
using Dapper;
using System.Reflection;
using System.Linq;
using Dapper.TableGeneration;

namespace Test
{
	[TestFixture]
	public class ModelWrapperTests
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
			var objWrapped = new ModelWrapper (new User ().GetType());
			Assert.IsTrue (objWrapped.getPrimaryKey() == "Id");
		
			var objWrapper2 = new ModelWrapper (new Address ().GetType());
			Assert.IsTrue (objWrapper2.getPrimaryKey () == "Street");
		}

		[Test]
		public void CanGetPrimaryKeyType(){
			var objWrapper = new ModelWrapper (new User ().GetType());
			Assert.Equals (objWrapper.getPrimaryKeyType (), typeof(int));

			var objWrapper2 = new ModelWrapper (new Address ().GetType());
			Assert.Equals(objWrapper2.getPrimaryKeyType (), typeof(string));
		}

		[Test]
		public void CanGetColumnProperties(){
			var objWrapper = new ModelWrapper (new Address ().GetType ());
			var columns = objWrapper.getTableColumns ();
			Assert.IsTrue(columns.Count() == 1);

			var objWrapper2 = new ModelWrapper (new User ().GetType ());
			var columns2 = objWrapper2.getTableColumns ();
			Assert.IsTrue(columns2.Count() == 3);
		}
	}
}

