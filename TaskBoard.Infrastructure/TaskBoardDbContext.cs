using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Core.Entities;
using TaskBoard.Infrastructure.Identity;

namespace TaskBoard.Infrastructure;

public class TaskBoardDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public TaskBoardDbContext(DbContextOptions<TaskBoardDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Key).HasMaxLength(10).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(p => p.Key).IsUnique();
        });

        builder.Entity<Issue>(entity =>
        {
            entity.Property(i => i.Title).HasMaxLength(300).IsRequired();
            entity.HasOne(i => i.Project)
                .WithMany(p => p.Issues)
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(i => new { i.ProjectId, i.Number }).IsUnique();
        });

        builder.Entity<Comment>(entity =>
        {
            entity.Property(c => c.Content).HasMaxLength(4000).IsRequired();
            entity.HasOne(c => c.Issue)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.IssueId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
