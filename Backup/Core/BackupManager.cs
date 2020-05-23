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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backup.Core.Impl;
using System.Threading;
using System.Threading.Tasks;

namespace Backup.Core
{
    public static class BackupManager
    {
        private static readonly string HISTORY = ".history";
        private static readonly string DATE_FOLDER_FOMRAT = "yyyy-MM-dd-HH-mm-ss";

        private static DbContextOptions<BackupDbContext> dbOptions;

        public static void Init(string databaseFile)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BackupDbContext>();
            optionsBuilder.UseSqlite($"Data Source={databaseFile}");
            dbOptions = optionsBuilder.Options;
            if (!File.Exists(databaseFile))
            {
                using var dbContext = new BackupDbContext(dbOptions);
                dbContext.Database.Migrate();
            }
        }

        public static List<string> GetAll()
        {
            using var dbContext = new BackupDbContext(dbOptions);
            return dbContext.BackupCollections
                .OrderBy(col => col.Title)
                .Select(col => col.Title)
                .ToList();
        }

        public static void InitNextBackupMapping(Dictionary<string, DateTime?> nextBackupMapping)
        {
            nextBackupMapping.Clear();
            using var dbContext = new BackupDbContext(dbOptions);
            var infos = dbContext.BackupCollections
                .OrderBy(col => col.Title)
                .Select(col => new { col.Title, col.Started, col.AutomaticBackup })
                .ToList();
            foreach (var info in infos)
            {
                DateTime? dt = null;
                if (info.AutomaticBackup > 0 && info.Started.HasValue)
                {
                    dt = info.Started.Value.AddMinutes(info.AutomaticBackup).ToLocalTime();
                }
                nextBackupMapping[info.Title] = dt;
            }
        }

        public static BackupModel Get(string title)
        {
            using var dbContext = new BackupDbContext(dbOptions);
            var col = dbContext.BackupCollections.SingleOrDefault(col => col.Title == title);
            if (col == null)
            {
                return null;
            }
            var model = new BackupModel { Id = col.BackupCollectionId, Title = col.Title };
            model.Started = col.Started?.ToLocalTime();
            model.Finished = col.Finished?.ToLocalTime();
            model.AutomaticBackup = col.AutomaticBackup;
            model.IncludePattern = col.IncludePattern;
            model.ExcludePattern = col.ExcludePattern;
            var sourceFiles = dbContext.SourceFiles.Where(sf => sf.BackupCollectionId == model.Id);
            foreach (var sf in sourceFiles)
            {
                model.SourceFiles.Add(sf.PathName);
            }
            var destDirs = dbContext.DestinationDirectories.Where(dd => dd.BackupCollectionId == model.Id);
            foreach (var dd in destDirs)
            {
                model.DestinationDirectories.Add(dd.PathName);
                var status = new BackupStatus();
                status.Started = dd.Started;
                status.Finished = dd.Finished;
                status.Copied = dd.Copied;
                dbContext.Entry(dd)
                    .Collection(dd => dd.CopyFailures)
                    .Load();
                status.Failed = dd.CopyFailures.Count;
                model.Status[dd.PathName] = status;
            }
            return model;
        }


        public static void Add(BackupModel model)
        {
            using var dbContext = new BackupDbContext(dbOptions);
            var col = new BackupCollection { Title = model.Title };
            foreach (var fname in model.SourceFiles)
            {
                col.SourceFiles.Add(new SourceFile { PathName = fname });
            }
            foreach (var dname in model.DestinationDirectories)
            {
                col.DestinationDirectories.Add(new DestinationDirectory { PathName = dname });
            }
            dbContext.BackupCollections.Add(col);
            dbContext.SaveChanges();
            model.Id = col.BackupCollectionId;
        }

