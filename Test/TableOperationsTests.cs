using System;
using PetaTest;
using Dapper;
using MySql.Data.MySqlClient;

namespace Test
{
	[TestFixture]
	public class TableOperationTests
	{
		MySqlConnection cn;

		class UserDB : Database<UserDB>
		{
			public Table<User> Users { get; set; }
		}
			
		class User {
			[PrimaryKey]
			public int Id { get; set; }
		}

		[TestFixtureSetUp]
		public void Setup()
		{            
			cn = new MySqlConnection(
				System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString);
			cn.Open();
			db = UserDB.Init(cn, 30);
		}

		[TestFixtureTearDown]
		public void Teardown(){
			cn.Close ();
		}

		UserDB db;
		[Test]
		public void CreateTable(){
			db.Execute ("drop table if exists user;");
			db.Users.Create ();
			var rows = db.Query("SHOW INDEXES FROM user WHERE Key_name = 'PRIMARY'");
			Assert.IsTrue (rows.AsList ().Count == 1);
			db.Execute ("drop table if exists user;");
		}

		[Test]
		public void DeleteTable(){
			db.Users.Drop ();
		}

		[Test]
		public void TableAlreadyExistsExceptionTest(){
			db.Users.Create ();
			Assert.Throws<TableAlreadyExistsException>(db.Users.Create);
		}
	}

}

