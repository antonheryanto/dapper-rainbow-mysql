using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper.TableGeneration
{		
	internal abstract class Constraint {
		protected string _column;
		public Constraint(string column){
			_column = column;
		}
		public abstract string getMySql ();
		public override string ToString ()
		{
			return getMySql();
		}
	}
	
}
