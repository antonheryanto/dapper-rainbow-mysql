using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using PetaTest;
using static PetaTest.Assert;

namespace Test
{
	[TestFixture]
	public class Test
	{
		[Test]
		public void SqlBuilderTest ()
		{
			var sql = new SqlBuilder ();
			var count = sql.AddTemplate ("SELECT COUNT(*) FROM profiles /**where**/");
			var selector = sql.AddTemplate ("SELECT * FROM profiles /**where**/ /**orderby**/");
			sql.Where ("id = @id", new { id = 1 });
			sql.Where ("city = @city", new { city = "Kajang" });
			sql.OrderBy ("id DESC");
			sql.OrderBy ("city");

			var total = db.Query<long> (count.RawSql, count.Parameters).Single ();
			var rows = db.Query<Profile> (selector.RawSql, selector.Parameters);
			AreEqual (total, 1);
			AreEqual (rows.First ().City, "Kajang");
		}

		[Test]
		public void PageTest () => AreEqual (db.Profiles.Page (1, 1).Items[0].City, "Kajang");

		[Test]
		public void CountTest ()
		{
			var x = db.Query ("SELECT COUNT(*) FROM profiles").Single () as IDictionary<string, object>;
			var y = x.Values.Single ().GetType ();
			AreEqual (typeof (long), y);
		}

		[Test]
		public void LastIdTest ()
		{
			var r = db.Query ("SELECT LAST_INSERT_ID()").Single () as IDictionary<string, object>;
			var t = Convert.ToInt64(r.Values.Single ());
			AreEqual (typeof(long), t.GetType());
		}

		[Test]
		public void InsertOrUpdateWithIdTest ()
		{
			var c = db.Profiles.Get (1);
			var id = db.Profiles.InsertOrUpdate (c.Id, new { City = "Bangi" });
			var p = db.Profiles.Page (where: new { id });
			AreEqual (p.Items.Count, 1);
		}

		[Test]
		public void InsertOrUpdateWithoutIdTest ()
		{
			var x = db.ReportNote.InsertOrUpdate (new { userId = 1, sessionId = 1, note = "note", noterId = 2 });
			var y = db.ReportNote.Get (new { userId = 1 });
			AreEqual (x, y.UserId);
		}

		[Test]
		public void UpdateTest ()
		{
			var city = "Bangi";
			var id = 1;
			var facultyId = 1;
			db.Profiles.Update (new { id, facultyId }, new { city });
			var p = db.Profiles.Get (new { id, facultyId });
			AreEqual (p.City, city);
		}

		[Test]
		public void UpdateAsyncTest ()
		{
			var id = 1;
			var city = "Kajang";
			db.Profiles.UpdateAsync (id, new { city });
			AreEqual (city, db.Profiles.Get (id).City);
		}

		[Test]
		public void GetTest () => AreEqual (db.Profiles.Get (1).FacultyId, 1);

		[Test]
		public async Task GetAsyncTest () => AreEqual ((await db.Profiles.GetAsync (1)).FacultyId, 1);

		[Test]
		public void AllTest () => AreEqual (db.Profiles.All().Count(), 1);

		[Test]
		public void AllWhereTest () => AreEqual (db.Profiles.All (new { facultyId = 1 }).First().Id, 1);

		[Test]
		public async Task AllAsyncTest () => AreEqual ((await db.Profiles.AllAsync ()).Count (), 1);

		[Test]
		public async Task AllAsyncWhereTest () => AreEqual ((await db.Profiles.AllAsync (new { facultyId = 1 })).First ().Id, 1);

		[Test]
		public void FirstTest () => AreEqual (db.Profiles.First ().Id, 1);

		[Test]
		public void FirstWhereTest () => AreEqual (db.Profiles.First (new { facultyId = 1 }).Id, 1);

		[Test]
		public async Task FirstAsyncTest () => AreEqual ((await db.Profiles.FirstAsync ()).FacultyId, 1);

		[Test]
		public async Task FirstWhereAsyncTest () => AreEqual ((await db.Profiles.FirstAsync (new { facultyId = 1 })).FacultyId, 1);


		Db db;
		[TestFixtureSetUp]
		public void Setup ()
		{
			var connectionString = ConfigurationManager.ConnectionStrings ["MySql"].ConnectionString;
			var cn = new MySqlConnection (connectionString);

			cn.Open ();

			db = Db.Init (cn, 30);
			db.Execute ("drop table if exists profiles;");
			db.Execute (@"CREATE TABLE profiles(
                id INT(11) NOT NULL AUTO_INCREMENT ,
                address VARCHAR(32),
                postcode VARCHAR(32),
                city VARCHAR(32),
                facultyId INT(11) NOT NULL DEFAULT 0,
                PRIMARY KEY (id),
			    KEY(facultyId)
            );");

			db.Execute ("drop table if exists reportnote");
			db.Execute (@"CREATE TABLE reportnote(
                UserId INT,
                SessionId INT,
                NoterId INT,
                Note VARCHAR(256),
                Changed DATETIME,
                PRIMARY KEY (NoterId),
                Key(UserId),
                Key(SessionId)
            );");

			if (db.Profiles.All ().Count () == 0) {
				db.Profiles.Insert (new { Address = "Alam Sari", City = "Kajang", PostCode = 43000, FacultyId = 1 });
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

		public void AddRange (dynamic param)
		{
			if (param as object == null) return;
			foreach (var property in System.ComponentModel.TypeDescriptor.GetProperties (param.GetType ()))
				members.Add (property.Name, property.GetValue (param));
		}

		public override bool TryGetMember (GetMemberBinder binder, out object result)
		{
			string name = binder.Name.ToLower ();
			return members.TryGetValue (name, out result);
		}
	}
}
