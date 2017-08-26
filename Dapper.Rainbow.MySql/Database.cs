using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Dapper
{
    /// <summary>
    /// Represents a database, assumes all the tables have an Id column named Id
    /// </summary>
    /// <typeparam name="TDatabase">The type of database this represents.</typeparam>
    public abstract partial class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
    {
        private DbConnection _connection;
        private int _commandTimeout;
        private DbTransaction _transaction;
        private bool _lowerCase = true;

        /// <summary>
        /// Initializes the database.
        /// </summary>
        /// <param name="connection">The connection to use.</param>
        /// <param name="commandTimeout">The timeout to use (in seconds).</param>
        /// <param name="lowerCase">opted for lowercase table name</param>
        /// <returns></returns>
        public static TDatabase Init(DbConnection connection, int commandTimeout, bool lowerCase = true)
        {
            TDatabase db = new TDatabase();
            db.InitDatabase(connection, commandTimeout, lowerCase);
            return db;
        }

        internal static Action<TDatabase> tableConstructor;

        internal void InitDatabase(DbConnection connection, int commandTimeout, bool lowerCase)
        {
            _connection = connection;
            _commandTimeout = commandTimeout;
            _lowerCase = lowerCase;
            tableConstructor = tableConstructor ?? CreateTableConstructorForTable();

            tableConstructor(this as TDatabase);
        }

        internal virtual Action<TDatabase> CreateTableConstructorForTable()
        {
            return CreateTableConstructor(typeof(Table<>), typeof(Table<,>));
        }

        /// <summary>
        /// Begins a transaction in this database.
        /// </summary>
        /// <param name="isolation">The isolation level to use.</param>
        public void BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted)
        {
            _transaction = _connection.BeginTransaction(isolation);
        }

        /// <summary>
        /// Commits the current transaction in this database.
        /// </summary>
        public void CommitTransaction()
        {
            _transaction.Commit();
            _transaction = null;
        }

        /// <summary>
        /// Rolls back the current transaction in this database.
        /// </summary>
        public void RollbackTransaction()
        {
            _transaction.Rollback();
            _transaction = null;
        }

        /// <summary>
        /// Gets a table creation function for the specified type.
        /// </summary>
        /// <param name="tableType">The object type to create a table for.</param>
        /// <returns>The function to create the <paramref name="tableType"/> table.</returns>
        protected Action<TDatabase> CreateTableConstructor(Type tableType)
        {
            return CreateTableConstructor(new[] { tableType });
        }

        /// <summary>
        /// Gets a table creation function for the specified types.
        /// </summary>
        /// <param name="tableTypes">The object types to create a table for.</param>
        /// <returns>The function to create the <paramref name="tableTypes"/> tables.</returns>
        protected Action<TDatabase> CreateTableConstructor(params Type[] tableTypes)
        {
            var dm = new DynamicMethod("ConstructInstances", null, new[] { typeof(TDatabase) }, true);
            var il = dm.GetILGenerator();

            var setters = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType() && tableTypes.Contains(p.PropertyType.GetGenericTypeDefinition()))
                .Select(p => Tuple.Create(
                        p.GetSetMethod(true),
                        p.PropertyType.GetConstructor(new[] { typeof(TDatabase), typeof(string) }),
                        p.Name,
                        p.DeclaringType
                 ));

            foreach (var setter in setters) {
                il.Emit(OpCodes.Ldarg_0);
                // [db]

                il.Emit(OpCodes.Ldstr, setter.Item3);
                // [db, likelyname]

                il.Emit(OpCodes.Newobj, setter.Item2);
                // [table]

                var table = il.DeclareLocal(setter.Item2.DeclaringType);
                il.Emit(OpCodes.Stloc, table);
                // []

                il.Emit(OpCodes.Ldarg_0);
                // [db]

                il.Emit(OpCodes.Castclass, setter.Item4);
                // [db cast to container]

                il.Emit(OpCodes.Ldloc, table);
                // [db cast to container, table]

                il.Emit(OpCodes.Callvirt, setter.Item1);
                // []
            }

            il.Emit(OpCodes.Ret);
            return (Action<TDatabase>)dm.CreateDelegate(typeof(Action<TDatabase>));
        }

        private static readonly ConcurrentDictionary<Type, string> tableNameMap = new ConcurrentDictionary<Type, string>();
        private string DetermineTableName<T>(string likelyTableName)
        {
            if (!tableNameMap.TryGetValue(typeof(T), out string name)) {
                name = !_lowerCase ? likelyTableName : likelyTableName.ToLower();
                if (!TableExists(name)) {
                    name = !_lowerCase ? typeof(T).Name : typeof(T).Name.ToLower();
                }

                tableNameMap[typeof(T)] = name;
            }
            return name;
        }

        private bool TableExists(string name)
        {
            return _connection.Query("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name AND TABLE_SCHEMA = DATABASE()",
                new { name }, _transaction).Count() == 1;
        }

        /// <summary>
        /// Executes SQL against the current database.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>The number of rows affected.</returns>
        public int Execute(string sql, dynamic param = null) =>
            _connection.Execute(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Queries the current database.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <param name="buffered">Whether to buffer the results.</param>
        /// <returns>An enumerable of <typeparamref name="T"/> for the rows fetched.</returns>
        public IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true) =>
            _connection.Query<T>(sql, param as object, _transaction, buffered, _commandTimeout);

        /// <summary>
        /// Queries the current database for a single record.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>An enumerable of <typeparamref name="T"/> for the rows fetched.</returns>
        public T QueryFirstOrDefault<T>(string sql, dynamic param = null) =>
            _connection.QueryFirstOrDefault<T>(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Perform a multi-mapping query with 2 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.Query(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a multi-mapping query with 3 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.Query(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a multi-mapping query with 4 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.Query(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a multi-mapping query with 5 input types. 
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TThird">The third type in the recordset.</typeparam>
        /// <typeparam name="TFourth">The fourth type in the recordset.</typeparam>
        /// <typeparam name="TFifth">The fifth type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="buffered">Whether to buffer the results in memory.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.Query(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Return a sequence of dynamic objects with properties matching the columns
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <param name="buffered">Whether the results should be buffered in memory.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public IEnumerable<dynamic> Query(string sql, dynamic param = null, bool buffered = true) =>
            _connection.Query(sql, param as object, _transaction, buffered);

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            SqlMapper.QueryMultiple(_connection, sql, param, transaction, commandTimeout, commandType);

        /// <summary>
        /// Disposes the current database, rolling back current transactions.
        /// </summary>
        public void Dispose()
        {
            if (_connection?.State == ConnectionState.Closed) return;

            _transaction?.Rollback();
            _connection?.Close();
            _connection = null;
        }
    }
}
