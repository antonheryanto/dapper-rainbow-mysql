/*
 License: http://www.apache.org/licenses/LICENSE-2.0 
 Home page: http://code.google.com/p/dapper-dot-net/
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Dapper;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Dapper
{
    /// <summary>
    /// A container for a database, assumes all the tables have an Id column named Id
    /// </summary>
    /// <typeparam name="TDatabase"></typeparam>
    public abstract class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
    {
        public class Table<T>
        {
            Database<TDatabase> database;
            string tableName;
            string likelyTableName;

            public Table(Database<TDatabase> database, string likelyTableName)
            {
                this.database = database;
                this.likelyTableName = likelyTableName;
            }

            public string TableName
            {
                get
                {
                    tableName = tableName ?? database.DetermineTableName<T>(likelyTableName);
                    return tableName;
                }
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public long Insert(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamNames(o);

                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                var sql = "INSERT INTO `" + TableName + "` (`" + cols + "`) VALUES (" + cols_params + "); SELECT LAST_INSERT_ID()";

                return database.Query<long>(sql, o).Single();
            }

            /// <summary>
            /// Update a record in the DB
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public int Update(long id, dynamic data)
            {
                return Update(new { id }, data);
            }

            public int Update(dynamic key, dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                string k = GetParamNames((object)key).Single();

                var b = new StringBuilder();
                b.Append("UPDATE `").Append(TableName).Append("` SET ");
                b.AppendLine(string.Join(",", paramNames.Select(p =>"`" + p + "`= @" + p)));
                b.Append("WHERE `").Append(k).Append("`= @").Append(k);

                var parameters = new DynamicParameters(data);
                parameters.AddDynamicParams(key);
                return database.Execute(b.ToString(), parameters);
            }

            /// <summary>
            /// Insert a row into the db or update when key is duplicated 
            /// only for autoincrement key
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public long InsertOrUpdate(long id, dynamic data)
            {
                return InsertOrUpdate(new { id }, data);
            }

            public long InsertOrUpdate(dynamic key, dynamic data)
            {   
                List<string> paramNames = GetParamNames((object)data);
                string k = GetParamNames((object)key).Single();
                
                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                string cols_update = string.Join(",", paramNames.Select(p => "`" + p + "` = @" + p));                
                string key_update = "`" + k + "` = LAST_INSERT_ID(`" + k + "`)";
                var b = new StringBuilder();
                b.Append("INSERT INTO `").Append(TableName).Append("` (`").Append(cols).Append("`,`").Append(k).Append("`) VALUES (")
                 .Append(cols_params).Append(", @").Append(k)
                 .Append(") ON DUPLICATE KEY UPDATE ").Append("`").Append(k).Append("` = LAST_INSERT_ID(`").Append(k).Append("`)")
                 .Append(", ").Append(cols_update).Append(";SELECT LAST_INSERT_ID()");                
                var parameters = new DynamicParameters(data);
                parameters.AddDynamicParams(key);
                return database.Query<long>(b.ToString(), parameters).Single();
            }

            public int InsertOrUpdate(dynamic data)
            {
                List<string> paramNames = GetParamNames((object)data);
                string cols = string.Join("`,`", paramNames);
                string cols_params = string.Join(",", paramNames.Select(p => "@" + p));
                string cols_update = string.Join(",", paramNames.Select(p => "`" + p + "` = @" + p));                
                var b = new StringBuilder();
                b.Append("INSERT INTO `").Append(TableName).Append("` (`").Append(cols).Append("`) VALUES (")
                 .Append(cols_params).Append(") ON DUPLICATE KEY UPDATE ").Append(cols_update);
                return database.Execute(b.ToString(), data);
            }

            /// <summary>
            /// Delete a record for the DB
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool Delete(long id)
            {
                return database.Execute("DELETE FROM `" + TableName + "` WHERE Id = @id", new { id }) > 0;
            }

            /// <summary>
            /// Grab a record with a particular Id from the DB 
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public T Get(long id)
            {
                return database.Query<T>("SELECT * FROM `" + TableName + "` WHERE id = @id", new { id }).FirstOrDefault();
            }

            public T Get(dynamic where)
            {
                return (All(where) as IEnumerable <T>).FirstOrDefault();
            }

            public T First()
            {
                return database.Query<T>("SELECT * FROM `" + TableName + "` LIMIT 1").FirstOrDefault();
            }

            public IEnumerable<T> All(dynamic where = null)
            {
                var sql = "SELECT * FROM " + TableName ;
                if (where == null) return database.Query<T>(sql);
                var paramNames = GetParamNames((object)where);
                var w = string.Join(" AND ", paramNames.Select(p => "`" + p + "` = @" + p));
                var parameters = new DynamicParameters(where);
                return database.Query<T>(sql + " WHERE " + w , parameters);
            }

            public Page<T> Page(int page = 1, int itemsPerPage = 10, dynamic where = null)
            {
                var sql = "SELECT * FROM `" + TableName + "` ";
                if (where == null) return database.Page<T>(sql, page, itemsPerPage: itemsPerPage);
                var paramNames = GetParamNames((object)where);
                var w = string.Join(" AND ", paramNames.Select(p => "`" + p + "` = @" + p));
                var parameters = new DynamicParameters(where);
                return database.Page<T>(sql + " WHERE " + w, page, parameters, itemsPerPage: itemsPerPage);
            }

            static ConcurrentDictionary<Type, List<string>> paramNameCache = new ConcurrentDictionary<Type, List<string>>();
            private static List<string> GetParamNames(object o)
            {
                if (o is DynamicParameters)
                {
                    return (o as DynamicParameters).ParameterNames.ToList();
                }

                List<string> paramNames;
                if (!paramNameCache.TryGetValue(o.GetType(), out paramNames))
                {
                    paramNames = new List<string>();
                    foreach (var prop in o.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public))
                    {
                        paramNames.Add(prop.Name);
                    }
                    paramNameCache[o.GetType()] = paramNames;
                }
                return paramNames;
            }
        }

        DbConnection connection;
        int commandTimeout;
        DbTransaction transaction;


        public static TDatabase Init(DbConnection connection, int commandTimeout)
        {
            TDatabase db = new TDatabase();
            db.InitDatabase(connection, commandTimeout);
            return db;
        }

        private static Action<Database<TDatabase>> tableConstructor;

        private void InitDatabase(DbConnection connection, int commandTimeout)
        {
            this.connection = connection;
            this.commandTimeout = commandTimeout;
            if (tableConstructor == null)
            {
                tableConstructor = CreateTableConstructor();
            }

            tableConstructor(this);
        }

        public void BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted)
        {
            transaction = connection.BeginTransaction(isolation);
        }

        public void CommitTransaction()
        {
            transaction.Commit();
            transaction = null;
        }

        public void RollbackTransaction()
        {
            transaction.Rollback();
            transaction = null;
        }

        protected Action<Database<TDatabase>> CreateTableConstructor()
        {
            var dm = new DynamicMethod("ConstructInstances", null, new Type[] { typeof(Database<TDatabase>) }, true);
            var il = dm.GetILGenerator();

            var setters = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Table<>))
                .Select(p => Tuple.Create(
                        p.GetSetMethod(true),
                        p.PropertyType.GetConstructor(new Type[] { typeof(Database<TDatabase>), typeof(string) }),
                        p.Name,
                        p.DeclaringType
                 ));

            foreach (var setter in setters)
            {
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
            return (Action<Database<TDatabase>>)dm.CreateDelegate(typeof(Action<Database<TDatabase>>));
        }

        static ConcurrentDictionary<Type, string> tableNameMap = new ConcurrentDictionary<Type, string>();
        private string DetermineTableName<T>(string likelyTableName)
        {
            string name;

            if (!tableNameMap.TryGetValue(typeof(T), out name))
            {
                name = likelyTableName.ToLower();
                if (!TableExists(name))
                {
                    name = typeof(T).Name.ToLower(); ;
                }

                tableNameMap[typeof(T)] = name;
            }
            return name;
        }

        private bool TableExists(string name)
        {
            return connection.Query("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name AND TABLE_SCHEMA = DATABASE()", 
                new { name }, transaction: transaction).Count() == 1;
        }

        public int Execute(string sql, dynamic param = null)
        {
            return SqlMapper.Execute(connection, sql, param as object, transaction, commandTimeout: this.commandTimeout);
        }

        public IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true)
        {
            
            return SqlMapper.Query<T>(connection, sql, param as object, transaction, buffered, commandTimeout);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<dynamic> Query(string sql, dynamic param = null, bool buffered = true)
        {
            return SqlMapper.Query(connection, sql, param as object, transaction, buffered);
        }

        public Dapper.SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return SqlMapper.QueryMultiple(connection, sql, param, transaction, commandTimeout, commandType);
        }


        public void Dispose()
        {
            if (connection == null) return;
            if (connection.State != ConnectionState.Closed)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                connection.Close();
                connection = null;
            }
        }

        #region paging
        static readonly Regex rxColumns = new Regex(@"\A\s*SELECT\s+((?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|.)*?)(?<!,\s+)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex rxOrderBy = new Regex(@"\bORDER\s+BY\s+(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?(?:\s*,\s*(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?)*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex rxDistinct = new Regex(@"\ADISTINCT\s", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        Page<T> Page<T>(string sql, int page, dynamic param, int itemsPerPage, out string sqlPage, out DynamicParameters pageParam)
        {
            const int totalPageDisplayed = 9;
            var s = page - totalPageDisplayed;
            if (s <= 0) s = 1;
            //replace SELECT <whatever> => SELECT count(*)
            var m = rxColumns.Match(sql);
            // Save column list and replace with COUNT(*)
            var g = m.Groups[1];
            var sqlSelectRemoved = sql.Substring(g.Index);
            var count = rxDistinct.IsMatch(sqlSelectRemoved) ? m.Groups[1].ToString().Trim() : "*";
            var sqlCount = string.Format("{0} COUNT({1}) {2}", sql.Substring(0, g.Index), count, sql.Substring(g.Index + g.Length));
            // Look for an "ORDER BY <whatever>" clause
            m = rxOrderBy.Match(sqlCount);
            if (m.Success) {
                g = m.Groups[0];
                sqlCount = sqlCount.Substring(0, g.Index) + sqlCount.Substring(g.Index + g.Length);
            }
            var total = SqlMapper.Query<long>(connection, sqlCount, param as object).FirstOrDefault();

            sqlPage = sql + "\n LIMIT @limit OFFSET @offset";
            pageParam = new DynamicParameters(param);
            pageParam.Add("@offset", (page - 1) * itemsPerPage);
            pageParam.Add("@limit", itemsPerPage);
            var totalPage = total / itemsPerPage;
            if (total % itemsPerPage != 0) totalPage++;
            long pageDisplayed = page + totalPageDisplayed;
            if (pageDisplayed > totalPage) pageDisplayed = totalPage;
            var p = new Page<T>
            {
                ItemsPerPage = itemsPerPage,
                CurrentPage = page,
                PageDisplayed = pageDisplayed,
                TotalPage = totalPage,
                Start = s,
                Numbering = (page - 1) * itemsPerPage,
                HasPrevious = page - 1 >= s,
                HasNext = page + 1 <= totalPage,
                TotalItems = total
            };
            return p;
        }

        public Page<T> Page<T>(string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<T>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query<T>(connection, sqlPage, pageParam).ToList();
            return p;
        }

        public Page<dynamic> Page(string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
        {
            return Page<dynamic>(sql, page, param as object, itemsPerPage);
        }

        public Page<TReturn> Page<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }

        public Page<TReturn> Page<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, int page, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }

        public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }

        public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            string sqlPage;
            DynamicParameters pageParam;
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out sqlPage, out pageParam);
            p.Items = SqlMapper.Query(connection, sqlPage, map, pageParam, splitOn: splitOn).ToList();
            return p;
        }
        #endregion
    }

    public class Page<T>
    {
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public long PageDisplayed { get; set; }
        public long TotalPage { get; set; }
        public long TotalItems { get; set; }
        public int Start { get; set; }
        public int Numbering { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
        public List<T> Items { get; set; }
    }
}