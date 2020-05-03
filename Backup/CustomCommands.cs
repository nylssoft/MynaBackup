/*
    Myna Bank
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
        public static readonly RoutedUICommand CreateBackupCollection =
            new RoutedUICommand(
            "Neue Sicherung _anlegen...",
            "CreateBackupCollection",
            typeof(CustomCommands));

        public static readonly RoutedUICommand RenameBackupCollection =
            new RoutedUICommand(
            "Sicherung _umbenennen...",
            "RenameBackupCollection",
            typeof(CustomCommands));

        public static readonly RoutedUICommand DeleteBackupCollection =
            new RoutedUICommand(
            "Sicherung _löschen",
            "DeleteBackupCollection",
            typeof(CustomCommands));

        public static readonly RoutedUICommand Refresh =
            new RoutedUICommand(
            "_Aktualisieren",
            "Refresh",
            typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.F5) });

        public static readonly RoutedUICommand AddSourceFile =
            new RoutedUICommand(
            "Quelldatei _hinzufügen...",
            "AddSourceFile",
            typeof(CustomCommands));

        public static readonly RoutedUICommand RemoveSourceFile =
            new RoutedUICommand(
            "Quelldatei _entfernen",
            "RemoveSourceFile",
            typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.Delete) });

        public static readonly RoutedUICommand AddSourceDirectory =
            new RoutedUICommand(
            "Quell_verzeichnis hinzufügen...",
            "AddSourceDirectory",
            typeof(CustomCommands));

        public static readonly RoutedUICommand AddDestinationDirectory =
            new RoutedUICommand(
            "_Zielverzeichnis hinzufügen...",
            "AddDestinationDirectory",
            typeof(CustomCommands));

        public static readonly RoutedUICommand RemoveDestinationDirectory =
            new RoutedUICommand(
            "Zielverzeichnis e_ntfernen",
            "RemoveDestinationDirectory",
            typeof(CustomCommands));

        public static readonly RoutedUICommand Backup =
            new RoutedUICommand(
            "Sicherung _starten",
            "Backup",
            typeof(CustomCommands));

        public static readonly RoutedUICommand Exit =
            new RoutedUICommand(
            "_Beenden",
            "Exit",
            typeof(CustomCommands),
            new InputGestureCollection() { new KeyGesture(Key.F4, ModifierKeys.Alt) });


        public static readonly RoutedUICommand About =
            new RoutedUICommand(
            "Ü_ber",
            "About",
            typeof(CustomCommands));


    }
}
