using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BulkMediaDownloader.Download;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BulkMediaDownloader.Model {
    class DatabaseContext : DbContext {
        public DbSet<Download.Downloadable> Downloadables { get; set; }
        public DbSet<Download.DownloadablesSource> DownloadableSources { get; set; }

        public void Add(ADownloadable d) {
            if (d is Downloadable)
                Downloadables.Add(d as Downloadable);
            if (d is DownloadablesSource)
                DownloadableSources.Add(d as DownloadablesSource);
            this.SaveChanges();
        }

        public void Update(ADownloadable d) {
            if (d is Downloadable)
                Downloadables.Update(d as Downloadable);
            if (d is DownloadablesSource)
                DownloadableSources.Update(d as DownloadablesSource);
            this.SaveChanges();
        }

        public void Remove(ADownloadable d, bool autoSave = true) {
            if (d is Downloadable)
                Downloadables.Remove(d as Downloadable);
            if (d is DownloadablesSource)
                DownloadableSources.Remove(d as DownloadablesSource);
            if(autoSave)
            this.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            String path = System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "BulkMediaDownloader", "BulkMediaDownloader.db");
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            optionsBuilder.UseSqlite("Filename=" + path);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
        }
    }
}
