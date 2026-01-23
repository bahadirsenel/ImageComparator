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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DiscreteCosineTransform;
using Microsoft.VisualBasic.FileIO;
using Ookii.Dialogs.Wpf;

namespace ImageComparator2 {

    [Serializable]
    public partial class MainWindow : Window, ISerializable {

        #region Variables
        System.Diagnostics.Process process;
        VistaFolderBrowserDialog folderBrowserDialog;
        VistaSaveFileDialog saveFileDialog;
        VistaOpenFileDialog openFileDialog;
        List<string> directories = new List<string>();
        List<string> files = new List<string>();
        ObservableCollection<string> console = new ObservableCollection<string>();
        ObservableCollection<ListViewDataItem> bindingList1 = new ObservableCollection<ListViewDataItem>();
        ObservableCollection<ListViewDataItem> bindingList2 = new ObservableCollection<ListViewDataItem>();
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
        int[,] hashArray;
        Thread processThread;
        MainWindow mainWindow;
        long firstTime, secondTime, pauseTime, pausedFirstTime, pausedSecondTime;
        int processThreadsiAsync, compareResultsiAsync, timeDifferenceInSeconds, duplicateImageCount, highConfidenceSimilarImageCount, mediumConfidenceSimilarImageCount;
        bool gotException = false, skipFilesWithDifferentOrientation = true, duplicatesOnly = false, comparing = false, includeSubfolders, jpegMenuItemChecked, gifMenuItemChecked, pngMenuItemChecked, bmpMenuItemChecked, tiffMenuItemChecked, icoMenuItemChecked, isEnglish, sendsToRecycleBin, opening = true;
        string path;

        public static DependencyProperty ImagePathProperty1 = DependencyProperty.Register("ImagePath1", typeof(string), typeof(MainWindow), null);
        public static DependencyProperty ImagePathProperty2 = DependencyProperty.Register("ImagePath2", typeof(string), typeof(MainWindow), null);

        public string ImagePath1 {
            get {
                return (string) GetValue(ImagePathProperty1);
            }
            set {
                SetValue(ImagePathProperty1, value);
            }
        }

        public string ImagePath2 {
            get {
                return (string) GetValue(ImagePathProperty2);
            }
            set {
                SetValue(ImagePathProperty2, value);
            }
        }
        #endregion

        #region Enums
        public enum Orientation {
            Horizontal,
            Vertical
        }

        public enum Confidence {
            Medium,
            High,
            Duplicate
        }
        #endregion

        [Serializable]
        public class ListViewDataItem : INotifyPropertyChanged {

            private bool selected;

            public string text {
                get;
                set;
            }

            public int confidence {
                get;
                set;
            }

            public int hammingDistance {
                get;
                set;
            }

            public bool isSelected {
                get {
                    return selected;
                }
                set {
                    selected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("isSelected"));
                }
            }

            public ListViewDataItem(string text, int confidence, int hammingDistance) {

                this.text = text;
                this.confidence = confidence;
                this.hammingDistance = hammingDistance;
                this.isSelected = false;
            }

            [field: NonSerialized]
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public MainWindow() {

            InitializeComponent();
            percentage.OnChange += percentageChanged;
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

            previewImage1.RenderTransform = new TransformGroup {
                Children = new TransformCollection {
                        new ScaleTransform(),
                        new TranslateTransform()
                    }
            };

            previewImage2.RenderTransform = new TransformGroup {
                Children = new TransformCollection {
                        new ScaleTransform(),
                        new TranslateTransform()
                    }
            };

            try {
                deserialize(path + @"\Bin\Image Comparator.imc");
            } catch {
                opening = false;
            }

            if (englishMenuItem.IsChecked) {
                console.Add("Drag-Drop to add folders:");
            }
            outputListView.ItemsSource = console;
        }

        private void Window_Closing(object sender, CancelEventArgs e) {

            try {
                serialize(path + @"\Bin\Image Comparator.imc");
            } catch {
            }

            try {
                process.Kill();
            } catch {
            }

            try {
                File.Delete(path + @"\Bin\Results.imc");
            } catch {
            }

            try {
                File.Delete(path + @"\Bin\Directories.imc");
            } catch {
            }

            try {
                File.Delete(path + @"\Bin\Filters.imc");
            } catch {
            }
            Environment.Exit(0);
        }

        private void saveResultsMenuItem_Click(object sender, RoutedEventArgs e) {

            if (englishMenuItem.IsChecked) {
                saveFileDialog.Title = "Save Results";
            } else {
                saveFileDialog.Title = "Sonuçları Kaydet";
            }

            if ((bool) saveFileDialog.ShowDialog()) {
                serialize(saveFileDialog.FileName);

                if (englishMenuItem.IsChecked) {
                    console.Add("Session have been saved to " + saveFileDialog.FileName);
                } else {
                    console.Add("Oturum " + saveFileDialog.FileName + " konumuna kaydedildi.");
                }
            }
        }

        private void loadResultsMenuItem_Click(object sender, RoutedEventArgs e) {

            if (englishMenuItem.IsChecked) {
                openFileDialog.Title = "Load Results";
            } else {
                openFileDialog.Title = "Sonuçları Yükle";
            }

            if ((bool) openFileDialog.ShowDialog()) {
                deserialize(openFileDialog.FileName);
            }
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e) {

            Close();
        }

        private void sendToRecycleBinMenuItem_Click(object sender, RoutedEventArgs e) {

            sendToRecycleBinMenuItem.IsChecked = true;
            deletePermanentlyMenuItem.IsChecked = false;
            sendToRecycleBinMenuItem.IsEnabled = false;
            deletePermanentlyMenuItem.IsEnabled = true;
        }

        private void deletePermanentlyMenuItem_Click(object sender, RoutedEventArgs e) {

            sendToRecycleBinMenuItem.IsChecked = false;
            deletePermanentlyMenuItem.IsChecked = true;
            sendToRecycleBinMenuItem.IsEnabled = true;
            deletePermanentlyMenuItem.IsEnabled = false;
        }

        private void englishMenuItem_Click(object sender, RoutedEventArgs e) {

            englishMenuItem.IsChecked = true;
            turkishMenuItem.IsChecked = false;
            englishMenuItem.IsEnabled = false;
            turkishMenuItem.IsEnabled = true;
            convertToEnglish();
        }

        private void turkishMenuItem_Click(object sender, RoutedEventArgs e) {

            englishMenuItem.IsChecked = false;
            turkishMenuItem.IsChecked = true;
            englishMenuItem.IsEnabled = true;
            turkishMenuItem.IsEnabled = false;
            convertToTurkish();
        }

