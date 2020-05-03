/*
    Myna Backup
    Copyright (C) 2020 Niels Stockfleth

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using Microsoft.EntityFrameworkCore;

namespace Backup.Core.Impl
{
    public class BackupDbContext : DbContext
    {
        public BackupDbContext(DbContextOptions<BackupDbContext> options) : base(options)
        {
        }

        public DbSet<BackupCollection> BackupCollections { get; set; }

        public DbSet<SourceFile> SourceFiles { get; set; }

        public DbSet<DestinationDirectory> DestinationDirectories { get; set; }

        public DbSet<DestinationFile> DestinationFiles { get; set; }

        public DbSet<CopyFailure> CopyFailures { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<BackupCollection>()
                .HasIndex(col => col.Title)
                .IsUnique();
            builder.Entity<SourceFile>()
                .HasIndex(sf => new { sf.BackupCollectionId, sf.PathName })
                .IsUnique();
            builder.Entity<DestinationDirectory>()
                .HasIndex(dd => new { dd.BackupCollectionId, dd.PathName })
                .IsUnique();
        }
    }
}
