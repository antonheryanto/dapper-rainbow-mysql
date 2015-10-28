using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper.TableGeneration
{		

	internal abstract class Modifier {
		public abstract string getMySql ();
		public override string ToString ()
		{
			return getMySql();
		}
	}
	
}
