using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ETMS.Models; // Adjust the namespace according to your project structure

namespace ETMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestSubmission> TestSubmissions { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<TestMode> TestModes { get; set; }
         public DbSet<TestModeUpdate> TestModesUpdate { get; set; }
        
        public DbSet<UserResponse> UserResponses { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        // ✅ Added for "Manage Modules"
        public DbSet<TrainingModule> TrainingModules { get; set; }
        public DbSet<LearningMaterial> LearningMaterials { get; set; }

        public DbSet<EmployeeModule> EmployeeModules { get; set; }

        public DbSet<Project> Projects { get; set; }
        public DbSet<EmployeeProject> EmployeeProjects { get; set; }

        public DbSet<ProjectSubmission> ProjectSubmissions { get; set; }
        public DbSet<EmployeeReport> EmployeeReports { get; set; } // Add this

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // ✅ Map tables explicitly to avoid EF Core table name changes
            modelBuilder.Entity<TrainingModule>().ToTable("TrainingModules");
            modelBuilder.Entity<LearningMaterial>().ToTable("LearningMaterials");
            modelBuilder.Entity<ApplicationUser>()
            .HasOne(e => e.Trainer)
            .WithMany(t => t.AssignedEmployees)
            .HasForeignKey(e => e.AssignedTrainerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete



            

            modelBuilder.Entity<EmployeeProject>()
    .HasOne(ep => ep.Employee)
    .WithMany(e => e.AssignedProjects)
    .HasForeignKey(ep => ep.EmployeeId)  // ✅ Ensure the correct column name
    .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<EmployeeModule>()
            .HasOne(em => em.Employee)
            .WithMany(e => e.AssignedModules)  // Explicitly referencing navigation property
             .HasForeignKey(em => em.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeModule>()
                .HasOne(em => em.Module)
                .WithMany(m => m.AssignedEmployees)  // Explicitly reference if it exists
                .HasForeignKey(em => em.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LearningMaterial>()
                .HasOne(lm => lm.Module)
                .WithMany(m => m.LearningMaterials)  // Explicitly reference LearningMaterials
                .HasForeignKey(lm => lm.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
            // Define many-to-many relationship between Employee and Projects
            modelBuilder.Entity<EmployeeProject>()
                .HasOne(ep => ep.Employee)
                .WithMany(e => e.AssignedProjects)
                .HasForeignKey(ep => ep.EmployeeId);

            modelBuilder.Entity<EmployeeProject>()
                .HasOne(ep => ep.Project)
                .WithMany(p => p.AssignedEmployees)
                .HasForeignKey(ep => ep.ProjectId);

            /* --------------------------------*/
            modelBuilder.Entity<EmployeeReport>()
        .Property(e => e.EmployeeId)
        .HasColumnType("varchar(450)"); // ✅ Ensure it's treated as varchar

            modelBuilder.Entity<EmployeeReport>()
                .HasOne(e => e.Employee)
                .WithMany(u => u.EmployeeReports)
                .HasForeignKey(e => e.EmployeeId)
                .HasPrincipalKey(u => u.Id); // ✅ Ensure foreign key matches IdentityUser

           /* -------------------------------------------*/



            modelBuilder.Entity<EmployeeProject>()
    .HasOne(ep => ep.Employee)
    .WithMany(e => e.AssignedProjects)
    .HasForeignKey(ep => ep.EmployeeId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeProject>()
                .HasOne(ep => ep.Project)
                .WithMany(p => p.AssignedEmployees)
                .HasForeignKey(ep => ep.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeProject>()
            .HasOne(ep => ep.Project)
            .WithMany(p => p.AssignedEmployees)
            .HasForeignKey(ep => ep.ProjectId);

            modelBuilder.Entity<ProjectSubmission>()
                .HasOne(ps => ps.Project)
                .WithMany(p => p.ProjectSubmissions)
                .HasForeignKey(ps => ps.ProjectId);
            modelBuilder.Entity<Project>()
       .HasOne(p => p.Trainer)
       .WithMany()
       .HasForeignKey(p => p.TrainerId)
       .OnDelete(DeleteBehavior.Restrict); // ✅ Prevent cascading deletion
            /*---------------------------------------*/
            // ✅ Define Foreign Key relationship between TrainingModules and ApplicationUser (Trainer)
            modelBuilder.Entity<TrainingModule>()
                .HasOne(tm => tm.Trainer)
                .WithMany()
                .HasForeignKey(tm => tm.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed roles
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = Guid.NewGuid().ToString(), Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = Guid.NewGuid().ToString(), Name = "Trainer", NormalizedName = "TRAINER" },
                new IdentityRole { Id = Guid.NewGuid().ToString(), Name = "Employee", NormalizedName = "EMPLOYEE" }
            );
        }
    }
}
