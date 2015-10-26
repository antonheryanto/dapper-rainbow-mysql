using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PetaTest;
using Dapper;
using System.Dynamic;

namespace Test
{
    [TestFixture]
    public class Test
    {
        //[Test]
        public void SqlBuilderTest()
        {
            var sql = new SqlBuilder();
            var count = sql.AddTemplate("SELECT COUNT(*) FROM profiles /**where**/");
            var selector = sql.AddTemplate("SELECT * FROM profiles /**where**/ /**orderby**/");
            sql.Where("id = @id", new { id = 1 });
            sql.Where("city = @city", new { city = "Bangi" });
            sql.OrderBy("id DESC");
            sql.OrderBy("city");
            var total = db.Query<long>(count.RawSql, count.Parameters).Single();
            var rows = db.Query<Profile>(selector.RawSql, selector.Parameters);

            Assert.Equals(total, 1);
            Assert.Equals(rows.First().City, "Bangi");
        }        

        //[Test]
        public void PageTest()
        {            
            var x = db.Profiles.Page(1, 1);            
            Assert.Equals(x.Items.First().City, "Kajang");
        }

		//[Test]
		public void CountTest ()
		{
			var x = db.Query("SELECT COUNT(*) FROM profiles").Single() as IDictionary<string, object>;
			var y = x.Values.Single().GetType();
			Assert.Equals(typeof(long), y);
		}

		//[Test]
		public void LastId ()
		{
			var r = db.Query("SELECT LAST_INSERT_ID()").Single() as IDictionary<string, object>;
			var t = r.Values.Single().GetType();
			var c = typeof(long);
			Assert.Equals(c, t);
		}


        //[Test]
        public void InsertOrUpdateWithId()
        {
            var c = db.Profiles.Get(1);
            var id = db.Profiles.InsertOrUpdate(c.Id, new { City = "Bangi" });
            //var x = db.Profiles.Update(3, new { postcode = "43650" });
            //var profiles = db.Profiles.All(new { id }).ToList();
            //var profile = db.Profiles.Get(new { id });
            var p = db.Profiles.Page(where: new { id });
            Assert.Equals(p.Items.Count, 1);
        }

		//[Test]
		public void InsertOrUpdateWithoutId()
		{
			var x = db.ReportNote.InsertOrUpdate(new { userId = 1, sessionId = 1, note = "note", noterId = 2 });
			var y = db.ReportNote.Get(new { userId = 1 });
			Assert.Equals(x,y);
		}

		//[Test]
		public void UpdateTest()
		{            
			var city = "Bangi";
			var id = 1;
			var facultyId = 1;
			db.Profiles.Update(new { id, facultyId }, new { city });   
			var p = db.Profiles.Get(new { id, facultyId });
			Assert.Equals(p.City, city);
		}

		[Test]
		public void DynamicParametersTest()
		{
			var p = new DynamicExpando (new { city = "Bangi", facultyId = 1 });
			p.AddRange (new { Address = "add late", PostCode = "43650" });
			dynamic o = new ExpandoObject ();
			o.City = "Bangi";
			var x = db.Query (@"SELECT * FROM profiles WHERE city=@city", o);
			Assert.Equals (0, 0);
		}

        Db db;
        [TestFixtureSetUp]
        public void Setup()
        {            
            var cn = new MySql.Data.MySqlClient.MySqlConnection(
                      System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString);
            cn.Open();
            db = Db.Init(cn, 30);
            db.Execute(@"CREATE TABLE IF NOT EXISTS profiles(
                id INT(11) NOT NULL AUTO_INCREMENT ,
                address VARCHAR(32), 
                postcode VARCHAR(32), 
                city VARCHAR(32), 
                facultyId INT(11) NOT NULL DEFAULT 0,
                PRIMARY KEY (id),
			    KEY(facultyId)
            );");
            if (db.Profiles.All().Count() == 0) {
                db.Profiles.Insert(new { Address = "Alam Sari", City = "Kajang", PostCode = 43000, FacultyId=1 });
            }
        }
    }

    public class Db : Database<Db>
    {
        public Table<Profile> Profiles { get; set; }
		public Table<ReportNote> ReportNote { get; set; }
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

	public class ReportNote
	{
		public int UserId { get; set; }
		public int SessionId { get; set; }
		public int NoterId { get; set; }
		public string Note { get; set; }
		public DateTime Changed { get; set; }
		
		public override string ToString ()
		{
			return Note;
		}
	}

	public class DynamicExpando : DynamicObject
	{
		IDictionary<string, object> members = new Dictionary<string, object> (StringComparer.InvariantCultureIgnoreCase);

		public DynamicExpando (dynamic param)
		{
			AddRange (param);
		}

		public void AddRange(dynamic param)
		{
			if (param as object == null) return;
			foreach (var property in System.ComponentModel.TypeDescriptor.GetProperties(param.GetType()))
				members.Add(property.Name, property.GetValue(param));
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			string name = binder.Name.ToLower();
			return members.TryGetValue(name, out result);
		}
	}
}
