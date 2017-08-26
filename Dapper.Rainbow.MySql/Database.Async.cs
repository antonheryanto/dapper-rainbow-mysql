using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Dapper
{
    public abstract partial class Database<TDatabase> where TDatabase : Database<TDatabase>, new()
    {
        /// <summary>
        /// Asynchronously executes SQL against the current database.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteAsync(string sql, dynamic param = null) =>
            _connection.ExecuteAsync(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Asynchronously queries the current database.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>An enumerable of <typeparamref name="T"/> for the rows fetched.</returns>
        public Task<IEnumerable<T>> QueryAsync<T>(string sql, dynamic param = null) =>
            _connection.QueryAsync<T>(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Asynchronously queries the current database for a single record.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <returns>An enumerable of <typeparamref name="T"/> for the rows fetched.</returns>
        public Task<T> QueryFirstOrDefaultAsync<T>(string sql, dynamic param = null) =>
            _connection.QueryFirstOrDefaultAsync<T>(sql, param as object, _transaction, _commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types. 
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
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 3 input types. 
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
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 4 input types. 
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
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 5 input types. 
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
        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null) =>
            _connection.QueryAsync(sql, map, param as object, transaction, buffered, splitOn, commandTimeout);

        /// <summary>
        /// Execute a query asynchronously using .NET 4.5 Task.
        /// </summary>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use.</param>
        /// <remarks>Note: each row can be accessed via "dynamic", or by casting to an IDictionary&lt;string,object&gt;</remarks>
        public Task<IEnumerable<dynamic>> QueryAsync(string sql, dynamic param = null) =>
            _connection.QueryAsync(sql, param as object, _transaction);

        /// <summary>
        /// Execute a command that returns multiple result sets, and access each in turn.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
        /// <param name="commandType">Is it a stored proc or a batch?</param>
        public Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
            SqlMapper.QueryMultipleAsync(_connection, sql, param, transaction, commandTimeout, commandType);
    }
}
