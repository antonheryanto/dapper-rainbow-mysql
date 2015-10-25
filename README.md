Dapper.Rainbow.MySql
=======================

This project is a reimplementation of Dapper.Rainbow designed for MySql. It is an addon that gives you basic crud operations while having to write even less sql.

    class User {
      public int Id { get; set; }
      public String Email { get; set; }
      public String Password { get; set; }
      public String Name { get; set; }
    }
    
    class UserDB : Database<UserDB> {
      public Table<User> Users { get; set; }
    }
    
    class Demo {
      public void Do(){
        using(var conn = new MysqlConnection(connectionString)){
          conn.Open();
          var db = UserDB.Init(conn, commandTimeout: 2);
          
          //drop the table if it exists
          db.Execute ("drop table if exists user;");
          
          //create the table
          db.Execute (@"create table user (
  							Id int NOT NULL,
  							Email varchar(100), 
  							Password varchar(32), 
  							Name varchar(32), 
  							PRIMARY KEY(Id));");
          
          /* 
            
            Do somthing interesting in here 
          
          */
        }
      }
    }


How it finds the tables
------------

Dapper.Rainbow.MySql knows what table to query based on the name of the class. 
In this situation the table that Rainbow looks in is the User table. It is not
pluralized. 

API
----------
    
### Get All The Users
    IEnumerable<User> all = db.Users.All();
    
### Get A User
    User user = db.Users.Get(userId);
    User same_user = db.Users.Get(new {Id = userId});

### Delete a User 
    bool isTrue = db.Users.Delete(user);
    bool isAnotherTrue = db.Users.Delete(new {Id = userId});
  
### Get The First User
    User user = db.Users.First();
  
### Insert A User
    long uid = db.Users.Insert (
      new { Email="foolio@coolio.com", 
            Name="Foolio Coolio", 
            Password="AHashedPasswordOfLengthThirtyTwo"});

### Insert Or Update A User
    int uid = db.Users.InsertOrUpdate(user);
    
### Update
    user.Name = "Foolio Jr."
    int uid = db.Users.Update(uid, user);
    int uid = db.Users.Update(new {Id = uid}, user);
