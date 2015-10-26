using System;
using System.Configuration;
using System.Reflection;

namespace Dapper
{
	public class MySqlColumnFactory
	{
		public static TableColumn TableColumnFromProperty(PropertyInfo pi){
			if (pi.PropertyType == typeof(int)) {
				return new TableColumn (pi.Name, "int");
			} else if(pi.PropertyType == typeof(string)){
				return new TableColumn (pi.Name, "text");
			} else {
				throw new NotSupportedException (pi.GetType().ToString());
			}
		}
	}

	public class TableColumn {
		private string _name;
		private string _type;

		public string getMySql ()
		{
			return _name + " " + _type;
		}

		public TableColumn(string Name, string Type){
			this._name = Name;
			this._type = Type;
		}
	}
}

