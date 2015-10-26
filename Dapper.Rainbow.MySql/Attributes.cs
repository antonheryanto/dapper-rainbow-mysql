using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper
{
	public class PrimaryKey : Attribute {}

	public class TableColumn {
		public string Name {get;}
		public Type Type {get;}

		public TableColumn(string Name, Type Type){
			this.Name = Name;
			this.Type = Type;
		}
	}
}


