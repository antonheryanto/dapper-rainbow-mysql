using System;
using PetaTest;
using System.Collections.Generic;

namespace Dapper
{
	[TestFixture]
	public class TableParameterFactoryTests
	{
		private class User {
			[PrimaryKey]
			public int Id { get; set; }
		}

		private class User2 {
			[PrimaryKey]
			public int Id { get; set; }
			public string Name { get; set; }
		}

		[Test]
		public void ColumnsCreatedTest(){
			var props = typeof(User2).GetProperties ();
			var columns = new List<TableColumn> ();

			foreach (var prop in props) {
				columns.Add (TableParameterFactory.TableColumnFromProperty (prop));
			}

			Assert.IsTrue (columns.Count == 2);
		}

		[Test]
		public void ConstraintsCreatedTest(){
			var props = typeof(User).GetProperties();
			var columns = new List<TableColumn>();

			foreach (var prop in props) {
				columns.Add (TableParameterFactory.TableColumnFromProperty (prop));
			}

			Assert.IsTrue(columns.Count == 1);
			Assert.IsTrue(columns [0].getConstraints().Count == 1);
		}
	}
}

