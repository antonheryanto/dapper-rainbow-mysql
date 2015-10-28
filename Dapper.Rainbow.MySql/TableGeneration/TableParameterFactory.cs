using System;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;

namespace Dapper.TableGeneration
{
	internal class TableParameterFactory
	{
		public static TableColumn TableColumnFromProperty(PropertyInfo pi){
			List<Constraint> constraints = new List<Constraint> ();
			List<Modifier> modifiers = new List<Modifier> ();

			foreach (var ci in pi.GetCustomAttributes()) {
				if (ci is TableConstraintAttribute) {
					var constraint = ci as TableConstraintAttribute;
					constraints.Add (constraint.getConstraint (pi.Name));
				} else if (ci is TableModifierAttribute) {
					var modifier = ci as TableModifierAttribute;
					modifiers.Add (modifier.getModifier ());
				}
			}

			if (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(Int32)) {
				return new TableColumn (pi.Name, "INT", constraints, modifiers);
			} else if (isBoolean (pi)) {
				return new TableColumn (pi.Name, "BOOL", constraints, modifiers);
			} else if (isString (pi)) {
				return new TableColumn (pi.Name, "VARCHAR", constraints, modifiers);
			} else if (isCharacter (pi)) {
				return new TableColumn (pi.Name, "CHAR", constraints, modifiers);
			} else if (isDateTime (pi)) {
				return new TableColumn (pi.Name, "DATETIME", constraints, modifiers);
			} else if (isDecimal (pi)) {
				return new TableColumn (pi.Name, "DECIMAL", constraints, modifiers);
			} else {
				throw new NotSupportedException (pi.GetType().ToString());
			}
		}

		private static bool isDecimal (PropertyInfo pi)
		{
			return pi.PropertyType == typeof(double);
		}

		private static bool isDateTime (PropertyInfo pi)
		{
			return pi.PropertyType == typeof(DateTime);
		}

		private static bool isCharacter (PropertyInfo pi)
		{
			return pi.PropertyType == typeof(char);
		}

		private static bool isString (PropertyInfo pi)
		{
			return pi.PropertyType == typeof(string);
		}

		private static bool isBoolean (PropertyInfo pi)
		{
			return pi.PropertyType == typeof(bool);
		}
	}
}
