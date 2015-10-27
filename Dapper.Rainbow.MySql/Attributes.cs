using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper
{
	/// <summary>
	/// The abstract Attribute class that all custom model attributes should inherit from
	/// </summary>
	public abstract class TableConstraintAttribute : Attribute { }

	/// <summary>
	/// adds a primary key constraint to the property for table generation
	/// </summary>
	public class PrimaryKey : TableConstraintAttribute { }

	/// <summary>
	/// adds the not null constraint to the property for table generation
	/// </summary>
	public class NotNull : TableConstraintAttribute { } 

	/// <summary>
	/// adds the auto_increment to the property in table generation
	/// </summary>
	public class AutoIncrement : TableConstraintAttribute { }

	internal class PrimaryKeyConstraint : Constraints
	{
		public PrimaryKeyConstraint(string column) : base(column){ }

		public override string getMySql(){
			return "PRIMARY KEY(" + _column + ") ";
		}
	}

	internal class NotNullModifier : Modifier
	{
		public override string getMySql(){
			return "NOT NULL";
		}
	}

	internal class AutoInfrementModifier : Modifier {
		public override string getMySql ()
		{
			return "AUTO_INCREMENT";
		}
	}
}