using ImageComparator.Commands;
using ImageComparator.Models;
using ImageComparator.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ImageComparator.ViewModels
{
    /// <summary>
    /// ViewModel for the main window. Manages application state, 
    /// comparison operations, and UI interactions.
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly IFileService _fileService;

        private bool _isComparing;
        private bool _isPaused;
        private int _progressPercentage;
        private string _selectedImagePath1;
        private string _selectedImagePath2;

        #region Properties

        /// <summary>
        /// Gets the list of directories to scan.
        /// </summary>
        public List<string> Directories { get; private set; } = new List<string>();

        /// <summary>
        /// Gets the list of files found during scanning.
        /// </summary>
        public List<string> Files { get; private set; } = new List<string>();

        /// <summary>
        /// Gets the console output messages.
        /// </summary>
        public ObservableCollection<string> ConsoleMessages { get; private set; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets the first list of comparison results.
        /// </summary>
        public ObservableCollection<ImageComparisonResult> ResultsList1 { get; private set; } = new ObservableCollection<ImageComparisonResult>();

        /// <summary>
        /// Gets the second list of comparison results.
        /// </summary>
        public ObservableCollection<ImageComparisonResult> ResultsList2 { get; private set; } = new ObservableCollection<ImageComparisonResult>();

        /// <summary>
        /// Gets or sets the comparison settings.
        /// </summary>
        public ComparisonSettings Settings { get; set; } = new ComparisonSettings();

        /// <summary>
        /// Gets or sets whether a comparison is currently in progress.
        /// </summary>
        public bool IsComparing
        {
            get => _isComparing;
            set => SetProperty(ref _isComparing, value);
        }

        /// <summary>
        /// Gets or sets whether the comparison is paused.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        /// <summary>
        /// Gets or sets the progress percentage (0-100).
        /// </summary>
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        /// <summary>
        /// Gets or sets the path of the selected image in the first list.
        /// </summary>
        public string SelectedImagePath1
        {
            get => _selectedImagePath1;
            set => SetProperty(ref _selectedImagePath1, value);
        }

        /// <summary>
        /// Gets or sets the path of the selected image in the second list.
        /// </summary>
        public string SelectedImagePath2
        {
            get => _selectedImagePath2;
            set => SetProperty(ref _selectedImagePath2, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to add a folder to the scan list.
        /// </summary>
        public ICommand AddFolderCommand { get; }

        /// <summary>
        /// Command to start finding duplicates.
        /// </summary>
        public ICommand FindDuplicatesCommand { get; }

        /// <summary>
        /// Command to pause the comparison.
        /// </summary>
        public ICommand PauseCommand { get; }

        /// <summary>
        /// Command to stop the comparison.
        /// </summary>
        public ICommand StopCommand { get; }

        /// <summary>
        /// Command to clear all results and directories.
        /// </summary>
        public ICommand ClearCommand { get; }

        /// <summary>
        /// Command to delete selected items.
        /// </summary>
        public ICommand DeleteSelectedCommand { get; }

        /// <summary>
        /// Command to mark items for deletion.
        /// </summary>
        public ICommand MarkForDeletionCommand { get; }

        /// <summary>
        /// Command to mark items as false positives.
        /// </summary>
        public ICommand MarkAsFalsePositiveCommand { get; }

        /// <summary>
        /// Command to remove marks from items.
        /// </summary>
        public ICommand RemoveMarkCommand { get; }

        /// <summary>
        /// Command to save results.
        /// </summary>
        public ICommand SaveResultsCommand { get; }

        /// <summary>
        /// Command to load results.
        /// </summary>
        public ICommand LoadResultsCommand { get; }

        #endregion

        public MainWindowViewModel(IDialogService dialogService, IFileService fileService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            // Initialize commands
            AddFolderCommand = new RelayCommand(ExecuteAddFolder, CanExecuteAddFolder);
            FindDuplicatesCommand = new RelayCommand(ExecuteFindDuplicates, CanExecuteFindDuplicates);
            PauseCommand = new RelayCommand(ExecutePause, CanExecutePause);
            StopCommand = new RelayCommand(ExecuteStop, CanExecuteStop);
            ClearCommand = new RelayCommand(ExecuteClear, CanExecuteClear);
            DeleteSelectedCommand = new RelayCommand(ExecuteDeleteSelected, CanExecuteDeleteSelected);
            MarkForDeletionCommand = new RelayCommand(ExecuteMarkForDeletion, CanExecuteMarkItems);
            MarkAsFalsePositiveCommand = new RelayCommand(ExecuteMarkAsFalsePositive, CanExecuteMarkItems);
            RemoveMarkCommand = new RelayCommand(ExecuteRemoveMark, CanExecuteMarkItems);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResults, CanExecuteSaveResults);
            LoadResultsCommand = new RelayCommand(ExecuteLoadResults, CanExecuteLoadResults);

            // Initialize console with welcome message
            ConsoleMessages.Add(LocalizationManager.GetString("Label.DragDropFolders"));
        }

        #region Command Implementations

        private bool CanExecuteAddFolder(object parameter)
        {
            return !IsComparing;
        }

        private void ExecuteAddFolder(object parameter)
        {
            var folderPath = _dialogService.ShowFolderBrowserDialog(
                LocalizationManager.GetString("Dialog.AddFolderTitle"));

            if (!string.IsNullOrEmpty(folderPath))
            {
                if (Directories.Count == 0)
                {
                    ConsoleMessages.Clear();
                    ConsoleMessages.Add(LocalizationManager.GetString("Label.DragDropFolders"));
                }

                if (!Directories.Contains(folderPath))
                {
                    Directories.Add(folderPath);
                    ConsoleMessages.Insert(ConsoleMessages.Count - 1,
                        LocalizationManager.GetString("Console.DirectoryAdded", folderPath));
                }
            }
        }

        private bool CanExecuteFindDuplicates(object parameter)
        {
            return Directories.Count > 0 && !IsComparing;
        }

        private void ExecuteFindDuplicates(object parameter)
        {
            // Warn if scanning system directories
            if (Directories.Contains("C:\\") || Directories.Contains("C:\\Windows"))
            {
                if (MessageBox.Show("Are you sure?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Clear previous results
            Files.Clear();
            ConsoleMessages.Clear();
            ResultsList1.Clear();
            ResultsList2.Clear();
            ProgressPercentage = 0;
            IsComparing = true;

            // Add console messages
            foreach (var directory in Directories)
            {
                if (Settings.IncludeSubfolders)
                {
                    ConsoleMessages.Add(LocalizationManager.GetString("Console.ComparingWithSubdirs", directory));
                }
                else
                {
                    ConsoleMessages.Add(LocalizationManager.GetString("Console.ComparingWithoutSubdirs", directory));
                }
            }

            // TODO: Start comparison process in background
            // This will be implemented with the ImageComparisonService
        }

        private bool CanExecutePause(object parameter)
        {
            return IsComparing && !IsPaused;
        }

        private void ExecutePause(object parameter)
        {
            IsPaused = true;
            // TODO: Implement pause logic
        }

        private bool CanExecuteStop(object parameter)
        {
            return IsComparing;
        }

        private void ExecuteStop(object parameter)
        {
            IsComparing = false;
            IsPaused = false;
            // TODO: Implement stop logic
        }

        private bool CanExecuteClear(object parameter)
        {
            return !IsComparing;
        }

        private void ExecuteClear(object parameter)
        {
            Directories.Clear();
            Files.Clear();
            ConsoleMessages.Clear();
            ResultsList1.Clear();
            ResultsList2.Clear();
            ProgressPercentage = 0;
            ConsoleMessages.Add(LocalizationManager.GetString("Label.DragDropFolders"));
        }

        private bool CanExecuteDeleteSelected(object parameter)
        {
            return !IsComparing && (ResultsList1.Count > 0 || ResultsList2.Count > 0);
        }

        private void ExecuteDeleteSelected(object parameter)
        {
            // TODO: Implement delete selected logic
        }

        private bool CanExecuteMarkItems(object parameter)
        {
            return !IsComparing && (ResultsList1.Count > 0 || ResultsList2.Count > 0);
        }

        private void ExecuteMarkForDeletion(object parameter)
        {
            // TODO: Implement mark for deletion logic
        }

        private void ExecuteMarkAsFalsePositive(object parameter)
        {
            // TODO: Implement mark as false positive logic
        }

        private void ExecuteRemoveMark(object parameter)
        {
            // TODO: Implement remove mark logic
        }

        private bool CanExecuteSaveResults(object parameter)
        {
            return !IsComparing && ResultsList1.Count > 0;
        }

        private void ExecuteSaveResults(object parameter)
        {
            var filePath = _dialogService.ShowSaveFileDialog("*.mff|*.mff", "mff");
            if (!string.IsNullOrEmpty(filePath))
            {
                // TODO: Implement save results logic
            }
        }

        private bool CanExecuteLoadResults(object parameter)
        {
            return !IsComparing;
        }

        private void ExecuteLoadResults(object parameter)
        {
            var filePath = _dialogService.ShowOpenFileDialog("*.mff|*.mff", "mff");
            if (!string.IsNullOrEmpty(filePath))
            {
                // TODO: Implement load results logic
            }
        }

        #endregion
    }
}