        public static DateTime? Update(BackupModel model)
        {
            DateTime? nextBackup = null;
            using var dbContext = new BackupDbContext(dbOptions);
            var col = dbContext.BackupCollections
                .SingleOrDefault(col => col.BackupCollectionId == model.Id);
            if (col == null) throw new ArgumentException($"Backup collection with ID {model.Id} not found.");
            col.Title = model.Title;
            col.AutomaticBackup = model.AutomaticBackup;
            col.IncludePattern = model.IncludePattern;
            col.ExcludePattern = model.ExcludePattern;
            dbContext.Entry(col)
                .Collection(col => col.SourceFiles)
                .Load();
            dbContext.Entry(col)
                .Collection(col => col.DestinationDirectories)
                .Load();
            var existingSourceFiles = new List<string>();
            foreach (var sf in col.SourceFiles)
            {
                existingSourceFiles.Add(sf.PathName);
            }
            var existingDestinationDirectores = new List<string>();
            foreach (var dd in col.DestinationDirectories)
            {
                existingDestinationDirectores.Add(dd.PathName);
            }
            var delDirectories = existingDestinationDirectores
                .Except(model.DestinationDirectories)
                .ToList();
            foreach (var dd in col.DestinationDirectories)
            {
                if (delDirectories.Contains(dd.PathName))
                {
                    dbContext.Entry(dd)
                        .Collection(dd => dd.CopyFailures)
                        .Load();
                    foreach (var cf in dd.CopyFailures)
                    {
                        dbContext.CopyFailures.Remove(cf);
                    }
                    dbContext.Entry(dd)
                        .Collection(dd => dd.DestinationFiles)
                        .Load();
                    foreach (var df in dd.DestinationFiles)
                    {
                        dbContext.DestinationFiles.Remove(df);
                    }
                    dbContext.DestinationDirectories.Remove(dd);
                }
            }
            var delSourceFiles = existingSourceFiles
                .Except(model.SourceFiles)
                .ToList();
            foreach (var sf in col.SourceFiles)
            {
                if (delSourceFiles.Contains(sf.PathName))
                {
                    dbContext.SourceFiles.Remove(sf);
                }
            }
            var addDirectories = model.DestinationDirectories.Except(existingDestinationDirectores).ToList();
            foreach (var dname in addDirectories)
            {
                col.DestinationDirectories.Add(new DestinationDirectory { PathName = dname });
            }
            var addSourceFiles = model.SourceFiles
                .Except(existingSourceFiles)
                .ToList();
            foreach (var fname in addSourceFiles)
            {
                col.SourceFiles.Add(new SourceFile { PathName = fname });
            }
            if (col.AutomaticBackup > 0 && col.Started.HasValue)
            {
                nextBackup = col.Started.Value.AddMinutes(col.AutomaticBackup).ToLocalTime();
            }
            dbContext.SaveChanges();
            return nextBackup;
        }

        public static void Remove(string title)
        {
            using var dbContext = new BackupDbContext(dbOptions);
            var col = dbContext.BackupCollections.SingleOrDefault(col => col.Title == title);
            if (col == null) throw new ArgumentException($"Backup collection '{title}' not found.");
            dbContext.Entry(col)
                .Collection(col => col.DestinationDirectories)
                .Load();
            foreach (var dd in col.DestinationDirectories)
            {
                dbContext.Entry(dd)
                    .Collection(dd => dd.CopyFailures)
                    .Load();
                foreach (var cf in dd.CopyFailures)
                {
                    dbContext.CopyFailures.Remove(cf);
                }
                dbContext.Entry(dd)
                    .Collection(dd => dd.DestinationFiles)
                    .Load();
                foreach (var df in dd.DestinationFiles)
                {
                    dbContext.DestinationFiles.Remove(df);
                }
                dbContext.DestinationDirectories.Remove(dd);
            }
            dbContext.Entry(col)
                .Collection(col => col.SourceFiles)
                .Load();
            foreach (var sf in col.SourceFiles)
            {
                dbContext.Remove(sf);
            }
            dbContext.BackupCollections.Remove(col);
            dbContext.SaveChanges();
        }

