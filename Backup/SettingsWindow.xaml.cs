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
    public partial class SettingsWindow : Window
    {
        private bool IsChanged { get; set; } = false;

        public SettingsWindow(Window owner, string title)
        {
            Owner = owner;
            Title = title;
            InitializeComponent();
            comboBoxLanguage.Items.Add(new ComboBoxItem() { Tag = "", Content = Properties.Resources.OPTION_SYSTEM });
            comboBoxLanguage.Items.Add(new ComboBoxItem() { Tag = "de", Content = Properties.Resources.OPTION_GERMAN });
            comboBoxLanguage.Items.Add(new ComboBoxItem() { Tag = "en", Content = Properties.Resources.OPTION_ENGLISH });
            var lang = Properties.Settings.Default.Language;
            foreach (ComboBoxItem cb in comboBoxLanguage.Items)
            {
                string language = (cb?.Tag as string) ?? "";
                if (language == lang)
                {
                    cb.IsSelected = true;
                    break;
                }
            }
            checkBoxMinimizeOnStartup.IsChecked = Properties.Settings.Default.MinimizeOnStartup;
            IsChanged = false;
            UpdateControls();
        }

        private void UpdateControls()
        {
            buttonOK.IsEnabled = IsChanged; 
            if (IsChanged)
            {
                labelRestartRequired.Content = Properties.Resources.TEXT_RESTART_REQUIRED;
                labelRestartRequired.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                labelRestartRequired.Content = "";
            }
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = true;
                if (IsChanged)
                {
                    var cb = comboBoxLanguage.SelectedItem as ComboBoxItem;
                    var language = (cb?.Tag as string) ?? "";
                    Properties.Settings.Default.Language = language;
                    Properties.Settings.Default.MinimizeOnStartup = checkBoxMinimizeOnStartup.IsChecked == true;
                    Properties.Settings.Default.Save();
                }
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBoxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsChanged = true;
            UpdateControls();
        }

        private void CheckBoxMinimizeOnStartup_Changed(object sender, RoutedEventArgs e)
        {
            IsChanged = true;
            UpdateControls();
        }
    }
}
