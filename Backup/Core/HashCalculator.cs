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
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Backup.Core
{
    public class HashCalculator
    {
        public static string GetContentHash(string fileName)
        {
            using var sha256Hash = SHA256.Create();
            using var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read);
            byte[] hashValue = sha256Hash.ComputeHash(fileStream);
            var sb = new StringBuilder();
            for (int i = 0; i < hashValue.Length; i++)
            {
                sb.Append(hashValue[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static Dictionary<string, string> GetContentHashes(List<string> fileNames, CancellationToken cancellationToken, IProgress<int> progress)
        {
            var ret = new Dictionary<string, string>();
            var cnt = 0;
            foreach (var fileName in fileNames)
            {
                cnt++;
                progress?.Report(cnt * 100 / fileNames.Count);
                cancellationToken.ThrowIfCancellationRequested();
                ret[fileName] = GetContentHash(fileName);
            }
            return ret;
        }
    }
}
