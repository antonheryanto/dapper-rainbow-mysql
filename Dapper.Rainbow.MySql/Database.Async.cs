using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper
{
	public abstract partial class Database<TDatabase> where TDatabase : Database<TDatabase>, new()
	{
		public partial class Table<T, TId>
		{
			/// <summary>
			/// Insert a row into the db asynchronously
			/// </summary>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public virtual async Task<long> InsertAsync (dynamic data)
			{
				var o = (object)data;
				List<string> paramNames = GetParamNames (o);
				paramNames.Remove ("Id");

				string cols = string.Join ("`,`", paramNames);
				string cols_params = string.Join (",", paramNames.Select (p => "@" + p));
				var sql = $"INSERT INTO `{TableName}` (`{cols}`) VALUES ({cols_params}); SELECT LAST_INSERT_ID()";
				var id = (await database.QueryAsync (sql, o).ConfigureAwait (false)).Single () as IDictionary<string, object>;

				return Convert.ToInt64 (id.Values.Single ());
			}

			/// <summary>
			/// Update a record in the DB asynchronously
			/// </summary>
			/// <param name="id"></param>
			/// <param name="data"></param>
			/// <returns></returns>
			public Task<int> UpdateAsync (TId id, dynamic data) => UpdateAsync (new { id }, data);


			/// <summary>
			/// Update a record in the DB asynchronously
			/// </summary>
			/// <param name="where"></param>
			/// <param name="data"></param>
			/// <returns></returns>
			public Task<int> UpdateAsync (dynamic where, dynamic data)
			{
				List<string> paramNames = GetParamNames ((object)data);
				List<string> keys = GetParamNames ((object)where);

				var cols_update = string.Join (",", paramNames.Select (p => $"`{p}`= @{p}"));
				var cols_where = string.Join (" AND ", keys.Select (p => $"`{p}` = @{p}"));
				var sql = $"UPDATE `{TableName}` SET {cols_update} WHERE {cols_where}";

				var parameters = new DynamicParameters (data);
				parameters.AddDynamicParams (where);
				return database.ExecuteAsync (sql, parameters);
			}


			/// <summary>
			/// Insert a row into the db or update when key is duplicated asynchronously
			/// only for autoincrement key
			/// </summary>
			/// <param name="id"></param>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public async Task<long> InsertOrUpdateAsync (TId id, dynamic data) => await InsertOrUpdateAsync (new { id }, data);

			/// <summary>
			/// Insert a row into the db or update when key is duplicated asynchronously
			/// for autoincrement key
			/// </summary>
			/// <param name="key"></param>
			/// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
			/// <returns></returns>
			public async Task<long> InsertOrUpdateAsync (dynamic key, dynamic data)
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
				var id = (await database.QueryAsync (sql, parameters).ConfigureAwait (false)).Single () as IDictionary<string, object>;

				return Convert.ToInt64 (id.Values.Single ());
			}

			/// <summary>
			/// Delete a record for the DB asynchronously
			/// </summary>
			/// <param name="id"></param>
			/// <returns></returns>
			public async Task<bool> DeleteAsync (TId id)
			{
				return (await database.ExecuteAsync ($"DELETE FROM `{TableName}` WHERE Id = @id", new { id }).ConfigureAwait (false)) > 0;
			}

			/// <summary>
			/// Grab a record with a particular Id from the DB asynchronously
			/// </summary>
			/// <param name="id"></param>
			/// <returns></returns>
			public async Task<T> GetAsync (TId id)
			{
				return (await database.QueryAsync<T> ($"SELECT * FROM `{TableName}` WHERE id = @id", new { id }).ConfigureAwait (false)).FirstOrDefault ();
			}

			/// <summary>
			/// Grab a record with where clause from the DB 
			/// </summary>
			/// <param name="where"></param>
			/// <returns></returns>
			public async Task<T> GetAsync (dynamic where) => (await FirstAsync (where));

			/// <summary>
			/// Firsts the async.
			/// </summary>
			/// <returns>The async.</returns>
			/// <param name="where">Where.</param>
			public virtual async Task<T> FirstAsync (dynamic where = null)
			{
				if (where == null) return database.Query<T> ($"SELECT * FROM `{TableName}` LIMIT 1").FirstOrDefault ();
				var owhere = where as object;
				var paramNames = GetParamNames (owhere);
				var w = string.Join (" AND ", paramNames.Select (p => $"`{p}` = @{p}"));
				return (await database.QueryAsync<T> ($"SELECT * FROM `{TableName}` WHERE {w} LIMIT 1", owhere).ConfigureAwait (false)).FirstOrDefault ();
			}

			/// <summary>
			/// Alls the async.
			/// </summary>
			/// <returns>The async.</returns>
			/// <param name="where">Where.</param>
			public async Task<IEnumerable<T>> AllAsync (dynamic where = null)
			{
				var sql = "SELECT * FROM " + TableName;
				if (where == null) return (await database.QueryAsync<T> (sql).ConfigureAwait(false));
				var paramNames = GetParamNames ((object)where);
				var w = string.Join (" AND ", paramNames.Select (p => $"`{p}` = @{p}"));
				return (await database.QueryAsync<T> (sql + " WHERE " + w, where).ConfigureAwait(false));
			}
		}

		/// <summary>
		/// Executes the async.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		public Task<int> ExecuteAsync (string sql, dynamic param = null)
		{
			return connection.ExecuteAsync (sql, param as object, transaction, commandTimeout);
		}

		/// <summary>
		/// Queries the async.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public Task<IEnumerable<T>> QueryAsync<T> (string sql, dynamic param = null)
		{
			return connection.QueryAsync<T> (sql, param as object, transaction, commandTimeout);
		}

		/// <summary>
		/// Queries the async.
		/// </summary>
		/// <returns>The async.</returns>
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
		public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn> (string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return connection.QueryAsync (sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Queries the async.
		/// </summary>
		/// <returns>The async.</returns>
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
		public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn> (string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return connection.QueryAsync (sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Queries the async.
		/// </summary>
		/// <returns>The async.</returns>
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
		public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return connection.QueryAsync (sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Queries the async.
		/// </summary>
		/// <returns>The async.</returns>
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
		public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
		{
			return connection.QueryAsync (sql, map, param as object, transaction, buffered, splitOn);
		}

		/// <summary>
		/// Queries the async.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		public Task<IEnumerable<dynamic>> QueryAsync (string sql, dynamic param = null)
		{
			return connection.QueryAsync (sql, param as object, transaction);
		}

		/// <summary>
		/// Queries the multiple async.
		/// </summary>
		/// <returns>The multiple async.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="transaction">Transaction.</param>
		/// <param name="commandTimeout">Command timeout.</param>
		/// <param name="commandType">Command type.</param>
		public Task<SqlMapper.GridReader> QueryMultipleAsync (string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			return SqlMapper.QueryMultipleAsync (connection, sql, param, transaction, commandTimeout, commandType);
		}
	}
}