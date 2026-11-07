using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ShotForge.Core.Models;

namespace ShotForge.Data
{
    public class ShotForgeDbContext : DbContext
    {
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Scene> Scenes => Set<Scene>();
        public DbSet<Shot> Shots => Set<Shot>();
        public DbSet<Character> Characters => Set<Character>();
        public DbSet<PlatformCreditLog> CreditLogs => Set<PlatformCreditLog>();

        /// <summary>
        /// Local-first storage: a single SQLite file living under the
        /// user's own AppData folder. Nothing here ever leaves the
        /// user's machine — there is no server component.
        /// </summary>
        public static string GetDatabasePath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var shotForgeFolder = Path.Combine(appDataFolder, "ShotForge");
            Directory.CreateDirectory(shotForgeFolder);
            return Path.Combine(shotForgeFolder, "data.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={GetDatabasePath()}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Project -> Scenes (one-to-many)
            modelBuilder.Entity<Scene>()
                .HasOne(s => s.Project)
                .WithMany(p => p.Scenes)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Project -> Characters (one-to-many)
            modelBuilder.Entity<Character>()
                .HasOne(c => c.Project)
                .WithMany(p => p.Characters)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Scene -> Shots (one-to-many)
            modelBuilder.Entity<Shot>()
                .HasOne(sh => sh.Scene)
                .WithMany(sc => sc.Shots)
                .HasForeignKey(sh => sh.SceneId)
                .OnDelete(DeleteBehavior.Cascade);

            // Shot <-> Character (many-to-many, EF Core 8 implicit join table)
            modelBuilder.Entity<Shot>()
                .HasMany(sh => sh.Characters)
                .WithMany(c => c.Shots)
                .UsingEntity(j => j.ToTable("ShotCharacters"));

            // Project -> CreditLogs (one-to-many)
            modelBuilder.Entity<PlatformCreditLog>()
                .HasOne(cl => cl.Project)
                .WithMany()
                .HasForeignKey(cl => cl.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
