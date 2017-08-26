using System;

namespace Dapper.Rainbow.MySql.Tests
{
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
        public string State { get; set; }
        public string Country { get; set; }
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

        public override string ToString()
        {
            return Note;
        }
    }
}
