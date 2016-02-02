﻿using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TreeGUI
{
    public partial class MainWindow : Window
    {
        public ICommand ToggleThemeCommand { get; } = new SimpleCommand(o => ApplyTheme((bool)o));

        #region Methods

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            DebugHelper.Init(Program.LogsAppFilePath);

            Program.LoadSettings();
            Program.Settings.SettingsChanged += MainWindow_SettingsChanged;
            Program.Config.SettingsSaved += Config_SettingsSaved;

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                if (args[1].EndsWith(".tgcj"))
                {
                    LoadConfig(args[1]);
                }
            }
            else
            {
                UpdateWindowUI();
            }
        }

        private static void ApplyTheme(bool isDarkTheme)
        {
            new PaletteHelper().SetLightDark(isDarkTheme);

            if (Program.Settings != null)
            {
                Program.Settings.IsDarkTheme = isDarkTheme;
            }
        }

        private void MainWindow_SettingsChanged(Settings settings)
        {
            this.Topmost = Program.Settings.AlwaysOnTop;
        }

        private void Config_SettingsSaved(Config settings, string filePath, bool result)
        {
            if (result)
            {
                Program.ConfigEdited = false;
            }
        }

        private void LoadConfig(string filePath)
        {
            if (Program.LoadConfig(filePath))
            {
                Program.ConfigFilePath = filePath;
                lbFolders.Items.Clear();
                Program.Config.Folders.ForEach(x => lbFolders.Items.Add(x));
                UpdateWindowUI();
            }
        }

        public bool SaveConfig()
        {
            if (!Program.ConfigEdited) return false;

            if (!File.Exists(Program.ConfigFilePath))
            {
                return SaveAsConfig();
            }
            else
            {
                Program.Config.SaveAsync(Program.ConfigFilePath);
                return true;
            }
        }

        public bool SaveAsConfig()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = Program.ConfigFileFilter;
            if (dlg.ShowDialog() == true)
            {
                Program.ConfigFilePath = dlg.FileName;
                Program.Config.SaveAsync(Program.ConfigFilePath);
                UpdateWindowUI();
                return true;
            }

            return false;
        }

        private void UpdateWindowUI()
        {
            string configName = File.Exists(Program.ConfigFilePath) ? Path.GetFileName(Program.ConfigFilePath) : Program.ConfigNewFileName;

            Title = $"TreeGUI - {configName}";
            ApplyTheme(Program.Settings.IsDarkTheme);
            tbIsDarkTheme.IsChecked = Program.Settings.IsDarkTheme;
            miToolsConfig.Header = $"{configName} Properties...";
            btnMoveUp.IsEnabled = btnMoveDown.IsEnabled = lbFolders.Items.Count > 1;
            btnIndex.IsEnabled = lbFolders.Items.Count > 0;
            miFolderOpenDir.IsEnabled = lbFolders.SelectedIndex > -1;
            if (miFolderOpenDir.IsEnabled)
            {
                miFolderOpenDir.Header = $"Browse {Path.GetFileName(lbFolders.SelectedItem.ToString())}...";
            }
            miFolderOpenOutputDir.IsEnabled = Directory.Exists(Program.Config.CustomDirectory);
        }

        private async Task<bool> IsConfigNotSaved()
        {
            if (Program.ConfigEdited)
            {
                CustomMessageBox messageBox = new CustomMessageBox($"Do you want to save changes to {Program.ConfigFileName}?", "Yes", "No");
                string result = await DialogHost.Show(messageBox) as string;
                if (result.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                {
                    return !SaveConfig();
                }
            }

            return false;
        }

        private void AddFolders(IEnumerable<string> dirs)
        {
            if (dirs.Count() > 0)
                dirs.ForEach(dir =>
                {
                    lbFolders.Items.Add(dir);
                    Program.Config.Folders.Add(dir);
                });

            Program.ConfigEdited = true;
        }

        #endregion Methods

        #region File menu

        private void FileNewCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void FileNewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!await IsConfigNotSaved())
            {
                Program.LoadNewConfig();
                lbFolders.Items.Clear();
                UpdateWindowUI();
            }
        }

        private async void FileOpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!await IsConfigNotSaved())
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = Program.ConfigFileFilter;
                if (dlg.ShowDialog() == true)
                {
                    LoadConfig(dlg.FileName);
                }
            }
        }

        private void FileOpenCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void FileExitCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void FileSaveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void FileSaveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveConfig();
        }

        private void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAsConfig();
        }

        private void FileExitCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion File menu

        #region Folders menu

        private void miFolderOpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (lbFolders.SelectedIndex > -1)
            {
                string dir = lbFolders.SelectedItem.ToString();
                Helpers.OpenFolder(dir);
            }
        }

        private void miFolderOpenOutputDir_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Program.Config.CustomDirectory))
            {
                Helpers.OpenFolder(Program.Config.CustomDirectory);
            }
        }

        #endregion Folders menu

        #region Logs menu

        private void miLogsApp_Click(object sender, RoutedEventArgs e)
        {
            Helpers.OpenFile(Program.LogsAppFilePath);
        }

        private void miLogsSvc_Click(object sender, RoutedEventArgs e)
        {
            Helpers.OpenFile(Program.LogsSvcFilePath);
        }

        #endregion Logs menu

        #region Tools menu

        private void ToolsConfigProperties_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow window = new ConfigWindow();
            window.Show();
        }

        private void ToolsSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow();
            window.Show();
        }

        #region Windows Service

        private async void miToolsSvcInstall_Click(object sender, RoutedEventArgs e)
        {
            LoginBoxData result = await ShowLoginAsync("Please enter your password to start the Windows Service using your credentials.");
            if (result != null)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "TreeGUISvc.exe"));
                    psi.Arguments = $"-install {result.UserName} {result.Password}";
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    DebugHelper.WriteException(ex);
                }
            }
        }

        private void miToolsSvcUninstall_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "TreeGUISvc.exe"));
            psi.Arguments = "-uninstall";
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private async Task<LoginBoxData> ShowLoginAsync(string question)
        {
            LoginBox dlg = new LoginBox(question);

            string result = await DialogHost.Show(dlg) as string;
            if (result.Equals("2", StringComparison.InvariantCultureIgnoreCase))
            {
                return dlg.Settings;
            }
            return null;
        }

        private void miToolsSvcStart_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "TreeGUISvc.exe"));
            psi.Arguments = "-start";
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private void miToolsSvcStop_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "TreeGUISvc.exe"));
            psi.Arguments = "-stop";
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        #endregion Windows Service

        #endregion Tools menu

        #region Help menu

        private void HelpVersionHistory_Click(object sender, RoutedEventArgs e)
        {
            URLHelpers.OpenURL("https://raw.githubusercontent.com/McoreD/TreeGUI/master/treegui3/VersionHistory.txt");
        }

        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow dlg = new AboutWindow();
            dlg.ShowDialog();
        }

        #endregion Help menu

        #region Buttons

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (CommonFileDialog.IsPlatformSupported)
            {
                CommonOpenFileDialog dlg = new CommonOpenFileDialog();
                dlg.EnsureReadOnly = true;
                dlg.IsFolderPicker = true;
                dlg.AllowNonFileSystemItems = false;
                dlg.Multiselect = true;
                dlg.Title = "Select folder to index";

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    AddFolders(dlg.FileNames);
                }
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            Program.ConfigEdited = lbFolders.SelectedItems.Count > 0;

            lbFolders.SelectedItems.Cast<string>().ToList().ForEach(x =>
            {
                Program.Config.Folders.Remove(x);
                lbFolders.Items.Remove(x);
            });
        }

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnIndex_Click(object sender, RoutedEventArgs e)
        {
            IndexerHelper.Index(Program.Config);
        }

        #endregion Buttons

        private void lbFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateWindowUI();
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            // e.Cancel = await IsConfigNotSaved();
            SaveConfig();
            Program.SaveSettings();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            AddFolders(paths.Where(dir => Directory.Exists(dir)));
        }
    }
}