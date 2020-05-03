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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Backup.Core
{
    public static class DirectoryInfoExtensions
    {
        public static List<(string,long,DateTime?)> GetAllFiles(this DirectoryInfo dirInfo, CancellationToken? cancellationToken)
        {
            var ret = new List<(string, long,DateTime?)>();
            var dirqueue = new Queue<string>();
            dirqueue.Enqueue(dirInfo.FullName);
            while (dirqueue.Count > 0)
            {
                var d = dirqueue.Dequeue();
                foreach (var fileName in Directory.EnumerateFileSystemEntries(d))
                {
                    cancellationToken?.ThrowIfCancellationRequested();
                    if (Directory.Exists(fileName))
                    {
                        dirqueue.Enqueue(fileName);
                    }
                    else
                    {
                        FileInfo fi = new FileInfo(fileName);
                        ret.Add((fileName, fi.Length, File.GetLastWriteTime(fileName)));
                    }
                }
            }
            return ret;
        }
    }
}
