using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AdaptiveLearningSystem.Models;

namespace AdaptiveLearningSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<LearningModule> LearningModules { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<StudentProgress> StudentProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent cascade delete cycles
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentProgress>()
                .HasOne(p => p.User)
                .WithMany(u => u.StudentProgresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentProgress>()
                .HasOne(p => p.Quiz)
                .WithMany(q => q.Progresses)
                .HasForeignKey(p => p.QuizId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentProgress>()
                .HasOne(p => p.Module)
                .WithMany(m => m.Progresses)
                .HasForeignKey(p => p.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Module)
                .WithMany(m => m.Enrollments)
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
