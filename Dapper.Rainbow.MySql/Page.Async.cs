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
			/// Return record in page object
			/// </summary>
			/// <param name="page"></param>
			/// <param name="itemsPerPage"></param>
			/// <param name="where"></param>
			/// <returns></returns>
            public async Task<Page<T>> PageAsync(int page = 1, int itemsPerPage = 10, dynamic where = null)
            {
                var sql = "SELECT * FROM `" + TableName + "` ";
                if (where == null) return await database.PageAsync<T>(sql, page, itemsPerPage: itemsPerPage);

                var paramNames = GetParamNames((object)where);
                var w = string.Join(" AND ", paramNames.Select(p => "`" + p + "` = @" + p));
                return await database.PageAsync<T>(sql + " WHERE " + w, page, where, itemsPerPage: itemsPerPage);
            }
        }

        internal async Task<PageParser<T>> PageParseAsync<T>(string sql, int page, dynamic param, int itemsPerPage)
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
            var total = await _connection.QueryFirstOrDefaultAsync<long>(sqlCount, param as object).ConfigureAwait(false);

            var p = new PageParser<T> {
                SqlPage = sql + "\n LIMIT @limit OFFSET @offset",
                PageParam = new DynamicParameters(param)
            };
            p.PageParam.Add("@offset", (page - 1) * itemsPerPage);
            p.PageParam.Add("@limit", itemsPerPage);
            var totalPage = total / itemsPerPage;
            if (total % itemsPerPage != 0) totalPage++;
            long pageDisplayed = page + totalPageDisplayed;
            if (pageDisplayed > totalPage) pageDisplayed = totalPage;
            p.Result = new Page<T> {
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

        /// <summary>
        /// Page the specified sql, page, param and itemsPerPage.
        /// </summary>
        /// <param name="sql">Sql.</param>
        /// <param name="page">Page.</param>
        /// <param name="param">Parameter.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<Page<T>> PageAsync<T>(string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
        {
            var p = await PageParseAsync<T>(sql, page, param as object, itemsPerPage).ConfigureAwait(false);
            var r = await _connection.QueryAsync<T>(p.SqlPage, p.PageParam).ConfigureAwait(false);
            p.Result.Items = r.ToList();
            return p.Result;
        }

        /// <summary>
        /// Page the specified sql, page, param and itemsPerPage.
        /// </summary>
        /// <param name="sql">Sql.</param>
        /// <param name="page">Page.</param>
        /// <param name="param">Parameter.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        public Task<Page<dynamic>> PageAsync(string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
        {
            return PageAsync<dynamic>(sql, page, param as object, itemsPerPage);
        }

        /// <summary>
        /// Page the specified sql, map, page, param, itemsPerPage and splitOn.
        /// </summary>
        /// <param name="sql">Sql.</param>
        /// <param name="map">Map.</param>
        /// <param name="page">Page.</param>
        /// <param name="param">Parameter.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="splitOn">Split on.</param>
        /// <typeparam name="TFirst">The 1st type parameter.</typeparam>
        /// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
        /// <typeparam name="TReturn">The 3rd type parameter.</typeparam>
        public async Task<Page<TReturn>> PageAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            var p = await PageParseAsync<TReturn>(sql, page, param as object, itemsPerPage).ConfigureAwait(false);
            var r = await _connection.QueryAsync(p.SqlPage, map, p.PageParam, splitOn: splitOn).ConfigureAwait(false);
            p.Result.Items = r.ToList();
            return p.Result;
        }

        /// <summary>
        /// Page the specified sql, map, page, param, itemsPerPage and splitOn.
        /// </summary>
        /// <param name="sql">Sql.</param>
        /// <param name="map">Map.</param>
        /// <param name="page">Page.</param>
        /// <param name="param">Parameter.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="splitOn">Split on.</param>
        /// <typeparam name="TFirst">The 1st type parameter.</typeparam>
        /// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
        /// <typeparam name="TThird">The 3rd type parameter.</typeparam>
        /// <typeparam name="TReturn">The 4th type parameter.</typeparam>
        public async Task<Page<TReturn>> PageAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, int page, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            var p = await PageParseAsync<TReturn>(sql, page, param as object, itemsPerPage).ConfigureAwait(false);
            var r = await _connection.QueryAsync(p.SqlPage, map, p.PageParam, splitOn: splitOn).ConfigureAwait(false);
            p.Result.Items = r.ToList();
            return p.Result;
        }

        /// <summary>
        /// Page the specified sql, map, page, param, itemsPerPage and splitOn.
        /// </summary>
        /// <param name="sql">Sql.</param>
        /// <param name="map">Map.</param>
        /// <param name="page">Page.</param>
        /// <param name="param">Parameter.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="splitOn">Split on.</param>
        /// <typeparam name="TFirst">The 1st type parameter.</typeparam>
        /// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
        /// <typeparam name="TThird">The 3rd type parameter.</typeparam>
        /// <typeparam name="TFourth">The 4th type parameter.</typeparam>
        /// <typeparam name="TReturn">The 5th type parameter.</typeparam>
        public async Task<Page<TReturn>> PageAsync<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            var p = await PageParseAsync<TReturn>(sql, page, param as object, itemsPerPage).ConfigureAwait(false);
            var r = await _connection.QueryAsync(p.SqlPage, map, p.PageParam, splitOn: splitOn).ConfigureAwait(false);
            p.Result.Items = r.ToList();
            return p.Result;
        }

        /// <summary>
        /// Page the specified sql, map, page, param, itemsPerPage and splitOn.
        /// </summary>
        /// <param name="sql">Sql.</param>
        /// <param name="map">Map.</param>
        /// <param name="page">Page.</param>
        /// <param name="param">Parameter.</param>
        /// <param name="itemsPerPage">Items per page.</param>
        /// <param name="splitOn">Split on.</param>
        /// <typeparam name="TFirst">The 1st type parameter.</typeparam>
        /// <typeparam name="TSecond">The 2nd type parameter.</typeparam>
        /// <typeparam name="TThird">The 3rd type parameter.</typeparam>
        /// <typeparam name="TFourth">The 4th type parameter.</typeparam>
        /// <typeparam name="TFifth">The 5th type parameter.</typeparam>
        /// <typeparam name="TReturn">The 6th type parameter.</typeparam>
        public async Task<Page<TReturn>> PageAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
        {
            var p = await PageParseAsync<TReturn>(sql, page, param as object, itemsPerPage).ConfigureAwait(false);
            var r = await _connection.QueryAsync(p.SqlPage, map, p.PageParam, splitOn: splitOn).ConfigureAwait(false);
            p.Result.Items = r.ToList();
            return p.Result;
        }
    }
}
