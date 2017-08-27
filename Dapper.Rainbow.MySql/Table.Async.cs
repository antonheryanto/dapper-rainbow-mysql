using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper
{
    public abstract partial class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
    {
        public partial class Table<T, TId>
        {
            /// <summary>
            /// Insert a row into the db asynchronously
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public virtual async Task<long> InsertAsync(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                paramNames.Remove("Id");

                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                var sql = $"INSERT INTO `{TableName}` (`{cols}`) VALUES ({cols_params}); SELECT LAST_INSERT_ID()";
                var id = (await database.QueryAsync(sql, o).ConfigureAwait(false)).Single() as IDictionary<string, object>;

                return Convert.ToInt64(id.Values.Single());
            }

            /// <summary>
            /// Update a record in the DB asynchronously
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public Task<int> UpdateAsync(TId id, dynamic data) => UpdateAsync(new { id }, data);


            /// <summary>
            /// Update a record in the DB asynchronously
            /// </summary>
            /// <param name="where"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public Task<int> UpdateAsync(dynamic where, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                List<string> keys = GetParamNames((object)where);

                var cols_update = string.Join(",", paramNames.Select(p => $"`{p}`= @{p}"));
                var cols_where = string.Join(" AND ", keys.Select(p => $"`{p}` = @{p}"));
                var sql = $"UPDATE `{TableName}` SET {cols_update} WHERE {cols_where}";

                var parameters = new DynamicParameters(data);
                parameters.AddDynamicParams(where);
                return database.ExecuteAsync(sql, parameters);
            }


            /// <summary>
            /// Insert a row into the db or update when key is duplicated asynchronously
            /// only for autoincrement key
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public async Task<long> InsertOrUpdateAsync(TId id, dynamic data) => await InsertOrUpdateAsync(new { id }, data);

            /// <summary>
            /// Insert a row into the db or update when key is duplicated asynchronously
            /// for autoincrement key
            /// </summary>
            /// <param name="key"></param>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public async Task<long> InsertOrUpdateAsync(dynamic key, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                string k = GetParamNames((object)key).Single();

                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                string cols_update = string.Join(",", paramNames.Select(p => $"`{p}` = @{p}"));
                var sql = $@"
INSERT INTO `{TableName}` (`{cols}`,`{k}`) VALUES ({cols_params}, @{k})
ON DUPLICATE KEY UPDATE `{k}` = LAST_INSERT_ID(`{k}`), {cols_update}; SELECT LAST_INSERT_ID()";
                var parameters = new DynamicParameters(data);
                parameters.AddDynamicParams(key);
                var id = (await database.QueryAsync(sql, parameters).ConfigureAwait(false)).Single() as IDictionary<string, object>;

                return Convert.ToInt64(id.Values.Single());
            }

            /// <summary>
            /// Delete a record for the DB asynchronously
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public async Task<bool> DeleteAsync(TId id)
            {
                return (await database.ExecuteAsync($"DELETE FROM `{TableName}` WHERE Id = @id", new { id }).ConfigureAwait(false)) > 0;
            }

            /// <summary>
            /// Grab a record with a particular Id from the DB asynchronously
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public Task<T> GetAsync(TId id)
            {
                return database.QueryFirstOrDefaultAsync<T>($"SELECT * FROM `{TableName}` WHERE id = @id", new { id });
            }

            /// <summary>
            /// Grab a record with where clause from the DB 
            /// </summary>
            /// <param name="where"></param>
            /// <returns></returns>
            public async Task<T> GetAsync(dynamic where) => (await FirstAsync(where));

            /// <summary>
            /// Firsts the async.
            /// </summary>
            /// <returns>The async.</returns>
            /// <param name="where">Where.</param>
            public virtual async Task<T> FirstAsync(dynamic where = null)
            {
                if (where == null) return database.Query<T>($"SELECT * FROM `{TableName}` LIMIT 1").FirstOrDefault();
                var owhere = where as object;
                var paramNames = GetParamNames(owhere);
                var w = string.Join(" AND ", paramNames.Select(p => $"`{p}` = @{p}"));
                return await database.QueryFirstOrDefaultAsync<T>($"SELECT * FROM `{TableName}` WHERE {w} LIMIT 1", owhere);
            }

            /// <summary>
            /// Alls the async.
            /// </summary>
            /// <returns>The async.</returns>
            /// <param name="where">Where.</param>
            public async Task<IEnumerable<T>> AllAsync(dynamic where = null)
            {
                var sql = $"SELECT * FROM `{TableName}`";
                if (where == null) return await database.QueryAsync<T>(sql);

                var paramNames = GetParamNames((object)where);
                var w = string.Join(" AND ", paramNames.Select(p => $"`{p}` = @{p}"));
                return await database.QueryAsync<T>($"{sql} WHERE {w}", where);
            }
        }
    }
}
