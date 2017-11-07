using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Rainbow.MySql.Tests
{
    public class DatabaseTest
    {
        private readonly Db db;

        public IConfigurationRoot Configuration { get; set; }

        public DatabaseTest()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json",
                optional: true, reloadOnChange: true);
            Configuration = builder.Build();

            var cs = Configuration.GetConnectionString("db");
            var cn = new MySqlConnection(cs);
            db = Db.Init(cn, 30);
            db.Execute(@"CREATE TABLE if not exists profiles(
                id INT(11) NOT NULL AUTO_INCREMENT ,
                address VARCHAR(32),
                postcode VARCHAR(32),
                city VARCHAR(32),
                state VARCHAR(32),
                country VARCHAR(64),
                facultyId INT(11) NOT NULL DEFAULT 0,
                PRIMARY KEY (id),
			    KEY(facultyId)
            );");

            db.Execute(@"CREATE TABLE if not exists reportnote(
                UserId INT,
                SessionId INT,
                NoterId INT,
                Note VARCHAR(256),
                Changed DATETIME,
                PRIMARY KEY (NoterId),
                Key(UserId),
                Key(SessionId)
            );");
            db.Execute("TRUNCATE profiles");

            if (!db.Profiles.All().Any()) {
                db.Profiles.Insert(new { Address = "Alam Sari", City = "Kajang", PostCode = 43000, State = "Selangor", Country = "Malaysia", FacultyId = 1 });
                db.Profiles.Insert(new { Address = "Marpuyan", City = "Pekanbaru", PostCode = 28284, State = "Riau", Country = "Indonesia", FacultyId = 2 });
            }
        }

        [Fact]
        public void Status()
        {
            Assert.False(db is null, "should be database");
            Assert.True(db.Query("select 1").Any(), "should has value");
        }

        [Fact]
        public void SqlBuilderTest()
        {
            var sql = new SqlBuilder();
            var count = sql.AddTemplate("SELECT COUNT(*) FROM profiles /**where**/");
            var selector = sql.AddTemplate("SELECT * FROM profiles /**where**/ /**orderby**/");
            sql.Where("id = @id", new { id = 1 });
            sql.Where("city = @city", new { city = "Kajang" });
            sql.OrderBy("id DESC");
            sql.OrderBy("city");

            var total = db.Query<long>(count.RawSql, count.Parameters).Single();
            var rows = db.Query<Profile>(selector.RawSql, selector.Parameters);
            Assert.Equal(total, 1);
            Assert.Equal(rows.First().City, "Kajang");
        }

        [Fact]
        public void PageTest() => Assert.Equal(db.Profiles.Page(1, 1).Items[0].City, "Kajang");

        [Fact]
        public async Task PageAsyncTest() => Assert.Equal((await db.Profiles.PageAsync(1, 1)).Items[0].City, "Kajang");

        [Fact]
        public void PageOrderTest()
        {
            var result = db.Page<Profile>("SELECT * FROM profiles ORDER BY country, city");
            Assert.Equal(result.Items[1].City, "Kajang");
        }

        [Fact]
        public async Task PageOrderAsyncTestAsync()
        {
            var result = await db.PageAsync<Profile>("SELECT * FROM profiles ORDER BY country, city").ConfigureAwait(false);
            Assert.Equal(result.Items[1].City, "Kajang");
        }

        [Fact]
        public void CountTest()
        {
            var x = db.Query("SELECT COUNT(*) FROM profiles").Single() as IDictionary<string, object>;
            var y = x.Values.Single().GetType();
            Assert.Equal(typeof(long), y);
        }

        [Fact]
        public void LastIdTest()
        {
            var r = db.Query("SELECT LAST_INSERT_ID()").Single() as IDictionary<string, object>;
            var t = Convert.ToInt64(r.Values.Single());
            Assert.Equal(typeof(long), t.GetType());
        }

        [Fact]
        public void InsertOrUpdateWithIdTest()
        {
            var c = db.Profiles.Get(1);
            var id = db.Profiles.InsertOrUpdate(c.Id, new { City = "Bangi" });
            var p = db.Profiles.Page(where: new { id });
            Assert.Equal(p.Items.Count, 1);
        }

        [Fact]
        public void InsertOrUpdateWithoutIdTest()
        {
            var u = new { userId = 1, sessionId = 1, note = "note", noterId = 2 };
            db.ReportNote.InsertOrUpdate(u);
            var y = db.ReportNote.Get(new { userId = 1 });
            Assert.Equal(u.userId, y.UserId);
        }

        [Fact]
        public void UpdateTest()
        {
            const string city = "Bangi";
            const int id = 1;
            const int facultyId = 1;
            db.Profiles.Update(new { id, facultyId }, new { city });
            var p = db.Profiles.Get(new { id, facultyId });
            Assert.Equal(p.City, city);
        }

        [Fact]
        public async Task UpdateAsyncTest()
        {
            const int id = 1;
            const string city = "Kajang";
            await db.Profiles.UpdateAsync(id, new { city });
            Assert.Equal(city, db.Profiles.Get(id).City);
        }

        [Fact]
        public void GetTest() => Assert.Equal(db.Profiles.Get(1).FacultyId, 1);

        [Fact]
        public async Task GetAsyncTest() => Assert.Equal((await db.Profiles.GetAsync(1)).FacultyId, 1);

        [Fact]
        public void AllTest() => Assert.Equal(db.Profiles.All().Count(), 2);

        [Fact]
        public void AllWhereTest() => Assert.Equal(db.Profiles.All(new { facultyId = 1 }).First().Id, 1);

        [Fact]
        public async Task AllAsyncTest() => Assert.Equal((await db.Profiles.AllAsync()).Count(), 2);

        [Fact]
        public async Task AllAsyncWhereTest() => Assert.Equal((await db.Profiles.AllAsync(new { facultyId = 1 })).Count(), 1);

        [Fact]
        public void FirstTest() => Assert.Equal(db.Profiles.First().Id, 1);

        [Fact]
        public void FirstWhereTest() => Assert.Equal(db.Profiles.First(new { facultyId = 1 }).Id, 1);

        [Fact]
        public async Task FirstAsyncTest() => Assert.Equal((await db.Profiles.FirstAsync()).FacultyId, 1);

        [Fact]
        public async Task FirstWhereAsyncTest() => Assert.Equal((await db.Profiles.FirstAsync(new { facultyId = 1 })).FacultyId, 1);
    }
}
