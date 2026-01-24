using DiscreteCosineTransform;
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
        public bool gotException = false, skipFilesWithDifferentOrientation = true, duplicatesOnly = false, comparing = false, includeSubfolders, jpegMenuItemChecked, gifMenuItemChecked, pngMenuItemChecked, bmpMenuItemChecked, tiffMenuItemChecked, icoMenuItemChecked, isEnglish = true, sendsToRecycleBin, opening = true, deleteMarkedItems = false;
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
            catch
            {
                opening = false;
            }

            // Initialize localization based on menu selection
            if (japaneseMenuItem.IsChecked)
            {
                currentLanguageCode = "ja-JP";
                LocalizationManager.SetLanguage("ja-JP");
            }
            else if (turkishMenuItem.IsChecked)
            {
                currentLanguageCode = "tr-TR";
                LocalizationManager.SetLanguage("tr-TR");
            }
            else
            {
                currentLanguageCode = "en-US";
                LocalizationManager.SetLanguage("en-US");
            }

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
            catch
            {
            }

            try
            {
                process?.Kill();
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
            }

            try
            {
                File.Delete(path + @"\Bin\Results.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
            }

            try
            {
                File.Delete(path + @"\Bin\Directories.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
            }

            try
            {
                File.Delete(path + @"\Bin\Filters.imc");
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
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
            englishMenuItem.IsChecked = true;
            turkishMenuItem.IsChecked = false;
            japaneseMenuItem.IsChecked = false;
            englishMenuItem.IsEnabled = false;
            turkishMenuItem.IsEnabled = true;
            japaneseMenuItem.IsEnabled = true;
            currentLanguageCode = "en-US";
            LocalizationManager.SetLanguage("en-US");
            UpdateUI();
        }

        private void TurkishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            englishMenuItem.IsChecked = false;
            turkishMenuItem.IsChecked = true;
            japaneseMenuItem.IsChecked = false;
            englishMenuItem.IsEnabled = true;
            turkishMenuItem.IsEnabled = false;
            japaneseMenuItem.IsEnabled = true;
            currentLanguageCode = "tr-TR";
            LocalizationManager.SetLanguage("tr-TR");
            UpdateUI();
        }

        private void JapaneseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            englishMenuItem.IsChecked = false;
            turkishMenuItem.IsChecked = false;
            japaneseMenuItem.IsChecked = true;
            englishMenuItem.IsEnabled = true;
            turkishMenuItem.IsEnabled = true;
            japaneseMenuItem.IsEnabled = false;
            currentLanguageCode = "ja-JP";
            LocalizationManager.SetLanguage("ja-JP");
            UpdateUI();
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
            catch
            {
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
                englishMenuItem.IsEnabled = !englishMenuItem.IsChecked;
                turkishMenuItem.IsEnabled = !turkishMenuItem.IsChecked;
                japaneseMenuItem.IsEnabled = !japaneseMenuItem.IsChecked;
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
                englishMenuItem.IsEnabled = false;
                turkishMenuItem.IsEnabled = false;
                japaneseMenuItem.IsEnabled = false;
                includeSubfoldersMenuItem.IsEnabled = false;
                skipFilesWithDifferentOrientationMenuItem.IsEnabled = false;
                findExactDuplicatesOnlyMenuItem.IsEnabled = false;
                clearFalsePositiveDatabaseButton.IsEnabled = false;
                resetToDefaultsMenuItem.IsEnabled = false;
                addFolderButton.IsEnabled = false;
                clearButton.IsEnabled = false;
            }
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            List<ListViewDataItem> selectedItems1 = new List<ListViewDataItem>();
            List<ListViewDataItem> selectedItems2 = new List<ListViewDataItem>();

            for (int i = 0; i < bindingList1.Count; i++)
            {
                if (((deleteMarkedItems && bindingList1[i].state == (int)State.MarkedForDeletion) || (!deleteMarkedItems && bindingList1[i].isChecked)) && !FileAddedBefore(selectedItems1, bindingList1[i]) && !FileAddedBefore(selectedItems2, bindingList1[i]))
                {
                    try
                    {
                        if (deletePermanentlyMenuItem.IsChecked)
                        {
                            FileSystem.DeleteFile(bindingList1[i].text, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        }
                        else
                        {
                            FileSystem.DeleteFile(bindingList1[i].text, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch
                    {
                    }
                    selectedItems1.Add(bindingList1[i]);
                }
            }

            for (int i = 0; i < bindingList2.Count; i++)
            {
                if (((deleteMarkedItems && bindingList2[i].state == (int)State.MarkedForDeletion) || (!deleteMarkedItems && bindingList2[i].isChecked)) && !FileAddedBefore(selectedItems1, bindingList2[i]) && !FileAddedBefore(selectedItems2, bindingList2[i]))
                {
                    try
                    {
                        if (deletePermanentlyMenuItem.IsChecked)
                        {
                            FileSystem.DeleteFile(bindingList2[i].text, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        }
                        else
                        {
                            FileSystem.DeleteFile(bindingList2[i].text, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch
                    {
                    }
                    selectedItems2.Add(bindingList2[i]);
                }
            }

            int tempIndex1, tempIndex2;
            ListViewDataItem tempListViewDataItem;

            for (int i = 0; i < selectedItems1.Count; i++)
            {
                tempListViewDataItem = selectedItems1[i];
                tempIndex1 = FindDuplicateIndex(bindingList1, tempListViewDataItem);
                tempIndex2 = FindDuplicateIndex(bindingList2, tempListViewDataItem);

                if (tempIndex1 == -1 && tempIndex2 == -1)
                {
                    for (int j = 0; j < bindingList1.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text) || bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.RemoveAt(j);
                            bindingList2.RemoveAt(j);
                            j--;
                        }
                    }
                }
                else if (tempIndex1 != -1 && tempIndex2 != -1)
                {
                    bindingList2.RemoveAt(tempIndex2);

                    if (tempIndex1 > tempIndex2)
                    {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1 - 1].text, bindingList2[tempIndex1 - 1].confidence, bindingList2[tempIndex1 - 1].pHashHammingDistance, bindingList2[tempIndex1 - 1].hdHashHammingDistance, bindingList2[tempIndex1 - 1].vdHashHammingDistance, bindingList2[tempIndex1 - 1].aHashHammingDistance, bindingList2[tempIndex1 - 1].sha256Checksum));
                    }
                    else
                    {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList2[tempIndex1].confidence, bindingList2[tempIndex1].pHashHammingDistance, bindingList2[tempIndex1].hdHashHammingDistance, bindingList2[tempIndex1].vdHashHammingDistance, bindingList2[tempIndex1].aHashHammingDistance, bindingList2[tempIndex1].sha256Checksum));
                    }

                    bindingList2.RemoveAt(tempIndex1);
                    bindingList1.RemoveAt(tempIndex1);

                    for (int j = 0; j < bindingList1.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                }
                else if (tempIndex1 != -1)
                {
                    for (int j = 0; j < bindingList1.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex1);
                    bindingList2.RemoveAt(tempIndex1);
                }
                else
                {
                    for (int j = 0; j < bindingList2.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex2);
                    bindingList2.RemoveAt(tempIndex2);
                }

                for (int j = 0; j < bindingList1.Count - 1; j++)
                {
                    for (int k = j + 1; k < bindingList1.Count; k++)
                    {
                        if (bindingList1[j].text.Equals(bindingList1[k].text) && bindingList2[j].text.Equals(bindingList2[k].text) || bindingList1[j].text.Equals(bindingList2[k].text) && bindingList2[j].text.Equals(bindingList1[k].text))
                        {
                            bindingList1.RemoveAt(k);
                            bindingList2.RemoveAt(k);
                            k--;
                        }
                    }
                }
            }

            for (int i = 0; i < selectedItems2.Count; i++)
            {
                tempListViewDataItem = selectedItems2[i];
                tempIndex1 = FindDuplicateIndex(bindingList1, tempListViewDataItem);
                tempIndex2 = FindDuplicateIndex(bindingList2, tempListViewDataItem);

                if (tempIndex1 == -1 && tempIndex2 == -1)
                {
                    for (int j = 0; j < bindingList1.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text) || bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.RemoveAt(j);
                            bindingList2.RemoveAt(j);
                            j--;
                        }
                    }
                }
                else if (tempIndex1 != -1 && tempIndex2 != -1)
                {
                    bindingList2.RemoveAt(tempIndex2);

                    if (tempIndex1 > tempIndex2)
                    {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1 - 1].text, bindingList2[tempIndex1 - 1].confidence, bindingList2[tempIndex1 - 1].pHashHammingDistance, bindingList2[tempIndex1 - 1].hdHashHammingDistance, bindingList2[tempIndex1 - 1].vdHashHammingDistance, bindingList2[tempIndex1 - 1].aHashHammingDistance, bindingList2[tempIndex1 - 1].sha256Checksum));
                    }
                    else
                    {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList2[tempIndex1].confidence, bindingList2[tempIndex1].pHashHammingDistance, bindingList2[tempIndex1].hdHashHammingDistance, bindingList2[tempIndex1].vdHashHammingDistance, bindingList2[tempIndex1].aHashHammingDistance, bindingList2[tempIndex1].sha256Checksum));
                    }
                    bindingList2.RemoveAt(tempIndex1);
                    bindingList1.RemoveAt(tempIndex1);

                    for (int j = 0; j < bindingList2.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList2[tempIndex2].text, bindingList2[j].confidence, bindingList2[j].pHashHammingDistance, bindingList2[j].hdHashHammingDistance, bindingList2[j].vdHashHammingDistance, bindingList2[j].aHashHammingDistance, bindingList2[j].sha256Checksum));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList2[tempIndex2].text, bindingList2[j].confidence, bindingList2[j].pHashHammingDistance, bindingList2[j].hdHashHammingDistance, bindingList2[j].vdHashHammingDistance, bindingList2[j].aHashHammingDistance, bindingList2[j].sha256Checksum));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                }
                else if (tempIndex1 != -1)
                {
                    for (int j = 0; j < bindingList1.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex1);
                    bindingList2.RemoveAt(tempIndex1);
                }
                else
                {
                    for (int j = 0; j < bindingList2.Count; j++)
                    {
                        if (bindingList1[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text))
                        {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].pHashHammingDistance, bindingList1[j].hdHashHammingDistance, bindingList1[j].vdHashHammingDistance, bindingList1[j].aHashHammingDistance, bindingList1[j].sha256Checksum));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex2);
                    bindingList2.RemoveAt(tempIndex2);
                }

                for (int j = 0; j < bindingList1.Count - 1; j++)
                {
                    for (int k = j + 1; k < bindingList1.Count; k++)
                    {
                        if (bindingList1[j].text.Equals(bindingList1[k].text) && bindingList2[j].text.Equals(bindingList2[k].text) || bindingList1[j].text.Equals(bindingList2[k].text) && bindingList2[j].text.Equals(bindingList1[k].text))
                        {
                            bindingList1.RemoveAt(k);
                            bindingList2.RemoveAt(k);
                            k--;
                        }
                    }
                }
            }

            if (selectedItems1.Count + selectedItems2.Count > 0)
            {
                if (sendToRecycleBinMenuItem.IsChecked)
                {
                    console.Add(LocalizationManager.GetString("Console.SentToRecycleBin", selectedItems1.Count + selectedItems2.Count));
                }
                else
                {
                    console.Add(LocalizationManager.GetString("Console.FilesDeleted", selectedItems1.Count + selectedItems2.Count));
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
            catch
            {
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
                    catch
                    {
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
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
                try
                {
                    System.Diagnostics.Process.Start(bindingList1[listView1.SelectedIndex].text);
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                    Console.WriteLine("Oops3");
                }
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
                try
                {
                    System.Diagnostics.Process.Start(bindingList2[listView2.SelectedIndex].text);
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                    Console.WriteLine("Oops4");
                }
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
                    try
                    {
                        System.Diagnostics.Process.Start(bindingList1[listView1.SelectedIndex].text);
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch
                    {
                        Console.WriteLine("Oops1");
                    }
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
                    try
                    {
                        System.Diagnostics.Process.Start(bindingList2[listView2.SelectedIndex].text);
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch
                    {
                        Console.WriteLine("Oops2");
                    }
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
                informationLabel1.Content = bindingList1.ElementAt(listView1.SelectedIndex).text.Substring(bindingList1.ElementAt(listView1.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView1.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView1.SelectedIndex).text)].Height/* + " pHash: " + bindingList1.ElementAt(listView1.SelectedIndex).pHashHammingDistance + " hdHash: " + bindingList1.ElementAt(listView1.SelectedIndex).hdHashHammingDistance + " vdHash: " + bindingList1.ElementAt(listView1.SelectedIndex).vdHashHammingDistance + " aHash: " + bindingList1.ElementAt(listView1.SelectedIndex).aHashHammingDistance*/;
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
                informationLabel1.Content = bindingList1.ElementAt(listView2.SelectedIndex).text.Substring(bindingList1.ElementAt(listView2.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView2.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView2.SelectedIndex).text)].Height/* + " pHash: " + bindingList1.ElementAt(listView2.SelectedIndex).pHashHammingDistance + " hdHash: " + bindingList1.ElementAt(listView2.SelectedIndex).hdHashHammingDistance + " vdHash: " + bindingList1.ElementAt(listView2.SelectedIndex).vdHashHammingDistance + " aHash: " + bindingList1.ElementAt(listView2.SelectedIndex).aHashHammingDistance*/;
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
                try
                {
                    System.Diagnostics.Process.Start(bindingList1[listView1.SelectedIndex].text);
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                    Console.WriteLine("Oops1");
                }
            }
        }

        private void ListView1_OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
            {
                try
                {
                    string filePath = bindingList1[listView1.SelectedIndex].text;
                    System.Diagnostics.Process.Start(filePath.Substring(0, filePath.LastIndexOf("\\")));
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                    Console.WriteLine("Oops1");
                }
            }
        }

        private void ListView2_OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView2.SelectedIndex != -1)
            {
                try
                {
                    System.Diagnostics.Process.Start(bindingList2[listView2.SelectedIndex].text);
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                    Console.WriteLine("Oops1");
                }
            }
        }

        private void ListView2_OpenFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listView2.SelectedIndex != -1)
            {
                try
                {
                    string filePath = bindingList2[listView2.SelectedIndex].text;
                    System.Diagnostics.Process.Start(filePath.Substring(0, filePath.LastIndexOf("\\")));
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch
                {
                    Console.WriteLine("Oops1");
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
            isEnglish = (bool)info.GetValue("isEnglish", typeof(bool));
            // Try to load currentLanguageCode for newer saves, default to en-US or tr-TR based on isEnglish for backward compatibility
            try
            {
                currentLanguageCode = (string)info.GetValue("currentLanguageCode", typeof(string));
            }
            catch
            {
                // For backward compatibility with old saves that don't have currentLanguageCode
                currentLanguageCode = isEnglish ? "en-US" : "tr-TR";
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
            info.AddValue("isEnglish", englishMenuItem.IsChecked);
            info.AddValue("currentLanguageCode", LocalizationManager.CurrentLanguage);
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

            Stream stream = File.Open(path, FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
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

            Stream stream = File.Open(path, FileMode.Open);
            BinaryFormatter bformatter = new BinaryFormatter();
            mainWindow = (MainWindow)bformatter.Deserialize(stream);
            stream.Close();

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

                if (!mainWindow.isEnglish)
                {
                    // Use currentLanguageCode if available, otherwise fall back to Turkish for backward compatibility
                    string languageToSet = mainWindow.currentLanguageCode ?? "tr-TR";
                    
                    // Set menu states based on the language
                    englishMenuItem.IsChecked = false;
                    turkishMenuItem.IsChecked = (languageToSet == "tr-TR");
                    japaneseMenuItem.IsChecked = (languageToSet == "ja-JP");
                    
                    englishMenuItem.IsEnabled = true;
                    turkishMenuItem.IsEnabled = (languageToSet != "tr-TR");
                    japaneseMenuItem.IsEnabled = (languageToSet != "ja-JP");
                    
                    LocalizationManager.SetLanguage(languageToSet);
                    UpdateUI();
                }
                else
                {
                    // Ensure Japanese and Turkish states are set correctly for English
                    turkishMenuItem.IsChecked = false;
                    japaneseMenuItem.IsChecked = false;
                    turkishMenuItem.IsEnabled = true;
                    japaneseMenuItem.IsEnabled = true;
                }
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

            // Update isEnglish flag for backward compatibility
            isEnglish = LocalizationManager.CurrentLanguage == "en-US";
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
            Bitmap image, resizedImage;
            FastDCT2D fastDCT2D;
            SHA256Managed sha = new SHA256Managed();
            int[,] result;
            double average;
            int i;

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
                        image = new Bitmap(files[i]);
                        resolutionArray[i] = image.Size;

                        if (image.Width > image.Height)
                        {
                            orientationArray[i] = Orientation.Horizontal;
                        }
                        else
                        {
                            orientationArray[i] = Orientation.Vertical;
                        }

                        //SHA256 Calculation
                        using (FileStream stream = File.OpenRead(files[i]))
                        {
                            sha256Array[i] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                        }
                        image.Dispose();
                    }
                    else
                    {
                        image = new Bitmap(files[i]);
                        resizedImage = ResizeImage(image, 32, 32);
                        fastDCT2D = new FastDCT2D(resizedImage, 32);
                        result = fastDCT2D.FastDCT();
                        resizedImage = ResizeImage(resizedImage, 9, 9);
                        resizedImage = ConvertToGrayscale(resizedImage);
                        average = 0;

                        resolutionArray[i] = image.Size;

                        if (image.Width > image.Height)
                        {
                            orientationArray[i] = Orientation.Horizontal;
                        }
                        else
                        {
                            orientationArray[i] = Orientation.Vertical;
                        }

                        //SHA256 Calculation
                        using (FileStream stream = File.OpenRead(files[i]))
                        {
                            sha256Array[i] = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                        }

                        //pHash(Perceptual Hash) Calculation
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
                                if (result[j, k] < average)
                                {
                                    pHashArray[i, j * 8 + k] = 0;
                                }
                                else
                                {
                                    pHashArray[i, j * 8 + k] = 1;
                                }
                            }
                        }

                        //hdHash(Horizontal Difference Hash) Calculation
                        for (int j = 0; j < 8; j++)
                        {
                            for (int k = 0; k < 9; k++)
                            {
                                if (resizedImage.GetPixel(j, k).R < resizedImage.GetPixel(j + 1, k).R)
                                {
                                    hdHashArray[i, j * 8 + k] = 0;
                                }
                                else
                                {
                                    hdHashArray[i, j * 8 + k] = 1;
                                }
                            }
                        }

                        //vdHash(Vertical Difference Hash) Calculation
                        for (int j = 0; j < 9; j++)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                if (resizedImage.GetPixel(j, k).R < resizedImage.GetPixel(k, k + 1).R)
                                {
                                    vdHashArray[i, j * 8 + k] = 0;
                                }
                                else
                                {
                                    vdHashArray[i, j * 8 + k] = 1;
                                }
                            }
                        }

                        //aHash(Average Hash) Calculation
                        resizedImage = ResizeImage(resizedImage, 8, 8);
                        average = 0;

                        for (int j = 0; j < 8; j++)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                average += resizedImage.GetPixel(j, k).R;
                            }
                        }

                        average /= 64;

                        for (int j = 0; j < 8; j++)
                        {
                            for (int k = 0; k < 8; k++)
                            {
                                if (resizedImage.GetPixel(j, k).R < average)
                                {
                                    aHashArray[i, j * 8 + k] = 0;
                                }
                                else
                                {
                                    aHashArray[i, j * 8 + k] = 1;
                                }
                            }
                        }
                        image.Dispose();
                        resizedImage.Dispose();
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
                    catch
                    {
                    }
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
                for (int i = list1.Count - 1; i >= 0; i--)
                {
                    for (int j = 0; j < falsePositiveList1.Count; j++)
                    {
                        if ((list1[i].sha256Checksum == falsePositiveList1[j] && list2[i].sha256Checksum == falsePositiveList2[j]) || (list1[i].sha256Checksum == falsePositiveList2[j] && list2[i].sha256Checksum == falsePositiveList1[j]))
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

                            // Break out of inner loop since we found a match
                            break;
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
            streamWriter = new StreamWriter(path + @"\Bin\Directories.imc");
            streamWriter.WriteLine(input.Count);

            for (int i = 0; i < input.Count; i++)
            {
                streamWriter.WriteLine(input.ElementAt(i));
            }
            streamWriter.Close();

            streamWriter = new StreamWriter(path + @"\Bin\Filters.imc");
            streamWriter.WriteLine(includeSubfolders);
            streamWriter.WriteLine(jpegMenuItemChecked);
            streamWriter.WriteLine(gifMenuItemChecked);
            streamWriter.WriteLine(pngMenuItemChecked);
            streamWriter.WriteLine(bmpMenuItemChecked);
            streamWriter.WriteLine(tiffMenuItemChecked);
            streamWriter.WriteLine(icoMenuItemChecked);
            streamWriter.Close();
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
            streamReader = new StreamReader(path + @"\Bin\Results.imc");
            gotException = bool.Parse(streamReader.ReadLine());
            count = int.Parse(streamReader.ReadLine());

            for (int i = 0; i < count; i++)
            {
                files.Add(streamReader.ReadLine());
            }
            streamReader.Close();
            File.Delete(path + @"\Bin\Results.imc");
        }

        private Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        private Bitmap ConvertToGrayscale(Bitmap inputImage)
        {
            Bitmap cloneImage = (Bitmap)inputImage.Clone();
            Graphics graphics = Graphics.FromImage(cloneImage);
            ImageAttributes attributes = new ImageAttributes();
            ColorMatrix colorMatrix = new ColorMatrix(new float[][]{
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {     0,      0,      0, 1, 0},
                new float[] {     0,      0,      0, 0, 0}
            });
            attributes.SetColorMatrix(colorMatrix);
            graphics.DrawImage(cloneImage, new Rectangle(0, 0, cloneImage.Width, cloneImage.Height), 0, 0, cloneImage.Width, cloneImage.Height, GraphicsUnit.Pixel, attributes);
            graphics.Dispose();
            return cloneImage;
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

                    if (sha256Array[i] == sha256Array[j] && pHashHammingDistance < 1 && hdHashHammingDistance < 1 && vdHashHammingDistance < 1 && aHashHammingDistance < 1)
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.Duplicate, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.Duplicate, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            duplicateImageCount++;
                        }
                        return true;
                    }
                    else if (pHashHammingDistance < 9 && hdHashHammingDistance < 10 && vdHashHammingDistance < 10 && aHashHammingDistance < 9)
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.High, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.High, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            highConfidenceSimilarImageCount++;
                        }
                    }
                    else if ((pHashHammingDistance < 9 && hdHashHammingDistance < 13 && vdHashHammingDistance < 13 && aHashHammingDistance < 12) || (hdHashHammingDistance < 10 && pHashHammingDistance < 12 && vdHashHammingDistance < 13 && aHashHammingDistance < 12) || (vdHashHammingDistance < 10 && pHashHammingDistance < 12 && hdHashHammingDistance < 13 && aHashHammingDistance < 12) || (aHashHammingDistance < 9 && pHashHammingDistance < 12 && hdHashHammingDistance < 13 && vdHashHammingDistance < 13))
                    {
                        lock (myLock2)
                        {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int)Confidence.Medium, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[i]));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int)Confidence.Medium, pHashHammingDistance, hdHashHammingDistance, vdHashHammingDistance, aHashHammingDistance, sha256Array[j]));
                            mediumConfidenceSimilarImageCount++;
                        }
                    }
                    else if ((pHashHammingDistance < 9 || hdHashHammingDistance < 10 || vdHashHammingDistance < 10) && aHashHammingDistance < 9 && pHashHammingDistance < 21 && hdHashHammingDistance < 18 && vdHashHammingDistance < 18)
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

        private int FindDuplicateIndex(ObservableCollection<ListViewDataItem> list, ListViewDataItem item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].text.Equals(item.text) && list[i].confidence == (int)Confidence.Duplicate)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool FileAddedBefore(List<ListViewDataItem> selectedList, ListViewDataItem data)
        {
            for (int i = 0; i < selectedList.Count; i++)
            {
                if (selectedList[i].text.Equals(data.text))
                {
                    return true;
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
                    GC.Collect();
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
                    GC.Collect();
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