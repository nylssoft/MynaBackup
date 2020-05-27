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
using Backup.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Backup
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<SourceFileModel> sourceFiles = new ObservableCollection<SourceFileModel>();

        private readonly ObservableCollection<DestDirModel> destDirectories = new ObservableCollection<DestDirModel>();

        private readonly SortDecorator sortSourceFilesDecorator = new SortDecorator(ListSortDirection.Descending);

        private readonly SortDecorator sortDestDirectoriesDecorator = new SortDecorator(ListSortDirection.Descending);

        private readonly Dictionary<string, DateTime?> nextBackupMapping = new Dictionary<string, DateTime?>();

        private OverviewWindow overviewWindow = null;

        private readonly ObservableCollection<OverviewModel> overviews = new ObservableCollection<OverviewModel>();

        private FailureWindow failureWindow = null;

        private readonly ObservableCollection<FailureModel> failures = new ObservableCollection<FailureModel>();

        private bool IsTaskRunning { get; set; } = false;

        private bool IsIncludePatternChanged { get; set; } = false;

        private bool IsExcludePatternChanged { get; set; } = false;

        private bool IsTimerProcessing { get; set; } = false;

        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (IsTaskRunning || IsTimerProcessing) return;
            IsTimerProcessing = true;
            var collectionNames = new List<string>(nextBackupMapping.Keys);
            foreach (var collectionName in collectionNames)
            {
                var next = nextBackupMapping[collectionName];
                if (!next.HasValue)
                {
                    continue;
                }
                var diff = DateTime.Now - next.Value;
                if (diff.TotalSeconds > 0)
                {
                    IsTaskRunning = true;
                    cts = new CancellationTokenSource();
                    SetProgress(string.Format(Properties.Resources.TEXT_AUTOMATIC_BACKUP_0, collectionName));
                    UpdateControls();
                    CommandManager.InvalidateRequerySuggested();
                    try
                    {
                        var model = await Task.Run(() => BackupManager.Get(collectionName));
                        await UpdateSourceFilesFromSourceDirectory(model);
                        var progress = new Progress<double>((percent) => UpdateProgress(percent));
                        next = await Task.Run(() => BackupManager.Backup(model, progress, cts.Token));
                        nextBackupMapping[collectionName] = next;
                        UpdateOverview(collectionName, DateTime.Now, next, model);
                        if (collectionName == comboBox.SelectedItem as string)
                        {
                            await InitBackupCollection(collectionName);
                        }
                        SetProgress();
                    }
                    catch (Exception ex)
                    {
                        if (!cts.IsCancellationRequested)
                        {
                            SetProgress(ex.Message);
                            // avoid recursive calls, retry after 1 hour
                            nextBackupMapping[collectionName] = DateTime.Now.AddHours(1);
                        }
                        break;
                    }
                }
            }
            IsTimerProcessing = false;
            if (IsTaskRunning)
            {
                IsTaskRunning = false;
                UpdateControls();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ListViewSourceFiles_ColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            var column = (sender as GridViewColumnHeader);
            if (column == null || column.Tag == null) return;
            sortSourceFilesDecorator.Click(column);
            string sortBy = column.Tag.ToString();
            var viewlist = (CollectionView)CollectionViewSource.GetDefaultView(listViewSourceFiles.ItemsSource);
            viewlist.SortDescriptions.Clear();
            viewlist.SortDescriptions.Add(new SortDescription(sortBy, sortSourceFilesDecorator.Direction));
        }

        private void ListViewDirectories_ColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            var column = (sender as GridViewColumnHeader);
            if (column == null || column.Tag == null) return;
            sortDestDirectoriesDecorator.Click(column);
            string sortBy = column.Tag.ToString();
            var viewlist = (CollectionView)CollectionViewSource.GetDefaultView(listViewDirectories.ItemsSource);
            viewlist.SortDescriptions.Clear();
            viewlist.SortDescriptions.Add(new SortDescription(sortBy, sortDestDirectoriesDecorator.Direction));
        }

        private async void ListViewDirectories_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_LOADING_COPY_ERRORS);
                UpdateControls();
                var mousePosition = e.GetPosition(listViewDirectories);
                var lvitem = listViewDirectories.GetItemAt(mousePosition);
                if (lvitem != null)
                {
                    var destDirModel = lvitem.Content as DestDirModel;
                    var backupFailures = await Task.Run(() => BackupManager.GetFailures(name, destDirModel.Name));
                    failures.Clear();
                    foreach (var bf in backupFailures)
                    {
                        failures.Add(new FailureModel { SourceFilePath = bf.SourceFilePath, ErrorMessage = bf.ErrorMessage });
                    }
                    if (failureWindow == null || failureWindow.IsClosed)
                    {
                        failureWindow = new FailureWindow(null,
                            Properties.Resources.TITLE_COPY_ERRORS,
                            name,
                            destDirModel.Name,
                            failures);
                        failureWindow.Show();
                    }
                    else
                    {
                        failureWindow.textBlockBackup.Text = name;
                        failureWindow.textBlockDestinationDirectory.Text = destDirModel.Name;
                        failureWindow.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            IsTaskRunning = false;
            SetProgress();
            UpdateControls();
            CommandManager.InvalidateRequerySuggested();
        }

        private async void ComboBoxBackupCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (comboBox.SelectedIndex >= 0)
                {
                    Properties.Settings.Default.LastUsedBackup = comboBox.SelectedIndex;
                }
                var name = comboBox.SelectedItem as string;
                await InitBackupCollection(name, true);
                if (!sortSourceFilesDecorator.HasAdorner)
                {
                    var viewlist = (CollectionView)CollectionViewSource.GetDefaultView(listViewSourceFiles.ItemsSource);
                    viewlist.SortDescriptions.Clear();
                    viewlist.SortDescriptions.Add(new SortDescription("ModifiedDate", sortSourceFilesDecorator.Direction));
                    sortSourceFilesDecorator.Click(gridViewColumModifiedDate);
                }
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private void RadioButtonAutomaticBackup_Checked(object sender, RoutedEventArgs e)
        {
            if (IsTaskRunning || comboBox.SelectedItem == null) return;
            try
            {
                var name = comboBox.SelectedItem as string;
                var model = BackupManager.Get(name);
                if (radioButtonNever.IsChecked == true)
                {
                    model.AutomaticBackup = 0;
                }
                else if (radioButtonHour.IsChecked == true)
                {
                    model.AutomaticBackup = 60;
                }
                else if (radioButtonDay.IsChecked == true)
                {
                    model.AutomaticBackup = 60 * 24;
                }
                var next = BackupManager.Update(model);
                nextBackupMapping[name] = next;
                if (next.HasValue)
                {
                    textBlockBackupNextStart.Text = next.Value.ToString();
                }
                else
                {
                    textBlockBackupNextStart.Text = "-";
                }
                UpdateOverview(name, model.Finished, next);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateControls();
        }

        private void Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            RoutedUICommand r = e.Command as RoutedUICommand;
            if (r == null) return;
            string name = comboBox?.SelectedItem as string;
            switch (r.Name)
            {
                case "CreateBackupCollection":
                case "ChangeSettings":
                case "About":
                case "Exit":
                    e.CanExecute = !IsTaskRunning;
                    break;
                case "DeleteBackupCollection":
                case "RenameBackupCollection":
                case "AddDestinationDirectory":
                    e.CanExecute = name != null && !IsTaskRunning;
                    break;
                case "AddSourceFile":
                case "AddSourceDirectory":
                    e.CanExecute = name != null && !IsTaskRunning && textBlockSourceDirectory.Text.Length == 0;
                    break;
                case "RemoveSourceFile":
                    e.CanExecute = name != null && !IsTaskRunning && listViewSourceFiles.SelectedItem != null && textBlockSourceDirectory.Text.Length == 0;
                    break;
                case "RemoveDestinationDirectory":
                    e.CanExecute = name != null && !IsTaskRunning && listViewDirectories.SelectedItem != null;
                    break;
                case "ShowOverview":
                    e.CanExecute = !IsTaskRunning && (overviewWindow == null || overviewWindow.IsClosed);
                    break;
                case "Refresh":
                    e.CanExecute = !IsTaskRunning && name != null && sourceFiles.Count > 0;
                    break;
                case "Backup":
                    e.CanExecute = !IsTaskRunning && name != null && sourceFiles.Count > 0 && destDirectories.Count > 0;
                    break;
                default:
                    break;
            }
        }

        private void Command_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RoutedUICommand r = e.Command as RoutedUICommand;
            if (r == null) return;
            switch (r.Name)
            {
                case "Exit":
                    Close();
                    break;
                case "ChangeSettings":
                    ChangeSettingsCmd();
                    break;
                case "CreateBackupCollection":
                    CreateBackupCollectionCmd();
                    break;
                case "DeleteBackupCollection":
                    DeleteBackupCollectionCmd();
                    break;
                case "RenameBackupCollection":
                    RenameBackupCollectionCmd();
                    break;
                case "AddSourceFile":
                    AddSourceFileCmd();
                    break;
                case "RemoveSourceFile":
                    RemoveSourceFileCmd();
                    break;
                case "AddSourceDirectory":
                    AddSourceDirectoryCmd();
                    break;
                case "AddDestinationDirectory":
                    AddDestinationDirectoryCmd();
                    break;
                case "RemoveDestinationDirectory":
                    RemoveDestinationDirectoryCmd();
                    break;
                case "ShowOverview":
                    ShowOverviewCmd();
                    break;
                case "Refresh":
                    RefreshCmd();
                    break;
                case "Backup":
                    BackupCmd();
                    break;
                case "About":
                    AboutCmd();
                    break;
                default:
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Init();
            }
            catch (Exception ex)
            {
                HandleError(ex);
                Close();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (IsTaskRunning)
                {
                    e.Cancel = true;
                    return;
                }
                if (overviewWindow != null && !overviewWindow.IsClosed)
                {
                    overviewWindow.Close();
                    overviewWindow = null;
                }
                if (failureWindow != null && !failureWindow.IsClosed)
                {
                    failureWindow.Close();
                    failureWindow = null;
                }
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                HandleError(ex);
                e.Cancel = true;
            }
        }

        private void TextBoxIncludePattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsTaskRunning) return;
            IsIncludePatternChanged = true;
            UpdateControls();
        }

        private void TextBoxExcludePattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsTaskRunning) return;
            IsExcludePatternChanged = true;
            UpdateControls();
        }

        private async void ButtonApplyIncludePattern_Click(object sender, RoutedEventArgs e)
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_FILTERING_SOURCE_FILES);
                UpdateControls();
                var model = await Task.Run(()=>BackupManager.Get(name));
                var regex = GetIncludePatternRegex(model);
                if (regex != null)
                {
                    await FilterSourceFileModels(name, regex, null);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void ButtonApplyExcludePattern_Click(object sender, RoutedEventArgs e)
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_FILTERING_SOURCE_FILES);
                UpdateControls();
                var model = await Task.Run(() => BackupManager.Get(name));
                var regex = GetExcludePatternRegex(model);
                if (regex != null)
                {
                    await FilterSourceFileModels(name, null, regex);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void ButtonClearSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_UPDATE_SOURCE_FILES_FROM_DIRECTORY);
                UpdateControls();
                var model = await Task.Run(() => BackupManager.Get(name));
                model.SourceDirectory = "";
                model.SourceFiles.Clear();
                await Task.Run(() => BackupManager.Update(model));
                await InitBackupCollection(name);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void ButtonSelectSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog
                {
                    UseDescriptionForTitle = true,
                    Description = Properties.Resources.TITLE_SELECT_SOURCE_DIRECTORY
                };
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    IsTaskRunning = true;
                    SetProgress(Properties.Resources.TEXT_UPDATE_SOURCE_FILES_FROM_DIRECTORY);
                    UpdateControls();
                    var model = await Task.Run(() => BackupManager.Get(name));
                    model.SourceDirectory = dlg.SelectedPath;
                    await Task.Run(() => BackupManager.Update(model));
                    await InitBackupCollection(name, true);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private void ButtonCancelProgress_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            UpdateControls();
        }

        // --- helper methods

        private void SetProgress(string txt = "")
        {
            textBlockProgress.Text = txt;
            progressBar.Visibility = Visibility.Hidden;
            progressTextBlock.Visibility = Visibility.Hidden;
            buttonCancelProgress.Visibility = Visibility.Hidden;
        }

        private void UpdateProgress(double percent)
        {
            progressBar.Visibility = Visibility.Visible;
            progressTextBlock.Visibility = Visibility.Visible;
            buttonCancelProgress.Visibility = Visibility.Visible;
            progressBar.Value = percent;
        }

        private void HandleError(Exception ex)
        {
            MessageBox.Show(
                this,
                string.Format(Properties.Resources.TEXT_ERROR_OCCURRED_0, ex.Message),
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void Init()
        {
            string filename = Properties.Settings.Default.DatabaseFile.ReplaceSpecialFolder();
            var di = new FileInfo(filename).Directory;
            if (!di.Exists)
            {
                Directory.CreateDirectory(di.FullName);
            }
            BackupManager.Init(filename);
            BackupManager.InitNextBackupMapping(nextBackupMapping);
            foreach (var name in BackupManager.GetAll())
            {
                comboBox.Items.Add(name);
            }
            if (comboBox.Items.Count > 0)
            {
                Properties.Settings.Default.LastUsedBackup = Math.Min(Properties.Settings.Default.LastUsedBackup, comboBox.Items.Count - 1);
                comboBox.SelectedIndex = Properties.Settings.Default.LastUsedBackup;
            }
            textBlockBackupNextStart.Text = "-";
            var timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            UpdateControls();
            if (Properties.Settings.Default.MinimizeOnStartup)
            {
                WindowState = WindowState.Minimized;
            }
        }

        private void UpdateControls()
        {
            comboBox.IsEnabled = !IsTaskRunning;
            radioButtonNever.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null;
            radioButtonHour.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null;
            radioButtonDay.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null;
            textBoxIncludePattern.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null;
            textBoxExcludePattern.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null;
            buttonApplyIncludePattern.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null && IsIncludePatternChanged;
            buttonApplyExcludePattern.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null && IsExcludePatternChanged;
            buttonClearSourceDirectory.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null && textBlockSourceDirectory.Text.Length > 0;
            buttonSelectSourceDirectory.IsEnabled = !IsTaskRunning && comboBox.SelectedItem != null && listViewSourceFiles.Items.Count == 0;
            buttonCancelProgress.IsEnabled = IsTaskRunning && cts != null && !cts.IsCancellationRequested;
            long totalFileSize = 0;
            if (listViewSourceFiles.SelectedItems.Count > 0)
            {
                foreach (SourceFileModel sf in listViewSourceFiles.SelectedItems)
                {
                    totalFileSize += sf.Size;
                }
                if (listViewSourceFiles.SelectedItems.Count == 1)
                {
                    labelSourceFilesInfo.Content = string.Format(
                        Properties.Resources.TEXT_SOURCE_FILE_SELECTED_0_1_2,
                        listViewSourceFiles.SelectedItems.Count,
                        sourceFiles.Count,
                        FormatFileSize(totalFileSize));
                }
                else if (listViewSourceFiles.SelectedItems.Count > 1)
                {
                    labelSourceFilesInfo.Content = string.Format(
                        Properties.Resources.TEXT_SOURCE_FILES_SELECTED_0_1_2,
                        listViewSourceFiles.SelectedItems.Count,
                        sourceFiles.Count,
                        FormatFileSize(totalFileSize));
                }
            }
            else
            {
                foreach (SourceFileModel sf in listViewSourceFiles.Items)
                {
                    totalFileSize += sf.Size;
                }
                if (sourceFiles.Count == 1)
                {
                    labelSourceFilesInfo.Content = string.Format(
                        Properties.Resources.TEXT_SOURCE_FILE_0_1,
                        sourceFiles.Count,
                        FormatFileSize(totalFileSize));
                }
                else
                {
                    labelSourceFilesInfo.Content = string.Format(
                        Properties.Resources.TEXT_SOURCE_FILES_0_1,
                        sourceFiles.Count,
                        FormatFileSize(totalFileSize));
                }
            }
            if (listViewDirectories.SelectedItems.Count == 1)
            {
                labelDirectoriesInfo.Content = string.Format(Properties.Resources.TEXT_DESTINATION_DIRECTORY_SELECTED_0_1,
                    listViewDirectories.SelectedItems.Count, destDirectories.Count);
            }
            else if (listViewDirectories.SelectedItems.Count > 1)
            {
                labelDirectoriesInfo.Content = string.Format(Properties.Resources.TEXT_DESTINATION_DIRECTORIES_SELECTED_0_1,
                    listViewDirectories.SelectedItems.Count, destDirectories.Count);
            }
            else
            {
                if (destDirectories.Count == 1)
                {
                    labelDirectoriesInfo.Content = string.Format(Properties.Resources.TEXT_DESTINATION_DIRECTORY_0,
                        destDirectories.Count);
                }
                else
                {
                    labelDirectoriesInfo.Content = string.Format(Properties.Resources.TEXT_DESTINATION_DIRECTORIES_0,
                        destDirectories.Count);
                }
            }
        }

        private async Task InitBackupCollection(string name, bool updateSourceFiles = false)
        {
            listViewSourceFiles.ItemsSource = null;
            listViewDirectories.ItemsSource = null;
            try
            {
                IsTaskRunning = true;
                IsExcludePatternChanged = false;
                IsIncludePatternChanged = false;
                SetProgress(Properties.Resources.TEXT_LOAD_BACKUP_COLLECTION);
                sourceFiles.Clear();
                destDirectories.Clear();
                textBlockBackupStarted.Text = "-";
                textBlockBackupFinished.Text = "-";
                textBlockBackupNextStart.Text = "-";
                textBoxExcludePattern.Text = "";
                textBoxIncludePattern.Text = "";
                textBlockSourceDirectory.Text = "";
                UpdateControls();
                if (name != null)
                {
                    var model = await Task.Run(() => BackupManager.Get(name));
                    if (updateSourceFiles)
                    {
                        SetProgress(Properties.Resources.TEXT_UPDATE_SOURCE_FILES_FROM_DIRECTORY);
                        await UpdateSourceFilesFromSourceDirectory(model);
                    }
                    if (model.Started != null)
                    {
                        textBlockBackupStarted.Text = model.Started.Value.ToString();
                    }
                    if (model.Finished != null)
                    {
                        textBlockBackupFinished.Text = model.Finished.Value.ToString();
                    }
                    if (nextBackupMapping[name].HasValue)
                    {
                        textBlockBackupNextStart.Text = nextBackupMapping[name].ToString();
                    }
                    if (model.AutomaticBackup == 0)
                    {
                        radioButtonNever.IsChecked = true;
                    }
                    else if (model.AutomaticBackup <= 60)
                    {
                        radioButtonHour.IsChecked = true;
                    }
                    else
                    {
                        radioButtonDay.IsChecked = true;
                    }
                    textBlockSourceDirectory.Text = model.SourceDirectory;
                    if (model.ExcludePattern != null)
                    {
                        textBoxExcludePattern.Text = model.ExcludePattern;
                    }
                    if (model.IncludePattern != null)
                    {
                        textBoxIncludePattern.Text = model.IncludePattern;
                    }
                    foreach (var pathName in model.SourceFiles)
                    {
                        var sfmodel = new SourceFileModel
                        {
                            Name = pathName,
                        };
                        if (File.Exists(pathName))
                        {
                            sfmodel.Size = new FileInfo(pathName).Length;
                            sfmodel.ModifiedDate = File.GetLastWriteTime(pathName);
                        }
                        sourceFiles.Add(sfmodel);
                    }
                    foreach (var pathName in model.DestinationDirectories)
                    {
                        var status = model.Status[pathName];
                        var ddmodel = new DestDirModel
                        {
                            Name = pathName,
                            Started = status.Started?.ToLocalTime(),
                            Finished = status.Finished?.ToLocalTime(),
                            Copied = status.Copied,
                            Failed = status.Failed
                        };
                        destDirectories.Add(ddmodel);
                    }
                    UpdateOverview(name, model.Finished, nextBackupMapping[name], model);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            listViewSourceFiles.ItemsSource = sourceFiles;
            listViewDirectories.ItemsSource = destDirectories;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private void UpdateOverview(string collectionName, DateTime? lastRun, DateTime? nextRun, BackupModel model = null, bool remove = false)
        {
            bool found = false;
            foreach (var overview in overviews)
            {
                if (overview.Name == collectionName)
                {
                    found = true;
                    overview.LastRun = lastRun;
                    overview.NextRun = nextRun;
                    if (model != null)
                    {
                        overview.Copied = model.Copied;
                        overview.Failed = model.Failed;
                    }
                    if (remove)
                    {
                        overviews.Remove(overview);
                    }
                    break;
                }
            }
            if (!found)
            {
                overviews.Add(new OverviewModel { Name = collectionName, LastRun = lastRun, NextRun = nextRun });
            }
        }

        private bool AddDestinationDirectory(string name, string path)
        {
            var model = BackupManager.Get(name);
            if (!model.DestinationDirectories.Contains(path))
            {
                model.DestinationDirectories.Add(path);
                BackupManager.Update(model);
                return true;
            }
            return false;
        }

        private void RemoveDestinationDirectories(string name, List<DestDirModel> del)
        {
            var model = BackupManager.Get(name);
            foreach (var d in del)
            {
                model.DestinationDirectories.Remove(d.Name);
            }
            BackupManager.Update(model);
        }

        private List<SourceFileModel> AddBackupFiles(BackupModel backupModel, List<SourceFileModel> add)
        {
            List<SourceFileModel> added = new List<SourceFileModel>();
            foreach (var sf in add)
            {
                if (!backupModel.SourceFiles.Contains(sf.Name))
                {
                    backupModel.SourceFiles.Add(sf.Name);
                    added.Add(sf);
                }
            }
            BackupManager.Update(backupModel);
            return added;
        }

        private void RemoveBackupFiles(string name, List<SourceFileModel> del)
        {
            var model = BackupManager.Get(name);
            foreach (var d in del)
            {
                model.SourceFiles.Remove(d.Name);
            }
            BackupManager.Update(model);
        }

        private Regex GetIncludePatternRegex(BackupModel backupModel)
        {
            Regex regex = null;
            if (textBoxIncludePattern.Text.Trim().Length > 0)
            {
                regex = new Regex(textBoxIncludePattern.Text, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            if (IsIncludePatternChanged)
            {
                backupModel.IncludePattern = textBoxIncludePattern.Text;
                BackupManager.Update(backupModel);
                IsIncludePatternChanged = false;
                UpdateControls();
            }
            return regex;
        }

        private Regex GetExcludePatternRegex(BackupModel backupModel)
        {
            Regex regex = null;
            if (textBoxExcludePattern.Text.Trim().Length > 0)
            {
                regex = new Regex(textBoxExcludePattern.Text, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            if (IsExcludePatternChanged)
            {
                backupModel.ExcludePattern = textBoxExcludePattern.Text;
                BackupManager.Update(backupModel);
                IsExcludePatternChanged = false;
                UpdateControls();
            }
            return regex;
        }

        private async Task FilterSourceFileModels(string name, Regex includeRegex, Regex excludeRegex)
        {
            var del = new List<SourceFileModel>();
            foreach (SourceFileModel sf in sourceFiles)
            {
                if (includeRegex != null && !includeRegex.IsMatch(sf.Name) ||
                    excludeRegex != null && excludeRegex.IsMatch(sf.Name) )
                {
                    del.Add(sf);
                }
            }
            await DeleteSourceFileModels(name, del);
        }

        private async Task DeleteSourceFileModels(string name, List<SourceFileModel> del)
        {
            if (del.Count > 0)
            {
                await Task.Run(() => RemoveBackupFiles(name, del));
                var idx = listViewSourceFiles.SelectedIndex;
                listViewSourceFiles.ItemsSource = null;
                foreach (var d in del)
                {
                    sourceFiles.Remove(d);
                }
                listViewSourceFiles.ItemsSource = sourceFiles;
                idx = Math.Min(idx, listViewSourceFiles.Items.Count - 1);
                if (idx >= 0)
                {
                    listViewSourceFiles.SelectedIndex = idx;
                    listViewSourceFiles.FocusItem(idx);
                }
            }
        }

        private static string FormatFileSize(long fileSize)
        {
            double f = fileSize;
            if (f < 1024)
            {
                return $"{f:F0} bytes";
            }
            f /= 1024;
            if (f < 1024)
            {
                return $"{f:F1} KB";
            }
            f /= 1024;
            if (f < 1024)
            {
                return $"{f:F2} MB";
            }
            f /= 1024;
            return $"{f:F2} GB";
        }

        private async Task UpdateSourceFilesFromSourceDirectory(BackupModel model)
        {
            if (!string.IsNullOrEmpty(model.SourceDirectory))
            {
                model.SourceFiles.Clear();
                Regex regexInclude = null;
                if (!string.IsNullOrEmpty(model.IncludePattern))
                {
                    regexInclude = new Regex(model.IncludePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                Regex regexExclude = null;
                if (!string.IsNullOrEmpty(model.ExcludePattern))
                {
                    regexExclude = new Regex(model.ExcludePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                var ret = await Task.Run(() => new DirectoryInfo(model.SourceDirectory).GetAllFiles(null));
                foreach (var f in ret)
                {
                    if (regexExclude != null && regexExclude.IsMatch(f.Item1) ||
                        regexInclude != null && !regexInclude.IsMatch(f.Item1))
                    {
                        continue;
                    }
                    var sfmodel = new SourceFileModel { Name = f.Item1, Size = f.Item2, ModifiedDate = f.Item3 };
                    model.SourceFiles.Add(sfmodel.Name);
                }
                BackupManager.Update(model);
            }
        }

        private void ChangeBackupSelection(string collectionName)
        {
            if (!IsTaskRunning)
            {
                comboBox.SelectedItem = collectionName;
            }
        }

        // --- commands

        private void ChangeSettingsCmd()
        {
            try
            {
                new SettingsWindow(this, Properties.Resources.TITLE_SETTINGS).ShowDialog();
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private void CreateBackupCollectionCmd()
        {
            try
            {
                var wnd = new CreateBackupCollectionWindow(this, Properties.Resources.TITLE_NEW_BACKUP_COLLECTION, null);
                if (wnd.ShowDialog() == true)
                {
                    BackupManager.Add(new BackupModel { Title = wnd.CollectionName });
                    nextBackupMapping[wnd.CollectionName] = null;
                    comboBox.Items.Add(wnd.CollectionName);
                    comboBox.SelectedItem = wnd.CollectionName;                    
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private void RenameBackupCollectionCmd()
        {
            var name = comboBox?.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                var wnd = new CreateBackupCollectionWindow(this, Properties.Resources.TITLE_RENAME_BACKUP_COLLECTION, name);
                if (wnd.ShowDialog() == true)
                {
                    var model = BackupManager.Get(name);
                    var oldname = model.Title;
                    model.Title = wnd.CollectionName;
                    var next = BackupManager.Update(model);
                    nextBackupMapping.Remove(oldname);
                    nextBackupMapping[model.Title] = next;
                    comboBox.Items[comboBox.SelectedIndex] = model.Title;
                    comboBox.SelectedItem = model.Title;
                    UpdateOverview(oldname, null, null, null, true);
                    UpdateOverview(model.Title, model.Finished, next);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private void DeleteBackupCollectionCmd()
        {
            var name = comboBox?.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            if (MessageBox.Show(
                string.Format(Properties.Resources.QUESTION_DELETE_BACKUP_COLLECTION_0, name),
                Title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                try
                {
                    BackupManager.Remove(name);
                    int idx = comboBox.SelectedIndex;
                    comboBox.Items.Remove(name);
                    if (comboBox.Items.Count > 0)
                    {
                        comboBox.SelectedIndex = Math.Min(idx, comboBox.Items.Count - 1);
                    }
                    nextBackupMapping.Remove(name);
                    UpdateOverview(name, null, null, null, true);
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                }
                CommandManager.InvalidateRequerySuggested();
                UpdateControls();
            }
        }

        private async void RefreshCmd()
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                await InitBackupCollection(name, true);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void BackupCmd()
        {
            if (IsTaskRunning) return;
            try
            {
                IsTaskRunning = true;
                cts = new CancellationTokenSource();
                UpdateControls();
                string name = comboBox.SelectedItem as string;
                if (name != null)
                {
                    SetProgress(Properties.Resources.TEXT_LOAD_BACKUP_COLLECTION);
                    var model = await Task.Run(() => BackupManager.Get(name));
                    if (textBlockSourceDirectory.Text.Length > 0)
                    {
                        SetProgress(Properties.Resources.TEXT_UPDATE_SOURCE_FILES_FROM_DIRECTORY);
                        await UpdateSourceFilesFromSourceDirectory(model);
                    }
                    SetProgress(Properties.Resources.TEXT_RUN_BACKUP_COLLECTION);
                    var progress = new Progress<double>((percent) => UpdateProgress(percent));
                    var next = await Task.Run( () => BackupManager.Backup(model, progress, cts.Token));
                    nextBackupMapping[name] = next;
                    await InitBackupCollection(name);
                }
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    HandleError(ex);
                }
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void AddSourceFileCmd()
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                var model = BackupManager.Get(name);
                var regexExclude = GetExcludePatternRegex(model);
                var regexInclude = GetIncludePatternRegex(model);
                var openFileDialog = new OpenFileDialog
                {
                    Title = Properties.Resources.TITLE_ADD_SOURCE_FILE,
                    Multiselect = true
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    listViewSourceFiles.ItemsSource = null;
                    List<SourceFileModel> add = new List<SourceFileModel>();
                    foreach (var fileName in openFileDialog.FileNames)
                    {
                        if (regexExclude != null && regexExclude.IsMatch(fileName) ||
                            regexInclude != null && !regexInclude.IsMatch(fileName) )
                        {
                            continue;
                        }
                        var sfmodel = new SourceFileModel
                        {
                            Name = fileName,
                            Size = new FileInfo(fileName).Length,
                            ModifiedDate = File.GetLastWriteTime(fileName)
                        };
                        add.Add(sfmodel);
                    }
                    var added = await Task.Run(() => AddBackupFiles(model, add));
                    foreach (var sf in added)
                    {
                        sourceFiles.Add(sf);
                    }
                    listViewSourceFiles.ItemsSource = sourceFiles;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void RemoveSourceFileCmd()
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_REMOVE_SOURCE_FILES);
                UpdateControls();
                var del = new List<SourceFileModel>();
                foreach (SourceFileModel b in listViewSourceFiles.SelectedItems)
                {
                    del.Add(b);
                }
                await DeleteSourceFileModels(name, del);
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            SetProgress();
            IsTaskRunning = false;
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void AddSourceDirectoryCmd()
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                var model = BackupManager.Get(name);
                var regexInclude = GetIncludePatternRegex(model);
                var regexExclude = GetExcludePatternRegex(model);
                var dlg = new System.Windows.Forms.FolderBrowserDialog
                {
                    UseDescriptionForTitle = true,
                    Description = Properties.Resources.TITLE_ADD_SOURCE_DIRECTORY
                };
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    IsTaskRunning = true;
                    SetProgress(Properties.Resources.TEXT_ADD_SOURCE_FILES_FROM_DIRECTORY);
                    UpdateControls();
                    var ret = await Task.Run(() => new DirectoryInfo(dlg.SelectedPath).GetAllFiles(null));
                    listViewSourceFiles.ItemsSource = null;
                    foreach (var f in ret)
                    {
                        if (!model.SourceFiles.Contains(f.Item1))
                        {
                            if (regexExclude != null && regexExclude.IsMatch(f.Item1) ||
                                regexInclude != null && !regexInclude.IsMatch(f.Item1) )
                            {
                                continue;
                            }
                            var sfmodel = new SourceFileModel { Name = f.Item1, Size = f.Item2, ModifiedDate = f.Item3 };
                            sourceFiles.Add(sfmodel);
                            model.SourceFiles.Add(sfmodel.Name);
                        }
                    }
                    BackupManager.Update(model);
                    listViewSourceFiles.ItemsSource = sourceFiles;
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            IsTaskRunning = false;
            SetProgress();
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private async void AddDestinationDirectoryCmd()
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                UseDescriptionForTitle = true,
                Description = Properties.Resources.TITLE_ADD_DESTINATION_DIRECTORY
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_ADD_DESTINATION_DIRECTORY);
                UpdateControls();
                try
                {
                    var added = await Task.Run(() => AddDestinationDirectory(name, dlg.SelectedPath));
                    if (added)
                    {
                        var ddmodel = new DestDirModel
                        {
                            Name = dlg.SelectedPath
                        };
                        destDirectories.Add(ddmodel);
                    }
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                }
                IsTaskRunning = false;
                SetProgress();
                CommandManager.InvalidateRequerySuggested();
                UpdateControls();
            }
        }

        private async void RemoveDestinationDirectoryCmd()
        {
            var name = comboBox.SelectedItem as string;
            if (IsTaskRunning || name == null) return;
            try
            {
                IsTaskRunning = true;
                SetProgress(Properties.Resources.TEXT_REMOVE_DESTINATION_DIRECTORY);
                UpdateControls();
                var del = new List<DestDirModel>();
                foreach (DestDirModel b in listViewDirectories.SelectedItems)
                {
                    del.Add(b);
                }
                var idx = listViewDirectories.SelectedIndex;
                listViewDirectories.ItemsSource = null;
                await Task.Run(() => RemoveDestinationDirectories(name, del));
                foreach (var d in del)
                {
                    destDirectories.Remove(d);
                }
                listViewDirectories.ItemsSource = destDirectories;
                idx = Math.Min(idx, listViewDirectories.Items.Count - 1);
                if (idx >= 0)
                {
                    listViewDirectories.SelectedIndex = idx;
                    listViewDirectories.FocusItem(idx);
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            IsTaskRunning = false;
            SetProgress();
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }

        private void AboutCmd()
        {
            try
            {
                var dlg = new AboutWindow(this);
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private async void ShowOverviewCmd()
        {
            try
            {
                if (overviewWindow == null || overviewWindow.IsClosed)
                {
                    IsTaskRunning = true;
                    SetProgress(Properties.Resources.TEXT_LOADING_OVERVIEW);
                    UpdateControls();
                    overviews.Clear();
                    var collectionNames = BackupManager.GetAll();
                    foreach (var collectionName in collectionNames)
                    {
                        var backupModel = await Task.Run(() => BackupManager.Get(collectionName));
                        var m = new OverviewModel {
                            Name = collectionName,
                            LastRun = backupModel.Finished,
                            Copied = backupModel.Copied,
                            Failed = backupModel.Failed };
                        var next = nextBackupMapping[collectionName];
                        if (next.HasValue)
                        {
                            m.NextRun = next;
                        }
                        overviews.Add(m);
                    }
                    overviewWindow = new OverviewWindow(null, Properties.Resources.TITLE_OVERVIEW, overviews);
                    overviewWindow.BackupSelectionChanged = ChangeBackupSelection;
                    overviewWindow.Show();
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            IsTaskRunning = false;
            SetProgress();
            CommandManager.InvalidateRequerySuggested();
            UpdateControls();
        }
    }
}
