using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dapper
{
    public abstract partial class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
    {
        /// <summary>
        /// Table implementation using id long
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Table<T> : Table<T, long>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="T:Dapper.Database`1.Table`1"/> class.
            /// </summary>
            /// <param name="database">Database.</param>
            /// <param name="likelyTableName">Likely table name.</param>
            public Table(Database<TDatabase> database, string likelyTableName)
                : base(database, likelyTableName)
            {
            }
        }

        /// <summary>
        /// A container for table with table type and id type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TId"></typeparam>
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
            public Table(Database<TDatabase> database, string likelyTableName)
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
                    tableName = tableName ?? database.DetermineTableName<T>(likelyTableName);
                    return tableName;
                }
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public virtual long Insert(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);
                paramNames.Remove("Id");

                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                var sql = $"INSERT INTO `{TableName}` (`{cols}`) VALUES ({cols_params}); SELECT LAST_INSERT_ID()";
                var id = database.Query(sql, o).Single() as IDictionary<string, object>;
                return Convert.ToInt64(id.Values.Single());
            }

            /// <summary>
            /// Update a record in the DB
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public int Update(TId id, dynamic data) => Update(new { id }, data);

            /// <summary>
            /// Update a record in the DB
            /// </summary>
            /// <param name="where"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public int Update(dynamic where, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                List<string> keys = GetParamNames((object)where);

                var cols_update = string.Join(",", paramNames.Select(p => $"`{p}`= @{p}"));
                var cols_where = string.Join(" AND ", keys.Select(p => $"`{p}` = @{p}"));
                var sql = $"UPDATE `{TableName}` SET {cols_update} WHERE {cols_where}";

                var parameters = new DynamicParameters(data);
                parameters.AddDynamicParams(where);
                return database.Execute(sql, parameters);
            }

            /// <summary>
            /// Insert a row into the db or update when key is duplicated 
            /// only for autoincrement key
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public long InsertOrUpdate(TId id, dynamic data) => InsertOrUpdate(new { id }, data);

            /// <summary>
            /// Insert a row into the db or update when key is duplicated 
            /// for autoincrement key
            /// </summary>
            /// <param name="key"></param>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public long InsertOrUpdate(dynamic key, dynamic data)
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
                var id = database.Query(sql, parameters).Single() as IDictionary<string, object>;
                return Convert.ToInt64(id.Values.Single());
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public int InsertOrUpdate(dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                string cols_update = string.Join(",", paramNames.Select(p => $"`{p}` = @{p}"));
                var sql = $"INSERT INTO `{TableName}` (`{cols}`) VALUES ({cols_params}) ON DUPLICATE KEY UPDATE {cols_update}";
                return database.Execute(sql, data);
            }

            /// <summary>
            /// Delete a record for the DB
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool Delete(TId id)
            {
                return database.Execute($"DELETE FROM `{TableName}` WHERE Id = @id", new { id }) > 0;
            }

            /// <summary>
            /// Delete a record for the DB
            /// </summary>
            /// <param name="where"></param>
            /// <returns></returns>
            public bool Delete(dynamic where = null)
            {
                if (where == null) return database.Execute($"TRUNCATE `{TableName}`") > 0;
                var owhere = where as object;
                var paramNames = GetParamNames(owhere);
                var w = string.Join(" AND ", paramNames.Select(p => $"`{p}` = @{p}"));
                return database.Execute($"DELETE FROM `{TableName}` WHERE {w}", owhere) > 0;
            }

            /// <summary>
            /// Grab a record with a particular Id from the DB 
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public T Get(TId id)
            {
                return database.QueryFirstOrDefault<T>($"SELECT * FROM `{TableName}` WHERE id = @id", new { id });
            }

            /// <summary>
            /// Grab a record with where clause from the DB 
            /// </summary>
            /// <param name="where"></param>
            /// <returns></returns>
            public T Get(dynamic where) => First(where);

            /// <summary>
            /// Grab a first record
            /// </summary>
            /// <param name="where"></param>
            /// <returns></returns>
            public T First(dynamic where = null)
            {
                if (where == null) return database.QueryFirstOrDefault<T>($"SELECT * FROM `{TableName}` LIMIT 1");
                var owhere = where as object;
                var paramNames = GetParamNames(owhere);
                var w = string.Join(" AND ", paramNames.Select(p => $"`{p}` = @{p}"));
                return database.QueryFirstOrDefault<T>($"SELECT * FROM `{TableName}` WHERE {w} LIMIT 1", owhere);
            }

            /// <summary>
            /// Return All record
            /// </summary>
            /// <param name="where"></param>
            /// <returns></returns>
            public IEnumerable<T> All(dynamic where = null)
            {
                var sql = $"SELECT * FROM `{TableName}`";
                if (where == null) return database.Query<T>(sql);
                var paramNames = GetParamNames((object)where);
                var w = string.Join(" AND ", paramNames.Select(p => $"`{p}` = @{p}"));
                return database.Query<T>(sql + " WHERE " + w, where);
            }

            private static readonly ConcurrentDictionary<Type, List<string>> paramNameCache = new ConcurrentDictionary<Type, List<string>>();

            internal static List<string> GetParamNames(object o)
            {
                if (o is DynamicParameters parameters) {
                    return parameters.ParameterNames.ToList();
                }

                if (!paramNameCache.TryGetValue(o.GetType(), out List<string> paramNames)) {
                    paramNames = new List<string>();
                    foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetGetMethod(false) != null)) {
                        var attribs = prop.GetCustomAttributes(typeof(IgnorePropertyAttribute), true);
                        var attr = attribs.FirstOrDefault() as IgnorePropertyAttribute;
                        if (attr == null || (!attr.Value)) {
                            paramNames.Add(prop.Name);
                        }
                    }
                    paramNameCache[o.GetType()] = paramNames;
                }
                return paramNames;
            }
        }
    }
}
