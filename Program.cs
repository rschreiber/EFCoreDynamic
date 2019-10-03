using System;
using EFCoreSandbox.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCoreSandbox
{
    class Program
    {
        private static string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB; Database=TestDb; Integrated Security=True;";

        static void Main(string[] args)
        {
            var dynDbContextType = DynamicContextBuilder.BuildDynamicDbContextType();

            var builder = new DbContextOptionsBuilder();

            builder.UseSqlServer(
                ConnectionString);

            DbContext dynDbContext = (DbContext) Activator.CreateInstance(dynDbContextType, builder.Options);

            dynDbContext.Add<Pet>(new Pet() {Name = "Daisy"});

            dynDbContext.Add<Person>(new Person() {FirstName = "Dave"});

            dynDbContext.SaveChanges();
        }
    }
}