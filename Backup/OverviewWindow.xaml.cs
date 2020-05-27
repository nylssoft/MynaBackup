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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Backup
{
    public partial class OverviewWindow : Window
    {
        private readonly SortDecorator sortDecorator = new SortDecorator(ListSortDirection.Ascending);

        public Action<string> BackupSelectionChanged { get; set; } = null;

        public bool IsClosed { get; set; } = false;

        public OverviewWindow(Window owner, string title, ObservableCollection<OverviewModel> overviews)
        {
            InitializeComponent();
            Owner = owner;
            Title = title;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            IsClosed = false;
            listViewOverviews.ItemsSource = overviews;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var viewlist = (CollectionView)CollectionViewSource.GetDefaultView(listViewOverviews.ItemsSource);
            viewlist.SortDescriptions.Clear();
            viewlist.SortDescriptions.Add(new SortDescription("Name", sortDecorator.Direction));
            sortDecorator.Click(gridViewColumnName);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
        }

        private void ListViewBackups_ColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            var column = (sender as GridViewColumnHeader);
            if (column == null || column.Tag == null) return;
            sortDecorator.Click(column);
            string sortBy = column.Tag.ToString();
            var viewlist = (CollectionView)CollectionViewSource.GetDefaultView(listViewOverviews.ItemsSource);
            viewlist.SortDescriptions.Clear();
            viewlist.SortDescriptions.Add(new SortDescription(sortBy, sortDecorator.Direction));
        }

        private void ListViewOverviews_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(listViewOverviews);
            var lvitem = listViewOverviews.GetItemAt(mousePosition);
            if (lvitem != null)
            {
                var overviewModel = lvitem.Content as OverviewModel;
                if (overviewModel != null)
                {
                    BackupSelectionChanged?.Invoke(overviewModel.Name);
                }
            }
        }
    }
}