        private void resetToDefaultsMenuItem_Click(object sender, RoutedEventArgs e) {

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

        private void howToUseMenuItem_Click(object sender, RoutedEventArgs e) {

        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e) {

        }

        private void addFolderButton_Click(object sender, RoutedEventArgs e) {

            if (englishMenuItem.IsChecked) {
                folderBrowserDialog.Description = "Add Folder";
            } else {
                folderBrowserDialog.Description = "Klasör Ekle";
            }

            if ((bool) folderBrowserDialog.ShowDialog()) {
                if (directories.Count == 0) {
                    console.Clear();

                    if (englishMenuItem.IsChecked) {
                        console.Add("Drag-Drop to add folders:");
                    } else {
                        console.Add("Klasör eklemek için sürükle-bırak:");
                    }
                }

                if (!directories.Contains(folderBrowserDialog.SelectedPath)) {
                    directories.Add(folderBrowserDialog.SelectedPath);

                    if (englishMenuItem.IsChecked) {
                        console.Insert(console.Count - 1, "Added " + folderBrowserDialog.SelectedPath);
                    } else {
                        console.Insert(console.Count - 1, "Klasör eklendi: " + folderBrowserDialog.SelectedPath);
                    }
                }
            }
        }

        private void findDuplicatesButton_Click(object sender, RoutedEventArgs e) {

            if (directories.Count > 0) {
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

                for (int i = 0; i < directories.Count; i++) {

                    if (includeSubfoldersMenuItem.IsChecked) {
                        if (englishMenuItem.IsChecked) {
                            console.Add("Comparing images from " + directories[i] + " and its subdirectories.");
                        } else {
                            console.Add(directories[i] + " konumundaki ve alt klasörlerindeki resimler karşılaştırılıyor.");
                        }
                    } else {
                        if (englishMenuItem.IsChecked) {
                            console.Add("Comparing images from " + directories[i] + ".");
                        } else {
                            console.Add(directories[i] + " konumundaki resimler karşılaştırılıyor.");
                        }
                    }
                }
                includeSubfolders = includeSubfoldersMenuItem.IsChecked;
                jpegMenuItemChecked = jpegMenuItem.IsChecked;
                gifMenuItemChecked = gifMenuItem.IsChecked;
                pngMenuItemChecked = pngMenuItem.IsChecked;
                bmpMenuItemChecked = bmpMenuItem.IsChecked;
                tiffMenuItemChecked = tiffMenuItem.IsChecked;
                icoMenuItemChecked = icoMenuItem.IsChecked;
                processThread = new Thread(run);
                processThread.Start();
            } else {
                if (englishMenuItem.IsChecked) {
                    console.Add("No directories added.");
                } else {
                    console.Add("Klasör eklenmedi.");
                }
            }
        }

        private void findDuplicatesButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {

            if (findDuplicatesButton.Visibility == Visibility.Visible) {
                saveResultsMenuItem.IsEnabled = true;
                loadResultsMenuItem.IsEnabled = true;
                jpegMenuItem.IsEnabled = true;
                bmpMenuItem.IsEnabled = true;
                pngMenuItem.IsEnabled = true;
                gifMenuItem.IsEnabled = true;
                tiffMenuItem.IsEnabled = true;
                icoMenuItem.IsEnabled = true;
                sendToRecycleBinMenuItem.IsEnabled = !sendToRecycleBinMenuItem.IsChecked;
                deletePermanentlyMenuItem.IsEnabled = !deletePermanentlyMenuItem.IsChecked;
                englishMenuItem.IsEnabled = !englishMenuItem.IsChecked;
                turkishMenuItem.IsEnabled = !turkishMenuItem.IsChecked;
                includeSubfoldersMenuItem.IsEnabled = true;
                skipFilesWithDifferentOrientationMenuItem.IsEnabled = true;
                findExactDuplicatesOnlyMenuItem.IsEnabled = true;
                resetToDefaultsMenuItem.IsEnabled = true;
                addFolderButton.IsEnabled = true;
                deleteSelectedButton.IsEnabled = true;
                removeFromListButton.IsEnabled = true;
                clearButton.IsEnabled = true;
            } else {
                saveResultsMenuItem.IsEnabled = false;
                loadResultsMenuItem.IsEnabled = false;
                jpegMenuItem.IsEnabled = false;
                bmpMenuItem.IsEnabled = false;
                pngMenuItem.IsEnabled = false;
                gifMenuItem.IsEnabled = false;
                tiffMenuItem.IsEnabled = false;
                icoMenuItem.IsEnabled = false;
                sendToRecycleBinMenuItem.IsEnabled = false;
                deletePermanentlyMenuItem.IsEnabled = false;
                englishMenuItem.IsEnabled = false;
                turkishMenuItem.IsEnabled = false;
                includeSubfoldersMenuItem.IsEnabled = false;
                skipFilesWithDifferentOrientationMenuItem.IsEnabled = false;
                findExactDuplicatesOnlyMenuItem.IsEnabled = false;
                resetToDefaultsMenuItem.IsEnabled = false;
                addFolderButton.IsEnabled = false;
                deleteSelectedButton.IsEnabled = false;
                removeFromListButton.IsEnabled = false;
                clearButton.IsEnabled = false;
            }
        }

        private void deleteSelectedButton_Click(object sender, RoutedEventArgs e) {

            List<ListViewDataItem> selectedItems1 = new List<ListViewDataItem>();
            List<ListViewDataItem> selectedItems2 = new List<ListViewDataItem>();

            for (int i = 0; i < bindingList1.Count; i++) {

                if (bindingList1[i].isSelected && !fileAddedBefore(selectedItems1, bindingList1[i]) && !fileAddedBefore(selectedItems2, bindingList1[i])) {
                    try {
                        if (deletePermanentlyMenuItem.IsChecked) {
                            FileSystem.DeleteFile(bindingList1[i].text, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        } else {
                            FileSystem.DeleteFile(bindingList1[i].text, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                    } catch {
                    }
                    selectedItems1.Add(bindingList1[i]);
                }
            }

            for (int i = 0; i < bindingList2.Count; i++) {

                if (bindingList2[i].isSelected && !fileAddedBefore(selectedItems1, bindingList2[i]) && !fileAddedBefore(selectedItems2, bindingList2[i])) {
                    try {
                        if (deletePermanentlyMenuItem.IsChecked) {
                            FileSystem.DeleteFile(bindingList2[i].text, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently);
                        } else {
                            FileSystem.DeleteFile(bindingList2[i].text, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                    } catch {
                    }
                    selectedItems2.Add(bindingList2[i]);
                }
            }

            int tempIndex1, tempIndex2;
            ListViewDataItem tempListViewDataItem;

            for (int i = 0; i < selectedItems1.Count; i++) {

                tempListViewDataItem = selectedItems1[i];
                tempIndex1 = findDuplicateIndex(bindingList1, tempListViewDataItem);
                tempIndex2 = findDuplicateIndex(bindingList2, tempListViewDataItem);

                if (tempIndex1 == -1 && tempIndex2 == -1) {
                    for (int j = 0; j < bindingList1.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text) || bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.RemoveAt(j);
                            bindingList2.RemoveAt(j);
                            j--;
                        }
                    }
                } else if (tempIndex1 != -1 && tempIndex2 != -1) {
                    bindingList2.RemoveAt(tempIndex2);

                    if (tempIndex1 > tempIndex2) {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1 - 1].text, bindingList2[tempIndex1 - 1].confidence, bindingList2[tempIndex1 - 1].hammingDistance));
                    } else {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList2[tempIndex1].confidence, bindingList2[tempIndex1].hammingDistance));
                    }

                    bindingList2.RemoveAt(tempIndex1);
                    bindingList1.RemoveAt(tempIndex1);

                    for (int j = 0; j < bindingList1.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                } else if (tempIndex1 != -1) {
                    for (int j = 0; j < bindingList1.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex1);
                    bindingList2.RemoveAt(tempIndex1);
                } else {
                    for (int j = 0; j < bindingList2.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex2);
                    bindingList2.RemoveAt(tempIndex2);
                }

                for (int j = 0; j < bindingList1.Count - 1; j++) {
                    for (int k = j + 1; k < bindingList1.Count; k++) {

                        if (bindingList1[j].text.Equals(bindingList1[k].text) && bindingList2[j].text.Equals(bindingList2[k].text) || bindingList1[j].text.Equals(bindingList2[k].text) && bindingList2[j].text.Equals(bindingList1[k].text)) {
                            bindingList1.RemoveAt(k);
                            bindingList2.RemoveAt(k);
                            k--;
                        }
                    }
                }
            }

            for (int i = 0; i < selectedItems2.Count; i++) {

                tempListViewDataItem = selectedItems2[i];
                tempIndex1 = findDuplicateIndex(bindingList1, tempListViewDataItem);
                tempIndex2 = findDuplicateIndex(bindingList2, tempListViewDataItem);

                if (tempIndex1 == -1 && tempIndex2 == -1) {
                    for (int j = 0; j < bindingList1.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text) || bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.RemoveAt(j);
                            bindingList2.RemoveAt(j);
                            j--;
                        }
                    }
                } else if (tempIndex1 != -1 && tempIndex2 != -1) {
                    bindingList2.RemoveAt(tempIndex2);

                    if (tempIndex1 > tempIndex2) {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1 - 1].text, bindingList2[tempIndex1 - 1].confidence, bindingList2[tempIndex1 - 1].hammingDistance));
                    } else {
                        bindingList2.Insert(tempIndex2, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList2[tempIndex1].confidence, bindingList2[tempIndex1].hammingDistance));
                    }
                    bindingList2.RemoveAt(tempIndex1);
                    bindingList1.RemoveAt(tempIndex1);

                    for (int j = 0; j < bindingList2.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList2[tempIndex2].text, bindingList2[j].confidence, bindingList2[j].hammingDistance));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList2[tempIndex2].text, bindingList2[j].confidence, bindingList2[j].hammingDistance));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                } else if (tempIndex1 != -1) {
                    for (int j = 0; j < bindingList1.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList2[tempIndex1].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex1);
                    bindingList2.RemoveAt(tempIndex1);
                } else {
                    for (int j = 0; j < bindingList2.Count; j++) {

                        if (bindingList1[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList1.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList1.RemoveAt(j + 1);
                        }

                        if (bindingList2[j].text.Equals(tempListViewDataItem.text)) {
                            bindingList2.Insert(j, new ListViewDataItem(bindingList1[tempIndex2].text, bindingList1[j].confidence, bindingList1[j].hammingDistance));
                            bindingList2.RemoveAt(j + 1);
                        }
                    }
                    bindingList1.RemoveAt(tempIndex2);
                    bindingList2.RemoveAt(tempIndex2);
                }

                for (int j = 0; j < bindingList1.Count - 1; j++) {
                    for (int k = j + 1; k < bindingList1.Count; k++) {

                        if (bindingList1[j].text.Equals(bindingList1[k].text) && bindingList2[j].text.Equals(bindingList2[k].text) || bindingList1[j].text.Equals(bindingList2[k].text) && bindingList2[j].text.Equals(bindingList1[k].text)) {
                            bindingList1.RemoveAt(k);
                            bindingList2.RemoveAt(k);
                            k--;
                        }
                    }
                }
            }

            if (sendToRecycleBinMenuItem.IsChecked) {
                if (englishMenuItem.IsChecked) {
                    console.Add((selectedItems1.Count + selectedItems2.Count) + " file(s) have been sent to recycle bin.");
                } else {
                    console.Add((selectedItems1.Count + selectedItems2.Count) + " dosya geri dönüşüm kutusuna gönderildi.");
                }
            } else {
                if (englishMenuItem.IsChecked) {
                    console.Add((selectedItems1.Count + selectedItems2.Count) + " file(s) have been deleted.");
                } else {
                    console.Add((selectedItems1.Count + selectedItems2.Count) + " dosya silindi.");
                }
            }
        }

        private void removeFromListButton_Click(object sender, RoutedEventArgs e) {

            List<int> selectedIndices = new List<int>();

            for (int i = 0; i < bindingList1.Count; i++) {

                if (bindingList1[i].isSelected) {
                    selectedIndices.Add(i);
                }
            }

            for (int i = 0; i < bindingList2.Count; i++) {

                if (bindingList2[i].isSelected) {
                    selectedIndices.Add(i);
                }
            }

            selectedIndices.Sort();

            for (int i = selectedIndices.Count - 1; i > -1; i--) {

                bindingList1.RemoveAt(selectedIndices[i]);
                bindingList2.RemoveAt(selectedIndices[i]);
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e) {

            directories.Clear();
            files.Clear();
            console.Clear();
            bindingList1.Clear();
            bindingList2.Clear();
            list1.Clear();
            list2.Clear();
            percentage.Value = 0;

            if (englishMenuItem.IsChecked) {
                console.Add("Drag-Drop to add folders:");
            } else {
                console.Add("Klasör eklemek için sürükle-bırak:");
            }
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e) {

            if (pauseButton.Tag.Equals("0")) {
                pauseButton.Tag = "1";

                if (englishMenuItem.IsChecked) {
                    pauseButton.Content = "Resume";
                    console.Add("Paused.");
                } else {
                    pauseButton.Content = "Devam Et";
                    console.Add("Duraklatıldı.");
                }

                pausedFirstTime = DateTime.Now.ToFileTime();

                bool otherThreadsRun = false;

                for (int i = 0; i < threadList.Count; i++) {

                    if (threadList.ElementAt(i).IsAlive) {
                        threadList.ElementAt(i).Suspend();
                        otherThreadsRun = true;
                    }
                }

                if (!otherThreadsRun) {
                    processThread.Suspend();
                }
            } else {
                pauseButton.Tag = "0";

                if (englishMenuItem.IsChecked) {
                    pauseButton.Content = "Pause";
                } else {
                    pauseButton.Content = "Duraklat";
                }

                console.RemoveAt(console.Count - 1);
                pausedSecondTime = DateTime.Now.ToFileTime();
                pauseTime += pausedSecondTime - pausedFirstTime;
                bool otherThreadsRun = false;

                for (int i = 0; i < threadList.Count; i++) {

                    if (threadList.ElementAt(i).ThreadState.Equals(ThreadState.Suspended)) {
                        threadList.ElementAt(i).Resume();
                        otherThreadsRun = true;
                    }
                }

                if (!otherThreadsRun) {
                    processThread.Resume();
                }
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e) {

            findDuplicatesButton.Visibility = Visibility.Visible;
            pauseButton.Visibility = Visibility.Collapsed;
            stopButton.Visibility = Visibility.Collapsed;

            for (int i = 0; i < threadList.Count; i++) {

                if (threadList.ElementAt(i).ThreadState.Equals(ThreadState.Suspended)) {
                    threadList.ElementAt(i).Resume();
                }
            }

            for (int i = 0; i < threadList.Count; i++) {
                try {
                    threadList.ElementAt(i).Abort();
                } catch {
                }
            }

            try {
                processThread.Abort();
            } catch (ThreadStateException) {
                processThread.Resume();
            }

            if (englishMenuItem.IsChecked) {
                pauseButton.Content = "Pause";
            } else {
                pauseButton.Content = "Duraklat";
            }

            if (pauseButton.Tag.Equals("1")) {
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

            if (englishMenuItem.IsChecked) {
                console[console.Count - 1] = "Interrupted by user.";
            } else {
                console[console.Count - 1] = "Kullanıcı tarafından durduruldu.";
            }
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e) {

            if (e.Key == Key.Enter && listView1.SelectedIndex != -1) {
                try {
                    System.Diagnostics.Process.Start(bindingList1[listView1.SelectedIndex].text);
                } catch {
                    Console.WriteLine("Oops3");
                }
            } else if (e.Key == Key.Right && listView1.SelectedIndex != -1) {
                listView2.SelectedIndex = listView1.SelectedIndex;
                ListViewItem item = listView2.ItemContainerGenerator.ContainerFromIndex(listView2.SelectedIndex) as ListViewItem;
                Keyboard.Focus(item);
            }
        }

        private void listView2_KeyDown(object sender, KeyEventArgs e) {

            if (e.Key == Key.Enter && listView2.SelectedIndex != -1) {
                try {
                    System.Diagnostics.Process.Start(bindingList2[listView2.SelectedIndex].text);
                } catch {
                    Console.WriteLine("Oops4");
                }
            } else if (e.Key == Key.Left && listView2.SelectedIndex != -1) {
                listView1.SelectedIndex = listView2.SelectedIndex;
                ListViewItem item = listView1.ItemContainerGenerator.ContainerFromIndex(listView1.SelectedIndex) as ListViewItem;
                Keyboard.Focus(item);
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            ListViewDataItem item = ((FrameworkElement) e.OriginalSource).DataContext as ListViewDataItem;

            if (item != null) {
                if (listView1.SelectedIndex != -1) {
                    try {
                        System.Diagnostics.Process.Start(bindingList1[listView1.SelectedIndex].text);
                    } catch {
                        Console.WriteLine("Oops1");
                    }
                }
            }
        }

        private void listView2_MouseDoubleClick(object sender, MouseButtonEventArgs e) {

            ListViewDataItem item = ((FrameworkElement) e.OriginalSource).DataContext as ListViewDataItem;

            if (item != null) {
                if (listView2.SelectedIndex != -1) {
                    try {
                        System.Diagnostics.Process.Start(bindingList2[listView2.SelectedIndex].text);
                    } catch {
                        Console.WriteLine("Oops2");
                    }
                }
            }
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (listView1.SelectedIndex == -1) {
                previewLabel.Visibility = Visibility.Visible;
                informationLabel1.Content = "";
                informationLabel2.Content = "";
                ReloadImage1(null);
                ReloadImage2(null);
            } else {
                bindingList2[listView1.Items.IndexOf(listView1.SelectedItems[listView1.SelectedItems.Count - 1])].isSelected = false;
                previewLabel.Visibility = Visibility.Collapsed;
                ReloadImage1(bindingList1.ElementAt(listView1.SelectedIndex).text);
                informationLabel1.Content = bindingList1.ElementAt(listView1.SelectedIndex).text.Substring(bindingList1.ElementAt(listView1.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView1.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView1.SelectedIndex).text)].Height;
                ReloadImage2(bindingList2.ElementAt(listView1.SelectedIndex).text);
                informationLabel2.Content = bindingList2.ElementAt(listView1.SelectedIndex).text.Substring(bindingList2.ElementAt(listView1.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView1.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView1.SelectedIndex).text)].Height;
            }
        }

        private void listView2_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (listView2.SelectedIndex == -1) {
                previewLabel.Visibility = Visibility.Visible;
                informationLabel1.Content = "";
                informationLabel2.Content = "";
                ReloadImage1(null);
                ReloadImage2(null);
            } else {
                bindingList1[listView2.Items.IndexOf(listView2.SelectedItems[listView2.SelectedItems.Count - 1])].isSelected = false;
                previewLabel.Visibility = Visibility.Collapsed;
                ReloadImage1(bindingList1.ElementAt(listView2.SelectedIndex).text);
                informationLabel1.Content = bindingList1.ElementAt(listView2.SelectedIndex).text.Substring(bindingList1.ElementAt(listView2.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView2.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList1.ElementAt(listView2.SelectedIndex).text)].Height;
                ReloadImage2(bindingList2.ElementAt(listView2.SelectedIndex).text);
                informationLabel2.Content = bindingList2.ElementAt(listView2.SelectedIndex).text.Substring(bindingList2.ElementAt(listView2.SelectedIndex).text.LastIndexOf("\\") + 1) + " - " + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView2.SelectedIndex).text)].Width + "x" + resolutionArray[files.IndexOf(bindingList2.ElementAt(listView2.SelectedIndex).text)].Height;
            }
        }

        private void listView1_ScrollChanged(object sender, RoutedEventArgs e) {

            ScrollViewer listView1ScrollViewer = GetDescendantByType(listView1, typeof(ScrollViewer)) as ScrollViewer;
            ScrollViewer listView2ScrollViewer = GetDescendantByType(listView2, typeof(ScrollViewer)) as ScrollViewer;
            listView2ScrollViewer.ScrollToVerticalOffset(listView1ScrollViewer.VerticalOffset);
        }

        private void listView2_ScrollChanged(object sender, RoutedEventArgs e) {

            ScrollViewer listView1ScrollViewer = GetDescendantByType(listView1, typeof(ScrollViewer)) as ScrollViewer;
            ScrollViewer listView2ScrollViewer = GetDescendantByType(listView2, typeof(ScrollViewer)) as ScrollViewer;
            listView1ScrollViewer.ScrollToVerticalOffset(listView2ScrollViewer.VerticalOffset);
        }

        private void outputListView_DragEnter(object sender, DragEventArgs e) {

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && findDuplicatesButton.Visibility == Visibility.Visible) {
                e.Effects = DragDropEffects.All;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void outputListView_Drop(object sender, DragEventArgs e) {

            if (findDuplicatesButton.Visibility == Visibility.Visible) {
                string[] dragDrop = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
                DirectoryInfo directoryInfo;

                if (directories.Count == 0) {
                    console.Clear();

                    if (englishMenuItem.IsChecked) {
                        console.Add("Drag-Drop to add folders:");
                    } else {
                        console.Add("Klasör eklemek için sürükle-bırak:");
                    }
                }

                for (int i = 0; i < dragDrop.Length; i++) {

                    directoryInfo = new DirectoryInfo(dragDrop[i]);

                    if (!directories.Contains(dragDrop[i])) {
                        if (directoryInfo.Exists) {
                            directories.Add(dragDrop[i]);
                            if (englishMenuItem.IsChecked) {
                                console.Insert(console.Count - 1, "Added " + dragDrop[i]);
                            } else {
                                console.Insert(console.Count - 1, "Klasör eklendi: " + dragDrop[i]);
                            }
                        } else {
                            if (englishMenuItem.IsChecked) {
                                console.Insert(console.Count - 1, "Directories only.");
                            } else {
                                console.Insert(console.Count - 1, "Sadece klasörler eklenebilir.");
                            }
                        }
                    }
                }
            }
        }

        private void percentageChanged(object sender, EventArgs e) {

            Action<int> updateProgressBar = delegate (int value) {
                progressBar.Value = value;

                if (!findDuplicatesButton.IsVisible) {
                    if (comparing) {
                        if (englishMenuItem.IsChecked) {
                            console[console.Count - 1] = value + "% done, comparing results...";
                        } else {
                            console[console.Count - 1] = value + "% tamamlandı, karşılaştırma yapılıyor...";
                        }
                    } else {
                        if (englishMenuItem.IsChecked) {
                            console[console.Count - 1] = value + "% done, processing files...";
                        } else {
                            console[console.Count - 1] = value + "% tamamlandı, dosyalar işleniyor...";
                        }
                    }
                }
            };

            lock (myLock) {
                Dispatcher.Invoke(DispatcherPriority.Normal, updateProgressBar, percentage.Value);
            }
        }

        private void previewImageBorder_ManipulationDelta(object sender, ManipulationDeltaEventArgs e) {

            var st1 = (ScaleTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var st2 = (ScaleTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt1 = (TranslateTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
            var tt2 = (TranslateTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);

            st1.ScaleX *= e.DeltaManipulation.Scale.X;
            st2.ScaleX *= e.DeltaManipulation.Scale.X;
            st1.ScaleY *= e.DeltaManipulation.Scale.X;
            st2.ScaleY *= e.DeltaManipulation.Scale.X;
            tt1.X += e.DeltaManipulation.Translation.X;
            tt2.X += e.DeltaManipulation.Translation.X;
            tt1.Y += e.DeltaManipulation.Translation.Y;
            tt2.Y += e.DeltaManipulation.Translation.Y;
        }

        private void previewImage_MouseWheel(object sender, MouseWheelEventArgs e) {

            var zoom = e.Delta > 0 ? .2 : -.2;
            var position = e.GetPosition(previewImage1);
            previewImage1.RenderTransformOrigin = new System.Windows.Point(position.X / previewImage1.ActualWidth, position.Y / previewImage1.ActualHeight);
            previewImage2.RenderTransformOrigin = new System.Windows.Point(position.X / previewImage2.ActualWidth, position.Y / previewImage2.ActualHeight);
            var st1 = (ScaleTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var st2 = (ScaleTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);

            if (zoom < 0 && (st1.ScaleX <= 1 || st2.ScaleX <= 1)) {
                return;
            }
            st1.ScaleX += zoom;
            st2.ScaleX += zoom;
            st1.ScaleY += zoom;
            st2.ScaleY += zoom;
        }

        private void previewImage1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            if (e.ClickCount == 2) {
                ResetPanZoom1();
                ResetPanZoom2();
            } else {
                previewImage1.CaptureMouse();
                var tt1 = (TranslateTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                previewImage1Start = e.GetPosition(this);
                previewImage2Start = e.GetPosition(this);
                previewImage1Origin = new System.Windows.Point(tt1.X, tt1.Y);
                previewImage2Origin = new System.Windows.Point(tt2.X, tt2.Y);
            }
        }

        private void previewImage2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            if (e.ClickCount == 2) {
                ResetPanZoom1();
                ResetPanZoom2();
            } else {
                previewImage2.CaptureMouse();
                var tt1 = (TranslateTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                previewImage1Start = e.GetPosition(this);
                previewImage2Start = e.GetPosition(this);
                previewImage1Origin = new System.Windows.Point(tt1.X, tt1.Y);
                previewImage2Origin = new System.Windows.Point(tt2.X, tt2.Y);
            }
        }

        private void previewImage1_MouseMove(object sender, MouseEventArgs e) {

            if (previewImage1.IsMouseCaptured) {
                var tt1 = (TranslateTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var v1 = previewImage1Start - e.GetPosition(this);
                var v2 = previewImage2Start - e.GetPosition(this);
                tt1.X = previewImage1Origin.X - v1.X;
                tt2.X = previewImage2Origin.X - v2.X;
                tt1.Y = previewImage1Origin.Y - v1.Y;
                tt2.Y = previewImage2Origin.Y - v2.Y;
            }
        }

        private void previewImage2_MouseMove(object sender, MouseEventArgs e) {

            if (previewImage2.IsMouseCaptured) {
                var tt1 = (TranslateTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var tt2 = (TranslateTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
                var v1 = previewImage1Start - e.GetPosition(this);
                var v2 = previewImage2Start - e.GetPosition(this);
                tt1.X = previewImage1Origin.X - v1.X;
                tt2.X = previewImage2Origin.X - v2.X;
                tt1.Y = previewImage1Origin.Y - v1.Y;
                tt2.Y = previewImage2Origin.Y - v2.Y;
            }
        }

        private void previewImage1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {

            previewImage1.ReleaseMouseCapture();
        }

        private void previewImage2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {

            previewImage2.ReleaseMouseCapture();
        }

        protected MainWindow(SerializationInfo info, StreamingContext ctxt) {

            jpegMenuItemChecked = (bool) info.GetValue("jpegMenuItemChecked", typeof(bool));
            gifMenuItemChecked = (bool) info.GetValue("gifMenuItemChecked", typeof(bool));
            pngMenuItemChecked = (bool) info.GetValue("pngMenuItemChecked", typeof(bool));
            bmpMenuItemChecked = (bool) info.GetValue("bmpMenuItemChecked", typeof(bool));
            tiffMenuItemChecked = (bool) info.GetValue("tiffMenuItemChecked", typeof(bool));
            icoMenuItemChecked = (bool) info.GetValue("icoMenuItemChecked", typeof(bool));
            sendsToRecycleBin = (bool) info.GetValue("sendsToRecycleBin", typeof(bool));
            isEnglish = (bool) info.GetValue("isEnglish", typeof(bool));
            includeSubfolders = (bool) info.GetValue("includeSubfolders", typeof(bool));
            skipFilesWithDifferentOrientation = (bool) info.GetValue("skipFilesWithDifferentOrientation", typeof(bool));
            duplicatesOnly = (bool) info.GetValue("duplicatesOnly", typeof(bool));
            files = (List<string>) info.GetValue("files", typeof(List<string>));
            resolutionArray = (System.Drawing.Size[]) info.GetValue("resolutionArray", typeof(System.Drawing.Size[]));
            bindingList1 = (ObservableCollection<ListViewDataItem>) info.GetValue("bindingList1", typeof(ObservableCollection<ListViewDataItem>));
            bindingList2 = (ObservableCollection<ListViewDataItem>) info.GetValue("bindingList2", typeof(ObservableCollection<ListViewDataItem>));
            console = (ObservableCollection<string>) info.GetValue("console", typeof(ObservableCollection<string>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {

            info.AddValue("jpegMenuItemChecked", jpegMenuItem.IsChecked);
            info.AddValue("gifMenuItemChecked", gifMenuItem.IsChecked);
            info.AddValue("pngMenuItemChecked", pngMenuItem.IsChecked);
            info.AddValue("bmpMenuItemChecked", bmpMenuItem.IsChecked);
            info.AddValue("tiffMenuItemChecked", tiffMenuItem.IsChecked);
            info.AddValue("icoMenuItemChecked", icoMenuItem.IsChecked);
            info.AddValue("sendsToRecycleBin", sendToRecycleBinMenuItem.IsChecked);
            info.AddValue("isEnglish", englishMenuItem.IsChecked);
            info.AddValue("includeSubfolders", includeSubfoldersMenuItem.IsChecked);
            info.AddValue("skipFilesWithDifferentOrientation", skipFilesWithDifferentOrientationMenuItem.IsChecked);
            info.AddValue("duplicatesOnly", findExactDuplicatesOnlyMenuItem.IsChecked);
            info.AddValue("files", files);
            info.AddValue("resolutionArray", resolutionArray);
            info.AddValue("bindingList1", bindingList1);
            info.AddValue("bindingList2", bindingList2);
            info.AddValue("console", console);
        }

        public void serialize(string path) {

            Stream stream = File.Open(path, FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter.Serialize(stream, this);
            stream.Close();
        }

        public void deserialize(string path) {

            Stream stream = File.Open(path, FileMode.Open);
            BinaryFormatter bformatter = new BinaryFormatter();
            mainWindow = (MainWindow) bformatter.Deserialize(stream);
            stream.Close();

            if (opening) {
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

                if (!mainWindow.sendsToRecycleBin) {
                    sendToRecycleBinMenuItem.IsChecked = false;
                    deletePermanentlyMenuItem.IsChecked = true;
                    sendToRecycleBinMenuItem.IsEnabled = true;
                    deletePermanentlyMenuItem.IsEnabled = false;
                }

                if (!mainWindow.isEnglish) {
                    englishMenuItem.IsChecked = false;
                    turkishMenuItem.IsChecked = true;
                    englishMenuItem.IsEnabled = true;
                    turkishMenuItem.IsEnabled = false;
                    convertToTurkish();
                }
            } else {
                this.bindingList1 = mainWindow.bindingList1;
                this.bindingList2 = mainWindow.bindingList2;
                this.console = mainWindow.console;
                this.files = mainWindow.files;
                this.resolutionArray = mainWindow.resolutionArray;
                listView1.ItemsSource = bindingList1;
                listView2.ItemsSource = bindingList2;
                outputListView.ItemsSource = console;
            }
            mainWindow = null;
        }

        private void convertToEnglish() {

            fileMenuItem.Header = "File";
            saveResultsMenuItem.Header = "Save Results...";
            loadResultsMenuItem.Header = "Load Results...";
            exitMenuItem.Header = "Exit";
            optionsMenuItem.Header = "Options";
            searchFormatsMenuItem.Header = "Search Formats";
            deletionMethodMenuItem.Header = "Deletion Method";
            sendToRecycleBinMenuItem.Header = "Send To Recycle Bin";
            deletePermanentlyMenuItem.Header = "Delete Permanently";
            languageMenuItem.Header = "Language";
            includeSubfoldersMenuItem.Header = "Include Subfolders";
            skipFilesWithDifferentOrientationMenuItem.Header = "Skip Files With Different Orientation";
            findExactDuplicatesOnlyMenuItem.Header = "Find Exact Duplicates Only";
            resetToDefaultsMenuItem.Header = "Reset To Defaults";
            helpMenuItem.Header = "Help";
            howToUseMenuItem.Header = "How To Use";
            aboutMenuItem.Header = "About";
            addFolderButton.Content = "Add Folder";
            findDuplicatesButton.Content = "Find Duplicates";
            pauseButton.Content = "Pause";
            stopButton.Content = "Stop";
            deleteSelectedButton.Content = "Delete Selected";
            removeFromListButton.Content = "Remove From List";
            clearButton.Content = "Clear";
            previewLabel.Content = "Select a file to preview:";
            console.Clear();
            console.Add("Drag-Drop to add folders:");
        }

        private void convertToTurkish() {

            fileMenuItem.Header = "Dosya";
            saveResultsMenuItem.Header = "Sonuçları Kaydet...";
            loadResultsMenuItem.Header = "Sonuçları Yükle...";
            exitMenuItem.Header = "Çıkış";
            optionsMenuItem.Header = "Seçenekler";
            searchFormatsMenuItem.Header = "Aranacak Formatlar";
            deletionMethodMenuItem.Header = "Silme Yöntemi";
            sendToRecycleBinMenuItem.Header = "Geri Dönüşüm Kutusuna Gönder";
            deletePermanentlyMenuItem.Header = "Kalıcı Olarak Sil";
            languageMenuItem.Header = "Dil";
            includeSubfoldersMenuItem.Header = "Alt Klasörlerde Ara";
            skipFilesWithDifferentOrientationMenuItem.Header = "Farklı Oryantasyondaki Dosyaları Geç";
            findExactDuplicatesOnlyMenuItem.Header = "Sadece Kopyaları Bul";
            resetToDefaultsMenuItem.Header = "Varsayılan Ayarlara Dön";
            helpMenuItem.Header = "Yardım";
            howToUseMenuItem.Header = "Nasıl Kullanılır";
            aboutMenuItem.Header = "Hakkında";
            addFolderButton.Content = "Klasör Ekle";
            findDuplicatesButton.Content = "Kopyaları Bul";
            pauseButton.Content = "Duraklat";
            stopButton.Content = "Durdur";
            deleteSelectedButton.Content = "Seçilenleri Sil";
            removeFromListButton.Content = "Listeden Kaldır";
            clearButton.Content = "Temizle";
            previewLabel.Content = "Görüntülemek için bir dosya seçin:";
            console.Clear();
            console.Add("Klasör eklemek için sürükle-bırak:");
        }

        private Visual GetDescendantByType(Visual element, Type type) {

            if (element == null) {
                return null;
            } else if (element.GetType() == type) {
                return element;
            } else {
                Visual foundElement = null;

                if (element is FrameworkElement) {
                    (element as FrameworkElement).ApplyTemplate();
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) {

                    Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                    foundElement = GetDescendantByType(visual, type);

                    if (foundElement != null) {
                        break;
                    }
                }
                return foundElement;
            }
        }

        private void processThreadStart() {

            Bitmap image, resizedImage;
            FastDCT2D fastDCT2D;
            int[,] result;
            double average;
            int i;

            while (processThreadsiAsync < files.Count) {

                lock (myLock) {
                    i = processThreadsiAsync;
                    processThreadsiAsync++;
                    percentage.Value = 100 - (int) Math.Round(100.0 * (files.Count - i) / files.Count);
                }

                try {
                    image = new Bitmap(files[i]);
                    resizedImage = resizeImage(image, 32, 32);
                    fastDCT2D = new FastDCT2D(resizedImage, 32);
                    result = fastDCT2D.FastDCT();
                    average = 0;

                    resolutionArray[i] = image.Size;

                    if (image.Width > image.Height) {
                        orientationArray[i] = Orientation.Horizontal;
                    } else {
                        orientationArray[i] = Orientation.Vertical;
                    }

                    for (int j = 0; j < 8; j++) {
                        for (int k = 0; k < 8; k++) {
                            average += result[j, k];
                        }
                    }

                    average -= result[0, 0];
                    average /= 63;

                    for (int j = 0; j < 8; j++) {
                        for (int k = 0; k < 8; k++) {

                            if (result[j, k] < average) {
                                hashArray[i, j * 8 + k] = 0;
                            } else {
                                hashArray[i, j * 8 + k] = 1;
                            }
                        }
                    }
                    image.Dispose();
                    resizedImage.Dispose();
                } catch (ArgumentException) {
                    hashArray[i, 0] = -1;
                } catch {
                }
            }
        }

        /*private void processThreadStart() {

            Bitmap image, resizedImage;
            int i;

            while (processThreadsiAsync < files.Count) {

                lock (myLock) {
                    i = processThreadsiAsync;
                    processThreadsiAsync++;
                    percentage.Value = 100 - (int) Math.Round(100.0 * (files.Count - i) / files.Count);
                }

                try {
                    image = new Bitmap(files[i]);
                    resizedImage = resizeImage(image, 9, 8);
                    resizedImage = convertToGrayscale(resizedImage);
                    resolutionArray[i] = image.Size;

                    if (image.Width > image.Height) {
                        orientationArray[i] = Orientation.Horizontal;
                    } else {
                        orientationArray[i] = Orientation.Vertical;
                    }
                    
                    for (int j = 0; j < 8; j++) {
                        for (int k = 0; k < 8; k++) {

                            if (resizedImage.GetPixel(j, k).R < resizedImage.GetPixel(j + 1, k).R) {
                                hashArray[i, j * 8 + k] = 0;
                            } else {
                                hashArray[i, j * 8 + k] = 1;
                            }
                        }
                    }
                    image.Dispose();
                    resizedImage.Dispose();
                } catch (ArgumentException) {
                    hashArray[i, 0] = -1;
                } catch {
                }
            }
        }*/

        private void compareResultsThreadStart() {

            int i, j;
            bool isDuplicate;

            while (compareResultsiAsync < files.Count - 1) {

                lock (myLock) {
                    i = compareResultsiAsync;
                    compareResultsiAsync++;
                    percentage.Value = 100 - (int) Math.Round(100.0 * (files.Count - i) / files.Count);
                }

                for (j = i + 1; j < files.Count; j++) {
                    if (!skipFilesWithDifferentOrientation || orientationArray[i] == orientationArray[j]) {
                        isDuplicate = findPHashSimilarity(i, j);

                        if (isDuplicate) {
                            break;
                        }
                    }
                }
            }
        }

        private void run() {

            Action updateUI = delegate () {
                mergeSort(list1, list2);
                bindingList1 = new ObservableCollection<ListViewDataItem>(list1);
                bindingList2 = new ObservableCollection<ListViewDataItem>(list2);
                listView1.ItemsSource = bindingList1;
                listView2.ItemsSource = bindingList2;
                findDuplicatesButton.Visibility = Visibility.Visible;
                pauseButton.Visibility = Visibility.Collapsed;
                stopButton.Visibility = Visibility.Collapsed;

                if (englishMenuItem.IsChecked) {
                    pauseButton.Content = "Pause";
                } else {
                    pauseButton.Content = "Duraklat";
                }

                if (englishMenuItem.IsChecked) {
                    console.Add("All done!");
                } else {
                    console.Add("Tamamlandı!");
                }

                secondTime = DateTime.Now.ToFileTime();
                timeDifferenceInSeconds = (int) Math.Ceiling(((secondTime - firstTime - pauseTime) / 10000000.0));

                if (timeDifferenceInSeconds >= 3600) {
                    int hours = timeDifferenceInSeconds / 3600;
                    int minutes = (timeDifferenceInSeconds / 60) % 60;
                    int seconds = timeDifferenceInSeconds % 60;
                    if (englishMenuItem.IsChecked) {
                        console.Add("Run time is " + hours + " hours " + minutes + " minutes " + seconds + " seconds.");
                    } else {
                        console.Add("Çalışma süresi: " + hours + " saat " + minutes + " dakika " + seconds + " saniye.");
                    }
                } else if (timeDifferenceInSeconds >= 60) {
                    int minutes = timeDifferenceInSeconds / 60;
                    int seconds = timeDifferenceInSeconds % 60;
                    if (englishMenuItem.IsChecked) {
                        console.Add("Run time is " + minutes + " minutes " + seconds + " seconds.");
                    } else {
                        console.Add("Çalışma süresi: " + minutes + " dakika " + seconds + " saniye.");
                    }
                } else {
                    if (englishMenuItem.IsChecked) {
                        console.Add("Run time is " + timeDifferenceInSeconds + " seconds.");
                    } else {
                        console.Add("Çalışma süresi: " + timeDifferenceInSeconds + " saniye.");
                    }
                }

                if (englishMenuItem.IsChecked) {
                    if (duplicateImageCount == 0 && highConfidenceSimilarImageCount == 0 && mediumConfidenceSimilarImageCount == 0) {
                        console.Add("No duplicates found.");
                    } else {
                        if (duplicateImageCount > 0) {
                            console.Add(duplicateImageCount + " duplicates found.");
                        }

                        if (highConfidenceSimilarImageCount > 0) {
                            console.Add(highConfidenceSimilarImageCount + " images found with high similarity.");
                        }

                        if (mediumConfidenceSimilarImageCount > 0) {
                            console.Add(mediumConfidenceSimilarImageCount + " images found with medium similarity.");
                        }
                    }
                } else {
                    if (duplicateImageCount == 0 && highConfidenceSimilarImageCount == 0 && mediumConfidenceSimilarImageCount == 0) {
                        console.Add("Hiç kopya bulunamadı.");
                    } else {
                        if (duplicateImageCount > 0) {
                            console.Add(duplicateImageCount + " kopya bulundu.");
                        }

                        if (highConfidenceSimilarImageCount > 0) {
                            console.Add(highConfidenceSimilarImageCount + " yüksek benzerlikte resim bulundu.");
                        }

                        if (mediumConfidenceSimilarImageCount > 0) {
                            console.Add(mediumConfidenceSimilarImageCount + " orta benzerlikte resim bulundu.");
                        }
                    }
                }
            };

            Action updateConsole1 = delegate () {
                if (englishMenuItem.IsChecked) {
                    console.Add("Paths added successfully. " + files.Count + " images found. Reading files from disk...");
                    console.Add("0% done, processing files...");
                } else {
                    console.Add("Konumlar başarıyla eklendi. " + files.Count + " resim doysası bulundu. Dosyalar okunuyor...");
                    console.Add("0% tamamlandı, dosyalar işleniyor...");
                }
            };

            Action updateConsole2 = delegate () {
                if (englishMenuItem.IsChecked) {
                    console[console.Count - 1] = "All files have been processed. Comparing...";
                } else {
                    console[console.Count - 1] = "Bütün dosyalar işlendi. Karşılaştırma yapılıyor...";
                }
            };

            addFiles(directories);
            directories.Clear();
            Dispatcher.Invoke(DispatcherPriority.Normal, updateConsole1);

            processThreadsiAsync = 0;
            compareResultsiAsync = 0;
            duplicateImageCount = 0;
            highConfidenceSimilarImageCount = 0;
            mediumConfidenceSimilarImageCount = 0;
            hashArray = new int[files.Count, 64];
            orientationArray = new Orientation[files.Count];
            resolutionArray = new System.Drawing.Size[files.Count];
            threadList = new List<Thread>();

            for (int i = 0; i < Environment.ProcessorCount; i++) {

                threadList.Add(new Thread(processThreadStart));
                threadList.ElementAt(i).Start();
            }

            for (int i = 0; i < Environment.ProcessorCount; i++) {
                threadList.ElementAt(i).Join();
            }

            threadList = new List<Thread>();
            comparing = true;

            for (int i = 0; i < Environment.ProcessorCount; i++) {

                threadList.Add(new Thread(compareResultsThreadStart));
                threadList.ElementAt(i).Start();
            }

            for (int i = 0; i < Environment.ProcessorCount; i++) {
                threadList.ElementAt(i).Join();
            }

            percentage.Value = 100;
            Dispatcher.Invoke(DispatcherPriority.Normal, updateConsole2);
            Dispatcher.Invoke(DispatcherPriority.Normal, updateUI);
        }

        private void writeToFile(List<string> input) {

            streamWriter = new StreamWriter(path + @"\Bin\Directories.imc");
            streamWriter.WriteLine(input.Count);

            for (int i = 0; i < input.Count; i++) {
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

        private void addFiles(List<string> directory) {

            writeToFile(directory);
            System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo(path + (@"\Bin\AddFiles.exe"));
            processStartInfo.RedirectStandardOutput = false;
            processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            processStartInfo.UseShellExecute = true;
            process = System.Diagnostics.Process.Start(processStartInfo);
            process.WaitForExit();
            readFromFile();
        }

        private void readFromFile() {

            int count;
            streamReader = new StreamReader(path + @"\Bin\Results.imc");
            gotException = bool.Parse(streamReader.ReadLine());
            count = int.Parse(streamReader.ReadLine());

            for (int i = 0; i < count; i++) {
                files.Add(streamReader.ReadLine());
            }
            streamReader.Close();
            File.Delete(path + @"\Bin\Results.imc");
        }

        private Bitmap resizeImage(System.Drawing.Image image, int width, int height) {

            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        private Bitmap convertToGrayscale(Bitmap inputImage) {

            Bitmap cloneImage = (Bitmap) inputImage.Clone();
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
            graphics.DrawImage(cloneImage, new System.Drawing.Rectangle(0, 0, cloneImage.Width, cloneImage.Height), 0, 0, cloneImage.Width, cloneImage.Height, GraphicsUnit.Pixel, attributes);
            graphics.Dispose();
            return cloneImage;
        }

        private bool findPHashSimilarity(int i, int j) {

            if (hashArray[i, 0] != -1 && hashArray[j, 0] != -1) {
                int hammingDistance = 0;

                if (duplicatesOnly) {
                    for (int k = 0; k < 64; k++) {

                        if (hashArray[i, k] != hashArray[j, k]) {
                            return false;
                        }
                    }

                    lock (myLock2) {
                        list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.Duplicate, hammingDistance));
                        list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.Duplicate, hammingDistance));
                        duplicateImageCount++;
                    }
                    return true;
                } else {
                    for (int k = 1; k < 64; k++) {

                        if (hashArray[i, k] != hashArray[j, k]) {
                            hammingDistance++;

                            if (hammingDistance > 11) {
                                return false;
                            }
                        }
                    }

                    if (hammingDistance < 1) {
                        lock (myLock2) {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.Duplicate, hammingDistance));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.Duplicate, hammingDistance));
                            duplicateImageCount++;
                        }
                        return true;
                    } else if (hammingDistance < 9) {
                        lock (myLock2) {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.High, hammingDistance));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.High, hammingDistance));
                            highConfidenceSimilarImageCount++;
                        }
                    } else {
                        lock (myLock2) {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.Medium, hammingDistance));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.Medium, hammingDistance));
                            mediumConfidenceSimilarImageCount++;
                        }
                    }
                }
            }
            return false;
        }

        private bool findDHashSimilarity(int i, int j) {

            if (hashArray[i, 0] != -1 && hashArray[j, 0] != -1) {
                int hammingDistance = 0;

                if (duplicatesOnly) {
                    for (int k = 0; k < 64; k++) {

                        if (hashArray[i, k] != hashArray[j, k]) {
                            return false;
                        }
                    }

                    lock (myLock2) {
                        list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.Duplicate, hammingDistance));
                        list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.Duplicate, hammingDistance));
                        duplicateImageCount++;
                    }
                    return true;
                } else {
                    for (int k = 0; k < 64; k++) {

                        if (hashArray[i, k] != hashArray[j, k]) {
                            hammingDistance++;

                            if (hammingDistance > 11) {
                                return false;
                            }
                        }
                    }

                    if (hammingDistance < 1) {
                        lock (myLock2) {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.Duplicate, hammingDistance));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.Duplicate, hammingDistance));
                            duplicateImageCount++;
                        }
                        return true;
                    } else if (hammingDistance < 9) {
                        lock (myLock2) {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.High, hammingDistance));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.High, hammingDistance));
                            highConfidenceSimilarImageCount++;
                        }
                    } else {
                        lock (myLock2) {
                            list1.Add(new ListViewDataItem(files.ElementAt(i), (int) Confidence.Medium, hammingDistance));
                            list2.Add(new ListViewDataItem(files.ElementAt(j), (int) Confidence.Medium, hammingDistance));
                            mediumConfidenceSimilarImageCount++;
                        }
                    }
                }
            }
            return false;
        }

        private int findDuplicateIndex(ObservableCollection<ListViewDataItem> list, ListViewDataItem item) {

            for (int i = 0; i < list.Count; i++) {

                if (list[i].text.Equals(item.text) && list[i].confidence == (int) Confidence.Duplicate) {
                    return i;
                }
            }
            return -1;
        }

        private bool fileAddedBefore(List<ListViewDataItem> selectedList, ListViewDataItem data) {

            for (int i = 0; i < selectedList.Count; i++) {

                if (selectedList[i].text.Equals(data.text)) {
                    return true;
                }
            }
            return false;
        }

        private void ResetPanZoom1() {

            var st = (ScaleTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt = (TranslateTransform) ((TransformGroup) previewImage1.RenderTransform).Children.First(tr => tr is TranslateTransform);
            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            previewImage1.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void ResetPanZoom2() {

            var st = (ScaleTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is ScaleTransform);
            var tt = (TranslateTransform) ((TransformGroup) previewImage2.RenderTransform).Children.First(tr => tr is TranslateTransform);
            st.ScaleX = st.ScaleY = 1;
            tt.X = tt.Y = 0;
            previewImage2.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void ReloadImage1(string path) {

            if (path == null) {
                previewImage1.Source = null;
            } else {
                try {
                    ResetPanZoom1();
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmapImage.EndInit();
                    previewImage1.Source = bitmapImage;
                } catch {
                }
            }
        }

        private void ReloadImage2(string path) {

            if (path == null) {
                previewImage2.Source = null;
            } else {
                try {
                    ResetPanZoom2();
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bitmapImage.EndInit();
                    previewImage2.Source = bitmapImage;
                } catch {
                }
            }
        }

        private void mergeSort(List<ListViewDataItem> list1, List<ListViewDataItem> list2) {

            if (list1.Count < 2) {
                return;
            }

            int startL, startR, step = 1;

            while (step < list1.Count) {

                startL = 0;
                startR = step;

                while (startR + step <= list1.Count) {
                    mergeLists(list1, list2, startL, startL + step, startR, startR + step);
                    startL = startR + step;
                    startR = startL + step;
                }

                if (startR < list1.Count) {
                    mergeLists(list1, list2, startL, startL + step, startR, list1.Count);
                }
                step *= 2;
            }
        }

        private void mergeLists(List<ListViewDataItem> list1, List<ListViewDataItem> list2, int startL, int stopL, int startR, int stopR) {

            ListViewDataItem[] right1 = new ListViewDataItem[stopR - startR + 1];
            ListViewDataItem[] right2 = new ListViewDataItem[stopR - startR + 1];
            ListViewDataItem[] left1 = new ListViewDataItem[stopL - startL + 1];
            ListViewDataItem[] left2 = new ListViewDataItem[stopL - startL + 1];

            for (int i = 0, k = startR; i < (right1.Length - 1); ++i, ++k) {

                right1[i] = list1[k];
                right2[i] = list2[k];
            }

            for (int i = 0, k = startL; i < (left1.Length - 1); ++i, ++k) {

                left1[i] = list1[k];
                left2[i] = list2[k];
            }

            right1[right1.Length - 1] = new ListViewDataItem("", 0, 64);
            right2[right2.Length - 1] = new ListViewDataItem("", 0, 64);
            left1[left1.Length - 1] = new ListViewDataItem("", 0, 64);
            left2[left2.Length - 1] = new ListViewDataItem("", 0, 64);

            for (int k = startL, m = 0, n = 0; k < stopR; ++k) {

                if (left1[m].hammingDistance <= right1[n].hammingDistance) {
                    list1[k] = left1[m];
                    list2[k] = left2[m];
                    m++;
                } else {
                    list1[k] = right1[n];
                    list2[k] = right2[n];
                    n++;
                }
            }
        }
    }
}
