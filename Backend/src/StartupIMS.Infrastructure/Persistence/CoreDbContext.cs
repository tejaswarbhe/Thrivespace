using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

namespace StartupIMS.Infrastructure.Persistence;

public partial class CoreDbContext : DbContext
{
    public CoreDbContext()
    {
    }

    public CoreDbContext(DbContextOptions<CoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Fundingrequest> Fundingrequests { get; set; }

    public virtual DbSet<Incubationapplication> Incubationapplications { get; set; }

    public virtual DbSet<Mentor> Mentors { get; set; }

    public virtual DbSet<Mentorassignment> Mentorassignments { get; set; }

    public virtual DbSet<Progressreport> Progressreports { get; set; }

    public virtual DbSet<Startup> Startups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Fundingrequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.ApprovalStatus).HasDefaultValueSql("'Pending'");

            entity.HasOne(d => d.Startup).WithMany(p => p.Fundingrequests).HasConstraintName("FK_FundingRequests_Startups");
        });

        modelBuilder.Entity<Incubationapplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Status).HasDefaultValueSql("'Submitted'");
            entity.Property(e => e.SubmissionDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Startup).WithMany(p => p.Incubationapplications).HasConstraintName("FK_Applications_Startups");
        });

        modelBuilder.Entity<Mentor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
        });

        modelBuilder.Entity<Mentorassignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.AssignedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Status).HasDefaultValueSql("'Active'");

            entity.HasOne(d => d.Mentor).WithMany(p => p.Mentorassignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MentorAssignments_Mentors");

            entity.HasOne(d => d.Startup).WithMany(p => p.Mentorassignments).HasConstraintName("FK_MentorAssignments_Startups");
        });

        modelBuilder.Entity<Progressreport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.SubmissionDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Startup).WithMany(p => p.Progressreports).HasConstraintName("FK_ProgressReports_Startups");
        });

        modelBuilder.Entity<Startup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Status).HasDefaultValueSql("'Applied'");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
