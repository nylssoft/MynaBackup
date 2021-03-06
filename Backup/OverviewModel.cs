﻿/*
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
using System.ComponentModel;

namespace Backup
{
    public class OverviewModel : INotifyPropertyChanged
    {
        private string name;
        private DateTime? lastRun;
        private DateTime? nextRun;
        private int copied;
        private int failed;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public DateTime? LastRun
        {
            get
            {
                return lastRun;
            }
            set
            {
                lastRun = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastRun"));
            }
        }

        public DateTime? NextRun
        {
            get
            {
                return nextRun;
            }
            set
            {
                nextRun = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NextRun"));
            }
        }

        public int Copied
        { 
            get
            {
                return copied;
            }
            set
            {
                copied = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Copied"));
            }
        }

        public int Failed
        {
            get
            {
                return failed;
            }
            set
            {
                failed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Failed"));
            }
        }

    }
}
