using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
	/// <summary>
	/// Snapshotter.
	/// </summary>
	public static class Snapshotter
	{
		/// <summary>
		/// Start the specified obj.
		/// </summary>
		/// <param name="obj">Object.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static Snapshot<T> Start<T> (T obj)
		{
			return new Snapshot<T> (obj);
		}

		/// <summary>
		/// Snapshot.
		/// </summary>
		public class Snapshot<T>
		{
			static Func<T, T> cloner;
			static Func<T, T, List<Change>> differ;
			T memberWiseClone;
			T trackedObject;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Dapper.Snapshotter.Snapshot`1"/> class.
			/// </summary>
			/// <param name="original">Original.</param>
			public Snapshot (T original)
			{
				memberWiseClone = Clone (original);
				trackedObject = original;
			}

			/// <summary>
			/// Change.
			/// </summary>
			public class Change
			{
				/// <summary>
				/// Gets or sets the name.
				/// </summary>
				/// <value>The name.</value>
				public string Name { get; set; }

				/// <summary>
				/// Gets or sets the new value.
				/// </summary>
				/// <value>The new value.</value>
				public object NewValue { get; set; }
			}

			/// <summary>
			/// Diff this instance.
			/// </summary>
			public DynamicParameters Diff ()
			{
				return Diff (memberWiseClone, trackedObject);
			}


			private static T Clone (T myObject)
			{
				cloner = cloner ?? GenerateCloner ();
				return cloner (myObject);
			}

			private static DynamicParameters Diff (T original, T current)
			{
				var dm = new DynamicParameters ();
				differ = differ ?? GenerateDiffer ();
				foreach (var pair in differ (original, current)) {
					dm.Add (pair.Name, pair.NewValue);
				}
				return dm;
			}


			static List<PropertyInfo> RelevantProperties ()
			{
				return typeof (T).GetProperties (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where (p =>
						 p.GetSetMethod (true) != null &&
						 p.GetGetMethod (true) != null &&
						 (p.PropertyType == typeof (string) ||
					      p.PropertyType.GetTypeInfo().IsValueType ||
						  (p.PropertyType.GetTypeInfo ().IsGenericType  && p.PropertyType.GetGenericTypeDefinition () == typeof (Nullable<>)))
						).ToList ();
			}

			// This is used by IL, ReSharper is wrong.
			// ReSharper disable UnusedMember.Local
			private static bool AreEqual<U> (U first, U second)
			{
				if (first == null && second == null) return true;
				if (first == null) return false;
				return first.Equals (second);
			}
			// ReSharper restore UnusedMember.Local

			private static Func<T, T, List<Change>> GenerateDiffer ()
			{

				var dm = new DynamicMethod ("DoDiff", typeof (List<Change>), new [] { typeof (T), typeof (T) }, true);

				var il = dm.GetILGenerator ();
				// change list
				il.DeclareLocal (typeof (List<Change>));
				il.DeclareLocal (typeof (Change));
				il.DeclareLocal (typeof (object)); // boxed change

				il.Emit (OpCodes.Newobj, typeof (List<Change>).GetConstructor (Type.EmptyTypes));
				// [list]
				il.Emit (OpCodes.Stloc_0);

				foreach (var prop in RelevantProperties ()) {
					// []
					il.Emit (OpCodes.Ldarg_0);
					// [original]
					il.Emit (OpCodes.Callvirt, prop.GetGetMethod (true));
					// [original prop val]
					il.Emit (OpCodes.Ldarg_1);
					// [original prop val, current]
					il.Emit (OpCodes.Callvirt, prop.GetGetMethod (true));
					// [original prop val, current prop val]

					il.Emit (OpCodes.Dup);
					// [original prop val, current prop val, current prop val]

					if (prop.PropertyType != typeof (string)) {
						il.Emit (OpCodes.Box, prop.PropertyType);
						// [original prop val, current prop val, current prop val boxed]
					}

					il.Emit (OpCodes.Stloc_2);
					// [original prop val, current prop val]

					il.EmitCall (OpCodes.Call, typeof (Snapshot<T>).GetMethod ("AreEqual", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod (new Type [] { prop.PropertyType }), null);
					// [result] 

					Label skip = il.DefineLabel ();
					il.Emit (OpCodes.Brtrue_S, skip);
					// []

					il.Emit (OpCodes.Newobj, typeof (Change).GetConstructor (Type.EmptyTypes));
					// [change]
					il.Emit (OpCodes.Dup);
					// [change,change]

					il.Emit (OpCodes.Stloc_1);
					// [change]

					il.Emit (OpCodes.Ldstr, prop.Name);
					// [change, name]
					il.Emit (OpCodes.Callvirt, typeof (Change).GetMethod ("set_Name"));
					// []

					il.Emit (OpCodes.Ldloc_1);
					// [change]

					il.Emit (OpCodes.Ldloc_2);
					// [change, boxed]

					il.Emit (OpCodes.Callvirt, typeof (Change).GetMethod ("set_NewValue"));
					// []

					il.Emit (OpCodes.Ldloc_0);
					// [change list]
					il.Emit (OpCodes.Ldloc_1);
					// [change list, change]
					il.Emit (OpCodes.Callvirt, typeof (List<Change>).GetMethod ("Add"));
					// []

					il.MarkLabel (skip);
				}

				il.Emit (OpCodes.Ldloc_0);
				// [change list]
				il.Emit (OpCodes.Ret);

				return (Func<T, T, List<Change>>)dm.CreateDelegate (typeof (Func<T, T, List<Change>>));
			}


			// adapted from http://stackoverflow.com/a/966466/17174
			private static Func<T, T> GenerateCloner ()
			{
				var dm = new DynamicMethod ("DoClone", typeof (T), new Type [] { typeof (T) }, true);
				var ctor = typeof (T).GetConstructor (new Type [] { });

				var il = dm.GetILGenerator ();

				il.DeclareLocal (typeof (T));

				il.Emit (OpCodes.Newobj, ctor);
				il.Emit (OpCodes.Stloc_0);

				foreach (var prop in RelevantProperties ()) {
					il.Emit (OpCodes.Ldloc_0);
					// [clone]
					il.Emit (OpCodes.Ldarg_0);
					// [clone, source]
					il.Emit (OpCodes.Callvirt, prop.GetGetMethod (true));
					// [clone, source val]
					il.Emit (OpCodes.Callvirt, prop.GetSetMethod (true));
					// []
				}

				// Load new constructed obj on eval stack -> 1 item on stack
				il.Emit (OpCodes.Ldloc_0);
				// Return constructed object.   --> 0 items on stack
				il.Emit (OpCodes.Ret);

				var myExec = dm.CreateDelegate (typeof (Func<T, T>));

				return (Func<T, T>)myExec;
			}
		}
	}
}
