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
			var total = SqlMapper.Query<long> (connection, sqlCount, param as object).FirstOrDefault ();

			sqlPage = sql + "\n LIMIT @limit OFFSET @offset";
			pageParam = new DynamicParameters (param);
			pageParam.Add ("@offset", (page - 1) * itemsPerPage);
			pageParam.Add ("@limit", itemsPerPage);
			var totalPage = total / itemsPerPage;
			if (total % itemsPerPage != 0) totalPage++;
			long pageDisplayed = page + totalPageDisplayed;
			if (pageDisplayed > totalPage) pageDisplayed = totalPage;
			var p = new Page<T> {
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

		public Page<T> Page<T> (string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
		{
			string sqlPage;
			DynamicParameters pageParam;
			var p = Page<T> (sql, page, param, itemsPerPage, out sqlPage, out pageParam);
			p.Items = SqlMapper.Query<T> (connection, sqlPage, pageParam).ToList ();
			return p;
		}

		public Page<dynamic> Page (string sql, int page = 1, dynamic param = null, int itemsPerPage = 10)
		{
			return Page<dynamic> (sql, page, param as object, itemsPerPage);
		}

		public Page<TReturn> Page<TFirst, TSecond, TReturn> (string sql, Func<TFirst, TSecond, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
			string sqlPage;
			DynamicParameters pageParam;
			var p = Page<TReturn> (sql, page, param, itemsPerPage, out sqlPage, out pageParam);
			p.Items = SqlMapper.Query (connection, sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
		}

		public Page<TReturn> Page<TFirst, TSecond, TThird, TReturn> (string sql, Func<TFirst, TSecond, TThird, TReturn> map, int page, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
			string sqlPage;
			DynamicParameters pageParam;
			var p = Page<TReturn> (sql, page, param, itemsPerPage, out sqlPage, out pageParam);
			p.Items = SqlMapper.Query (connection, sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
		}

		public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
			string sqlPage;
			DynamicParameters pageParam;
			var p = Page<TReturn> (sql, page, param, itemsPerPage, out sqlPage, out pageParam);
			p.Items = SqlMapper.Query (connection, sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
		}

		public Page<TReturn> Page<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> (string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, int page = 1, dynamic param = null, int itemsPerPage = 10, string splitOn = "Id")
		{
			string sqlPage;
			DynamicParameters pageParam;
			var p = Page<TReturn> (sql, page, param, itemsPerPage, out sqlPage, out pageParam);
			p.Items = SqlMapper.Query (connection, sqlPage, map, pageParam, splitOn: splitOn).ToList ();
			return p;
		}
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
