using DiscreteCosineTransform;
using ImageComparator.Helpers;
using Microsoft.VisualBasic.FileIO;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageComparator
{
    [Serializable]
    public partial class MainWindow : Window, ISerializable
    {
        #region Variables
        System.Diagnostics.Process process;
        VistaFolderBrowserDialog folderBrowserDialog;
        VistaSaveFileDialog saveFileDialog;
        VistaOpenFileDialog openFileDialog;
        List<string> directories = new List<string>();
        List<string> files = new List<string>();
        List<string> falsePositiveList1 = new List<string>();
        List<string> falsePositiveList2 = new List<string>();
        ObservableCollection<string> console = new ObservableCollection<string>();
        public ObservableCollection<ListViewDataItem> bindingList1 = new ObservableCollection<ListViewDataItem>();
        public ObservableCollection<ListViewDataItem> bindingList2 = new ObservableCollection<ListViewDataItem>();
        List<ListViewDataItem> list1 = new List<ListViewDataItem>();
        List<ListViewDataItem> list2 = new List<ListViewDataItem>();
        List<Thread> threadList;
        StreamWriter streamWriter;
        StreamReader streamReader;
        object myLock = new object();
        object myLock2 = new object();
        MyInt percentage = new MyInt();
        System.Windows.Point previewImage1Start, previewImage1Origin, previewImage2Start, previewImage2Origin;
        System.Drawing.Size[] resolutionArray;
        Orientation[] orientationArray;
        int[,] pHashArray, hdHashArray, vdHashArray, aHashArray;
        string[] sha256Array;
        Thread processThread;
        MainWindow mainWindow;
        long firstTime, secondTime, pauseTime, pausedFirstTime, pausedSecondTime;
        int processThreadsiAsync, compareResultsiAsync, timeDifferenceInSeconds, duplicateImageCount, highConfidenceSimilarImageCount, mediumConfidenceSimilarImageCount, lowConfidenceSimilarImageCount;
        public bool gotException = false, skipFilesWithDifferentOrientation = true, duplicatesOnly = false, comparing = false, includeSubfolders, jpegMenuItemChecked, gifMenuItemChecked, pngMenuItemChecked, bmpMenuItemChecked, tiffMenuItemChecked, icoMenuItemChecked, sendsToRecycleBin, opening = true, deleteMarkedItems = false;
        string path;
        string currentLanguageCode = "en-US"; // Track current language for serialization

        public static DependencyProperty ImagePathProperty1 = DependencyProperty.Register("ImagePath1", typeof(string), typeof(MainWindow), null);
        public static DependencyProperty ImagePathProperty2 = DependencyProperty.Register("ImagePath2", typeof(string), typeof(MainWindow), null);

        public string ImagePath1
        {
            get
            {
                return (string)GetValue(ImagePathProperty1);
            }
            set
            {
                SetValue(ImagePathProperty1, value);
            }
        }

        public string ImagePath2
        {
            get
            {
                return (string)GetValue(ImagePathProperty2);
            }
            set
            {
                SetValue(ImagePathProperty2, value);
            }
        }
        #endregion

        #region Constants
        // Hash calculation constants
        private const int PHASH_RESIZE_DIMENSION = 32;
        private const int DHASH_RESIZE_DIMENSION = 9;
        private const int AHASH_RESIZE_DIMENSION = 8;

        // Hash comparison thresholds
        private const int EXACT_DUPLICATE_THRESHOLD = 1;
        private const int PHASH_HIGH_CONFIDENCE_THRESHOLD = 9;
        private const int PHASH_MEDIUM_CONFIDENCE_THRESHOLD = 12;
        private const int PHASH_LOW_CONFIDENCE_THRESHOLD = 21;
        private const int HDHASH_HIGH_CONFIDENCE_THRESHOLD = 10;
        private const int HDHASH_MEDIUM_CONFIDENCE_THRESHOLD = 13;
        private const int HDHASH_LOW_CONFIDENCE_THRESHOLD = 18;
        private const int VDHASH_HIGH_CONFIDENCE_THRESHOLD = 10;
        private const int VDHASH_MEDIUM_CONFIDENCE_THRESHOLD = 13;
        private const int VDHASH_LOW_CONFIDENCE_THRESHOLD = 18;
        private const int AHASH_HIGH_CONFIDENCE_THRESHOLD = 9;
        private const int AHASH_MEDIUM_CONFIDENCE_THRESHOLD = 12;
        #endregion

        #region Enums
        public enum Orientation
        {
            Horizontal,
            Vertical
        }

        public enum Confidence
        {
            Low,
            Medium,
            High,
            Duplicate
        }

        public enum State
        {
            Normal,
            MarkedForDeletion,
            MarkedAsFalsePositive
        }
        #endregion

        [Serializable]
        public class ListViewDataItem : INotifyPropertyChanged
        {
            private bool selected;
            private int pState;
            private bool pIsChecked;
            private bool pCheckboxEnabled;
            public string text { get; set; }
            public int confidence { get; set; }
            public int pHashHammingDistance { get; set; }
            public int hdHashHammingDistance { get; set; }
            public int vdHashHammingDistance { get; set; }
            public int aHashHammingDistance { get; set; }
            public string sha256Checksum { get; set; }

            public bool isSelected
            {
                get
                {
                    return selected;
                }
                set
                {
                    selected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isSelected"));
                }
            }

            public int state
            {
                get
                {
                    return pState;
                }
                set
                {
                    pState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("state"));
                }
            }

            public bool isChecked
            {
                get
                {
                    return pIsChecked;
                }
                set
                {
                    pIsChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isChecked"));
                }
            }

            public bool CheckboxEnabled
            {
                get
                {
                    return pCheckboxEnabled;
                }
                set
                {
                    pCheckboxEnabled = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckboxEnabled"));
                }
            }

            public ListViewDataItem(string text, int confidence, int pHashHammingDistance, int hdHashHammingDistance, int vdHashHammingDistance, int aHashHammingDistance, string sha256Checksum)
            {

                this.text = text;
                this.confidence = confidence;
                this.pHashHammingDistance = pHashHammingDistance;
                this.hdHashHammingDistance = hdHashHammingDistance;
                this.vdHashHammingDistance = vdHashHammingDistance;
                this.aHashHammingDistance = aHashHammingDistance;
                this.sha256Checksum = sha256Checksum;
                isSelected = false;
                state = (int)State.Normal;
                isChecked = false;
                CheckboxEnabled = true;
            }

            [field: NonSerialized]
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public MainWindow()
        {
            InitializeComponent();
            percentage.OnChange += PercentageChanged;
            path = Environment.GetCommandLineArgs().ElementAt(0).Substring(0, Environment.GetCommandLineArgs().ElementAt(0).LastIndexOf("\\"));
            folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyPictures;
            folderBrowserDialog.UseDescriptionForTitle = true;
            folderBrowserDialog.ShowNewFolderButton = true;

            saveFileDialog = new VistaSaveFileDialog();
            saveFileDialog.DefaultExt = "mff";
            saveFileDialog.Filter = "*.mff|*.mff";
            saveFileDialog.AddExtension = true;

            openFileDialog = new VistaOpenFileDialog();
            openFileDialog.DefaultExt = "mff";
            openFileDialog.Filter = "*.mff|*.mff";
            openFileDialog.AddExtension = true;

            previewImage1.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                    new ScaleTransform(),
                    new TranslateTransform()
                }
            };

            previewImage2.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection {
                    new ScaleTransform(),
                    new TranslateTransform()
                }
            };

            try
            {
                Deserialize(path + @"\Bin\Image Comparator.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("MainWindow_Constructor - Deserialize", ex);
                opening = false;
            }

            // Initialize localization based on menu selection
            currentLanguageCode = GetCurrentLanguageFromMenu();
            LocalizationManager.SetLanguage(currentLanguageCode);

            // Clean up old error logs (async to not block startup)
            System.Threading.Tasks.Task.Run(() => ErrorLogger.CleanupOldLogs());

            UpdateUI();
            outputListView.ItemsSource = console;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                Serialize(path + @"\Bin\Image Comparator.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Serialize", ex);
            }

            try
            {
                process?.Kill();
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Kill Process", ex);
            }

            try
            {
                File.Delete(path + @"\Bin\Results.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Delete Results.imc", ex);
            }

            try
            {
                File.Delete(path + @"\Bin\Directories.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Delete Directories.imc", ex);
            }

            try
            {
                File.Delete(path + @"\Bin\Filters.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Delete Filters.imc", ex);
            }
            Environment.Exit(0);
        }

        private void SaveResultsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveResults();
        }

        public bool SaveResults()
        {
            saveFileDialog.Title = LocalizationManager.GetString("Dialog.SaveTitle");

            if (saveFileDialog.ShowDialog().Value)
            {
                Serialize(saveFileDialog.FileName);
                console.Add(LocalizationManager.GetString("Console.SessionSaved", saveFileDialog.FileName));
                return true;
            }
            else
            {
                return false;
            }
        }

        private void LoadResultsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog.Title = LocalizationManager.GetString("Dialog.LoadTitle");

            if (openFileDialog.ShowDialog().Value)
            {
                Deserialize(openFileDialog.FileName);
                console.Add(LocalizationManager.GetString("Console.SessionLoaded", openFileDialog.FileName));
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SendToRecycleBinMenuItem_Click(object sender, RoutedEventArgs e)
        {
            sendToRecycleBinMenuItem.IsChecked = true;
            deletePermanentlyMenuItem.IsChecked = false;
            sendToRecycleBinMenuItem.IsEnabled = false;
            deletePermanentlyMenuItem.IsEnabled = true;
        }

        private void DeletePermanentlyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            sendToRecycleBinMenuItem.IsChecked = false;
            deletePermanentlyMenuItem.IsChecked = true;
            sendToRecycleBinMenuItem.IsEnabled = true;
            deletePermanentlyMenuItem.IsEnabled = false;
        }

        private void EnglishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("en-US", englishMenuItem);
        }

        private void TurkishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("tr-TR", turkishMenuItem);
        }

        private void JapaneseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("ja-JP", japaneseMenuItem);
        }

        private void SpanishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("es-ES", spanishMenuItem);
        }

        private void FrenchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("fr-FR", frenchMenuItem);
        }

        private void GermanMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("de-DE", germanMenuItem);
        }

        private void ItalianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("it-IT", italianMenuItem);
        }

        private void PortugueseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("pt-BR", portugueseMenuItem);
        }

        private void RussianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("ru-RU", russianMenuItem);
        }

        private void ChineseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("zh-CN", chineseMenuItem);
        }

        private void KoreanMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("ko-KR", koreanMenuItem);
        }

        private void ArabicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("ar-SA", arabicMenuItem);
        }

        private void PersianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("fa-IR", persianMenuItem);
        }

        private void HindiMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("hi-IN", hindiMenuItem);
        }

        private void DutchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("nl-NL", dutchMenuItem);
        }

        private void PolishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("pl-PL", polishMenuItem);
        }

        private void SwedishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("sv-SE", swedishMenuItem);
        }

        private void NorwegianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("nb-NO", norwegianMenuItem);
        }

        private void DanishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("da-DK", danishMenuItem);
        }

        /// <summary>
        /// Helper method to set the language and update UI state
        /// </summary>
        /// <param name="languageCode">The language code (e.g., "en-US")</param>
        /// <param name="selectedMenuItem">The menu item that was clicked</param>
        private void SetLanguage(string languageCode, MenuItem selectedMenuItem)
        {
            // Use the centralized menu state management
            SetLanguageMenuStates(languageCode);

            // Set the language
            currentLanguageCode = languageCode;
            LocalizationManager.SetLanguage(languageCode);
            UpdateUI();
        }

        /// <summary>
        /// Helper method to get the currently selected language code based on menu item states
        /// </summary>
        /// <returns>The language code of the checked menu item, or "en-US" as default</returns>
        private string GetCurrentLanguageFromMenu()
        {
            if (englishMenuItem.IsChecked) return "en-US";
            if (turkishMenuItem.IsChecked) return "tr-TR";
            if (japaneseMenuItem.IsChecked) return "ja-JP";
            if (spanishMenuItem.IsChecked) return "es-ES";
            if (frenchMenuItem.IsChecked) return "fr-FR";
            if (germanMenuItem.IsChecked) return "de-DE";
            if (italianMenuItem.IsChecked) return "it-IT";
            if (portugueseMenuItem.IsChecked) return "pt-BR";
            if (russianMenuItem.IsChecked) return "ru-RU";
            if (chineseMenuItem.IsChecked) return "zh-CN";
            if (koreanMenuItem.IsChecked) return "ko-KR";
            if (arabicMenuItem.IsChecked) return "ar-SA";
            if (persianMenuItem.IsChecked) return "fa-IR";
            if (hindiMenuItem.IsChecked) return "hi-IN";
            if (dutchMenuItem.IsChecked) return "nl-NL";
            if (polishMenuItem.IsChecked) return "pl-PL";
            if (swedishMenuItem.IsChecked) return "sv-SE";
            if (norwegianMenuItem.IsChecked) return "nb-NO";
            if (danishMenuItem.IsChecked) return "da-DK";
            return "en-US"; // Default
        }

        /// <summary>
        /// Helper method to set menu states for a specific language code
        /// </summary>
        /// <param name="languageCode">The language code to activate</param>
        private void SetLanguageMenuStates(string languageCode)
        {
            // Map of language codes to menu items
            var languageMenuItems = new Dictionary<string, MenuItem>
            {
                { "en-US", englishMenuItem },
                { "tr-TR", turkishMenuItem },
                { "ja-JP", japaneseMenuItem },
                { "es-ES", spanishMenuItem },
                { "fr-FR", frenchMenuItem },
                { "de-DE", germanMenuItem },
                { "it-IT", italianMenuItem },
                { "pt-BR", portugueseMenuItem },
                { "ru-RU", russianMenuItem },
                { "zh-CN", chineseMenuItem },
                { "ko-KR", koreanMenuItem },
                { "ar-SA", arabicMenuItem },
                { "fa-IR", persianMenuItem },
                { "hi-IN", hindiMenuItem },
                { "nl-NL", dutchMenuItem },
                { "pl-PL", polishMenuItem },
                { "sv-SE", swedishMenuItem },
                { "nb-NO", norwegianMenuItem },
                { "da-DK", danishMenuItem }
            };

            // Uncheck and enable all menu items
            foreach (var menuItem in languageMenuItems.Values)
            {
                menuItem.IsChecked = false;
                menuItem.IsEnabled = true;
            }

            // Check and disable the selected language
            if (languageMenuItems.TryGetValue(languageCode, out MenuItem selectedMenuItem))
            {
                selectedMenuItem.IsChecked = true;
                selectedMenuItem.IsEnabled = false;
            }
        }

        private void ClearFalsePositiveDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            int count = falsePositiveList1.Count;
            falsePositiveList1.Clear();
            falsePositiveList2.Clear();

            try
            {
                Serialize(path + @"\Bin\Image Comparator.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("ClearFalsePositiveDatabaseButton_Click - Serialize", ex);
            }

            if (count > 0)
            {
                console.Add(LocalizationManager.GetString("Console.FalsePositiveDatabaseCleared"));
            }
        }

        private void ResetToDefaultsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            jpegMenuItem.IsChecked = true;
            bmpMenuItem.IsChecked = true;
            pngMenuItem.IsChecked = true;
            gifMenuItem.IsChecked = true;
            tiffMenuItem.IsChecked = true;
            icoMenuItem.IsChecked = true;
            sendToRecycleBinMenuItem.IsChecked = true;
            sendToRecycleBinMenuItem.IsEnabled = false;
            deletePermanentlyMenuItem.IsChecked = false;
            deletePermanentlyMenuItem.IsEnabled = true;
            includeSubfoldersMenuItem.IsChecked = true;
            skipFilesWithDifferentOrientationMenuItem.IsChecked = true;
            findExactDuplicatesOnlyMenuItem.IsChecked = false;
        }

        private void HowToUseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HowToUseWindow howToUseWindow = new HowToUseWindow();
            howToUseWindow.ShowDialog();
        }

        private void ViewErrorLogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = ErrorLogger.GetCurrentLogPath();
                
                if (File.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", logPath);
                }
                else
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("Error.NoErrorLog"),
                        LocalizationManager.GetString("Error.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("ViewErrorLogMenuItem_Click", ex);
                MessageBox.Show(
                    LocalizationManager.GetString("Error.CannotOpenFile", ex.Message),
                    LocalizationManager.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            folderBrowserDialog.Description = LocalizationManager.GetString("Dialog.AddFolderTitle");

            if ((bool)folderBrowserDialog.ShowDialog())
            {
                if (directories.Count == 0)
                {
                    console.Clear();
                    console.Add(LocalizationManager.GetString("Label.DragDropFolders"));
                }

                if (!directories.Contains(folderBrowserDialog.SelectedPath))
                {
                    directories.Add(folderBrowserDialog.SelectedPath);
                    console.Insert(console.Count - 1, LocalizationManager.GetString("Console.DirectoryAdded", folderBrowserDialog.SelectedPath));
                }
            }
        }

        private void FindDuplicatesButton_Click(object sender, RoutedEventArgs e)
        {
            if (directories.Count > 0)
            {
                if (directories.Contains("C:\\") || directories.Contains("C:\\Windows"))
                {
                    if (MessageBox.Show("Are you sure?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                firstTime = DateTime.Now.ToFileTime();
                files.Clear();
                console.Clear();
                bindingList1.Clear();
                bindingList2.Clear();
                list1.Clear();
                list2.Clear();
                comparing = false;
                percentage.Value = 0;
                findDuplicatesButton.Visibility = Visibility.Collapsed;
                pauseButton.Visibility = Visibility.Visible;
                stopButton.Visibility = Visibility.Visible;

                skipFilesWithDifferentOrientation = skipFilesWithDifferentOrientationMenuItem.IsChecked;
                duplicatesOnly = findExactDuplicatesOnlyMenuItem.IsChecked;

                for (int i = 0; i < directories.Count; i++)
                {
                    if (includeSubfoldersMenuItem.IsChecked)
                    {
                        console.Add(LocalizationManager.GetString("Console.ComparingWithSubdirs", directories[i]));
                    }
                    else
                    {
                        console.Add(LocalizationManager.GetString("Console.ComparingWithoutSubdirs", directories[i]));
                    }
                }
                includeSubfolders = includeSubfoldersMenuItem.IsChecked;
                jpegMenuItemChecked = jpegMenuItem.IsChecked;
                gifMenuItemChecked = gifMenuItem.IsChecked;
                pngMenuItemChecked = pngMenuItem.IsChecked;
                bmpMenuItemChecked = bmpMenuItem.IsChecked;
                tiffMenuItemChecked = tiffMenuItem.IsChecked;
                icoMenuItemChecked = icoMenuItem.IsChecked;
                processThread = new Thread(Run);
                processThread.Start();
            }
            else
            {
                console.Add(LocalizationManager.GetString("Console.NoDirectoriesAdded"));
            }
        }

        private void FindDuplicatesButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Array of all language menu items
            var languageMenuItems = new[]
            {
                englishMenuItem,
                turkishMenuItem,
                japaneseMenuItem,
                spanishMenuItem,
                frenchMenuItem,
                germanMenuItem,
                italianMenuItem,
                portugueseMenuItem,
                russianMenuItem,
                chineseMenuItem,
                koreanMenuItem,
                arabicMenuItem,
                hindiMenuItem,
                dutchMenuItem,
                polishMenuItem,
                swedishMenuItem,
                norwegianMenuItem,
                danishMenuItem
            };

            if (findDuplicatesButton.Visibility == Visibility.Visible)
            {
                saveResultsMenuItem.IsEnabled = true;
                loadResultsMenuItem.IsEnabled = true;
                jpegMenuItem.IsEnabled = true;
                bmpMenuItem.IsEnabled = true;
                pngMenuItem.IsEnabled = true;
                gifMenuItem.IsEnabled = true;
                tiffMenuItem.IsEnabled = true;
                icoMenuItem.IsEnabled = true;
                
                // Enable language menu items (disable the checked one)
                foreach (var languageMenuItem in languageMenuItems)
                {
                    languageMenuItem.IsEnabled = !languageMenuItem.IsChecked;
                }
                
                includeSubfoldersMenuItem.IsEnabled = true;
                skipFilesWithDifferentOrientationMenuItem.IsEnabled = true;
                findExactDuplicatesOnlyMenuItem.IsEnabled = true;
                clearFalsePositiveDatabaseButton.IsEnabled = true;
                resetToDefaultsMenuItem.IsEnabled = true;
                addFolderButton.IsEnabled = true;
                deleteSelectedButton.IsEnabled = false;
                removeFromListButton.IsEnabled = false;
                clearButton.IsEnabled = true;
                markForDeletionButton.IsEnabled = false;
                markAsFalsePositiveButton.IsEnabled = false;
                removeMarkButton.IsEnabled = false;
            }
            else
            {
                saveResultsMenuItem.IsEnabled = false;
                loadResultsMenuItem.IsEnabled = false;
                jpegMenuItem.IsEnabled = false;
                bmpMenuItem.IsEnabled = false;
                pngMenuItem.IsEnabled = false;
                gifMenuItem.IsEnabled = false;
                tiffMenuItem.IsEnabled = false;
                icoMenuItem.IsEnabled = false;
                
                // Disable all language menu items
                foreach (var languageMenuItem in languageMenuItems)
                {
                    languageMenuItem.IsEnabled = false;
                }
                
                includeSubfoldersMenuItem.IsEnabled = false;
                skipFilesWithDifferentOrientationMenuItem.IsEnabled = false;
                findExactDuplicatesOnlyMenuItem.IsEnabled = false;
                clearFalsePositiveDatabaseButton.IsEnabled = false;
                resetToDefaultsMenuItem.IsEnabled = false;
                addFolderButton.IsEnabled = false;
                clearButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Handles the delete button click event to delete selected or marked items.
        /// This method orchestrates the deletion workflow by coordinating helper methods
        /// that collect files, delete them from disk, update the UI lists, and report results.
        /// </summary>
        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            // Step 1: Collect files to delete
            var filesToDelete = CollectFilesToDelete();
            
            // Step 2: Delete files from disk and get successfully deleted files
            var deletedFiles = DeleteFilesFromDisk(filesToDelete);
            
            // Step 3: Remove deleted items from lists
            RemoveDeletedItemsFromLists(deletedFiles);
            
            // Step 4: Remove duplicate pairs
            RemoveDuplicatePairs();
            
            // Step 5: Show results to user
            ReportDeletionResults(deletedFiles.Count, filesToDelete.Count);
        }

        /// <summary>
        /// Collects unique file paths to delete from both binding lists.
        /// Uses HashSet for O(1) lookups and automatic duplicate removal.
        /// Time Complexity: O(n) where n is the total number of items in binding lists.
        /// </summary>
        /// <returns>Set of unique file paths to delete</returns>
        private HashSet<string> CollectFilesToDelete()
        {
            var filesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Collect from bindingList1
            foreach (var item in bindingList1.Where(ShouldDelete))
            {
                filesToDelete.Add(item.text);
            }

            // Collect from bindingList2
            foreach (var item in bindingList2.Where(ShouldDelete))
            {
                filesToDelete.Add(item.text);
            }

            return filesToDelete;
        }

        /// <summary>
        /// Determines if an item should be deleted based on current mode
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if item should be deleted, false otherwise</returns>
        private bool ShouldDelete(ListViewDataItem item)
        {
            return deleteMarkedItems 
                ? item.state == (int)State.MarkedForDeletion 
                : item.isChecked;
        }

        /// <summary>
        /// Deletes files from disk (either permanently or to recycle bin)
        /// </summary>
        /// <param name="filesToDelete">Set of file paths to attempt to delete</param>
        /// <returns>A set of file paths that were successfully deleted from the filesystem</returns>
        private HashSet<string> DeleteFilesFromDisk(HashSet<string> filesToDelete)
        {
            var deletedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in filesToDelete)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        var recycleOption = deletePermanentlyMenuItem.IsChecked 
                            ? RecycleOption.DeletePermanently 
                            : RecycleOption.SendToRecycleBin;
                        
                        FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, recycleOption);
                        deletedFiles.Add(filePath);
                    }
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError($"DeleteFilesFromDisk - {Path.GetFileName(filePath)}", ex);
                }
            }

            return deletedFiles;
        }

        /// <summary>
        /// Removes items from both binding lists where at least one file in the pair was successfully deleted.
        /// Uses OR condition: removes a pair if either file1 OR file2 was successfully deleted.
        /// This is appropriate for duplicate pairs where deleting either file removes the duplicate relationship.
        /// Time Complexity: O(n) where n is the number of items in binding lists.
        /// </summary>
        /// <param name="deletedFiles">Set of file paths that were successfully deleted from disk</param>
        private void RemoveDeletedItemsFromLists(HashSet<string> deletedFiles)
        {
            // Remove from end to avoid index shifting issues
            for (int i = bindingList1.Count - 1; i >= 0; i--)
            {
                if (deletedFiles.Contains(bindingList1[i].text) || 
                    deletedFiles.Contains(bindingList2[i].text))
                {
                    bindingList1.RemoveAt(i);
                    bindingList2.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Removes duplicate pairs from the binding lists.
        /// Two pairs are considered duplicates if they contain the same files 
        /// in any order: (A,B) is the same as (B,A).
        /// Time Complexity: O(n) where n is the number of items in binding lists.
        /// Uses HashSet for O(1) lookup performance.
        /// </summary>
        private void RemoveDuplicatePairs()
        {
            var seenPairs = new HashSet<string>();

            for (int i = bindingList1.Count - 1; i >= 0; i--)
            {
                // Create a normalized pair key - order doesn't matter (A,B) == (B,A)
                // Note: Pipe delimiter is safe as Windows prohibits it in file paths
                string pairKey = string.Compare(bindingList1[i].text, bindingList2[i].text, StringComparison.OrdinalIgnoreCase) < 0
                    ? bindingList1[i].text + "|" + bindingList2[i].text
                    : bindingList2[i].text + "|" + bindingList1[i].text;

                if (seenPairs.Contains(pairKey))
                {
                    // Duplicate pair found, remove it
                    bindingList1.RemoveAt(i);
                    bindingList2.RemoveAt(i);
                }
                else
                {
                    seenPairs.Add(pairKey);
                }
            }
        }

        /// <summary>
        /// Shows deletion results to the user via the application log panel
        /// </summary>
        /// <param name="deletedCount">Number of files successfully deleted from disk</param>
        /// <param name="requestedCount">Total number of files that were requested for deletion</param>
        private void ReportDeletionResults(int deletedCount, int requestedCount)
        {
            if (deletedCount > 0)
            {
                if (sendToRecycleBinMenuItem.IsChecked)
                {
                    console.Add(LocalizationManager.GetString("Console.SentToRecycleBin", deletedCount));
                }
                else
                {
                    console.Add(LocalizationManager.GetString("Console.FilesDeleted", deletedCount));
                }
            }
            else if (requestedCount > 0)
            {
                // Files were selected for deletion but none were successfully deleted
                if (sendToRecycleBinMenuItem.IsChecked)
                {
                    console.Add(LocalizationManager.GetString("Console.SentToRecycleBin", 0));
                }
                else
                {
                    console.Add(LocalizationManager.GetString("Console.FilesDeleted", 0));
                }
            }
        }

        private void RemoveFromListButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> selectedIndices = new List<int>();

            for (int i = 0; i < bindingList1.Count; i++)
            {
                if (bindingList1[i].isChecked)
                {
                    selectedIndices.Add(i);
                }
            }

            for (int i = 0; i < bindingList2.Count; i++)
            {
                if (bindingList2[i].isChecked)
                {
                    selectedIndices.Add(i);
                }
            }

            selectedIndices.Sort();

            for (int i = selectedIndices.Count - 1; i > -1; i--)
            {
                bindingList1.RemoveAt(selectedIndices[i]);
                bindingList2.RemoveAt(selectedIndices[i]);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (bindingList1 != null && bindingList1.Any())
            {
                ClearPopupWindow clearPopupWindow = new ClearPopupWindow(this);
                clearPopupWindow.ShowDialog();
            }
            else
            {
                Clear();
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int deleteItemCount = bindingList1.Count(x => x.state == (int)State.MarkedForDeletion) + bindingList2.Count(x => x.state == (int)State.MarkedForDeletion);
            int markAsFalsePositiveItemCount = bindingList1.Count(x => x.state == (int)State.MarkedAsFalsePositive);

            if (deleteItemCount > 0 || markAsFalsePositiveItemCount > 0)
            {
                ApplyPopupWindow applyPopupWindow = new ApplyPopupWindow(this, deleteItemCount, markAsFalsePositiveItemCount);
                applyPopupWindow.ShowDialog();
            }
        }

        private void MarkForDeletionButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < bindingList1.Count; i++)
            {
                if (bindingList1[i].isChecked)
                {
                    if (bindingList2[i].state != (int)State.MarkedForDeletion)
                    {
                        bindingList1[i].state = (int)State.MarkedForDeletion;
                    }
                    bindingList1[i].isChecked = false;
                }
            }

            for (int i = 0; i < bindingList2.Count; i++)
            {
                if (bindingList2[i].isChecked)
                {
                    if (bindingList1[i].state != (int)State.MarkedForDeletion)
                    {
                        bindingList2[i].state = (int)State.MarkedForDeletion;
                    }
                    bindingList2[i].isChecked = false;
                }
            }
        }

        private void MarkAsFalsePositiveButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < bindingList1.Count; i++)
            {
                if (bindingList1[i].isChecked)
                {
                    bindingList1[i].state = (int)State.MarkedAsFalsePositive;
                    bindingList2[i].state = (int)State.MarkedAsFalsePositive;
                    bindingList1[i].isChecked = false;
                }
            }

            for (int i = 0; i < bindingList2.Count; i++)
            {
                if (bindingList2[i].isChecked)
                {
                    bindingList1[i].state = (int)State.MarkedAsFalsePositive;
                    bindingList2[i].state = (int)State.MarkedAsFalsePositive;
                    bindingList2[i].isChecked = false;
                }
            }
        }

        private void RemoveMarkButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < bindingList1.Count; i++)
            {
                if (bindingList1[i].isChecked)
                {
                    if (bindingList1[i].state == (int)State.MarkedForDeletion)
                    {
                        bindingList1[i].state = (int)State.Normal;
                    }
                    else if (bindingList1[i].state == (int)State.MarkedAsFalsePositive)
                    {
                        bindingList1[i].state = (int)State.Normal;
                        bindingList2[i].state = (int)State.Normal;
                    }
                    bindingList1[i].isChecked = false;
                }
            }

            for (int i = 0; i < bindingList2.Count; i++)
            {
                if (bindingList2[i].isChecked)
                {
                    if (bindingList2[i].state == (int)State.MarkedForDeletion)
                    {
                        bindingList2[i].state = (int)State.Normal;
                    }
                    else if (bindingList2[i].state == (int)State.MarkedAsFalsePositive)
                    {
                        bindingList1[i].state = (int)State.Normal;
                        bindingList2[i].state = (int)State.Normal;
                    }
                    bindingList2[i].isChecked = false;
                }
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (pauseButton.Tag.Equals("0"))
            {
                pauseButton.Tag = "1";
                pauseButton.Content = LocalizationManager.GetString("Button.Resume");
                console.Add(LocalizationManager.GetString("Console.Paused"));

                pausedFirstTime = DateTime.Now.ToFileTime();

                bool otherThreadsRun = false;

                for (int i = 0; i < threadList.Count; i++)
                {
                    if (threadList.ElementAt(i).IsAlive)
                    {
                        threadList.ElementAt(i).Suspend();
                        otherThreadsRun = true;
                    }
                }

                if (!otherThreadsRun)
                {
                    processThread.Suspend();
                }
            }
            else
            {
                pauseButton.Tag = "0";
                pauseButton.Content = LocalizationManager.GetString("Button.Pause");

                console.RemoveAt(console.Count - 1);
                pausedSecondTime = DateTime.Now.ToFileTime();
                pauseTime += pausedSecondTime - pausedFirstTime;
                bool otherThreadsRun = false;

                for (int i = 0; i < threadList.Count; i++)
                {
                    if (threadList.ElementAt(i).ThreadState.Equals(ThreadState.Suspended))
                    {
                        threadList.ElementAt(i).Resume();
                        otherThreadsRun = true;
                    }
                }

                if (!otherThreadsRun)
                {
                    processThread.Resume();
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            findDuplicatesButton.Visibility = Visibility.Visible;
            pauseButton.Visibility = Visibility.Collapsed;
            stopButton.Visibility = Visibility.Collapsed;

            try
            {
                for (int i = 0; i < threadList.Count; i++)
                {
                    if (threadList.ElementAt(i).ThreadState.Equals(ThreadState.Suspended))
                    {
                        threadList.ElementAt(i).Resume();
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("StopButton_Click - Resume Threads", ex);
            }

            try
            {
                for (int i = 0; i < threadList.Count; i++)
                {
                    try
                    {
                        threadList.ElementAt(i).Abort();
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError($"StopButton_Click - Abort Thread {i}", ex);
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("StopButton_Click - Abort All Threads", ex);
            }

            try
            {
                processThread.Abort();
            }
            catch (ThreadStateException)
            {
                processThread.Resume();
            }

            pauseButton.Content = LocalizationManager.GetString("Button.Pause");

            if (pauseButton.Tag.Equals("1"))
            {
                console.RemoveAt(console.Count - 1);
            }

            directories = new List<string>();
            pauseButton.Tag = "0";
            percentage.Value = 0;
            files.Clear();
            bindingList1.Clear();
            bindingList2.Clear();
            list1.Clear();
            list2.Clear();

            console[console.Count - 1] = LocalizationManager.GetString("Console.InterruptedByUser");
        }

        private void ListView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && listView1.SelectedIndex != -1)
            {
                FileSystemHelper.SafeOpenFile(bindingList1[listView1.SelectedIndex].text);
            }
            else if (e.Key == Key.Right && listView1.SelectedIndex != -1)
            {
                listView2.SelectedIndex = listView1.SelectedIndex;
                ListViewItem item = listView2.ItemContainerGenerator.ContainerFromIndex(listView2.SelectedIndex) as ListViewItem;
                Keyboard.Focus(item);
            }
        }

        private void ListView2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && listView2.SelectedIndex != -1)
            {
                FileSystemHelper.SafeOpenFile(bindingList2[listView2.SelectedIndex].text);
            }
            else if (e.Key == Key.Left && listView2.SelectedIndex != -1)
            {
                listView1.SelectedIndex = listView2.SelectedIndex;
                ListViewItem item = listView1.ItemContainerGenerator.ContainerFromIndex(listView1.SelectedIndex) as ListViewItem;
                Keyboard.Focus(item);
            }
        }

        private void ListView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewDataItem item = ((FrameworkElement)e.OriginalSource).DataContext as ListViewDataItem;

            if (item != null)
            {
                if (listView1.SelectedIndex != -1)
                {
                    FileSystemHelper.SafeOpenFile(bindingList1[listView1.SelectedIndex].text);
                }
            }
        }

        private void ListView2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewDataItem item = ((FrameworkElement)e.OriginalSource).DataContext as ListViewDataItem;

            if (item != null)
            {
                if (listView2.SelectedIndex != -1)
                {
                    FileSystemHelper.SafeOpenFile(bindingList2[listView2.SelectedIndex].text);
                }
            }
        }

        private void ListView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView1.SelectedIndex == -1)
            {
                previewLabel.Visibility = Visibility.Visible;
                informationLabel1.Content = "";
                informationLabel2.Content = "";
                ReloadImage1(null);
                ReloadImage2(null);
            }
            else
            {
                bindingList2[listView1.Items.IndexOf(listView1.SelectedItems[listView1.SelectedItems.Count - 1])].isSelected = false;
                previewLabel.Visibility = Visibility.Collapsed;
                ReloadImage1(bindingList1.ElementAt(listView1.SelectedIndex).text);
                informationLabel1.Content = bindingList1.ElementAt(listView1.SelectedIndex).text.Substring(bindingList1.ElementAt(listView1.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView1.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView1.SelectedIndex).text)].Height;
                ReloadImage2(bindingList2.ElementAt(listView1.SelectedIndex).text);
                informationLabel2.Content = bindingList2.ElementAt(listView1.SelectedIndex).text.Substring(bindingList2.ElementAt(listView1.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView1.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView1.SelectedIndex).text)].Height;
            }
        }

        private void ListView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView2.SelectedIndex == -1)
            {
                previewLabel.Visibility = Visibility.Visible;
                informationLabel1.Content = "";
                informationLabel2.Content = "";
                ReloadImage1(null);
                ReloadImage2(null);
            }
            else
            {
                bindingList1[listView2.Items.IndexOf(listView2.SelectedItems[listView2.SelectedItems.Count - 1])].isSelected = false;
                previewLabel.Visibility = Visibility.Collapsed;
                ReloadImage1(bindingList1.ElementAt(listView2.SelectedIndex).text);
                informationLabel1.Content = bindingList1.ElementAt(listView2.SelectedIndex).text.Substring(bindingList1.ElementAt(listView2.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView2.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView2.SelectedIndex).text)].Height;
                ReloadImage2(bindingList2.ElementAt(listView2.SelectedIndex).text);
                informationLabel2.Content = bindingList2.ElementAt(listView2.SelectedIndex).text.Substring(bindingList2.ElementAt(listView2.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView2.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView2.SelectedIndex).text)].Height;
            }
        }

        private void ListView1_ScrollChanged(object sender, RoutedEventArgs e)
        {
            ScrollViewer listView1ScrollViewer = GetDescendantByType(listView1, typeof(ScrollViewer)) as ScrollViewer;
            ScrollViewer listView2ScrollViewer = GetDescendantByType(listView2, typeof(ScrollViewer)) as ScrollViewer;
            listView2ScrollViewer.ScrollToVerticalOffset(listView1ScrollViewer.VerticalOffset);
        }

        private void ListView2_ScrollChanged(object sender, RoutedEventArgs e)
        {
            ScrollViewer listView1ScrollViewer = GetDescendantByType(listView1, typeof(ScrollViewer)) as ScrollViewer;
            ScrollViewer listView2ScrollViewer = GetDescendantByType(listView2, typeof(ScrollViewer)) as ScrollViewer;
            listView1ScrollViewer.ScrollToVerticalOffset(listView2ScrollViewer.VerticalOffset);
        }

        private void ListView1_ListItemCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ListViewDataItem selectedItem = (ListViewDataItem)((CheckBox)sender).DataContext;
            bindingList2[bindingList1.IndexOf(selectedItem)].CheckboxEnabled = false;
        }

        private void ListView1_ListItemCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ListViewDataItem selectedItem = (ListViewDataItem)((CheckBox)sender).DataContext;
            bindingList2[bindingList1.IndexOf(selectedItem)].CheckboxEnabled = true;
        }

        private void ListView2_ListItemCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ListViewDataItem selectedItem = (ListViewDataItem)((CheckBox)sender).DataContext;
            bindingList1[bindingList2.IndexOf(selectedItem)].CheckboxEnabled = false;
        }

        private void ListView2_ListItemCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ListViewDataItem selectedItem = (ListViewDataItem)((CheckBox)sender).DataContext;
            bindingList1[bindingList2.IndexOf(selectedItem)].CheckboxEnabled = true;
        }

        private void ListView1_OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
            {
                FileSystemHelper.SafeOpenFile(bindingList1[listView1.SelectedIndex].text);
            }
        }

        private void ListView1_OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
            {
                string folderPath = FileSystemHelper.SafeGetDirectory(bindingList1[listView1.SelectedIndex].text);
                if (folderPath != null)
                {
                    FileSystemHelper.SafeOpenFolder(folderPath);
                }
            }
        }

        private void ListView2_OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView2.SelectedIndex != -1)
            {
                FileSystemHelper.SafeOpenFile(bindingList2[listView2.SelectedIndex].text);
            }
        }

        private void ListView2_OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView2.SelectedIndex != -1)
            {
                string folderPath = FileSystemHelper.SafeGetDirectory(bindingList2[listView2.SelectedIndex].text);
                if (folderPath != null)
                {
                    FileSystemHelper.SafeOpenFolder(folderPath);
                }
            }
        }

        private void OutputListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && findDuplicatesButton.Visibility == Visibility.Visible)
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OutputListView_Drop(object sender, DragEventArgs e)
        {
            if (findDuplicatesButton.Visibility == Visibility.Visible)
            {
                string[] dragDrop = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                if (dragDrop != null)
                {
                    DirectoryInfo directoryInfo;

                    if (directories.Count == 0)
                    {
                        console.Clear();
                        console.Add(LocalizationManager.GetString("Label.DragDropFolders"));
                    }

                    for (int i = 0; i < dragDrop.Length; i++)
                    {
                        directoryInfo = new DirectoryInfo(dragDrop[i]);

                        if (!directories.Contains(dragDrop[i]))
                        {
                            if (directoryInfo.Exists)
                            {
                                directories.Add(dragDrop[i]);
                                console.Insert(console.Count - 1, LocalizationManager.GetString("Console.DirectoryAdded", dragDrop[i]));
                            }
                            else
                            {
                                console.Insert(console.Count - 1, LocalizationManager.GetString("Console.DirectoriesOnly"));
                            }
                        }
                    }
                }
            }
        }

        private void PercentageChanged(object sender, EventArgs e)
        {
            Action<int> updateProgressBar = delegate (int value)
            {
                progressBar.Value = value;

                if (!findDuplicatesButton.IsVisible)
                {
                    if (comparing)
                    {
                        console[console.Count - 1] = LocalizationManager.GetString("Console.ComparingResults", value);
                    }
                    else
                    {
                        console[console.Count - 1] = LocalizationManager.GetString("Console.ProcessingFiles", value);
                    }
                }
            };

            lock (myLock)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, updateProgressBar, percentage.Value);
            }
        }

        private void PreviewImageBorder_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var st1 = (ScaleTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var st2 = (ScaleTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt1 = (TranslateTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
            var tt2 = (TranslateTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);

            st1.ScaleX *= e.DeltaManipulation.Scale.X;
            st2.ScaleX *= e.DeltaManipulation.Scale.X;
            st1.ScaleY *= e.DeltaManipulation.Scale.X;
            st2.ScaleY *= e.DeltaManipulation.Scale.X;
            tt1.X += e.DeltaManipulation.Translation.X;
            tt2.X += e.DeltaManipulation.Translation.X;
            tt1.Y += e.DeltaManipulation.Translation.Y;
            tt2.Y += e.DeltaManipulation.Translation.Y;
        }

        private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoom = e.Delta > 0 ? .2 : -.2;
            var position = e.GetPosition(previewImage1);
            previewImage1.RenderTransformOrigin = new System.Windows.Point(position.X / previewImage1.ActualWidth, position.Y / previewImage1.ActualHeight);
            previewImage2.RenderTransformOrigin = new System.Windows.Point(position.X / previewImage2.ActualWidth, position.Y / previewImage2.ActualHeight);
            var st1 = (ScaleTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var st2 = (ScaleTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);

            if (zoom < 0 && (st1.ScaleX <= 1 || st2.ScaleX <= 1))
            {
                return;
            }
            st1.ScaleX += zoom;
            st2.ScaleX += zoom;
            st1.ScaleY += zoom;
            st2.ScaleY += zoom;
        }

        private void PreviewImage1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ResetPanZoom1();
                ResetPanZoom2();
            }
            else
            {
                previewImage1.CaptureMouse();
                var tt1 = (TranslateTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                previewImage1Start = e.GetPosition(this);
                previewImage2Start = e.GetPosition(this);
                previewImage1Origin = new System.Windows.Point(tt1.X, tt1.Y);
                previewImage2Origin = new System.Windows.Point(tt2.X, tt2.Y);
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            var zoom = .2;
            previewImage1.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            previewImage2.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            var st1 = (ScaleTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var st2 = (ScaleTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);

            if (zoom < 0 && (st1.ScaleX <= 1 || st2.ScaleX <= 1))
            {
                return;
            }
            st1.ScaleX += zoom;
            st2.ScaleX += zoom;
            st1.ScaleY += zoom;
            st2.ScaleY += zoom;
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            var zoom = -.2;
            previewImage1.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            previewImage2.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            var st1 = (ScaleTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var st2 = (ScaleTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);

            if (zoom < 0 && (st1.ScaleX <= 1 || st2.ScaleX <= 1))
            {
                return;
            }
            st1.ScaleX += zoom;
            st2.ScaleX += zoom;
            st1.ScaleY += zoom;
            st2.ScaleY += zoom;
        }

        private void PreviewImage2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ResetPanZoom1();
                ResetPanZoom2();
            }
            else
            {
                previewImage2.CaptureMouse();
                var tt1 = (TranslateTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                previewImage1Start = e.GetPosition(this);
                previewImage2Start = e.GetPosition(this);
                previewImage1Origin = new System.Windows.Point(tt1.X, tt1.Y);
                previewImage2Origin = new System.Windows.Point(tt2.X, tt2.Y);
            }
        }

        private void PreviewImage1_MouseMove(object sender, MouseEventArgs e)
        {
            if (previewImage1.IsMouseCaptured)
            {
                var tt1 = (TranslateTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var v1 = previewImage1Start - e.GetPosition(this);
                var v2 = previewImage2Start - e.GetPosition(this);
                tt1.X = previewImage1Origin.X - v1.X;
                tt2.X = previewImage2Origin.X - v2.X;
                tt1.Y = previewImage1Origin.Y - v1.Y;
                tt2.Y = previewImage2Origin.Y - v2.Y;
            }
        }

        private void PreviewImage2_MouseMove(object sender, MouseEventArgs e)
        {
            if (previewImage2.IsMouseCaptured)
            {
                var tt1 = (TranslateTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var v1 = previewImage1Start - e.GetPosition(this);
                var v2 = previewImage2Start - e.GetPosition(this);
                tt1.X = previewImage1Origin.X - v1.X;
                tt2.X = previewImage2Origin.X - v2.X;
                tt1.Y = previewImage1Origin.Y - v1.Y;
                tt2.Y = previewImage2Origin.Y - v2.Y;
            }
        }

        private void PreviewImage1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            previewImage1.ReleaseMouseCapture();
        }

        private void PreviewImage2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            previewImage2.ReleaseMouseCapture();
        }

        protected MainWindow(SerializationInfo info, StreamingContext ctxt)
        {
            jpegMenuItemChecked = (bool)info.GetValue("jpegMenuItemChecked", typeof(bool));
            gifMenuItemChecked = (bool)info.GetValue("gifMenuItemChecked", typeof(bool));
            pngMenuItemChecked = (bool)info.GetValue("pngMenuItemChecked", typeof(bool));
            bmpMenuItemChecked = (bool)info.GetValue("bmpMenuItemChecked", typeof(bool));
            tiffMenuItemChecked = (bool)info.GetValue("tiffMenuItemChecked", typeof(bool));
            icoMenuItemChecked = (bool)info.GetValue("icoMenuItemChecked", typeof(bool));
            sendsToRecycleBin = (bool)info.GetValue("sendsToRecycleBin", typeof(bool));
            try
            {
                currentLanguageCode = (string)info.GetValue("currentLanguageCode", typeof(string));
                
                // List of valid language codes
                var validLanguages = new[] { 
                    "en-US", "tr-TR", "ja-JP", "es-ES", "fr-FR", "de-DE", "it-IT", 
                    "pt-BR", "ru-RU", "zh-CN", "ko-KR", "ar-SA", "fa-IR", "hi-IN", 
                    "nl-NL", "pl-PL", "sv-SE", "nb-NO", "da-DK" 
                };
                
                // Validate and default to en-US if null or invalid
                if (string.IsNullOrEmpty(currentLanguageCode) || !validLanguages.Contains(currentLanguageCode))
                {
                    currentLanguageCode = "en-US";
                }
            }
            catch (Exception ex)
            {
                // Default to English if field doesn't exist or there's any serialization error
                ErrorLogger.LogError("Deserialize - Get Language Code", ex);
                currentLanguageCode = "en-US";
            }
            includeSubfolders = (bool)info.GetValue("includeSubfolders", typeof(bool));
            skipFilesWithDifferentOrientation = (bool)info.GetValue("skipFilesWithDifferentOrientation", typeof(bool));
            duplicatesOnly = (bool)info.GetValue("duplicatesOnly", typeof(bool));
            files = (List<string>)info.GetValue("files", typeof(List<string>));
            falsePositiveList1 = (List<string>)info.GetValue("falsePositiveList1", typeof(List<string>));
            falsePositiveList2 = (List<string>)info.GetValue("falsePositiveList2", typeof(List<string>));
            resolutionArray = (System.Drawing.Size[])info.GetValue("resolutionArray", typeof(System.Drawing.Size[]));
            bindingList1 = (ObservableCollection<ListViewDataItem>)info.GetValue("bindingList1", typeof(ObservableCollection<ListViewDataItem>));
            bindingList2 = (ObservableCollection<ListViewDataItem>)info.GetValue("bindingList2", typeof(ObservableCollection<ListViewDataItem>));
            console = (ObservableCollection<string>)info.GetValue("console", typeof(ObservableCollection<string>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("jpegMenuItemChecked", jpegMenuItem.IsChecked);
            info.AddValue("gifMenuItemChecked", gifMenuItem.IsChecked);
            info.AddValue("pngMenuItemChecked", pngMenuItem.IsChecked);
            info.AddValue("bmpMenuItemChecked", bmpMenuItem.IsChecked);
            info.AddValue("tiffMenuItemChecked", tiffMenuItem.IsChecked);
            info.AddValue("icoMenuItemChecked", icoMenuItem.IsChecked);
            info.AddValue("sendsToRecycleBin", sendToRecycleBinMenuItem.IsChecked);
            info.AddValue("currentLanguageCode", currentLanguageCode);
            info.AddValue("includeSubfolders", includeSubfoldersMenuItem.IsChecked);
            info.AddValue("skipFilesWithDifferentOrientation", skipFilesWithDifferentOrientationMenuItem.IsChecked);
            info.AddValue("duplicatesOnly", findExactDuplicatesOnlyMenuItem.IsChecked);
            info.AddValue("files", files);
            info.AddValue("falsePositiveList1", falsePositiveList1);
            info.AddValue("falsePositiveList2", falsePositiveList2);
            info.AddValue("resolutionArray", resolutionArray);
            info.AddValue("bindingList1", bindingList1);
            info.AddValue("bindingList2", bindingList2);
            info.AddValue("console", console);
        }

        public void Serialize(string path)
        {
            // Klasör yolunu al ve oluştur
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (Stream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(stream, this);
            }
        }

        public void Deserialize(string path)
        {
            // Klasör yolunu al ve oluştur
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Dosya yoksa hiçbir şey yapma (ilk kez açılıyorsa)
            if (!File.Exists(path))
            {
                opening = false;
                return;
            }

            using (Stream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                mainWindow = (MainWindow)bformatter.Deserialize(stream);
            }

            if (opening)
            {
                opening = false;

                skipFilesWithDifferentOrientationMenuItem.IsChecked = mainWindow.skipFilesWithDifferentOrientation;
                findExactDuplicatesOnlyMenuItem.IsChecked = mainWindow.duplicatesOnly;
                includeSubfoldersMenuItem.IsChecked = mainWindow.includeSubfolders;
                jpegMenuItem.IsChecked = mainWindow.jpegMenuItemChecked;
                gifMenuItem.IsChecked = mainWindow.gifMenuItemChecked;
                pngMenuItem.IsChecked = mainWindow.pngMenuItemChecked;
                bmpMenuItem.IsChecked = mainWindow.bmpMenuItemChecked;
                tiffMenuItem.IsChecked = mainWindow.tiffMenuItemChecked;
                icoMenuItem.IsChecked = mainWindow.icoMenuItemChecked;
                falsePositiveList1 = mainWindow.falsePositiveList1;
                falsePositiveList2 = mainWindow.falsePositiveList2;

                if (!mainWindow.sendsToRecycleBin)
                {
                    sendToRecycleBinMenuItem.IsChecked = false;
                    deletePermanentlyMenuItem.IsChecked = true;
                    sendToRecycleBinMenuItem.IsEnabled = true;
                    deletePermanentlyMenuItem.IsEnabled = false;
                }

                // Set language based on saved currentLanguageCode
                string languageToSet = mainWindow.currentLanguageCode;
                
                if (string.IsNullOrEmpty(languageToSet))
                {
                    languageToSet = "en-US";
                }
                
                // Set menu states for the language
                SetLanguageMenuStates(languageToSet);
                
                currentLanguageCode = languageToSet;
                LocalizationManager.SetLanguage(languageToSet);
                UpdateUI();
            }
            else
            {
                bindingList1 = mainWindow.bindingList1;
                bindingList2 = mainWindow.bindingList2;
                console = mainWindow.console;
                files = mainWindow.files;
                resolutionArray = mainWindow.resolutionArray;
                listView1.ItemsSource = bindingList1;
                listView2.ItemsSource = bindingList2;
                outputListView.ItemsSource = console;
                addFolderButton.Visibility = Visibility.Hidden;
                findDuplicatesButton.Visibility = Visibility.Hidden;
                clearResultsButton.Visibility = Visibility.Visible;
                applyButton.Visibility = Visibility.Visible;
                deleteSelectedButton.IsEnabled = true;
                removeFromListButton.IsEnabled = true;
                markForDeletionButton.IsEnabled = true;
                markAsFalsePositiveButton.IsEnabled = true;
                removeMarkButton.IsEnabled = true;
                saveResultsMenuItem.IsEnabled = true;
            }
            mainWindow = null;
        }

        private void UpdateUI()
        {
            // Update menu items
            fileMenuItem.Header = LocalizationManager.GetString("Menu.File");
            saveResultsMenuItem.Header = LocalizationManager.GetString("Menu.SaveResults");
            loadResultsMenuItem.Header = LocalizationManager.GetString("Menu.LoadResults");
            exitMenuItem.Header = LocalizationManager.GetString("Menu.Exit");
            optionsMenuItem.Header = LocalizationManager.GetString("Menu.Options");
            searchFormatsMenuItem.Header = LocalizationManager.GetString("Menu.SearchFormats");
            deletionMethodMenuItem.Header = LocalizationManager.GetString("Menu.DeletionMethod");
            sendToRecycleBinMenuItem.Header = LocalizationManager.GetString("Menu.SendToRecycleBin");
            deletePermanentlyMenuItem.Header = LocalizationManager.GetString("Menu.DeletePermanently");
            languageMenuItem.Header = LocalizationManager.GetString("Menu.Language");
            includeSubfoldersMenuItem.Header = LocalizationManager.GetString("Menu.IncludeSubfolders");
            skipFilesWithDifferentOrientationMenuItem.Header = LocalizationManager.GetString("Menu.SkipDifferentOrientation");
            findExactDuplicatesOnlyMenuItem.Header = LocalizationManager.GetString("Menu.FindExactDuplicatesOnly");
            clearFalsePositiveDatabaseButton.Header = LocalizationManager.GetString("Menu.ClearFalsePositiveDB");
            resetToDefaultsMenuItem.Header = LocalizationManager.GetString("Menu.ResetToDefaults");
            helpMenuItem.Header = LocalizationManager.GetString("Menu.Help");
            howToUseMenuItem.Header = LocalizationManager.GetString("Menu.HowToUse");
            viewErrorLogMenuItem.Header = LocalizationManager.GetString("Menu.ViewErrorLog");
            aboutMenuItem.Header = LocalizationManager.GetString("Menu.About");

            // Update buttons
            addFolderButton.Content = LocalizationManager.GetString("Button.AddFolder");
            findDuplicatesButton.Content = LocalizationManager.GetString("Button.FindDuplicates");
            pauseButton.Content = LocalizationManager.GetString("Button.Pause");
            stopButton.Content = LocalizationManager.GetString("Button.Stop");
            clearResultsButton.Content = LocalizationManager.GetString("Button.ClearResults");
            applyButton.Content = LocalizationManager.GetString("Button.Apply");
            deleteSelectedButton.Content = LocalizationManager.GetString("Button.DeleteSelected");
            removeFromListButton.Content = LocalizationManager.GetString("Button.RemoveFromList");
            clearButton.Content = LocalizationManager.GetString("Button.Clear");
            markForDeletionButton.Content = LocalizationManager.GetString("Button.MarkForDeletion");
            markAsFalsePositiveButton.Content = LocalizationManager.GetString("Button.MarkAsFalsePositive");
            removeMarkButton.Content = LocalizationManager.GetString("Button.RemoveMark");

            // Update context menu items
            listView1OpenMenuItem.Header = LocalizationManager.GetString("ContextMenu.Open");
            listView1OpenFileLocationMenuItem.Header = LocalizationManager.GetString("ContextMenu.OpenFileLocation");
            listView2OpenMenuItem.Header = LocalizationManager.GetString("ContextMenu.Open");
            listView2OpenFileLocationMenuItem.Header = LocalizationManager.GetString("ContextMenu.OpenFileLocation");

            // Update labels
            previewLabel.Content = LocalizationManager.GetString("Label.PreviewSelect");

            // Update console
            console.Clear();
            console.Add(LocalizationManager.GetString("Label.DragDropFolders"));
        }

        public void Clear()
        {
            clearResultsButton.Visibility = Visibility.Hidden;
            applyButton.Visibility = Visibility.Hidden;
            addFolderButton.Visibility = Visibility.Visible;
            findDuplicatesButton.Visibility = Visibility.Visible;
            directories.Clear();
            files.Clear();
            console.Clear();
            bindingList1.Clear();
            bindingList2.Clear();
            list1.Clear();
            list2.Clear();
            percentage.Value = 0;

            console.Add(LocalizationManager.GetString("Label.DragDropFolders"));
        }

        public void Apply(int deleteItemCount, int markAsFalsePositiveItemCount)
        {
            if (deleteItemCount > 0)
            {
                deleteMarkedItems = true;
                DeleteSelectedButton_Click(this, null);
                deleteMarkedItems = false;
            }

            if (markAsFalsePositiveItemCount > 0)
            {
                List<int> falsePositiveIndices = new List<int>();

                for (int i = 0; i < bindingList1.Count; i++)
                {
                    if (bindingList1[i].state == (int)State.MarkedAsFalsePositive)
                    {
                        falsePositiveList1.Add(bindingList1[i].sha256Checksum);
                        falsePositiveList2.Add(bindingList2[i].sha256Checksum);
                        falsePositiveIndices.Add(i);
                    }
                }

                falsePositiveIndices.Sort();

                for (int i = falsePositiveIndices.Count - 1; i > -1; i--)
                {
                    bindingList1.RemoveAt(falsePositiveIndices[i]);
                    bindingList2.RemoveAt(falsePositiveIndices[i]);
                }

                try
                {
                    Serialize(path + @"\Bin\Image Comparator.imc");
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                }

                console.Add(LocalizationManager.GetString("Console.MarkedAsFalsePositive", markAsFalsePositiveItemCount));
            }
        }

        private Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            else if (element.GetType() == type)
            {
                return element;
            }
            else
            {
                Visual foundElement = null;

                if (element is FrameworkElement)
                {
                    (element as FrameworkElement).ApplyTemplate();
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {

                    Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                    foundElement = GetDescendantByType(visual, type);

                    if (foundElement != null)
                    {
                        break;
                    }
                }
                return foundElement;
            }
        }

        private void ProcessThreadStart()
        {
            FastDCT2D fastDCT2D;
            int[,] result;
            double average;
            int i;

            using (SHA256Managed sha = new SHA256Managed())
            {
                while (processThreadsiAsync < files.Count)
                {
                    lock (myLock)
                    {
                        i = processThreadsiAsync;
                        processThreadsiAsync++;
                        percentage.Value = 100 - (int)Math.Round(100.0 * (files.Count - i) / files.Count);
                    }

                    try
                    {
                        if (duplicatesOnly)
                        {
                            using (Bitmap image = new Bitmap(files[i]))
                            {
                                resolutionArray[i] = image.Size;

                                if (image.Width > image.Height)
                                {
                                    orientationArray[i] = Orientation.Horizontal;
                                }
                                else
                                {
                                    orientationArray[i] = Orientation.Vertical;
                                }
                            }

                            //SHA256 Calculation
                            using (FileStream stream = File.OpenRead(files[i]))
                            {
                                byte[] hash = sha.ComputeHash(stream);
                                sha256Array[i] = BitConverter.ToString(hash).Replace("-", string.Empty);
                            }
                        }
                        else
                        {
                            using (Bitmap image = new Bitmap(files[i]))
                            {
                                resolutionArray[i] = image.Size;

                                if (image.Width > image.Height)
                                {
                                    orientationArray[i] = Orientation.Horizontal;
                                }
                                else
                                {
                                    orientationArray[i] = Orientation.Vertical;
                                }

                                using (Bitmap resized32 = ResizeImage(image, PHASH_RESIZE_DIMENSION, PHASH_RESIZE_DIMENSION))
                                {
                                    fastDCT2D = new FastDCT2D(resized32, 32);
                                    result = fastDCT2D.FastDCT();

                                    //pHash(Perceptual Hash) Calculation
                                    average = 0;
                                    for (int j = 0; j < 8; j++)
                                    {
                                        for (int k = 0; k < 8; k++)
                                        {
                                            average += result[j, k];
                                        }
                                    }

                                    average -= result[0, 0];
                                    average /= 63;

                                    for (int j = 0; j < 8; j++)
                                    {
                                        for (int k = 0; k < 8; k++)
                                        {
                                            pHashArray[i, j * 8 + k] = result[j, k] < average ? 0 : 1;
                                        }
                                    }

                                    using (Bitmap resized9 = ResizeImage(resized32, DHASH_RESIZE_DIMENSION, DHASH_RESIZE_DIMENSION))
                                    using (Bitmap grayscale = ConvertToGrayscale(resized9))
                                    {
                                        //hdHash(Horizontal Difference Hash) Calculation
                                        for (int j = 0; j < 8; j++)
                                        {
                                            for (int k = 0; k < 9; k++)
                                            {
                                                hdHashArray[i, j * 8 + k] = grayscale.GetPixel(j, k).R < grayscale.GetPixel(j + 1, k).R ? 0 : 1;
                                            }
                                        }

                                        //vdHash(Vertical Difference Hash) Calculation
                                        for (int j = 0; j < 9; j++)
                                        {
                                            for (int k = 0; k < 8; k++)
                                            {
                                                vdHashArray[i, j * 8 + k] = grayscale.GetPixel(j, k).R < grayscale.GetPixel(j, k + 1).R ? 0 : 1;
                                            }
                                        }

                                        //aHash(Average Hash) Calculation
                                        using (Bitmap resized8 = ResizeImage(grayscale, AHASH_RESIZE_DIMENSION, AHASH_RESIZE_DIMENSION))
                                        {
                                            average = 0;

                                            for (int j = 0; j < 8; j++)
                                            {
                                                for (int k = 0; k < 8; k++)
                                                {
                                                    average += resized8.GetPixel(j, k).R;
                                                }
                                            }

                                            average /= 64;

                                            for (int j = 0; j < 8; j++)
                                            {
                                                for (int k = 0; k < 8; k++)
                                                {
                                                    aHashArray[i, j * 8 + k] = resized8.GetPixel(j, k).R < average ? 0 : 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            //SHA256 Calculation
                            using (FileStream stream = File.OpenRead(files[i]))
                            {
                                byte[] hash = sha.ComputeHash(stream);
                                sha256Array[i] = BitConverter.ToString(hash).Replace("-", string.Empty);
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            pHashArray[i, 0] = -1;
                        }
                        catch (OutOfMemoryException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.LogError($"ProcessThreadStart - Mark Invalid Image {i}", ex);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogError($"ProcessThreadStart - Process Image {i} ({Path.GetFileName(files[i])})", ex);
                    }
                }
            }
        }

        private void CompareResultsThreadStart()
        {
            int i, j;
            bool isDuplicate;

            while (compareResultsiAsync < files.Count - 1)
            {
                lock (myLock)
                {
                    i = compareResultsiAsync;
                    compareResultsiAsync++;
                    percentage.Value = 100 - (int)Math.Round(100.0 * (files.Count - i) / files.Count);
                }

                for (j = i + 1; j < files.Count; j++)
                {
                    if (!skipFilesWithDifferentOrientation || orientationArray[i] == orientationArray[j])
                    {
                        isDuplicate = FindSimilarity(i, j);

                        if (isDuplicate)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void Run()
        {
            Action updateUI = delegate ()
            {
                // Optimize false positive removal using HashSet for O(1) lookups instead of O(n²) nested loops
                // Build HashSet of false positive pairs - O(n)
                var falsePositivePairs = new HashSet<string>();
                for (int i = 0; i < falsePositiveList1.Count; i++)
                {
                    // Create normalized pair key - order doesn't matter (A,B) == (B,A)
                    // Note: Pipe delimiter is safe as SHA256 checksums are hexadecimal without pipe characters
                    string falsePositivePairKey = string.Compare(falsePositiveList1[i], falsePositiveList2[i], StringComparison.OrdinalIgnoreCase) < 0
                        ? falsePositiveList1[i] + "|" + falsePositiveList2[i]
                        : falsePositiveList2[i] + "|" + falsePositiveList1[i];
                    falsePositivePairs.Add(falsePositivePairKey);
                }

                // Single pass removal - O(n) with O(1) lookups
                for (int i = list1.Count - 1; i >= 0; i--)
                {
                    // Create normalized pair key for current item
                    string pairKey = string.Compare(list1[i].sha256Checksum, list2[i].sha256Checksum, StringComparison.OrdinalIgnoreCase) < 0
                        ? list1[i].sha256Checksum + "|" + list2[i].sha256Checksum
                        : list2[i].sha256Checksum + "|" + list1[i].sha256Checksum;

                    if (falsePositivePairs.Contains(pairKey))  // O(1) lookup
                    {
                        // Save confidence before removing the item
                        int confidence = list1[i].confidence;

                        list1.RemoveAt(i);
                        list2.RemoveAt(i);

                        if (confidence == (int)Confidence.Low)
                        {
                            lowConfidenceSimilarImageCount--;
                        }
                        else if (confidence == (int)Confidence.Medium)
                        {
                            mediumConfidenceSimilarImageCount--;
                        }
                        else if (confidence == (int)Confidence.High)
                        {
                            highConfidenceSimilarImageCount--;
                        }
                        else
                        {
                            duplicateImageCount--;
                        }
                    }
                }

                MergeSort(list1, list2);
                bindingList1 = new ObservableCollection<ListViewDataItem>(list1);
                bindingList2 = new ObservableCollection<ListViewDataItem>(list2);
                listView1.ItemsSource = bindingList1;
                listView2.ItemsSource = bindingList2;
                addFolderButton.Visibility = Visibility.Collapsed;
                pauseButton.Visibility = Visibility.Collapsed;
                stopButton.Visibility = Visibility.Collapsed;
                clearResultsButton.Visibility = Visibility.Visible;
                applyButton.Visibility = Visibility.Visible;
                deleteSelectedButton.IsEnabled = true;
                removeFromListButton.IsEnabled = true;
                markForDeletionButton.IsEnabled = true;
                markAsFalsePositiveButton.IsEnabled = true;
                removeMarkButton.IsEnabled = true;
                saveResultsMenuItem.IsEnabled = true;

                pauseButton.Content = LocalizationManager.GetString("Button.Pause");

                console.Add(LocalizationManager.GetString("Console.AllDone"));

                secondTime = DateTime.Now.ToFileTime();
                timeDifferenceInSeconds = (int)Math.Ceiling(((secondTime - firstTime - pauseTime) / 10000000.0));

                if (timeDifferenceInSeconds >= 3600)
                {
                    int hours = timeDifferenceInSeconds / 3600;
                    int minutes = (timeDifferenceInSeconds / 60) % 60;
                    int seconds = timeDifferenceInSeconds % 60;
                    console.Add(LocalizationManager.GetString("Console.RunTimeHours", hours, minutes, seconds));
                }
                else if (timeDifferenceInSeconds >= 60)
                {
                    int minutes = timeDifferenceInSeconds / 60;
                    int seconds = timeDifferenceInSeconds % 60;
                    console.Add(LocalizationManager.GetString("Console.RunTimeMinutes", minutes, seconds));
                }
                else
                {
                    console.Add(LocalizationManager.GetString("Console.RunTimeSeconds", timeDifferenceInSeconds));
                }

                if (duplicateImageCount == 0 && highConfidenceSimilarImageCount == 0 && mediumConfidenceSimilarImageCount == 0 && lowConfidenceSimilarImageCount == 0)
                {
                    console.Add(LocalizationManager.GetString("Console.NoDuplicatesFound"));
                }
                else
                {
                    if (duplicateImageCount > 0)
                    {
                        console.Add(LocalizationManager.GetString("Console.DuplicatesFound", duplicateImageCount));
                    }

                    if (highConfidenceSimilarImageCount > 0)
                    {
                        console.Add(LocalizationManager.GetString("Console.HighSimilarity", highConfidenceSimilarImageCount));
                    }

                    if (mediumConfidenceSimilarImageCount > 0)
                    {
                        console.Add(LocalizationManager.GetString("Console.MediumSimilarity", mediumConfidenceSimilarImageCount));
                    }

                    if (lowConfidenceSimilarImageCount > 0)
                    {
                        console.Add(LocalizationManager.GetString("Console.LowSimilarity", lowConfidenceSimilarImageCount));
                    }
                }
            };

            Action updateConsole1 = delegate ()
            {
                console.Add(LocalizationManager.GetString("Console.PathsAddedSuccessfully", files.Count));
                console.Add(LocalizationManager.GetString("Console.ProcessingFiles", 0));
            };

            Action updateConsole2 = delegate ()
            {
                console[console.Count - 1] = LocalizationManager.GetString("Console.FilesProcessedComparing");
            };

            AddFiles(directories);
            directories.Clear();
            Dispatcher.Invoke(DispatcherPriority.Normal, updateConsole1);

            processThreadsiAsync = 0;
            compareResultsiAsync = 0;
            duplicateImageCount = 0;
            highConfidenceSimilarImageCount = 0;
            mediumConfidenceSimilarImageCount = 0;
            lowConfidenceSimilarImageCount = 0;
            pHashArray = new int[files.Count, 64];
            hdHashArray = new int[files.Count, 72];
            vdHashArray = new int[files.Count, 72];
            aHashArray = new int[files.Count, 64];
            sha256Array = new string[files.Count];
            orientationArray = new Orientation[files.Count];
            resolutionArray = new System.Drawing.Size[files.Count];
            threadList = new List<Thread>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.Add(new Thread(ProcessThreadStart));
                threadList.ElementAt(i).Start();
            }

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.ElementAt(i).Join();
            }

            threadList = new List<Thread>();
            comparing = true;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.Add(new Thread(CompareResultsThreadStart));
                threadList.ElementAt(i).Start();
            }

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.ElementAt(i).Join();
            }

            percentage.Value = 100;
            Dispatcher.Invoke(DispatcherPriority.Normal, updateConsole2);
            Dispatcher.Invoke(DispatcherPriority.Normal, updateUI);
        }

        private void WriteToFile(List<string> input)
        {
            using (streamWriter = new StreamWriter(path + @"\Bin\Directories.imc"))
            {
                streamWriter.WriteLine(input.Count);

                for (int i = 0; i < input.Count; i++)
                {
                    streamWriter.WriteLine(input.ElementAt(i));
                }
            }

            using (streamWriter = new StreamWriter(path + @"\Bin\Filters.imc"))
            {
                streamWriter.WriteLine(includeSubfolders);
                streamWriter.WriteLine(jpegMenuItemChecked);
                streamWriter.WriteLine(gifMenuItemChecked);
                streamWriter.WriteLine(pngMenuItemChecked);
                streamWriter.WriteLine(bmpMenuItemChecked);
                streamWriter.WriteLine(tiffMenuItemChecked);
                streamWriter.WriteLine(icoMenuItemChecked);
            }
        }

        private void AddFiles(List<string> directory)
        {
            WriteToFile(directory);
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo(path + (@"\Bin\AddFiles.exe"));
            processStartInfo.RedirectStandardOutput = false;
            processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            processStartInfo.UseShellExecute = true;
            process = System.Diagnostics.Process.Start(processStartInfo);
            process.WaitForExit();
            ReadFromFile();
        }

        private void ReadFromFile()
        {
            int count;
            using (streamReader = new StreamReader(path + @"\Bin\Results.imc"))
            {
                gotException = bool.Parse(streamReader.ReadLine());
                count = int.Parse(streamReader.ReadLine());

                for (int i = 0; i < count; i++)
                {
                    files.Add(streamReader.ReadLine());
                }
            }
            File.Delete(path + @"\Bin\Results.imc");
        }

        private Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            try
            {
                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (Graphics graphics = Graphics.FromImage(destImage))
                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
                
                return destImage;
            }
            catch
            {
                destImage?.Dispose();
                throw;
            }
        }

        private Bitmap ConvertToGrayscale(Bitmap inputImage)
        {
            Bitmap cloneImage = (Bitmap)inputImage.Clone();
            
            try
            {
                using (Graphics graphics = Graphics.FromImage(cloneImage))
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    ColorMatrix colorMatrix = new ColorMatrix(new float[][]{
                        new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                        new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                        new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                        new float[] {     0,      0,      0, 1, 0},
                        new float[] {     0,      0,      0, 0, 0}
                    });
                    attributes.SetColorMatrix(colorMatrix);
                    graphics.DrawImage(cloneImage, new Rectangle(0, 0, cloneImage.Width, cloneImage.Height), 0, 0, cloneImage.Width, cloneImage.Height, GraphicsUnit.Pixel, attributes);
                }
                
                return cloneImage;
            }
            catch
            {
                cloneImage?.Dispose();
                throw;
            }
        }

        private bool FindSimilarity(int i, int j)
        {
            if (pHashArray[i, 0] != -1 && pHashArray[j, 0] != -1)
            {
                int pHashHammingDistance = 0, hdHashHammingDistance = 0, vdHashHammingDistance = 0, aHashHammingDistance = 0;

                if (duplicatesOnly)
                {
                    if (sha256Array[i] != sha256Array[j])
                    {
                        return false;
                    }

                    lock (myLock2)
                    {
                        list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.Duplicate, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                        list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.Duplicate, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                        duplicateImageCount++;
                    }
                    return true;
                }
                else
                {
                    for (int k = 1; k < 64; k++)
                    {
                        if (pHashArray[i, k] != pHashArray[j, k])
                        {
                            pHashHammingDistance++;
                        }
                    }

                    for (int k = 0; k < 72; k++)
                    {
                        if (hdHashArray[i, k] != hdHashArray[j, k])
                        {
                            hdHashHammingDistance++;
                        }
                    }

                    for (int k = 0; k < 72; k++)
                    {
                        if (vdHashArray[i, k] != vdHashArray[j, k])
                        {
                            vdHashHammingDistance++;
                        }
                    }

                    for (int k = 0; k < 64; k++)
                    {
                        if (aHashArray[i, k] != aHashArray[j, k])
                        {
                            aHashHammingDistance++;
                        }
                    }

                    if (sha256Array[i] == sha256Array[j] && pHashHammingDistance < EXACT_DUPLICATE_THRESHOLD && hdHashHammingDistance < EXACT_DUPLICATE_THRESHOLD && vdHashHammingDistance < EXACT_DUPLICATE_THRESHOLD && aHashHammingDistance < EXACT_DUPLICATE_THRESHOLD)
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.Duplicate, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.Duplicate, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            duplicateImageCount++;
                        }
                        return true;
                    }
                    else if (pHashHammingDistance < PHASH_HIGH_CONFIDENCE_THRESHOLD && hdHashHammingDistance < HDHASH_HIGH_CONFIDENCE_THRESHOLD && vdHashHammingDistance < VDHASH_HIGH_CONFIDENCE_THRESHOLD && aHashHammingDistance < AHASH_HIGH_CONFIDENCE_THRESHOLD)
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.High, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.High, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            highConfidenceSimilarImageCount++;
                        }
                    }
                    else if ((pHashHammingDistance < PHASH_HIGH_CONFIDENCE_THRESHOLD && hdHashHammingDistance < HDHASH_MEDIUM_CONFIDENCE_THRESHOLD && vdHashHammingDistance < VDHASH_MEDIUM_CONFIDENCE_THRESHOLD && aHashHammingDistance < AHASH_MEDIUM_CONFIDENCE_THRESHOLD) || (hdHashHammingDistance < HDHASH_HIGH_CONFIDENCE_THRESHOLD && pHashHammingDistance < PHASH_MEDIUM_CONFIDENCE_THRESHOLD && vdHashHammingDistance < VDHASH_MEDIUM_CONFIDENCE_THRESHOLD && aHashHammingDistance < AHASH_MEDIUM_CONFIDENCE_THRESHOLD) || (vdHashHammingDistance < VDHASH_HIGH_CONFIDENCE_THRESHOLD && pHashHammingDistance < PHASH_MEDIUM_CONFIDENCE_THRESHOLD && hdHashHammingDistance < HDHASH_MEDIUM_CONFIDENCE_THRESHOLD && aHashHammingDistance < AHASH_MEDIUM_CONFIDENCE_THRESHOLD) || (aHashHammingDistance < AHASH_HIGH_CONFIDENCE_THRESHOLD && pHashHammingDistance < PHASH_MEDIUM_CONFIDENCE_THRESHOLD && hdHashHammingDistance < HDHASH_MEDIUM_CONFIDENCE_THRESHOLD && vdHashHammingDistance < VDHASH_MEDIUM_CONFIDENCE_THRESHOLD))
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.Medium, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.Medium, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            mediumConfidenceSimilarImageCount++;
                        }
                    }
                    else if ((pHashHammingDistance < PHASH_HIGH_CONFIDENCE_THRESHOLD || hdHashHammingDistance < HDHASH_HIGH_CONFIDENCE_THRESHOLD || vdHashHammingDistance < VDHASH_HIGH_CONFIDENCE_THRESHOLD) && aHashHammingDistance < AHASH_HIGH_CONFIDENCE_THRESHOLD && pHashHammingDistance < PHASH_LOW_CONFIDENCE_THRESHOLD && hdHashHammingDistance < HDHASH_LOW_CONFIDENCE_THRESHOLD && vdHashHammingDistance < VDHASH_LOW_CONFIDENCE_THRESHOLD)
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.Low, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.Low, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            lowConfidenceSimilarImageCount++;
                        }
                    }
                }
            }
            return false;
        }

        private void ResetPanZoom1()
        {
            var st = (ScaleTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt = (TranslateTransform)((TransformGroup)previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            previewImage1.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void ResetPanZoom2()
        {
            var st = (ScaleTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt = (TranslateTransform)((TransformGroup)previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            previewImage2.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void ReloadImage1(string path)
        {
            if (path == null)
            {
                previewImage1.Source = null;
            }
            else
            {
                try
                {
                    ResetPanZoom1();
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmapImage.EndInit();
                    previewImage1.Source = bitmapImage;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                }
            }
        }

        private void ReloadImage2(string path)
        {
            if (path == null)
            {
                previewImage2.Source = null;
            }
            else
            {
                try
                {
                    ResetPanZoom2();
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmapImage.EndInit();
                    previewImage2.Source = bitmapImage;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                }
            }
        }

        private void MergeSort(List<ListViewDataItem> list1, List<ListViewDataItem> list2)
        {
            if (list1.Count < 2)
            {
                return;
            }

            int startL, startR, step = 1;

            while (step < list1.Count)
            {
                startL = 0;
                startR = step;

                while (startR + step <= list1.Count)
                {
                    MergeLists(list1, list2, startL, startL + step, startR, startR + step);
                    startL = startR + step;
                    startR = startL + step;
                }

                if (startR < list1.Count)
                {
                    MergeLists(list1, list2, startL, startL + step, startR, list1.Count);
                }
                step *= 2;
            }
        }

        private void MergeLists(List<ListViewDataItem> list1, List<ListViewDataItem> list2, int startL, int stopL, int startR, int stopR)
        {
            ListViewDataItem[] right1 = new ListViewDataItem[stopR - startR + 1];
            ListViewDataItem[] right2 = new ListViewDataItem[stopR - startR + 1];
            ListViewDataItem[] left1 = new ListViewDataItem[stopL - startL + 1];
            ListViewDataItem[] left2 = new ListViewDataItem[stopL - startL + 1];

            for (int i = 0, k = startR; i < (right1.Length - 1); ++i, ++k)
            {
                right1[i] = list1[k];
                right2[i] = list2[k];
            }

            for (int i = 0, k = startL; i < (left1.Length - 1); ++i, ++k)
            {
                left1[i] = list1[k];
                left2[i] = list2[k];
            }

            right1[right1.Length - 1] = new ListViewDataItem("", -1, 64, 64, 64, 64, "");
            right2[right2.Length - 1] = new ListViewDataItem("", -1, 64, 64, 64, 64, "");
            left1[left1.Length - 1] = new ListViewDataItem("", -1, 64, 64, 64, 64, "");
            left2[left2.Length - 1] = new ListViewDataItem("", -1, 64, 64, 64, 64, "");

            for (int k = startL, m = 0, n = 0; k < stopR; ++k)
            {
                if (left1[m].confidence > right1[n].confidence || (left1[m].confidence == right1[n].confidence && left1[m].pHashHammingDistance + left1[m].hdHashHammingDistance + left1[m].vdHashHammingDistance + left1[m].aHashHammingDistance < right1[n].pHashHammingDistance + right1[n].hdHashHammingDistance + right1[n].vdHashHammingDistance + right1[n].aHashHammingDistance))
                {
                    list1[k] = left1[m];
                    list2[k] = left2[m];
                    m++;
                }
                else
                {
                    list1[k] = right1[n];
                    list2[k] = right2[n];
                    n++;
                }
            }
        }
    }
}