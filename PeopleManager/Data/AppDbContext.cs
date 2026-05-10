using Microsoft.EntityFrameworkCore;
using PeopleManager.Models;

namespace PeopleManager.Data
{
    /// <summary>
    /// Main database context for the PeopleManager application.
    /// Manages all database operations via Entity Framework Core.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Represents the People table in the database.
        /// </summary>
        public DbSet<Person> People { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraint on Email
            modelBuilder.Entity<Person>()
                .HasIndex(p => p.Email)
                .IsUnique();
        }
    }
}
