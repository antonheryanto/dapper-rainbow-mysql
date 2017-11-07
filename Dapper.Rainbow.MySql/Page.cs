using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
			public Page<T> Page (int page = 1, int itemsPerPage = 10, dynamic where = null)
			{
				var sql = "SELECT * FROM `" + TableName + "` ";
				if (where == null) return database.Page<T> (sql, page, itemsPerPage: itemsPerPage);
				var paramNames = GetParamNames ((object)where);
				var w = string.Join (" AND ", paramNames.Select (p => "`" + p + "` = @" + p));
				return database.Page<T> (sql + " WHERE " + w, page, where, itemsPerPage: itemsPerPage);
			}
		}

		static readonly Regex rxColumns = new Regex (@"\A\s*SELECT\s+((?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|.)*?)(?<!,\s+)\bFROM\b", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
		static readonly Regex rxOrderBy = new Regex (@"\bORDER\s+BY\s+(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?(?:\s*,\s*(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?)*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
		static readonly Regex rxDistinct = new Regex (@"\ADISTINCT\s", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
		Page<T> Page<T> (string sql, int page, dynamic param, int itemsPerPage, out string sqlPage, out DynamicParameters pageParam)
		{
			const int totalPageDisplayed = 9;
			var s = page - totalPageDisplayed;
			if (s <= 0) s = 1;
			//replace SELECT <whatever> => SELECT count(*)
			var m = rxColumns.Match (sql);
			// Save column list and replace with COUNT(*)
			var g = m.Groups [1];
			var sqlSelectRemoved = sql.Substring (g.Index);
			var count = rxDistinct.IsMatch (sqlSelectRemoved) ? m.Groups [1].ToString ().Trim () : "*";
			var sqlCount = string.Format ("{0} COUNT({1}) {2}", sql.Substring (0, g.Index), count, sql.Substring (g.Index + g.Length));
			// Look for an "ORDER BY <whatever>" clause
			m = rxOrderBy.Match (sqlCount);
			if (m.Success) {
				g = m.Groups [0];
				sqlCount = sqlCount.Substring (0, g.Index) + sqlCount.Substring (g.Index + g.Length);
			}
			var total = _connection.QueryFirstOrDefault<long> (sqlCount, param as object);

			sqlPage = sql + "\n LIMIT @limit OFFSET @offset";
			pageParam = new DynamicParameters (param);
			pageParam.Add ("@offset", (page - 1) * itemsPerPage);
			pageParam.Add ("@limit", itemsPerPage);
			var totalPage = total / itemsPerPage;
			if (total % itemsPerPage != 0) totalPage++;
			long pageDisplayed = page + totalPageDisplayed;
			if (pageDisplayed > totalPage) pageDisplayed = totalPage;
			return new Page<T> {
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
		}

		/// <summary>
		/// Page the specified sql, page, param and itemsPerPage.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="page">Page.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="itemsPerPage">Items per page.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public Page<T> Page<T> (string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
		{
            var p = Page<T>(sql, page, param, itemsPerPage, out string sqlPage, out DynamicParameters pageParam);
            p.Items = _connection.Query<T> (sqlPage, pageParam).ToList ();
			return p;
		}

		/// <summary>
		/// Page the specified sql, page, param and itemsPerPage.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="page">Page.</param>
		/// <param name="param">Parameter.</param>
		/// <param name="itemsPerPage">Items per page.</param>
		public Page<dynamic> Page (string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
		{
			return Page<dynamic> (sql, page, param as object, itemsPerPage);
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
		public Page<TReturn> Page<TFirst, TSecond, TReturn> (string sql, Func<TFirst, TSecond, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out string sqlPage, out DynamicParameters pageParam);
            p.Items = _connection.Query (sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
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
		public Page<TReturn> Page<TFirst, TSecond, TThird, TReturn> (string sql, Func<TFirst, TSecond, TThird, TReturn> map, int page, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out string sqlPage, out DynamicParameters pageParam);
            p.Items = _connection.Query (sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
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
		public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out string sqlPage, out DynamicParameters pageParam);
            p.Items = _connection.Query (sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
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
		public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
            var p = Page<TReturn>(sql, page, param, itemsPerPage, out string sqlPage, out DynamicParameters pageParam);
            p.Items = _connection.Query (sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
		}
	}

    public class PageParser<T>
    {
        public Page<T> Result { get; set; }
        public string SqlPage { get; set; }
        public DynamicParameters PageParam { get; set; }
    }

    /// <summary>
    /// Paging Class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Page<T>
	{
		/// <summary>
		/// Gets or sets the items per page.
		/// </summary>
		/// <value>The items per page.</value>
		public int ItemsPerPage { get; set; }

		/// <summary>
		/// Gets or sets the current page.
		/// </summary>
		/// <value>The current page.</value>
		public int CurrentPage { get; set; }

		/// <summary>
		/// Gets or sets the page displayed.
		/// </summary>
		/// <value>The page displayed.</value>
		public long PageDisplayed { get; set; }

		/// <summary>
		/// Gets or sets the total page.
		/// </summary>
		/// <value>The total page.</value>
		public long TotalPage { get; set; }

		/// <summary>
		/// Gets or sets the total items.
		/// </summary>
		/// <value>The total items.</value>
		public long TotalItems { get; set; }

		/// <summary>
		/// Gets or sets the start.
		/// </summary>
		/// <value>The start.</value>
		public int Start { get; set; }

		/// <summary>
		/// Gets or sets the numbering.
		/// </summary>
		/// <value>The numbering.</value>
		public int Numbering { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Dapper.Page`1"/> has previous.
		/// </summary>
		/// <value><c>true</c> if has previous; otherwise, <c>false</c>.</value>
		public bool HasPrevious { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Dapper.Page`1"/> has next.
		/// </summary>
		/// <value><c>true</c> if has next; otherwise, <c>false</c>.</value>
		public bool HasNext { get; set; }

		/// <summary>
		/// Gets or sets the items.
		/// </summary>
		/// <value>The items.</value>
		public List<T> Items { get; set; }
	}
}
