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
        
        public void TableName()
        {
           
            var a = db.Profiles.Get(1);
            var r = db.Profiles.InsertOrUpdate(new { id = a.Id, a.Address, a.PostCode, city = "Kajangx", a.FacultyId });
            var b = db.Profiles.Get(1);
            Assert.AreEqual<string>(b.City, "Kajangx");
        }

        [Test]
        public void PageTest()
        {
            var x = db.Page<Profile, User, Profile>("SELECT * FROM profiles p LEFT JOIN users u ON u.id=p.id", 
                (p, u) => { p.User = u; return p; });
            var y = x.Items;
            Assert.Equals(0, 0);
        }

        Db db;
        [TestFixtureSetUp]
        public void Setup()
        {            
            var cn = new MySql.Data.MySqlClient.MySqlConnection(
                      System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString);
            cn.Open();
            db = Db.Init(cn, 30);
        }
    }

    public class Db : Database<Db>
    {
        public Table<Profile> Profiles { get; set; }
    }

    public class Profile
    {
        public uint Id { get; set; }                
        public string Address { get; set; }
        public string PostCode { get; set; }
        public string City { get; set; }
        public uint FacultyId { get; set; }
        public User User { get; set; }
    }

    public class User
    {
        public uint Id { get; set; }
        public string Name { get; set; }
    }
}