        public static DateTime? Backup(string title, IProgress<double> progress, CancellationToken cancellationToken)
        {
            DateTime? nextBackup = null;
            using var dbContext = new BackupDbContext(dbOptions);
            string dateFolder = DateTime.Now.ToString(DATE_FOLDER_FOMRAT);
            var col = dbContext.BackupCollections.SingleOrDefault(col => col.Title == title);
            if (col == null)
            {
                throw new ArgumentException($"Backup collection '{title}' not found.");
            }
            col.Started = DateTime.UtcNow;
            dbContext.Entry(col)
                .Collection(col => col.DestinationDirectories)
                .Load();
            dbContext.Entry(col)
                .Collection(col => col.SourceFiles)
                .Load();
            int total = col.SourceFiles.Count * col.DestinationDirectories.Count;
            int current = 0;
            foreach (var dd in col.DestinationDirectories)
            {
                if (!Directory.Exists(dd.PathName))
                {
                    current += col.SourceFiles.Count;
                    double percent = total > 0 ? current * 100.0 / total : 0.0;
                    progress?.Report(percent);
                    cancellationToken.ThrowIfCancellationRequested();
                    continue;
                }
                dbContext.Entry(dd)
                    .Collection(dd => dd.DestinationFiles)
                    .Load();
                dbContext.Entry(dd)
                    .Collection(dd => dd.CopyFailures)
                    .Load();
                dd.Started = DateTime.UtcNow;
                dd.Copied = 0;
                dd.CopyFailures.Clear();
                var baseDir = GetBaseDirectory(col.SourceFiles);
                if (!string.IsNullOrEmpty(baseDir) &&
                    !string.IsNullOrEmpty(col.BaseDirectory) &&
                    baseDir != col.BaseDirectory)
                {
                    MoveDestinationFilesToHistoryDirectory(dbContext, dd, dateFolder);
                }
                col.BaseDirectory = baseDir;
                var usedSourceFileIds = new HashSet<long>();
                foreach (var sf in col.SourceFiles)
                {
                    current++;
                    double percent = total > 0 ? current * 100.0 / total : 0.0;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (File.Exists(sf.PathName))
                    {
                        BackupToDestinationDirectory(sf, baseDir, dd, dateFolder, cancellationToken);
                        usedSourceFileIds.Add(sf.SourceFileId);
                    }
                }
                MoveDestinationFilesToHistoryDirectory(dbContext, dd, dateFolder, usedSourceFileIds);
                dd.Finished = DateTime.UtcNow;
            }
            col.Finished = DateTime.UtcNow;
            if (col.AutomaticBackup > 0)
            {
                nextBackup = col.Started.Value.AddMinutes(col.AutomaticBackup).ToLocalTime();
            }
            dbContext.SaveChanges();
            return nextBackup;
        }

        // --- private functions

        private static void MoveDestinationFilesToHistoryDirectory(
            BackupDbContext dbContext,
            DestinationDirectory dd,
            string dateFolder,
            ISet<long> usedSourceFileIds = null)
        {
            var movedFiles = new List<DestinationFile>();
            foreach (var df in dd.DestinationFiles)
            {
                if (usedSourceFileIds != null && usedSourceFileIds.Contains(df.SourceFileId))
                {
                    continue;
                }
                MoveToHistoryFile(dd.PathName, dateFolder, df.PathName);
                movedFiles.Add(df);
            }
            if (movedFiles.Any())
            {
                foreach (var df in movedFiles)
                {
                    dbContext.DestinationFiles.Remove(df);
                    dd.DestinationFiles.Remove(df);
                }
                DeleteEmptyDirectories(dd.PathName);
            }
        }

        private static void MoveToHistoryFile(string rootDirectory, string dateFolder, string sourceFileName)
        {
            if (File.Exists(sourceFileName))
            {
                string sourceDirectory = Path.GetDirectoryName(sourceFileName);
                if (sourceDirectory.StartsWith(rootDirectory))
                {
                    int startidx = rootDirectory.Length;
                    if (sourceDirectory.Length > rootDirectory.Length)
                    {
                        startidx += 1;
                    }
                    string historyContentDir = Path.Combine(
                        rootDirectory,
                        HISTORY,
                        dateFolder,
                        sourceDirectory.Substring(startidx));
                    if (!Directory.Exists(historyContentDir))
                    {
                        Directory.CreateDirectory(historyContentDir);
                    }
                    string historyPathName = GetUnique(
                        Path.Combine(historyContentDir, Path.GetFileName(sourceFileName)));
                    File.Move(sourceFileName, historyPathName);
                }
            }
        }

