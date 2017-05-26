using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
	/// <summary>
	/// A container for a database, assumes all the tables have an Id column named Id
	/// </summary>
	/// <typeparam name="TDatabase"></typeparam>
	public abstract partial class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
	{
		/// <summary>
		/// A container for table with table type and id type
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TId"></typeparam>
		/// </summary>
		public partial class Table<T, TId>
		{
			internal Database<TDatabase> database;
			internal string tableName;
			internal string likelyTableName;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Dapper.Database`1.Table`2"/> class.
			/// </summary>
			/// <param name="database">Database.</param>
			/// <param name="likelyTableName">Likely table name.</param>
			public Table (Database<TDatabase> database, string likelyTableName)
			{
				this.database = database;
				this.likelyTableName = likelyTableName;
			}

			/// <summary>
			/// Gets the name of the table.
			/// </summary>
			/// <value>The name of the table.</value>
			public string TableName {
				get {
					tableName = tableName ?? database.DetermineTableName<T> (likelyTableName);
					return tableName;
				}
			}

			/// <summary>
			/// Insert a row into the db
			/// </summary>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public virtual long Insert (dynamic data)
			{
				var o = (object)data;
				List<string> paramNames = GetParamNames (o);
				paramNames.Remove ("Id");

				string cols = string.Join ("`,`", paramNames);
				string cols_params = string.Join (",", paramNames.Select (p => "@" + p));
				var sql = $"INSERT INTO `{TableName}` (`{cols}`) VALUES ({cols_params}); SELECT LAST_INSERT_ID()";
				var id = database.Query (sql, o).Single () as IDictionary<string, object>;
				return Convert.ToInt64 (id.Values.Single ());
			}

			/// <summary>
			/// Update a record in the DB
			/// </summary>
			/// <param name="id"></param>
			/// <param name="data"></param>
			/// <returns></returns>
			public int Update (TId id, dynamic data) => Update (new { id }, data);

			/// <summary>
			/// Update a record in the DB
			/// </summary>
			/// <param name="where"></param>
			/// <param name="data"></param>
			/// <returns></returns>
			public int Update (dynamic where, dynamic data)
			{
				List<string> paramNames = GetParamNames ((object)data);
				List<string> keys = GetParamNames ((object)where);

				var cols_update = string.Join (",", paramNames.Select (p => $"`{p}`= @{p}"));
				var cols_where = string.Join (" AND ", keys.Select (p => $"`{p}` = @{p}"));
				var sql = $"UPDATE `{TableName}` SET {cols_update} WHERE {cols_where}";

				var parameters = new DynamicParameters (data);
				parameters.AddDynamicParams (where);
				return database.Execute (sql, parameters);
			}

			/// <summary>
			/// Insert a row into the db or update when key is duplicated 
			/// only for autoincrement key
			/// </summary>
			/// <param name="id"></param>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public long InsertOrUpdate (TId id, dynamic data) => InsertOrUpdate (new { id }, data);

			/// <summary>
			/// Insert a row into the db or update when key is duplicated 
			/// for autoincrement key
			/// </summary>
			/// <param name="key"></param>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public long InsertOrUpdate (dynamic key, dynamic data)
			{
				List<string> paramNames = GetParamNames ((object)data);
				string k = GetParamNames ((object)key).Single ();

				string cols = string.Join ("`,`", paramNames);
				string cols_params = string.Join (",", paramNames.Select (p => "@" + p));
				string cols_update = string.Join (",", paramNames.Select (p => $"`{p}` = @{p}"));
				var sql = $@"
INSERT INTO `{TableName}` (`{cols}`,`{k}`) VALUES ({cols_params}, @{k})
ON DUPLICATE KEY UPDATE `{k}` = LAST_INSERT_ID(`{k}`), {cols_update}; SELECT LAST_INSERT_ID()";
				var parameters = new DynamicParameters (data);
				parameters.AddDynamicParams (key);
				var id = database.Query (sql, parameters).Single () as IDictionary<string, object>;
				return Convert.ToInt64 (id.Values.Single ());
			}

			/// <summary>
			/// Insert a row into the db
			/// </summary>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public int InsertOrUpdate (dynamic data)
			{
				List<string> paramNames = GetParamNames ((object)data);
				string cols = string.Join ("`,`", paramNames);
				string cols_params = string.Join (",", paramNames.Select (p => "@" + p));
				string cols_update = string.Join (",", paramNames.Select (p => $"`{p}` = @{p}"));
				var sql = $"INSERT INTO `{TableName}` (`{cols}`) VALUES ({cols_params}) ON DUPLICATE KEY UPDATE {cols_update}";
				return database.Execute (sql, data);
			}

			/// <summary>
			/// Delete a record for the DB
			/// </summary>
			/// <param name="id"></param>
			/// <returns></returns>
			public bool Delete (TId id)
			{
				return database.Execute ($"DELETE FROM `{TableName}` WHERE Id = @id", new { id }) > 0;
			}

			/// <summary>
			/// Delete a record for the DB
			/// </summary>
			/// <param name="where"></param>
			/// <returns></returns>
			public bool Delete (dynamic where = null)
			{
				if (where == null) return database.Execute ($"TRUNCATE `{TableName}`") > 0;
				var owhere = where as object;
				var paramNames = GetParamNames (owhere);
				var w = string.Join (" AND ", paramNames.Select (p => $"`{p}` = @{p}"));
				return database.Execute ($"DELETE FROM `{TableName}` WHERE {w}", owhere) > 0;
			}

			/// <summary>
			/// Grab a record with a particular Id from the DB 
			/// </summary>
			/// <param name="id"></param>
			/// <returns></returns>
			public T Get (TId id)
			{
				return database.Query<T> ($"SELECT * FROM `{TableName}` WHERE id = @id", new { id }).FirstOrDefault ();
			}

			/// <summary>
			/// Grab a record with where clause from the DB 
			/// </summary>
			/// <param name="where"></param>
			/// <returns></returns>
			public T Get (dynamic where) => First (where);

			/// <summary>
			/// Grab a first record
			/// </summary>
			/// <param name="where"></param>
			/// <returns></returns>
			public T First (dynamic where = null)
			{
				if (where == null) return database.Query<T> ($"SELECT * FROM `{TableName}` LIMIT 1").FirstOrDefault ();
				var owhere = where as object;
				var paramNames = GetParamNames (owhere);
				var w = string.Join (" AND ", paramNames.Select (p => $"`{p}` = @{p}"));
				return database.Query<T> ($"SELECT * FROM `{TableName}` WHERE {w} LIMIT 1", owhere).FirstOrDefault();
			}

			/// <summary>
			/// Return All record
			/// </summary>
			/// <param name="where"></param>
			/// <returns></returns>
			public IEnumerable<T> All (dynamic where = null)
			{
				var sql = $"SELECT * FROM `{TableName}`";
				if (where == null) return database.Query<T> (sql);
				var paramNames = GetParamNames ((object)where);
				var w = string.Join (" AND ", paramNames.Select (p => $"`{p}` = @{p}"));
				return database.Query<T> (sql + " WHERE " + w, where);
			}

			static ConcurrentDictionary<Type, List<string>> paramNameCache = new ConcurrentDictionary<Type, List<string>> ();

			internal static List<string> GetParamNames (object o)
			{
				if (o is DynamicParameters) {
					return (o as DynamicParameters).ParameterNames.ToList ();
				}

				List<string> paramNames;
				if (!paramNameCache.TryGetValue (o.GetType (), out paramNames)) {
					paramNames = new List<string> ();
					foreach (var prop in o.GetType ().GetProperties (BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public)) {
						var attribs = prop.GetCustomAttributes (typeof (IgnorePropertyAttribute), true);
						var attr = attribs.FirstOrDefault () as IgnorePropertyAttribute;
						if (attr == null || (attr != null && !attr.Value)) {
							paramNames.Add (prop.Name);
						}
					}
					paramNameCache [o.GetType ()] = paramNames;
				}
				return paramNames;
			}
		}

		/// <summary>
		/// Table implementation using id long
		/// </summary>
		public class Table<T> : Table<T, long>
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="T:Dapper.Database`1.Table`1"/> class.
			/// </summary>
			/// <param name="database">Database.</param>
			/// <param name="likelyTableName">Likely table name.</param>
			public Table (Database<TDatabase> database, string likelyTableName)
				: base (database, likelyTableName)
			{
			}
		}

		IDbConnection connection;
		int commandTimeout;
		IDbTransaction transaction;
		bool lowerCase = true;

		/// <summary>
		/// Initiate Database
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="commandTimeout"></param>
		/// <param name="lowerCase"></param>
		/// <returns></returns>
		public static TDatabase Init (IDbConnection connection, int commandTimeout, bool lowerCase = true)
		{
			TDatabase db = new TDatabase ();
			db.InitDatabase (connection, commandTimeout, lowerCase);
			return db;
		}

		internal static Action<TDatabase> tableConstructor;

		internal void InitDatabase (IDbConnection connection, int commandTimeout, bool lowerCase)
		{
			this.connection = connection;
			this.commandTimeout = commandTimeout;
			this.lowerCase = lowerCase;
			if (tableConstructor == null) {
				tableConstructor = CreateTableConstructorForTable ();
			}

			tableConstructor (this as TDatabase);
		}

		internal virtual Action<TDatabase> CreateTableConstructorForTable ()
		{
			return CreateTableConstructor (typeof (Table<>));
		}

		/// <summary>
		/// Begins the transaction.
		/// </summary>
		/// <param name="isolation">Isolation.</param>
		public void BeginTransaction (IsolationLevel isolation = IsolationLevel.ReadCommitted)
		{
			transaction = connection.BeginTransaction (isolation);
		}

		/// <summary>
		/// Commits the transaction.
		/// </summary>
		public void CommitTransaction ()
		{
			transaction.Commit ();
			transaction = null;
		}

		/// <summary>
		/// Rollbacks the transaction.
		/// </summary>
		public void RollbackTransaction ()
		{
			transaction.Rollback ();
			transaction = null;
		}

		/// <summary>
		/// Creates the table constructor.
		/// </summary>
		/// <returns>The table constructor.</returns>
		/// <param name="tableType">Table type.</param>
		protected Action<TDatabase> CreateTableConstructor (Type tableType)
		{
			var dm = new DynamicMethod ("ConstructInstances", null, new Type [] { typeof (TDatabase) }, true);
			var il = dm.GetILGenerator ();

			var setters = GetType ().GetProperties ()
				.Where (p => p.PropertyType.GetTypeInfo ().IsGenericType && p.PropertyType.GetGenericTypeDefinition () == tableType)
				.Select (p => Tuple.Create (
						 p.GetSetMethod (true),
						 p.PropertyType.GetConstructor (new Type [] { typeof (TDatabase), typeof (string) }),
						 p.Name,
						 p.DeclaringType
				  ));

			foreach (var setter in setters) {
				il.Emit (OpCodes.Ldarg_0);
				// [db]

				il.Emit (OpCodes.Ldstr, setter.Item3);
				// [db, likelyname]

				il.Emit (OpCodes.Newobj, setter.Item2);
				// [table]

				var table = il.DeclareLocal (setter.Item2.DeclaringType);
				il.Emit (OpCodes.Stloc, table);
				// []

				il.Emit (OpCodes.Ldarg_0);
				// [db]

				il.Emit (OpCodes.Castclass, setter.Item4);
				// [db cast to container]

				il.Emit (OpCodes.Ldloc, table);
				// [db cast to container, table]

				il.Emit (OpCodes.Callvirt, setter.Item1);
				// []
			}

			il.Emit (OpCodes.Ret);
			return (Action<TDatabase>)dm.CreateDelegate (typeof (Action<TDatabase>));
		}

		static ConcurrentDictionary<Type, string> tableNameMap = new ConcurrentDictionary<Type, string> ();
		private string DetermineTableName<T> (string likelyTableName)
		{
			string name;

			if (!tableNameMap.TryGetValue (typeof (T), out name)) {
				name = !lowerCase ? likelyTableName : likelyTableName.ToLower ();
				if (!TableExists (name)) {
					name = !lowerCase ? typeof (T).Name : typeof (T).Name.ToLower ();
				}

				tableNameMap [typeof (T)] = name;
			}
			return name;
		}

		private bool TableExists (string name)
		{
			return connection.Query ("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name AND TABLE_SCHEMA = DATABASE()",
				new { name }, transaction: transaction).Count () == 1;
		}

		/// <summary>
		/// Execute the specified sql and param.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		public int Execute (string sql, dynamic param = null)
		{
			return SqlMapper.Execute (connection, sql, param as object, transaction, commandTimeout: this.commandTimeout);
		}

		/// <summary>
		/// Query the specified sql, param and buffered.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="buffered">If set to <c>true</c> buffered.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public IEnumerable<T> Query<T> (string sql, dynamic param = null, bool buffered = true)
		{

			return SqlMapper.Query<T> (connection, sql, param as object, transaction, buffered, commandTimeout);
		}

		/// <summary>
		/// Query the specified sql, map, param, transaction, buffered, splitOn and commandTimeout.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="map">Map.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="transaction">Transaction.</param>
		/// <param name="buffered">If set to <c>true</c> buffered.</param>
		/// <param name="splitOn">Split on.</param>
		/// <param name="commandTimeout">Command timeout.</param>
		/// <typeparam name="TFirst">The 1st type parameter.</typeparam>
		/// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
		/// <typeparam name="TReturn">The 3rd type parameter.</typeparam>
		public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn> (string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return SqlMapper.Query (connection, sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Query the specified sql, map, param, transaction, buffered, splitOn and commandTimeout.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="map">Map.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="transaction">Transaction.</param>
		/// <param name="buffered">If set to <c>true</c> buffered.</param>
		/// <param name="splitOn">Split on.</param>
		/// <param name="commandTimeout">Command timeout.</param>
		/// <typeparam name="TFirst">The 1st type parameter.</typeparam>
		/// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
		/// <typeparam name="TThird">The 3rd type parameter.</typeparam>
		/// <typeparam name="TReturn">The 4th type parameter.</typeparam>
		public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn> (string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return SqlMapper.Query (connection, sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Query the specified sql, map, param, transaction, buffered, splitOn and commandTimeout.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="map">Map.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="transaction">Transaction.</param>
		/// <param name="buffered">If set to <c>true</c> buffered.</param>
		/// <param name="splitOn">Split on.</param>
		/// <param name="commandTimeout">Command timeout.</param>
		/// <typeparam name="TFirst">The 1st type parameter.</typeparam>
		/// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
		/// <typeparam name="TThird">The 3rd type parameter.</typeparam>
		/// <typeparam name="TFourth">The 4th type parameter.</typeparam>
		/// <typeparam name="TReturn">The 5th type parameter.</typeparam>
		public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return SqlMapper.Query (connection, sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Query the specified sql, map, param, transaction, buffered, splitOn and commandTimeout.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="map">Map.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="transaction">Transaction.</param>
		/// <param name="buffered">If set to <c>true</c> buffered.</param>
		/// <param name="splitOn">Split on.</param>
		/// <param name="commandTimeout">Command timeout.</param>
		/// <typeparam name="TFirst">The 1st type parameter.</typeparam>
		/// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
		/// <typeparam name="TThird">The 3rd type parameter.</typeparam>
		/// <typeparam name="TFourth">The 4th type parameter.</typeparam>
		/// <typeparam name="TFifth">The 5th type parameter.</typeparam>
		/// <typeparam name="TReturn">The 6th type parameter.</typeparam>
		public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return SqlMapper.Query (connection, sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Query the specified sql, param and buffered.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="buffered">If set to <c>true</c> buffered.</param>
		public IEnumerable<dynamic> Query (string sql, dynamic param = null, bool buffered = true)
		{
			return SqlMapper.Query (connection, sql, param as object, transaction, buffered);
		}

		/// <summary>
		/// Queries the multiple.
		/// </summary>
		/// <returns>The multiple.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="transaction">Transaction.</param>
		/// <param name="commandTimeout">Command timeout.</param>
		/// <param name="commandType">Command type.</param>
		public Dapper.SqlMapper.GridReader QueryMultiple (string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			return SqlMapper.QueryMultiple (connection, sql, param, transaction, commandTimeout, commandType);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="T:Dapper.Database`1"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:Dapper.Database`1"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="T:Dapper.Database`1"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="T:Dapper.Database`1"/> so the garbage
		/// collector can reclaim the memory that the <see cref="T:Dapper.Database`1"/> was occupying.</remarks>
		public void Dispose ()
		{
			if (connection == null) return;
			if (connection.State != ConnectionState.Closed) {
				if (transaction != null) {
					transaction.Rollback ();
				}

				connection.Close ();
				connection = null;
			}
		}
	}
}