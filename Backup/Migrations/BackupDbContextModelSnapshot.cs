﻿// <auto-generated />
using System;
using Backup.Core.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backup.Migrations
{
    [DbContext(typeof(BackupDbContext))]
    partial class BackupDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.3");

            modelBuilder.Entity("Backup.Core.Impl.BackupCollection", b =>
                {
                    b.Property<long>("BackupCollectionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AutomaticBackup")
                        .HasColumnType("INTEGER");

                    b.Property<string>("BaseDirectory")
                        .HasColumnType("TEXT");

                    b.Property<string>("ExcludePattern")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Finished")
                        .HasColumnType("TEXT");

                    b.Property<string>("IncludePattern")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Started")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("BackupCollectionId");

                    b.HasIndex("Title")
                        .IsUnique();

                    b.ToTable("BackupCollections");
                });

            modelBuilder.Entity("Backup.Core.Impl.CopyFailure", b =>
                {
                    b.Property<long>("CopyFailureId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long?>("DestinationDirectoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("TEXT");

                    b.Property<long>("SourceFileId")
                        .HasColumnType("INTEGER");

                    b.HasKey("CopyFailureId");

                    b.HasIndex("DestinationDirectoryId");

                    b.ToTable("CopyFailures");
                });

            modelBuilder.Entity("Backup.Core.Impl.DestinationDirectory", b =>
                {
                    b.Property<long>("DestinationDirectoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("BackupCollectionId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Copied")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("Finished")
                        .HasColumnType("TEXT");

                    b.Property<string>("PathName")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Started")
                        .HasColumnType("TEXT");

                    b.HasKey("DestinationDirectoryId");

                    b.HasIndex("BackupCollectionId", "PathName")
                        .IsUnique();

                    b.ToTable("DestinationDirectories");
                });

            modelBuilder.Entity("Backup.Core.Impl.DestinationFile", b =>
                {
                    b.Property<long>("DestinationFileId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ContentHash")
                        .HasColumnType("TEXT");

                    b.Property<long?>("DestinationDirectoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PathName")
                        .HasColumnType("TEXT");

                    b.Property<long>("SourceFileId")
                        .HasColumnType("INTEGER");

                    b.HasKey("DestinationFileId");

                    b.HasIndex("DestinationDirectoryId");

                    b.ToTable("DestinationFiles");
                });

            modelBuilder.Entity("Backup.Core.Impl.SourceFile", b =>
                {
                    b.Property<long>("SourceFileId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("BackupCollectionId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ContentHash")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastWrittenTimeUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("PathName")
                        .HasColumnType("TEXT");

                    b.HasKey("SourceFileId");

                    b.HasIndex("BackupCollectionId", "PathName")
                        .IsUnique();

                    b.ToTable("SourceFiles");
                });

            modelBuilder.Entity("Backup.Core.Impl.CopyFailure", b =>
                {
                    b.HasOne("Backup.Core.Impl.DestinationDirectory", null)
                        .WithMany("CopyFailures")
                        .HasForeignKey("DestinationDirectoryId");
                });

            modelBuilder.Entity("Backup.Core.Impl.DestinationDirectory", b =>
                {
                    b.HasOne("Backup.Core.Impl.BackupCollection", null)
                        .WithMany("DestinationDirectories")
                        .HasForeignKey("BackupCollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Backup.Core.Impl.DestinationFile", b =>
                {
                    b.HasOne("Backup.Core.Impl.DestinationDirectory", null)
                        .WithMany("DestinationFiles")
                        .HasForeignKey("DestinationDirectoryId");
                });

            modelBuilder.Entity("Backup.Core.Impl.SourceFile", b =>
                {
                    b.HasOne("Backup.Core.Impl.BackupCollection", null)
                        .WithMany("SourceFiles")
                        .HasForeignKey("BackupCollectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
