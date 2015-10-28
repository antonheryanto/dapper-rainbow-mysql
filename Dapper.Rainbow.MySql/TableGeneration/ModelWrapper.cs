using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using Dapper.TableGeneration;

namespace Dapper.TableGeneration
{
	internal class ModelWrapper
	{
		private Type poco;
		public ModelWrapper(Type poco){
			this.poco = poco;
		}

		public IEnumerable<PropertyInfo> getProperties(){
			return poco.GetProperties ().Where (e => e.CanRead == true);
		}

		private PropertyInfo getPrimaryKeyProperty(){
			return getProperties ().Where (e => Attribute.IsDefined (e, typeof(PrimaryKey))).First();
		}
			
		public string getPrimaryKey(){
			return getPrimaryKeyProperty ().Name;
		}

		public Type getPrimaryKeyType(){
			return getPrimaryKeyProperty ().PropertyType;
		}

		public List<TableColumn> getTableColumns(){
			var columns = new List<TableColumn> ();
			getProperties().ToList().ForEach(e => columns.Add(TableParameterFactory.TableColumnFromProperty(e)));
			return columns;
		}
	}
}
