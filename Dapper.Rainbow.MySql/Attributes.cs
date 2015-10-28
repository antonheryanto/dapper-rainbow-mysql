using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Dapper.TableGeneration;

namespace Dapper
{
	/// <summary>
	/// The abstract attribute class that all custom model attributes should inherit from
	/// </summary>
	public abstract class TableAttribute : Attribute { }

	/// <summary>
	/// The abstract attribute class that all custom model attributes for Table Constraints should inherit from
	/// </summary>
	public abstract class TableConstraintAttribute : TableAttribute { 
		internal abstract Constraint getConstraint(string column);
	}

	/// <summary>
	/// The abstract attribute class that all custom model attributes for Table Modifiers should inherit from
	/// </summary>
	public abstract class TableModifierAttribute : TableAttribute {
		internal abstract Modifier getModifier ();
	}

	/// <summary>
	/// adds a primary key constraint to the property for table generation
	/// </summary>
	public class PrimaryKey : TableConstraintAttribute {
		internal override Constraint getConstraint (string column)
		{
			return new PrimaryKeyConstraint (column);
		}
 	}

	/// <summary>
	/// adds the not null constraint to the property for table generation
	/// </summary>
	public class NotNull : TableModifierAttribute {
		internal override Modifier getModifier(){
			return new NotNullModifier();
		}
	} 

	/// <summary>
	/// adds the auto_increment to the property in table generation
	/// </summary>
	public class AutoIncrement : TableModifierAttribute { 
		internal override Modifier getModifier ()
		{
			return new AutoInfrementModifier ();
		}
	}

	internal class PrimaryKeyConstraint : Constraint
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