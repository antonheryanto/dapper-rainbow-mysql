using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PetaTest;
using Dapper;

namespace Test
{
    [TestFixture]
    public class Test
    {
        [Test]
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
            Assert.Equals(total, 1);
            Assert.Equals(rows.First().City, "Kajang");
        }        

        [Test]
        public void PageTest()
        {            
            var x = db.Profiles.Page(1, 1);            
            Assert.Equals(x.Items.First().City, "Kajang");
        }

        Db db;
        [TestFixtureSetUp]
        public void Setup()
        {            
            var cn = new MySql.Data.MySqlClient.MySqlConnection(
                      System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString);
            cn.Open();
            db = Db.Init(cn, 30);
            var x = db.Execute(@"CREATE TABLE IF NOT EXISTS profiles(
                id INT(11),
                address VARCHAR(32), 
                postcode VARCHAR(32), 
                city VARCHAR(32), 
                facultyId INT(11),
                PRIMARY KEY (id)     
            );");
            if (db.Profiles.All().Count() == 0) {
                var id = db.Profiles.Insert(new { Address = "Alam Sari", City = "Kajang", PostCode = 43000 });
            }
        }
    }

    public class Db : Database<Db>
    {
        public Table<Profile> Profiles { get; set; }
    }

    public class Profile
    {
        public int Id { get; set; }                
        public string Address { get; set; }
        public string PostCode { get; set; }
        public string City { get; set; }
        public int FacultyId { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public uint Id { get; set; }
        public string Name { get; set; }
    }
}
