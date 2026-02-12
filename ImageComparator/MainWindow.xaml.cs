using Common.Helpers;
using DiscreteCosineTransform;
using ImageComparator.Helpers;
using ImageComparator.Models;
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
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageComparator
{
    /// <summary>
    /// Main window for the Image Comparator application.
    /// Handles duplicate image detection using multiple perceptual hashing algorithms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This window provides functionality to:
    /// - Scan directories for image files
    /// - Calculate perceptual hashes (pHash, dHash, aHash) and SHA256 checksums
    /// - Compare images to find duplicates and similar images
    /// - Manage results and delete unwanted duplicates
    /// </para>
    /// <para>
    /// Threading Model:
    /// - Phase 1: Parallel image processing (hash calculation)
    /// - Phase 2: Parallel hash comparison
    /// - UI updates via Dispatcher
    /// </para>
    /// </remarks>
    public partial class MainWindow : Window
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

        // Modern threading infrastructure
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isPaused;
        private readonly object _pauseLock = new object();
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);

        /// <summary>
        /// Identifies the <see cref="ImagePath1"/> dependency property.
        /// </summary>
        public static DependencyProperty ImagePathProperty1 = DependencyProperty.Register("ImagePath1", typeof(string), typeof(MainWindow), null);
        
        /// <summary>
        /// Identifies the <see cref="ImagePath2"/> dependency property.
        /// </summary>
        public static DependencyProperty ImagePathProperty2 = DependencyProperty.Register("ImagePath2", typeof(string), typeof(MainWindow), null);

        /// <summary>
        /// Gets or sets the file path for the first preview image.
        /// </summary>
        /// <value>
        /// The absolute file path to the image, or <c>null</c> if no image is selected.
        /// </value>
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

        /// <summary>
        /// Gets or sets the file path for the second preview image.
        /// </summary>
        /// <value>
        /// The absolute file path to the image, or <c>null</c> if no image is selected.
        /// </value>
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
        /// <summary>
        /// The size in pixels for resizing images before pHash DCT calculation.
        /// </summary>
        /// <remarks>
        /// A 32x32 resize provides a good balance between:
        /// <list type="bullet">
        /// <item>Computational efficiency (faster DCT)</item>
        /// <item>Hash accuracy (retains important features)</item>
        /// </list>
        /// </remarks>
        private const int PHASH_RESIZE_DIMENSION = 32;
        
        /// <summary>
        /// The size in pixels for resizing images before dHash calculation.
        /// </summary>
        /// <remarks>
        /// A 9x9 resize is used to calculate two separate 72-bit hashes:
        /// <list type="bullet">
        /// <item><b>hdHash</b> (Horizontal): Compares each pixel with its right neighbor, producing 8×9 = 72 comparisons</item>
        /// <item><b>vdHash</b> (Vertical): Compares each pixel with its bottom neighbor, producing 9×8 = 72 comparisons</item>
        /// </list>
        /// Each hash is stored separately and used independently for similarity comparison.
        /// </remarks>
        private const int DHASH_RESIZE_DIMENSION = 9;
        
        /// <summary>
        /// The size in pixels for resizing images before aHash calculation.
        /// </summary>
        /// <remarks>
        /// 8x8 resize produces a 64-bit hash after average comparison.
        /// </remarks>
        private const int AHASH_RESIZE_DIMENSION = 8;

        // Hash comparison thresholds
        /// <summary>
        /// Hamming distance threshold for exact duplicates.
        /// </summary>
        /// <remarks>
        /// Images with Hamming distance less than 1 (essentially 0) combined with
        /// matching SHA256 checksums are considered exact duplicates.
        /// </remarks>
        private const int EXACT_DUPLICATE_THRESHOLD = 1;
        
        /// <summary>
        /// Hamming distance threshold for high-confidence similar images (pHash).
        /// </summary>
        /// <remarks>
        /// A distance of less than 9 indicates images are very similar.
        /// This threshold was determined empirically through testing.
        /// </remarks>
        private const int PHASH_HIGH_CONFIDENCE_THRESHOLD = 9;
        
        /// <summary>
        /// Hamming distance threshold for medium-confidence similar images (pHash).
        /// </summary>
        /// <remarks>
        /// A distance of 12 or less indicates images are similar with minor differences.
        /// </remarks>
        private const int PHASH_MEDIUM_CONFIDENCE_THRESHOLD = 12;
        
        /// <summary>
        /// Hamming distance threshold for low-confidence similar images (pHash).
        /// </summary>
        /// <remarks>
        /// A distance of less than 21 indicates images may be related but differences are noticeable.
        /// </remarks>
        private const int PHASH_LOW_CONFIDENCE_THRESHOLD = 21;
        
        /// <summary>
        /// Hamming distance threshold for high-confidence similar images (horizontal dHash).
        /// </summary>
        private const int HDHASH_HIGH_CONFIDENCE_THRESHOLD = 10;
        
        /// <summary>
        /// Hamming distance threshold for medium-confidence similar images (horizontal dHash).
        /// </summary>
        private const int HDHASH_MEDIUM_CONFIDENCE_THRESHOLD = 13;
        
        /// <summary>
        /// Hamming distance threshold for low-confidence similar images (horizontal dHash).
        /// </summary>
        private const int HDHASH_LOW_CONFIDENCE_THRESHOLD = 18;
        
        /// <summary>
        /// Hamming distance threshold for high-confidence similar images (vertical dHash).
        /// </summary>
        private const int VDHASH_HIGH_CONFIDENCE_THRESHOLD = 10;
        
        /// <summary>
        /// Hamming distance threshold for medium-confidence similar images (vertical dHash).
        /// </summary>
        private const int VDHASH_MEDIUM_CONFIDENCE_THRESHOLD = 13;
        
        /// <summary>
        /// Hamming distance threshold for low-confidence similar images (vertical dHash).
        /// </summary>
        private const int VDHASH_LOW_CONFIDENCE_THRESHOLD = 18;
        
        /// <summary>
        /// Hamming distance threshold for high-confidence similar images (aHash).
        /// </summary>
        private const int AHASH_HIGH_CONFIDENCE_THRESHOLD = 9;
        
        /// <summary>
        /// Hamming distance threshold for medium-confidence similar images (aHash).
        /// </summary>
        private const int AHASH_MEDIUM_CONFIDENCE_THRESHOLD = 12;
        #endregion

        #region Enums
        /// <summary>
        /// Specifies the orientation of an image based on aspect ratio.
        /// </summary>
        public enum Orientation
        {
            /// <summary>
            /// Image is wider than it is tall (width &gt; height).
            /// </summary>
            Horizontal,
            
            /// <summary>
            /// Image is taller than it is wide (height &gt; width) or square.
            /// </summary>
            Vertical
        }

        /// <summary>
        /// Specifies the confidence level of an image similarity match.
        /// </summary>
        /// <remarks>
        /// Confidence levels are determined by combining Hamming distances from multiple hash algorithms.
        /// The ranges below show approximate pHash distances, but actual classification uses
        /// a combination of pHash, hdHash, vdHash, and aHash thresholds.
        /// </remarks>
        public enum Confidence
        {
            /// <summary>
            /// Low confidence match. Images may be related but differences are noticeable.
            /// Typical pHash Hamming distance: 13-20
            /// </summary>
            Low,
            
            /// <summary>
            /// Medium confidence match. Images are similar with minor differences.
            /// Typical pHash Hamming distance: 10-12
            /// </summary>
            Medium,
            
            /// <summary>
            /// High confidence match. Images are very similar.
            /// Typical pHash Hamming distance: 0-9
            /// </summary>
            High,
            
            /// <summary>
            /// Exact duplicate. Images are identical (same SHA256 hash and Hamming distance &lt; 1).
            /// </summary>
            Duplicate
        }

        /// <summary>
        /// Specifies the current state of a comparison result item.
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Normal state. No action pending on this item.
            /// </summary>
            Normal,
            
            /// <summary>
            /// Item is marked for deletion and will be deleted when Apply is clicked.
            /// </summary>
            MarkedForDeletion,
            
            /// <summary>
            /// Item is marked as a false positive and will be excluded from future comparisons.
            /// </summary>
            MarkedAsFalsePositive
        }
        #endregion

        /// <summary>
        /// Represents a single row in the comparison results list view.
        /// Implements <see cref="INotifyPropertyChanged"/> for data binding support.
        /// </summary>
        /// <remarks>
        /// This class stores all comparison metadata for a pair of images:
        /// <list type="bullet">
        /// <item>File path and confidence level</item>
        /// <item>Hamming distances for multiple hash algorithms (pHash, hdHash, vdHash, aHash)</item>
        /// <item>SHA256 checksum for exact duplicate detection</item>
        /// <item>UI state (selected, checked, pending action)</item>
        /// </list>
        /// </remarks>
        public class ListViewDataItem : INotifyPropertyChanged
        {
            private bool selected;
            private int pState;
            private bool pIsChecked;
            private bool pCheckboxEnabled;
            
            /// <summary>
            /// Gets or sets the file path or name for this comparison result.
            /// </summary>
            public string text { get; set; }
            
            /// <summary>
            /// Gets or sets the confidence level of this match.
            /// </summary>
            /// <value>
            /// Integer value corresponding to <see cref="Confidence"/> enum:
            /// 0 (Low), 1 (Medium), 2 (High), 3 (Duplicate)
            /// </value>
            public int confidence { get; set; }
            
            /// <summary>
            /// Gets or sets the perceptual hash Hamming distance.
            /// </summary>
            /// <value>
            /// Hamming distance between pHash values. Lower values indicate more similar images.
            /// </value>
            public int pHashHammingDistance { get; set; }
            
            /// <summary>
            /// Gets or sets the horizontal difference hash Hamming distance.
            /// </summary>
            /// <value>
            /// Hamming distance between hdHash values. Lower values indicate more similar images.
            /// </value>
            public int hdHashHammingDistance { get; set; }
            
            /// <summary>
            /// Gets or sets the vertical difference hash Hamming distance.
            /// </summary>
            /// <value>
            /// Hamming distance between vdHash values. Lower values indicate more similar images.
            /// </value>
            public int vdHashHammingDistance { get; set; }
            
            /// <summary>
            /// Gets or sets the average hash Hamming distance.
            /// </summary>
            /// <value>
            /// Hamming distance between aHash values. Lower values indicate more similar images.
            /// </value>
            public int aHashHammingDistance { get; set; }
            
            /// <summary>
            /// Gets or sets the SHA256 checksum for exact duplicate detection.
            /// </summary>
            /// <value>
            /// Hexadecimal string representation of SHA256 hash, or <c>null</c> if hash computation failed.
            /// </value>
            public string sha256Checksum { get; set; }

            /// <summary>
            /// Gets or sets whether this item is currently selected in the UI.
            /// </summary>
            /// <value>
            /// <c>true</c> if the item is selected; otherwise, <c>false</c>.
            /// </value>
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

            /// <summary>
            /// Gets or sets the current state of this item.
            /// </summary>
            /// <value>
            /// Integer value corresponding to <see cref="State"/> enum:
            /// 0 (Normal), 1 (MarkedForDeletion), 2 (MarkedAsFalsePositive)
            /// </value>
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

            /// <summary>
            /// Gets or sets whether the checkbox for this item is checked.
            /// </summary>
            /// <value>
            /// <c>true</c> if the checkbox is checked; otherwise, <c>false</c>.
            /// </value>
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

            /// <summary>
            /// Gets or sets whether the checkbox for this item is enabled.
            /// </summary>
            /// <value>
            /// <c>true</c> if the checkbox can be interacted with; otherwise, <c>false</c>.
            /// </value>
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

            /// <summary>
            /// Initializes a new instance of the <see cref="ListViewDataItem"/> class.
            /// </summary>
            /// <param name="text">File path or name</param>
            /// <param name="confidence">Match confidence level (0-3)</param>
            /// <param name="pHashHammingDistance">Perceptual hash Hamming distance</param>
            /// <param name="hdHashHammingDistance">Horizontal difference hash Hamming distance</param>
            /// <param name="vdHashHammingDistance">Vertical difference hash Hamming distance</param>
            /// <param name="aHashHammingDistance">Average hash Hamming distance</param>
            /// <param name="sha256Checksum">SHA256 checksum for exact duplicate detection</param>
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

            /// <summary>
            /// Occurs when a property value changes.
            /// </summary>
            [field: NonSerialized]
            public event PropertyChangedEventHandler PropertyChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <remarks>
        /// Performs the following initialization:
        /// <list type="bullet">
        /// <item>Loads application settings from JSON</item>
        /// <item>Initializes localization</item>
        /// <item>Sets up folder/file dialogs</item>
        /// <item>Configures image transformation groups for pan/zoom</item>
        /// <item>Cleans up old error logs</item>
        /// </list>
        /// </remarks>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when insufficient memory is available for initialization.
        /// </exception>
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
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.Filter = "JSON Files (*.json)|*.json";
            saveFileDialog.AddExtension = true;

            openFileDialog = new VistaOpenFileDialog();
            openFileDialog.DefaultExt = "json";
            openFileDialog.Filter = "JSON Files (*.json)|*.json";
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
                Deserialize(path + @"\Bin\Image Comparator.json");
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
            // Unpause threads before cancelling to ensure they can respond
            lock (_pauseLock)
            {
                if (_isPaused)
                {
                    _isPaused = false;
                    _pauseEvent?.Set();
                }
            }

            // Cancel any running operations
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Cancel Operations", ex);
            }

            // Wait for both processThread and worker threads to complete (with timeout)
            try
            {
                bool completed = SpinWait.SpinUntil(() =>
                    (processThread == null || !processThread.IsAlive) &&
                    (threadList == null || threadList.All(t => !t.IsAlive)),
                    TimeSpan.FromSeconds(5)
                );

                if (!completed)
                {
                    ErrorLogger.LogWarning("Window_Closing", "Threads did not complete within timeout");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Wait for Threads", ex);
            }

            // Dispose resources
            try
            {
                _cancellationTokenSource?.Dispose();
                _pauseEvent?.Dispose();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Dispose Resources", ex);
            }

            try
            {
                Serialize(path + @"\Bin\Image Comparator.json");
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
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Kill Process", ex);
            }
            finally
            {
                process?.Dispose();
                process = null;
            }

            try
            {
                File.Delete(path + @"\Bin\Results.json");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Delete Results.json", ex);
            }

            try
            {
                File.Delete(path + @"\Bin\Directories.json");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Delete Directories.json", ex);
            }

            try
            {
                File.Delete(path + @"\Bin\Filters.json");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Window_Closing - Delete Filters.json", ex);
            }
            Environment.Exit(0);
        }

        private void SaveResultsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveResults();
        }

        /// <summary>
        /// Saves the current comparison session to a JSON file.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the session was saved successfully; 
        /// <c>false</c> if the user cancelled the save dialog or an error occurred.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The session includes:
        /// <list type="bullet">
        /// <item>Application settings (file filters, language, etc.)</item>
        /// <item>Comparison results (duplicate pairs)</item>
        /// <item>Console messages</item>
        /// <item>False positive database</item>
        /// </list>
        /// </para>
        /// <para>
        /// All exceptions except <see cref="OutOfMemoryException"/> are caught and handled by
        /// displaying an error message to the user. The method returns <c>false</c> on any error.
        /// </para>
        /// </remarks>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when insufficient memory is available for serialization.
        /// </exception>
        /// <example>
        /// <code>
        /// if (SaveResults())
        /// {
        ///     console.Add("Session saved successfully");
        /// }
        /// </code>
        /// </example>
        public bool SaveResults()
        {
            saveFileDialog.Title = LocalizationManager.GetString("Dialog.SaveTitle");

            if (saveFileDialog.ShowDialog().Value)
            {
                try
                {
                    Serialize(saveFileDialog.FileName);
                    console.Add(LocalizationManager.GetString("Console.SessionSaved", saveFileDialog.FileName));
                    return true;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("SaveResults - Serialize", ex);
                    MessageBox.Show(
                        LocalizationManager.GetString("Error.SerializationFailed", ex.Message),
                        LocalizationManager.GetString("Error.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return false;
                }
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
                try
                {
                    Deserialize(openFileDialog.FileName);
                    console.Add(LocalizationManager.GetString("Console.SessionLoaded", openFileDialog.FileName));
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("LoadResultsMenuItem_Click - Deserialize", ex);
                    MessageBox.Show(
                        LocalizationManager.GetString("Error.DeserializationFailed", ex.Message),
                        LocalizationManager.GetString("Error.Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
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
                Serialize(path + @"\Bin\Image Comparator.json");
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
                
                // Initialize cancellation token and pause state
                _cancellationTokenSource = new CancellationTokenSource();
                _isPaused = false;
                
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
                persianMenuItem,
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
            var deletedFiles = DeleteFilesFromDisk(filesToDelete, out var failedDeletions);

            // Step 3: Remove deleted items from lists
            RemoveDeletedItemsFromLists(deletedFiles);

            // Step 4: Remove duplicate pairs
            RemoveDuplicatePairs();

            // Step 5: Show results to user
            ReportDeletionResults(deletedFiles.Count, filesToDelete.Count, failedDeletions);
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
        /// <param name="failedDeletions">Output list of files that failed to delete with error messages</param>
        /// <returns>A set of file paths that were successfully deleted from the filesystem</returns>
        private HashSet<string> DeleteFilesFromDisk(HashSet<string> filesToDelete, out List<(string path, string error)> failedDeletions)
        {
            var deletedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            failedDeletions = new List<(string path, string error)>();

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
                    failedDeletions.Add((filePath, ex.Message));
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
        /// <param name="failedDeletions">List of files that failed to delete with error messages</param>
        private void ReportDeletionResults(int deletedCount, int requestedCount, List<(string path, string error)> failedDeletions)
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

            // Report any deletion failures to the user
            if (failedDeletions.Count > 0)
            {
                console.Add(LocalizationManager.GetString("Console.DeletionErrors", failedDeletions.Count));

                // Show detailed error message to user for the first few failures
                var details = string.Join("\n", failedDeletions.Take(5).Select(f =>
                    $"  • {Path.GetFileName(f.path)}: {f.error}"));

                if (failedDeletions.Count > 5)
                {
                    details += $"\n  ... and {failedDeletions.Count - 5} more";
                }

                MessageBox.Show(
                    LocalizationManager.GetString("Error.DeletionFailed", failedDeletions.Count, details),
                    LocalizationManager.GetString("Error.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
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
            lock (_pauseLock)
            {
                _isPaused = !_isPaused;

                if (_isPaused)
                {
                    pauseButton.Tag = "1";
                    pauseButton.Content = LocalizationManager.GetString("Button.Resume");
                    console.Add(LocalizationManager.GetString("Console.Paused"));
                    pausedFirstTime = DateTime.Now.ToFileTime();
                    _pauseEvent.Reset();
                }
                else
                {
                    pauseButton.Tag = "0";
                    pauseButton.Content = LocalizationManager.GetString("Button.Pause");
                    if (console.Count > 0)
                    {
                        console.RemoveAt(console.Count - 1);
                    }
                    pausedSecondTime = DateTime.Now.ToFileTime();
                    pauseTime += pausedSecondTime - pausedFirstTime;

                    // Signal all waiting threads to continue
                    _pauseEvent.Set();
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Request cancellation
            _cancellationTokenSource?.Cancel();

            // Unpause if paused
            lock (_pauseLock)
            {
                if (_isPaused)
                {
                    _isPaused = false;
                    _pauseEvent.Set();
                }
            }

            // Update UI immediately
            findDuplicatesButton.Visibility = Visibility.Visible;
            pauseButton.Visibility = Visibility.Collapsed;
            stopButton.Visibility = Visibility.Collapsed;
            pauseButton.Content = LocalizationManager.GetString("Button.Pause");

            if (pauseButton.Tag?.Equals("1") == true)
            {
                if (console.Count > 0)
                {
                    console.RemoveAt(console.Count - 1);
                }
            }

            // Wait for both processThread and worker threads to finish gracefully (with timeout)
            try
            {
                bool completed = SpinWait.SpinUntil(() =>
                    (processThread == null || !processThread.IsAlive) &&
                    (threadList == null || threadList.All(t => !t.IsAlive)),
                    TimeSpan.FromSeconds(5)
                );

                if (!completed)
                {
                    ErrorLogger.LogWarning("StopButton", "Threads did not complete within timeout");
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("StopButton_Click - Wait for threads", ex);
            }

            // Cleanup
            directories = new List<string>();
            pauseButton.Tag = "0";
            percentage.Value = 0;
            files.Clear();
            bindingList1.Clear();
            bindingList2.Clear();
            list1.Clear();
            list2.Clear();

            if (console.Count > 0)
            {
                console[console.Count - 1] = LocalizationManager.GetString("Console.InterruptedByUser");
            }
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

        /// <summary>
        /// Serializes application state to JSON format.
        /// </summary>
        /// <param name="path">
        /// The absolute file path where the JSON file will be saved.
        /// Directory will be created if it doesn't exist.
        /// </param>
        /// <remarks>
        /// <para>
        /// Serializes the following data:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Application settings (filters, language, etc.)</description></item>
        /// <item><description>Comparison results and bindings</description></item>
        /// <item><description>False positive database</description></item>
        /// <item><description>Console messages</description></item>
        /// </list>
        /// <para>
        /// The JSON format uses indented formatting for readability.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="path"/> is null or empty.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the application lacks permissions to write to the specified path.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when the directory portion of the path cannot be found or created.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when an I/O error occurs during file operations.
        /// </exception>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when insufficient memory is available for serialization.
        /// </exception>
        /// <seealso cref="Deserialize(string)"/>
        public void Serialize(string path)
        {
            try
            {
                // Get and create the folder path
                string directory = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new AppSettings
                {
                    Version = 1,
                    JpegMenuItemChecked = jpegMenuItem.IsChecked,
                    GifMenuItemChecked = gifMenuItem.IsChecked,
                    PngMenuItemChecked = pngMenuItem.IsChecked,
                    BmpMenuItemChecked = bmpMenuItem.IsChecked,
                    TiffMenuItemChecked = tiffMenuItem.IsChecked,
                    IcoMenuItemChecked = icoMenuItem.IsChecked,
                    SendsToRecycleBin = sendToRecycleBinMenuItem.IsChecked,
                    CurrentLanguageCode = currentLanguageCode,
                    IncludeSubfolders = includeSubfoldersMenuItem.IsChecked,
                    SkipFilesWithDifferentOrientation = skipFilesWithDifferentOrientationMenuItem.IsChecked,
                    DuplicatesOnly = findExactDuplicatesOnlyMenuItem.IsChecked,
                    Files = files ?? new List<string>(),
                    FalsePositiveList1 = falsePositiveList1 ?? new List<string>(),
                    FalsePositiveList2 = falsePositiveList2 ?? new List<string>(),
                    ResolutionArray = resolutionArray?.Select(s => new SerializableSize(s)).ToArray(),
                    BindingList1 = bindingList1?.Select(item => new SerializableListViewDataItem(item)).ToList() ?? new List<SerializableListViewDataItem>(),
                    BindingList2 = bindingList2?.Select(item => new SerializableListViewDataItem(item)).ToList() ?? new List<SerializableListViewDataItem>(),
                    ConsoleMessages = console?.ToList() ?? new List<string>()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                string jsonString = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(path, jsonString);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ErrorLogger.LogError("Serialize", ex);
                throw;
            }
        }

        /// <summary>
        /// Deserializes application state from a JSON file.
        /// </summary>
        /// <param name="path">
        /// The absolute file path to the JSON file to load.
        /// </param>
        /// <remarks>
        /// <para>
        /// If the file doesn't exist (first time opening), this method does nothing.
        /// </para>
        /// <para>
        /// Validates the settings version for forward/backward compatibility.
        /// Currently supports version 1 only.
        /// </para>
        /// <para>
        /// All exceptions except <see cref="OutOfMemoryException"/> are caught and handled by
        /// displaying an error message to the user. The method does not propagate exceptions.
        /// </para>
        /// </remarks>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when insufficient memory is available for deserialization.
        /// </exception>
        /// <seealso cref="Serialize(string)"/>
        public void Deserialize(string path)
        {
            try
            {
                // If file doesn't exist, do nothing (first time opening)
                if (!File.Exists(path))
                {
                    opening = false;
                    return;
                }

                string jsonString = File.ReadAllText(path);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString, options);

                if (settings == null)
                {
                    throw new InvalidOperationException("Failed to deserialize settings");
                }

                // Validate settings version for forward/backward compatibility
                const int SupportedVersion = 1;
                // Treat 0 or missing version as SupportedVersion to avoid breaking older files
                var loadedVersion = settings.Version;
                if (loadedVersion != 0 && loadedVersion != SupportedVersion)
                {
                    throw new NotSupportedException(
                        $"Unsupported settings version: {loadedVersion}. Supported version: {SupportedVersion}.");
                }

                // Ensure collections are not null
                settings.Files = settings.Files ?? new List<string>();
                settings.FalsePositiveList1 = settings.FalsePositiveList1 ?? new List<string>();
                settings.FalsePositiveList2 = settings.FalsePositiveList2 ?? new List<string>();
                settings.BindingList1 = settings.BindingList1 ?? new List<SerializableListViewDataItem>();
                settings.BindingList2 = settings.BindingList2 ?? new List<SerializableListViewDataItem>();
                settings.ConsoleMessages = settings.ConsoleMessages ?? new List<string>();

                ApplySettings(settings);
            }
            catch (JsonException ex)
            {
                ErrorLogger.LogError("Deserialize - JSON Parse Error", ex);
                MessageBox.Show(
                    "The session file is corrupted or invalid. Please delete the file and restart the application.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                opening = false;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ErrorLogger.LogError("Deserialize", ex);
                MessageBox.Show(
                    "Failed to load session file. The file may be corrupted.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                opening = false;
            }
        }

        /// <summary>
        /// Apply settings to UI
        /// </summary>
        private void ApplySettings(AppSettings settings)
        {
            if (opening)
            {
                opening = false;

                skipFilesWithDifferentOrientationMenuItem.IsChecked = settings.SkipFilesWithDifferentOrientation;
                findExactDuplicatesOnlyMenuItem.IsChecked = settings.DuplicatesOnly;
                includeSubfoldersMenuItem.IsChecked = settings.IncludeSubfolders;
                jpegMenuItem.IsChecked = settings.JpegMenuItemChecked;
                gifMenuItem.IsChecked = settings.GifMenuItemChecked;
                pngMenuItem.IsChecked = settings.PngMenuItemChecked;
                bmpMenuItem.IsChecked = settings.BmpMenuItemChecked;
                tiffMenuItem.IsChecked = settings.TiffMenuItemChecked;
                icoMenuItem.IsChecked = settings.IcoMenuItemChecked;
                falsePositiveList1 = settings.FalsePositiveList1;
                falsePositiveList2 = settings.FalsePositiveList2;

                if (!settings.SendsToRecycleBin)
                {
                    sendToRecycleBinMenuItem.IsChecked = false;
                    deletePermanentlyMenuItem.IsChecked = true;
                    sendToRecycleBinMenuItem.IsEnabled = true;
                    deletePermanentlyMenuItem.IsEnabled = false;
                }

                // Set language based on saved currentLanguageCode with validation
                string languageToSet = settings.CurrentLanguageCode;

                // List of valid language codes
                var validLanguages = new[] {
                    "en-US", "tr-TR", "ja-JP", "es-ES", "fr-FR", "de-DE", "it-IT",
                    "pt-BR", "ru-RU", "zh-CN", "ko-KR", "ar-SA", "fa-IR", "hi-IN",
                    "nl-NL", "pl-PL", "sv-SE", "nb-NO", "da-DK"
                };

                // Validate and default to en-US if null or invalid
                if (string.IsNullOrEmpty(languageToSet) || !validLanguages.Contains(languageToSet))
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
                bindingList1 = new ObservableCollection<ListViewDataItem>(
                    settings.BindingList1.Select(item => item.ToListViewDataItem())
                );
                bindingList2 = new ObservableCollection<ListViewDataItem>(
                    settings.BindingList2.Select(item => item.ToListViewDataItem())
                );
                console = new ObservableCollection<string>(settings.ConsoleMessages);
                files = settings.Files;
                resolutionArray = settings.ResolutionArray?.Select(s => s.ToSize()).ToArray();

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

        /// <summary>
        /// Clears all comparison results and resets the UI to its initial state.
        /// </summary>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item>Hides result-related buttons and shows initial scan buttons</item>
        /// <item>Clears all directories, files, and console messages</item>
        /// <item>Clears all comparison result bindings</item>
        /// <item>Resets the progress percentage to 0</item>
        /// </list>
        /// </remarks>
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

        /// <summary>
        /// Applies pending operations (deletions and false positive markings) to comparison results.
        /// </summary>
        /// <param name="deleteItemCount">Number of items marked for deletion</param>
        /// <param name="markAsFalsePositiveItemCount">Number of items marked as false positives</param>
        /// <remarks>
        /// <para>
        /// This method performs the following actions:
        /// </para>
        /// <list type="number">
        /// <item>Deletes files marked for deletion (if <paramref name="deleteItemCount"/> &gt; 0)</item>
        /// <item>Adds false positive pairs to exclusion database (if <paramref name="markAsFalsePositiveItemCount"/> &gt; 0)</item>
        /// <item>Removes false positive items from result bindings</item>
        /// <item>Saves updated false positive database to disk</item>
        /// </list>
        /// <para>
        /// False positive pairs are stored by their SHA256 checksums to prevent them
        /// from appearing in future comparison results.
        /// </para>
        /// </remarks>
        /// <exception cref="OutOfMemoryException">
        /// Thrown when insufficient memory is available for serialization.
        /// </exception>
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
                    Serialize(path + @"\Bin\Image Comparator.json");
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

        /// <summary>
        /// Worker thread method for Phase 1: Image Processing.
        /// </summary>
        /// <param name="cancellationToken">
        /// Token for cancellation support. When cancelled, the thread exits gracefully.
        /// </param>
        /// <remarks>
        /// <para><b>Thread Safety:</b></para>
        /// <list type="bullet">
        /// <item>Each thread atomically obtains unique file index via Interlocked.Increment</item>
        /// <item>Writes to unique array indices (thread-safe by design)</item>
        /// <item>Reads from shared 'files' list (immutable during processing)</item>
        /// <item>SHA256 instance is thread-local (automatically disposed)</item>
        /// </list>
        /// <para><b>Hash Algorithms Computed:</b></para>
        /// <list type="number">
        /// <item><description><b>SHA256</b>: Cryptographic hash for exact duplicate detection</description></item>
        /// <item><description><b>pHash</b>: Perceptual hash using DCT (64-bit)</description></item>
        /// <item><description><b>hdHash</b>: Horizontal difference hash (72-bit)</description></item>
        /// <item><description><b>vdHash</b>: Vertical difference hash (72-bit)</description></item>
        /// <item><description><b>aHash</b>: Average hash (64-bit)</description></item>
        /// </list>
        /// <para><b>Error Handling:</b></para>
        /// <para>
        /// Invalid images are marked using <see cref="MarkFileAsInvalid"/> and excluded from comparison.
        /// Errors are logged but don't stop processing of other files.
        /// </para>
        /// </remarks>
        /// <seealso cref="CompareResultsThreadStart(CancellationToken)"/>
        private void ProcessThreadStart(CancellationToken cancellationToken)
        {
            FastDCT2D fastDCT2D;
            int[,] result;
            double average;
            int i;

            using (SHA256Managed sha = new SHA256Managed())
            {
                while (true)
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Check for pause
                    WaitWhilePaused(cancellationToken);

                    // Atomic increment - no lock needed
                    i = Interlocked.Increment(ref processThreadsiAsync) - 1;

                    // Bounds check
                    if (i >= files.Count)
                    {
                        break;
                    }

                    // Re-check for cancellation after claiming the index
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Update percentage based on shared monotonic counter (atomic monotonic update)
                    // Phase 1 (processing) maps to 0-50% of total progress
                    int currentIndex = Math.Min(processThreadsiAsync, files.Count);
                    int newPercentage = (int)Math.Round(50.0 * currentIndex / files.Count);
                    percentage.SetMaximum(newPercentage);

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

                                    // pHash (Perceptual Hash) Calculation
                                    // Applies Discrete Cosine Transform (DCT) to capture frequency patterns
                                    // Compares each DCT coefficient against the average (excluding DC component)
                                    // Creates 64-bit hash representing low-frequency image structure
                                    // Result: robust to minor image modifications (scaling, compression, brightness)
                                    average = 0;
                                    for (int j = 0; j < 8; j++)
                                    {
                                        for (int k = 0; k < 8; k++)
                                        {
                                            average += result[j, k];
                                        }
                                    }

                                    average -= result[0, 0];  // Exclude DC component (overall brightness)
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
                                        // hdHash (Horizontal Difference Hash) Calculation
                                        // Compares each pixel with its RIGHT neighbor: GetPixel(j, k) vs GetPixel(j+1, k)
                                        // Creates 8x9=72 bits by comparing columns (j to j+1) across 9 rows (k)
                                        // Result: horizontal gradient detection
                                        for (int j = 0; j < 8; j++)
                                        {
                                            for (int k = 0; k < 9; k++)
                                            {
                                                hdHashArray[i, j * 8 + k] = grayscale.GetPixel(j, k).R < grayscale.GetPixel(j + 1, k).R ? 0 : 1;
                                            }
                                        }

                                        // vdHash (Vertical Difference Hash) Calculation
                                        // Compares each pixel with its BOTTOM neighbor: GetPixel(j, k) vs GetPixel(j, k+1)
                                        // Creates 9x8=72 bits by comparing rows (k to k+1) across 9 columns (j)
                                        // Result: vertical gradient detection
                                        for (int j = 0; j < 9; j++)
                                        {
                                            for (int k = 0; k < 8; k++)
                                            {
                                                vdHashArray[i, j * 8 + k] = grayscale.GetPixel(j, k).R < grayscale.GetPixel(j, k + 1).R ? 0 : 1;
                                            }
                                        }

                                        // aHash (Average Hash) Calculation
                                        // Compares each pixel's brightness against the average brightness of all pixels
                                        // Creates 64-bit hash where each bit represents above/below average
                                        // Result: fast computation, good for finding similar layouts and structures
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

                                            average /= 64;  // Calculate mean brightness

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
                        MarkFileAsInvalid(i);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelled
                        return;
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Mark file as invalid for all exceptions to prevent false duplicate detection
                        MarkFileAsInvalid(i);
                        
                        string fileNameInfo = string.Empty;
                        if (i >= 0 && i < files.Count)
                        {
                            fileNameInfo = $" ({Path.GetFileName(files[i])})";
                        }
                        
                        ErrorLogger.LogError($"ProcessThreadStart - Process Image {i}{fileNameInfo}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Marks a file as invalid to exclude it from comparison.
        /// </summary>
        /// <param name="index">The index of the file to mark as invalid.</param>
        /// <remarks>
        /// <para>
        /// Sets sentinel values to indicate the file should be skipped:
        /// <list type="bullet">
        /// <item><c>pHashArray[index, 0] = -1</c> - Signals invalid file in comparison logic</item>
        /// <item><c>sha256Array[index] = null</c> - Prevents false SHA256 matches</item>
        /// </list>
        /// </para>
        /// <para>
        /// Includes bounds checking to prevent array index exceptions.
        /// Called when image loading or hash calculation fails.
        /// </para>
        /// </remarks>
        /// <exception cref="OutOfMemoryException">
        /// Rethrown if insufficient memory is available.
        /// </exception>
        private void MarkFileAsInvalid(int index)
        {
            try
            {
                if (index >= 0 && index < pHashArray.GetLength(0))
                {
                    pHashArray[index, 0] = -1;
                }
                if (index >= 0 && index < sha256Array.Length)
                {
                    sha256Array[index] = null;
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Log if we fail to mark the file as invalid (shouldn't happen in normal operation)
                ErrorLogger.LogError($"MarkFileAsInvalid - Failed to mark file {index} as invalid", ex);
            }
        }

        /// <summary>
        /// Waits while the processing is paused, periodically checking for cancellation.
        /// </summary>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <remarks>
        /// Uses a <see cref="ManualResetEventSlim"/> to efficiently wait for resume signal.
        /// Checks cancellation status every 100ms to ensure responsive shutdown.
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled during the wait.
        /// </exception>
        private void WaitWhilePaused(CancellationToken cancellationToken)
        {
            while (_isPaused && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _pauseEvent.Wait(100, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Exit on cancellation
                    throw;
                }
            }
        }

        /// <summary>
        /// Worker thread method for Phase 2: Hash Comparison.
        /// </summary>
        /// <param name="cancellationToken">
        /// Token for cancellation support. When cancelled, the thread exits gracefully.
        /// </param>
        /// <remarks>
        /// <para><b>Thread Safety:</b></para>
        /// <list type="bullet">
        /// <item>Each thread atomically obtains unique 'i' index via Interlocked.Increment</item>
        /// <item>Compares file[i] with all files[j] where j &gt; i (no overlap between threads)</item>
        /// <item>Reads from hash arrays computed in Phase 1 (immutable - all writes completed)</item>
        /// <item>Writes to shared lists (list1/list2) protected by myLock2 in <see cref="FindSimilarity"/></item>
        /// </list>
        /// <para><b>Algorithm:</b></para>
        /// <para>
        /// For each unique pair (i, j) where i &lt; j:
        /// </para>
        /// <list type="bullet">
        /// <item>Skips pairs with different orientations (if configured)</item>
        /// <item>Calculates Hamming distance for all hash types</item>
        /// <item>Classifies similarity: Duplicate, High, Medium, Low confidence</item>
        /// <item>Adds matches to result lists</item>
        /// </list>
        /// </remarks>
        /// <seealso cref="ProcessThreadStart(CancellationToken)"/>
        /// <seealso cref="FindSimilarity"/>
        private void CompareResultsThreadStart(CancellationToken cancellationToken)
        {
            int i, j;
            bool isDuplicate;

            while (true)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Check for pause
                WaitWhilePaused(cancellationToken);

                // Atomic increment - no lock needed
                i = Interlocked.Increment(ref compareResultsiAsync) - 1;

                // Bounds check
                if (i >= files.Count - 1)
                {
                    break;
                }

                // Quick cancellation re-check after claiming a new index
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Update percentage based on shared monotonic counter (atomic monotonic update)
                // Phase 2 (comparison) maps to 50-100% of total progress
                int currentIndex = Math.Min(compareResultsiAsync, files.Count - 1);
                int newPercentage = 50 + (int)Math.Round(50.0 * currentIndex / (files.Count - 1));
                percentage.SetMaximum(newPercentage);

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

        /// <summary>
        /// Main processing pipeline with two-phase threading model for image comparison.
        /// 
        /// Threading Architecture:
        /// ======================
        /// 
        /// Phase 1 - Image Processing (Parallel):
        /// --------------------------------------
        /// - Multiple worker threads (CPU core count) process images concurrently
        /// - Each thread:
        ///   * Gets unique file index atomically via Interlocked.Increment on processThreadsiAsync counter
        ///   * Computes image hashes (SHA256, pHash, hdHash, vdHash, aHash)
        ///   * Writes results to unique array indices (thread-safe by design)
        /// - Reads from shared 'files' list (immutable during processing - no modifications)
        /// - Main thread waits for all processing threads to complete via Join()
        /// 
        /// Phase 2 - Comparison (Parallel):
        /// --------------------------------
        /// - Starts AFTER all Phase 1 threads complete (Join() ensures happens-before relationship)
        /// - Multiple worker threads compare image pairs concurrently
        /// - Each thread:
        ///   * Gets unique 'i' index atomically via Interlocked.Increment on compareResultsiAsync counter
        ///   * Compares file[i] with all files[j] where j > i
        ///   * Reads from hash arrays (immutable in this phase - all writes completed in Phase 1)
        ///   * Writes to shared result lists (protected by myLock2)
        /// - Main thread waits for all comparison threads to complete via Join()
        /// 
        /// Thread Safety Guarantees:
        /// ========================
        /// 1. Counter Synchronization: processThreadsiAsync and compareResultsiAsync incremented atomically via Interlocked operations
        /// 2. Array Access: Each thread writes to unique indices (i) obtained atomically
        /// 3. Phase Separation: Phase 2 only begins after Phase 1 completes (Join barrier)
        /// 4. Result Lists: list1/list2/counters protected by myLock2
        /// 5. Cancellation: CancellationToken checked throughout for clean shutdown
        /// </summary>
        private void Run()
        {
            try
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

            // Get cancellation token
            var cts = _cancellationTokenSource;
            if (cts == null)
            {
                // Cancellation source is not available; abort processing.
                return;
            }
            var token = cts.Token;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.Add(new Thread(() => ProcessThreadStart(token)));
                threadList.ElementAt(i).Start();
            }

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.ElementAt(i).Join();
            }

            // Check if cancelled
            if (token.IsCancellationRequested)
            {
                return;
            }

            threadList = new List<Thread>();
            comparing = true;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.Add(new Thread(() => CompareResultsThreadStart(token)));
                threadList.ElementAt(i).Start();
            }

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                threadList.ElementAt(i).Join();
            }

            // Check if cancelled before updating UI
            if (token.IsCancellationRequested)
            {
                return;
            }

            percentage.Value = 100;
            Dispatcher.Invoke(DispatcherPriority.Normal, updateConsole2);
            Dispatcher.Invoke(DispatcherPriority.Normal, updateUI);
            }
            finally
            {
                // Cleanup - only dispose if this is still the same instance
                var currentCts = _cancellationTokenSource;
                if (currentCts != null)
                {
                    try
                    {
                        currentCts.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                    // Only set to null if we disposed the current instance
                    if (_cancellationTokenSource == currentCts)
                    {
                        _cancellationTokenSource = null;
                    }
                }
            }
        }

        private void WriteToFile(List<string> input)
        {
            try
            {
                // Create data objects
                var directoriesData = new DirectoriesData
                {
                    Directories = input
                };

                var filtersData = new FiltersData
                {
                    IncludeSubfolders = includeSubfolders,
                    JpegFiles = jpegMenuItemChecked,
                    GifFiles = gifMenuItemChecked,
                    PngFiles = pngMenuItemChecked,
                    BmpFiles = bmpMenuItemChecked,
                    TiffFiles = tiffMenuItemChecked,
                    IcoFiles = icoMenuItemChecked
                };

                // Serialize to JSON
                var options = new JsonSerializerOptions { WriteIndented = true };

                string directoriesJson = JsonSerializer.Serialize(directoriesData, options);
                string filtersJson = JsonSerializer.Serialize(filtersData, options);

                // Write both files - if either fails, both should fail
                File.WriteAllText(path + @"\Bin\Directories.json", directoriesJson);
                File.WriteAllText(path + @"\Bin\Filters.json", filtersJson);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("WriteToFile", ex);
                throw;
            }
        }

        private void AddFiles(List<string> directory)
        {
            WriteToFile(directory);
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo(path + (@"\Bin\AddFiles.exe"));
            processStartInfo.RedirectStandardOutput = false;
            processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            processStartInfo.UseShellExecute = true;

            try
            {
                process = System.Diagnostics.Process.Start(processStartInfo);
                if (process != null)
                {
                    process.WaitForExit();

                    // Check for error log files
                    string errorLogFile = path + @"\Bin\AddFiles_Error.log";
                    string tempErrorLog = System.IO.Path.GetTempPath() + "AddFiles_Error.log";

                    if (File.Exists(errorLogFile))
                    {
                        string errorContent = File.ReadAllText(errorLogFile);
                        ErrorLogger.LogError("AddFiles.exe Error", new Exception($"AddFiles.exe reported error:\n{errorContent}"));

                        // Delete error log file after it has been read and logged
                        try
                        {
                            File.Delete(errorLogFile);
                        }
                        catch
                        {
                            // Ignore any errors during cleanup
                        }
                    }
                    else if (File.Exists(tempErrorLog))
                    {
                        string errorContent = File.ReadAllText(tempErrorLog);
                        ErrorLogger.LogError("AddFiles.exe Error (from temp)", new Exception($"AddFiles.exe reported error:\n{errorContent}"));

                        // Delete temp error log file after it has been read and logged
                        try
                        {
                            File.Delete(tempErrorLog);
                        }
                        catch
                        {
                            // Ignore any errors during cleanup
                        }
                    }
                }
                else
                {
                    ErrorLogger.LogError("AddFiles.exe", new Exception("Failed to start AddFiles.exe process"));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("AddFiles.exe Start", ex);
            }

            ReadFromFile();
        }

        private void ReadFromFile()
        {
            string resultsPath = path + @"\Bin\Results.json";

            try
            {
                // Check if file exists before trying to read
                if (!File.Exists(resultsPath))
                {
                    ErrorLogger.LogError("ReadFromFile - Results", new FileNotFoundException($"Results.json not found at {resultsPath}. AddFiles.exe may have failed."));
                    return;
                }

                string jsonString = File.ReadAllText(resultsPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultsData = JsonSerializer.Deserialize<ResultsData>(jsonString, options);

                if (resultsData != null)
                {
                    gotException = resultsData.GotException;
                    if (resultsData.Files != null)
                    {
                        files.AddRange(resultsData.Files);
                    }
                }

                File.Delete(resultsPath);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("ReadFromFile - Results", ex);
            }
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
                    // Prevent false duplicate detection if either SHA256 is null/empty (failed image loading)
                    if (string.IsNullOrEmpty(sha256Array[i]) || string.IsNullOrEmpty(sha256Array[j]))
                    {
                        return false;
                    }
                    
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

                    // Check for exact duplicates (same SHA256 hash and very low Hamming distances)
                    // Prevent false duplicate detection if either SHA256 is null/empty (failed image loading)
                    bool hasSameHash = !string.IsNullOrEmpty(sha256Array[i]) && !string.IsNullOrEmpty(sha256Array[j]) && sha256Array[i] == sha256Array[j];
                    bool hasLowHammingDistances = pHashHammingDistance < EXACT_DUPLICATE_THRESHOLD && 
                                                   hdHashHammingDistance < EXACT_DUPLICATE_THRESHOLD && 
                                                   vdHashHammingDistance < EXACT_DUPLICATE_THRESHOLD && 
                                                   aHashHammingDistance < EXACT_DUPLICATE_THRESHOLD;
                    
                    if (hasSameHash && hasLowHammingDistances)
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