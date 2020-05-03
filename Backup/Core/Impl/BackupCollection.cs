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

namespace Backup.Core.Impl
{
    public class BackupCollection
    {
        public long BackupCollectionId { get; set; }

        public string Title { get; set; }

        public List<SourceFile> SourceFiles { get; set; } = new List<SourceFile>();

        public List<DestinationDirectory> DestinationDirectories { get; set; } = new List<DestinationDirectory>();

        public DateTime? Started { get; set; }

        public DateTime? Finished { get; set; }

        public string BaseDirectory { get; set; } = "";

        public string IncludePattern { get; set; } = "";

        public string ExcludePattern { get; set; } = "";

        public int AutomaticBackup { get; set; } = 0;
    }
}