        private static string CopyToDestinationFile(
            string rootDirectory, string baseDirectory, string dateFolder, string sourceFileName, CancellationToken cancellationToken)
        {
            string relativDir = Path.GetDirectoryName(sourceFileName);
            if (!relativDir.StartsWith(baseDirectory))
            {
                throw new ArgumentException($"Source file '{sourceFileName}' does not start with base directory '{baseDirectory}'.");
            }
            int startidx = baseDirectory.Length;
            if (baseDirectory.Length < relativDir.Length)
            {
                startidx += 1;
            }
            relativDir = relativDir.Substring(startidx);
            string contentDir = Path.Combine(rootDirectory, relativDir);
            if (!Directory.Exists(contentDir))
            {
                Directory.CreateDirectory(contentDir);
            }
            string destinationFileName = Path.Combine(contentDir, Path.GetFileName(sourceFileName));
            if (File.Exists(destinationFileName))
            {
                MoveToHistoryFile(rootDirectory, dateFolder, destinationFileName);
            }
            try
            {
                CopyFileAsync(sourceFileName, destinationFileName, cancellationToken).Wait();
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception)
            {
                // cleanup if copy has been cancelled
                if (File.Exists(destinationFileName))
                {
                    File.Delete(destinationFileName);
                }
                throw;
            }
            return destinationFileName;
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            using Stream source = File.Open(sourcePath, FileMode.Open);
            using Stream destination = File.Create(destinationPath);
            await source.CopyToAsync(destination, cancellationToken);
        }

        private static void DeleteEmptyDirectories(string dir)
        {
            try
            {
                foreach (var subDir in Directory.EnumerateDirectories(dir))
                {
                    DeleteEmptyDirectories(subDir);
                }
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                }
            }
            catch
            {
                // @TODO: return warning message for end user
                // @TODO: new WarningMessage in BackupDbContext
            }
        }

        private static string GetBaseDirectory(List<SourceFile> sourceFiles)
        {
            string baseDir = "";
            if (sourceFiles.Count > 0)
            {
                baseDir = Path.GetDirectoryName(sourceFiles[0].PathName);
                for (int idx = 1; idx < sourceFiles.Count; idx++)
                {
                    string dirname = Path.GetDirectoryName(sourceFiles[idx].PathName);
                    while (!string.IsNullOrEmpty(dirname))
                    {
                        if (baseDir.StartsWith(dirname))
                        {
                            baseDir = dirname;
                            break;
                        }
                        dirname = Path.GetDirectoryName(dirname);
                    }
                }
            }
            return baseDir;
        }

        private static void BackupToDestinationDirectory(
            SourceFile sf, string baseDir, DestinationDirectory dd, string dateFolder, CancellationToken cancellationToken)
        {
            var lastWrittenTimeUtc = File.GetLastWriteTimeUtc(sf.PathName);
            if (sf.ContentHash == null || lastWrittenTimeUtc != sf.LastWrittenTimeUtc)
            {
                sf.ContentHash = HashCalculator.GetContentHash(sf.PathName);
                sf.LastWrittenTimeUtc = lastWrittenTimeUtc;
            }
            var df = dd.DestinationFiles.SingleOrDefault(f => f.SourceFileId == sf.SourceFileId);
            var createnew = df == null;
            if (createnew || df.ContentHash != sf.ContentHash || !File.Exists(df.PathName))
            {
                try
                {
                    string destPathName = CopyToDestinationFile(
                        dd.PathName, baseDir, dateFolder, sf.PathName, cancellationToken);
                    if (createnew)
                    {
                        df = new DestinationFile
                        {
                            SourceFileId = sf.SourceFileId,
                        };
                        dd.DestinationFiles.Add(df);
                    }
                    df.PathName = destPathName;
                    df.ContentHash = sf.ContentHash;
                    dd.Copied += 1;
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    dd.CopyFailures.Add(new CopyFailure
                    {
                        SourceFileId = sf.SourceFileId,
                        ErrorMessage = ex.Message
                    });
                }
            }
        }

        private static string GetUnique(string pathName)
        {
            var cnt = 1;
            var fname = pathName;
            while (File.Exists(fname))
            {
                fname = Path.Combine(
                    Path.GetDirectoryName(pathName),
                    $"{Path.GetFileNameWithoutExtension(pathName)}." +
                    $"{cnt}{Path.GetExtension(pathName)}");
                cnt++;
            }
            return fname;
        }
    }
}
