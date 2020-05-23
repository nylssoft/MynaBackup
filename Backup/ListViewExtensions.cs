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
using System.Windows.Media;

namespace Backup
{
    public static class ListViewExtensions
    {
        public static ListViewItem GetItemAt(this ListView listView, Point clientRelativePosition)
        {
            var hitTestResult = VisualTreeHelper.HitTest(listView, clientRelativePosition);
            var selectedItem = hitTestResult.VisualHit;
            while (selectedItem != null)
            {
                if (selectedItem is ListViewItem)
                {
                    break;
                }
                selectedItem = VisualTreeHelper.GetParent(selectedItem);
            }
            return selectedItem != null ? ((ListViewItem)selectedItem) : null;
        }

        public static bool FocusItem(this ListView listView, int idx)
        {
            Object lvi = listView.ItemContainerGenerator.ContainerFromIndex(idx);
            if (lvi == null)
            {
                listView.UpdateLayout();
                lvi = listView.ItemContainerGenerator.ContainerFromIndex(idx);
            }
            if (lvi is ListViewItem)
            {
                return (lvi as ListViewItem).Focus();
            }
            return false;
        }
    }
}
