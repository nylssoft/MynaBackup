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
using System.Windows;
using System.Windows.Controls;

namespace Backup
{
    public partial class CreateBackupCollectionWindow : Window
    {
        public string CollectionName { get; private set; }

        public CreateBackupCollectionWindow(Window owner, string title, string collectionName)
        {
            Owner = owner;
            Title = title;
            InitializeComponent();
            if (!string.IsNullOrEmpty(collectionName))
            {
                textBoxName.Text = collectionName;
            }
            textBoxName.Focus();
            UpdateControls();
        }

        private void UpdateControls()
        {
            buttonOK.IsEnabled = !string.IsNullOrEmpty(textBoxName.Text);
        }

        private void TextBox_Changed(object sender, TextChangedEventArgs e)
        {
            UpdateControls();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CollectionName = textBoxName.Text;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
