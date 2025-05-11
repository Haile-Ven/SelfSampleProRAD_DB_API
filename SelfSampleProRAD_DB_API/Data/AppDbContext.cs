using SelfSampleProRAD_DB_API.Models;
using Microsoft.EntityFrameworkCore;

namespace SelfSampleProRAD_DB_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // This is only used for design-time tools like migrations
                // when a connection isn't explicitly provided
                // Explicitly use the SQL Server connection string to avoid environment variable override
                var sqlServerConnectionString = "Data Source=HAILE-WORK;Initial Catalog=EmployeeTaskDB;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
                optionsBuilder.UseSqlServer(sqlServerConnectionString);
            }
        }

        public DbSet<Account> Account { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<Tasks> Tasks { get; set; }
        public DbSet<EmployeeTasks> EmployeeTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Account
            modelBuilder.Entity<Account>()
                .HasKey(a => a.UserId);

            // Configure Employee
            modelBuilder.Entity<Employee>()
                .HasKey(e => e.EmployeeId);

            // Configure Tasks
            modelBuilder.Entity<Tasks>()
                .HasKey(t => t.TaskId);

            // Configure Relationships
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Account)
                .WithOne(a => a.Employee)
                .HasForeignKey<Employee>(e => e.UserId)
                .IsRequired(false);

            // EmployeeTasks
            modelBuilder.Entity<EmployeeTasks>()
                .HasKey(et => et.ETID);

            modelBuilder.Entity<EmployeeTasks>()
                .HasOne(et => et.AssignedTo)
                .WithMany() 
                .HasForeignKey(et => et.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTasks>()
                .HasOne(et => et.AssignedBy)
                .WithMany() 
                .HasForeignKey(et => et.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTasks>()
                .HasOne(t => t.Tasks)
                .WithMany(t => t.EmployeeTasks)
                .HasForeignKey(et => et.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
