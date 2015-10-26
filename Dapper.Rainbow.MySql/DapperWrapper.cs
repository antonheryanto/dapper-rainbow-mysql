using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Dapper
{
	public class DapperWrapper
	{
		private Object poco;
		public DapperWrapper(Object poco){
			this.poco = poco;
		}

		private IEnumerable<PropertyInfo> getReadableProperties(){
			return poco.GetType ().GetProperties ().Where (e => e.CanRead == true);
		}

		private PropertyInfo getPropertyInfo(Type t){
			var props = poco.GetType().GetProperties ().AsList();
			return props.Where (e => e.GetCustomAttributes(true).AsList().Where(j => j.GetType() == typeof(PrimaryKey)).Count() == 1).FirstOrDefault();
		}

		public string getPrimaryKey(){
			return getPropertyInfo (typeof(PrimaryKey)).Name;
		}

		public Type getPrimaryKeyType(){
			return getPropertyInfo (typeof(PrimaryKey)).PropertyType;
		}

		public List<TableColumn> getTableColumns(){
			var columns = new List<TableColumn> ();
			getReadableProperties().ToList().ForEach(e => columns.Add(new TableColumn(e.Name,e.GetType())));
			return columns;
		}
	}
}

