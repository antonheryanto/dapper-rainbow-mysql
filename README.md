Dapper.Rainbow.MySql
=======================

Implementation of Dapper.Rainbow targeting MySql,
with addons that gives you basic crud operations while having to write even less sql.

Table of Contents
=================
* [Usage](#usage)
* [How to find the tables](#how-to-find-the-tables)
* [API](#api)
* [Change Log](#changelog)

Usage
-----
```cs
    public class User {
      public int Id { get; set; }
      public string Email { get; set; }
      public string Password { get; set; }
      public string Name { get; set; }
    }
    
    public class Db : Database<Db> {
      public Table<User> Users { get; set; }
    }
    
    public static class Current {
      public Db DbInit()
      {
        var conn = new MysqlConnection(connectionString)){
        conn.Open();
        return Db.Init(conn, commandTimeout: 30);
      }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var u = Current.Db.Users.Get(1);
            System.Console.WriteLine($"got user Id {u.Id} with name {u.Name}");
        }
    }
```

How to find the tables
----------------------

Dapper.Rainbow.MySql knows what table to query based on the name of the class. 
In this situation the table that Rainbow looks in is the User table. It is not
pluralized. 

API
---
    
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

ChangeLog
---------
### 0.8.2
* Fix First and All API where its return dynamic instead of T
* Fix tests and add more test for old API and async API

### 0.8.1
* Improve netstandard1.6 dependency (based on Dapper.contrib)
* Add missing xmldocs

### 0.8.0
* Sync with changes in latest dapper.rainbow
* Add Async Methods
* Separated into multiple files using partial class
* support net451 and netstandard1.6
