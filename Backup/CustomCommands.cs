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
using System.Windows.Input;

namespace Backup
{
    public class CustomCommands
    {
        public static readonly RoutedUICommand ChangeSettings =
            new RoutedUICommand(
                Properties.Resources.CMD_SETTINGS,
                "ChangeSettings",
                typeof(CustomCommands));

        public static readonly RoutedUICommand CreateBackupCollection =
            new RoutedUICommand(
            Properties.Resources.CMD_CREATE_BACKUP_COLLECTION,
            "CreateBackupCollection",
            typeof(CustomCommands));

        public static readonly RoutedUICommand RenameBackupCollection =
            new RoutedUICommand(
            Properties.Resources.CMD_RENAME_BACKUP_COLLECTION,
            "RenameBackupCollection",
            typeof(CustomCommands));

        public static readonly RoutedUICommand DeleteBackupCollection =
            new RoutedUICommand(
            Properties.Resources.CMD_DELETE_BACKUP_COLLECTION,
            "DeleteBackupCollection",
            typeof(CustomCommands));

        public static readonly RoutedUICommand ShowOverview =
            new RoutedUICommand(
                Properties.Resources.CMD_SHOW_OVERVIEW,
                "ShowOverview",
                typeof(CustomCommands));

        public static readonly RoutedUICommand Refresh =
            new RoutedUICommand(
            Properties.Resources.CMD_REFRESH,
            "Refresh",
            typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.F5) });

        public static readonly RoutedUICommand AddSourceFile =
            new RoutedUICommand(
            Properties.Resources.CMD_ADD_SOURCE_FILE,
            "AddSourceFile",
            typeof(CustomCommands));

        public static readonly RoutedUICommand RemoveSourceFile =
            new RoutedUICommand(
            Properties.Resources.CMD_REMOVE_SOURCE_FILE,
            "RemoveSourceFile",
            typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Delete) });

        public static readonly RoutedUICommand AddSourceDirectory =
            new RoutedUICommand(
            Properties.Resources.CMD_ADD_SOURCE_DIRECTORY,
            "AddSourceDirectory",
            typeof(CustomCommands));

        public static readonly RoutedUICommand AddDestinationDirectory =
            new RoutedUICommand(
            Properties.Resources.CMD_ADD_DESTINATION_DIRECTORY,
            "AddDestinationDirectory",
            typeof(CustomCommands));

        public static readonly RoutedUICommand RemoveDestinationDirectory =
            new RoutedUICommand(
            Properties.Resources.CMD_REMOVE_DESTINATION_DIRECTORY,
            "RemoveDestinationDirectory",
            typeof(CustomCommands));

        public static readonly RoutedUICommand Backup =
            new RoutedUICommand(
            Properties.Resources.CMD_BACKUP,
            "Backup",
            typeof(CustomCommands));

        public static readonly RoutedUICommand Exit =
            new RoutedUICommand(
            Properties.Resources.CMD_EXIT,
            "Exit",
            typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });

        public static readonly RoutedUICommand About =
            new RoutedUICommand(
            Properties.Resources.CMD_ABOUT,
            "About",
            typeof(CustomCommands));

    }
}
