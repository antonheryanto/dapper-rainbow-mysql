using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper.TableGeneration
{
	internal class TableColumn {
		private string _name;
		private string _type;
		private List<Constraint> _constraints;
		private List<Modifier> _modifiers;

		public TableColumn(string Name, string Type, List<Constraint> constraints, List<Modifier> modifiers){
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

		internal List<Constraint> getConstraints(){
			return _constraints;
		}

		internal List<Modifier> getModifiers(){
			return _modifiers;
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

