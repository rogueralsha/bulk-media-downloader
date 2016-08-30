using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using BulkMediaDownloader.Model;

namespace BulkMediaDownloader.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("BulkMediaDownloader.Download.Downloadable", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DownloadDir");

                    b.Property<long>("Length");

                    b.Property<string>("RefererURLString");

                    b.Property<bool>("SimpleHeaders");

                    b.Property<string>("SiteString");

                    b.Property<string>("Source");

                    b.Property<int>("State");

                    b.Property<string>("URLString");

                    b.HasKey("Id");

                    b.ToTable("Downloadables");
                });

            modelBuilder.Entity("BulkMediaDownloader.Download.DownloadablesSource", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DownloadDir");

                    b.Property<string>("MediaSourceName");

                    b.Property<string>("RefererURLString");

                    b.Property<string>("SourceStage");

                    b.Property<string>("SourceURLString");

                    b.Property<int>("State");

                    b.Property<string>("URLString");

                    b.HasKey("Id");

                    b.ToTable("DownloadableSources");
                });
        }
    }
}
