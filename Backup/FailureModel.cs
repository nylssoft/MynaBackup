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
using System.ComponentModel;

namespace Backup
{
    public class FailureModel : INotifyPropertyChanged
    {
        private string sourceFilePath;
        private string errorMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public string SourceFilePath
        {
            get
            {
                return sourceFilePath;
            }
            set
            {
                sourceFilePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceFilePath"));
            }
        }

        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
            set
            {
                errorMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ErrorMessage"));
            }
        }
    }
}
