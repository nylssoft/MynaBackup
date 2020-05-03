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

namespace Backup.Core
{
    public static class StringExtensions
    {
        public static string ReplaceSpecialFolder(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                if (str.Contains("%MyDocuments%"))
                {
                    str = str.Replace("%MyDocuments%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                }
                if (str.Contains("%ProgramData%"))
                {
                    str = str.Replace("%ProgramData%", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
                }
                if (str.Contains("%Module%"))
                {
                    string moddir = AppDomain.CurrentDomain.BaseDirectory;
                    if (moddir.EndsWith("\\"))
                    {
                        moddir = moddir.Substring(0, moddir.Length - 1);
                    }
                    str = str.Replace("%Module%", moddir);
                }
            }
            return str;
        }
    }
}
