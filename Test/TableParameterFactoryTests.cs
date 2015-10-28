using System;
using PetaTest;
using System.Collections.Generic;
using Dapper.TableGeneration;

namespace Dapper
{
	[TestFixture]
	public class TableParameterFactoryTests
	{
		private class Names {
			[AutoIncrement]
			[PrimaryKey]
			public int Id { get; set; }
			[NotNull]
			public string Name { get; set; }

		}

		[Test]
		public void ColumnsCreatedTest(){
			var props = typeof(Names).GetProperties ();
			var columns = new List<TableColumn> ();

			foreach (var prop in props) {
				columns.Add (TableParameterFactory.TableColumnFromProperty (prop));
			}

			Assert.IsTrue (columns.Count == 2);
		}

		[Test]
		public void ConstraintsCreatedTest(){
			var props = typeof(Names).GetProperties();
			var columns = new List<TableColumn>();

			foreach (var prop in props) {
				columns.Add (TableParameterFactory.TableColumnFromProperty (prop));
			}

			Assert.IsTrue (hasTwoColumns (columns));
			Assert.IsTrue (hasAConstraint (columns[0]));
			Assert.IsTrue (hasAModifier (columns[0]));
			Assert.IsTrue (hasAModifier (columns[1]));
		}

		static bool hasTwoColumns (List<TableColumn> columns)
		{
			return columns.Count == 2;
		}

		static bool hasAModifier (TableColumn column)
		{
			return column.getModifiers ().Count == 1;
		}

		static bool hasAConstraint (TableColumn column)
		{
			return column.getConstraints ().Count == 1;
		}

	}
}

