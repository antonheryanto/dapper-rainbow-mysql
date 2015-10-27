using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper
{
	internal class TableParameterFactory
	{
		public static TableColumn TableColumnFromProperty(PropertyInfo pi){
			List<Constraints> constraints = new List<Constraints> ();
			List<Modifier> modifiers = new List<Modifier> ();

			foreach (var ci in pi.GetCustomAttributes()) {
				if (ci is PrimaryKey) {
					constraints.Add (new PrimaryKeyConstraint (pi.Name));
				} else if (ci is NotNull) {
					modifiers.Add (new NotNullModifier());
				} else {
					throw new InvalidOperationException ();
				}
			}

			if (pi.PropertyType == typeof(int)) {
				return new TableColumn (pi.Name, "INT", constraints, modifiers);
			} else if(pi.PropertyType == typeof(string)){
				return new TableColumn (pi.Name, "TEXT", constraints, modifiers);
			} else if(pi.PropertyType == typeof(DateTime)){
				return new TableColumn (pi.Name, "DATETIME", constraints, modifiers);
			} else if(pi.PropertyType == typeof(double) || pi.PropertyType == typeof(float)){
				return new TableColumn (pi.Name, "DECIMAL", constraints, modifiers);
			} {
				throw new NotSupportedException (pi.GetType().ToString());
			}
		}
	}
		
	internal abstract class Constraints {
		protected string _column;
		public Constraints(string column){
			_column = column;
		}
		public abstract string getMySql ();
		public override string ToString ()
		{
			return getMySql();
		}
	}

	internal abstract class Modifier {
		public abstract string getMySql ();
		public override string ToString ()
		{
			return getMySql();
		}
	}

	internal class TableColumn {
		private string _name;
		private string _type;
		private List<Constraints> _constraints;
		private List<Modifier> _modifiers;

		public TableColumn(string Name, string Type, List<Constraints> constraints, List<Modifier> modifiers){
			this._name = Name;
			this._type = Type;
			this._constraints = constraints;
			this._modifiers = modifiers;
		}

		private bool hasConstraints(){
			return _constraints.Count > 0;
		}

		private string getConstraintMySql(){
			if (_constraints.Count > 0)
				return ", " + string.Join (" , ", _constraints);
			return "";
		}

		private string getModifierMySql(){
			if(_modifiers.Count > 0)
				return " " + string.Join (" ", _modifiers);
			return "";
		}

		internal List<Constraints> getConstraints(){
			return _constraints;
		}

		public string getMySql ()
		{
			return _name + " " + _type + getModifierMySql() + getConstraintMySql();
		}

		public override string ToString ()
		{
			return getMySql ();
		}
	}
}

