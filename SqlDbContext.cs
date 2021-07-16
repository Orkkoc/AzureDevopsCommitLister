using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace TfsCommitSearcher
{
    public partial class SqlDbContext : DbContext
    {
        public SqlDbContext()
        {
        }

        public SqlDbContext(DbContextOptions<SqlDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<DevOpsChangelog> TfsYazilimChangelogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string sourceIP=""; //Source IP for Database 
                string catalogName = ""; // Catalogname for Database 
                string dbUser = ""; // Database username information
                string dbpass = ""; // Database user password information
                optionsBuilder.UseSqlServer("Data Source="+sourceIP+";Initial Catalog="+catalogName+ ";Persist Security Info=True;User ID=" + dbUser + "; Password=" + dbpass + "");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Turkish_CI_AS");

            modelBuilder.Entity<DevOpsChangelog>(entity =>
            {
                entity.HasKey(e => new { e.ProjectName, e.Git, e.ChangesetId })
                    .HasName("PK_Value");

                entity.ToTable("Table_Name", "Schema_Name");

                entity.Property(e => e.ProjectName).HasMaxLength(255);

                entity.Property(e => e.DisplayName).HasMaxLength(255);

                entity.Property(e => e.UniqueName).HasMaxLength(255);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
