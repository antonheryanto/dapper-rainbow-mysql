using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dapper
{
    /// <summary>
    /// Sql builder.
    /// </summary>
    public class SqlBuilder
    {
        private readonly Dictionary<string, Clauses> _data = new Dictionary<string, Clauses>();
        private int _seq;

        private class Clause
        {
            public string Sql { get; set; }
            public object Parameters { get; set; }
            public bool IsInclusive { get; set; }
        }

        private class Clauses : List<Clause>
        {
            private readonly string _joiner, _prefix, _postfix;

            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                _joiner = joiner;
                _prefix = prefix;
                _postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this) {
                    p.AddDynamicParams(item.Parameters);
                }
                return this.Any(a => a.IsInclusive)
                    ? _prefix +
                      string.Join(_joiner,
                          this.Where(a => !a.IsInclusive)
                              .Select(c => c.Sql)
                              .Union(new[]
                              {
                                  " ( " +
                                  string.Join(" OR ", this.Where(a => a.IsInclusive).Select(c => c.Sql).ToArray()) +
                                  " ) "
                              }).ToArray()) + _postfix
                    : _prefix + string.Join(_joiner, this.Select(c => c.Sql).ToArray()) + _postfix;
            }
        }

        /// <summary>
		/// Template.
		/// </summary>
        public class Template
        {
            private readonly string _sql;
            private readonly SqlBuilder _builder;
            private readonly object _initParams;
            private int _dataSeq = -1; // Unresolved

            /// <summary>
			/// Initializes a new instance of the <see cref="T:Dapper.SqlBuilder.Template"/> class.
			/// </summary>
			/// <param name="builder">Builder.</param>
			/// <param name="sql">Sql.</param>
			/// <param name="parameters">Parameters.</param>
            public Template(SqlBuilder builder, string sql, dynamic parameters)
            {
                _initParams = parameters;
                _sql = sql;
                _builder = builder;
            }

            private static readonly Regex _regex = new Regex(@"\/\*\*.+?\*\*\/", RegexOptions.Compiled | RegexOptions.Multiline);

            private void ResolveSql()
            {
                if (_dataSeq != _builder._seq) {
                    var p = new DynamicParameters(_initParams);

                    rawSql = _sql;

                    foreach (var pair in _builder._data) {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }
                    parameters = p;

                    // replace all that is left with empty
                    rawSql = _regex.Replace(rawSql, "");

                    _dataSeq = _builder._seq;
                }
            }

            private string rawSql;
            private object parameters;

            /// <summary>
			/// Gets the raw sql.
			/// </summary>
			/// <value>The raw sql.</value>
            public string RawSql {
                get { ResolveSql(); return rawSql; }
            }

            /// <summary>
			/// Gets the parameters.
			/// </summary>
			/// <value>The parameters.</value>
            public object Parameters {
                get { ResolveSql(); return parameters; }
            }
        }

        /// <summary>
		/// Adds the template.
		/// </summary>
		/// <returns>The template.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public Template AddTemplate(string sql, dynamic parameters = null) =>
            new Template(this, sql, parameters);

        /// <summary>
		/// Adds the clause.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
		/// <param name="joiner">Joiner.</param>
		/// <param name="prefix">Prefix.</param>
		/// <param name="postfix">Postfix.</param>
		/// <param name="isInclusive">If set to <c>true</c> is inclusive.</param>
        protected SqlBuilder AddClause(string name, string sql, object parameters, string joiner, string prefix = "", string postfix = "", bool isInclusive = false)
        {
            if (!_data.TryGetValue(name, out Clauses clauses)) {
                clauses = new Clauses(joiner, prefix, postfix);
                _data[name] = clauses;
            }
            clauses.Add(new Clause { Sql = sql, Parameters = parameters, IsInclusive = isInclusive });
            _seq++;
            return this;
        }

        /// <summary>
		/// Intersect the specified sql and parameters.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder Intersect(string sql, dynamic parameters = null) =>
            AddClause("intersect", sql, parameters, "\nINTERSECT\n ", "\n ", "\n", false);

        /// <summary>
		/// Inners the join.
		/// </summary>
		/// <returns>The join.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder InnerJoin(string sql, dynamic parameters = null) =>
            AddClause("innerjoin", sql, parameters, "\nINNER JOIN ", "\nINNER JOIN ", "\n", false);

        /// <summary>
		/// Lefts the join.
		/// </summary>
		/// <returns>The join.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder LeftJoin(string sql, dynamic parameters = null) =>
            AddClause("leftjoin", sql, parameters, "\nLEFT JOIN ", "\nLEFT JOIN ", "\n", false);

        /// <summary>
		/// Rights the join.
		/// </summary>
		/// <returns>The join.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder RightJoin(string sql, dynamic parameters = null) =>
            AddClause("rightjoin", sql, parameters, "\nRIGHT JOIN ", "\nRIGHT JOIN ", "\n", false);

        /// <summary>
		/// Where the specified sql and parameters.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder Where(string sql, dynamic parameters = null) =>
            AddClause("where", sql, parameters, " AND ", "WHERE ", "\n", false);

        /// <summary>
		/// Ors the where.
		/// </summary>
		/// <returns>The where.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder OrWhere(string sql, dynamic parameters = null) =>
            AddClause("where", sql, parameters, " OR ", "WHERE ", "\n", true);

        /// <summary>
		/// Orders the by.
		/// </summary>
		/// <returns>The by.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder OrderBy(string sql, dynamic parameters = null) =>
            AddClause("orderby", sql, parameters, " , ", "ORDER BY ", "\n", false);

        /// <summary>
		/// Select the specified sql and parameters.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder Select(string sql, dynamic parameters = null) =>
            AddClause("select", sql, parameters, " , ", "", "\n", false);

        /// <summary>
		/// Adds the parameters.
		/// </summary>
		/// <returns>The parameters.</returns>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder AddParameters(dynamic parameters) =>
            AddClause("--parameters", "", parameters, "", "", "", false);

        /// <summary>
		/// Join the specified sql and parameters.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder Join(string sql, dynamic parameters = null) =>
            AddClause("join", sql, parameters, "\nJOIN ", "\nJOIN ", "\n", false);

        /// <summary>
		/// Groups the by.
		/// </summary>
		/// <returns>The by.</returns>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder GroupBy(string sql, dynamic parameters = null) =>
            AddClause("groupby", sql, parameters, " , ", "\nGROUP BY ", "\n", false);

        /// <summary>
		/// Having the specified sql and parameters.
		/// </summary>
		/// <param name="sql">Sql.</param>
		/// <param name="parameters">Parameters.</param>
        public SqlBuilder Having(string sql, dynamic parameters = null) =>
            AddClause("having", sql, parameters, "\nAND ", "HAVING ", "\n", false);
    }
}
