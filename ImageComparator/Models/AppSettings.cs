using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageComparator.Models
{
    /// <summary>
    /// Data Transfer Object for safe serialization of application state
    /// </summary>
    public class AppSettings
    {
        public int Version { get; set; } = 1; // For future schema changes
        
        // Menu Settings
        public bool JpegMenuItemChecked { get; set; }
        public bool GifMenuItemChecked { get; set; }
        public bool PngMenuItemChecked { get; set; }
        public bool BmpMenuItemChecked { get; set; }
        public bool TiffMenuItemChecked { get; set; }
        public bool IcoMenuItemChecked { get; set; }
        public bool SendsToRecycleBin { get; set; }
        public string CurrentLanguageCode { get; set; }
        
        // Options
        public bool IncludeSubfolders { get; set; }
        public bool SkipFilesWithDifferentOrientation { get; set; }
        public bool DuplicatesOnly { get; set; }
        
        // Data
        public List<string> Files { get; set; }
        public List<string> FalsePositiveList1 { get; set; }
        public List<string> FalsePositiveList2 { get; set; }
        public SerializableSize[] ResolutionArray { get; set; }
        public List<SerializableListViewDataItem> BindingList1 { get; set; }
        public List<SerializableListViewDataItem> BindingList2 { get; set; }
        public List<string> ConsoleMessages { get; set; }
    }

    /// <summary>
    /// Serializable wrapper for System.Drawing.Size
    /// </summary>
    public class SerializableSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
        
        public SerializableSize() { }
        
        public SerializableSize(Size size)
        {
            Width = size.Width;
            Height = size.Height;
        }
        
        public Size ToSize() => new Size(Width, Height);
    }

    /// <summary>
    /// Serializable version of ListViewDataItem
    /// </summary>
    public class SerializableListViewDataItem
    {
        public string Text { get; set; }
        public int Confidence { get; set; }
        public int PHashHammingDistance { get; set; }
        public int HdHashHammingDistance { get; set; }
        public int VdHashHammingDistance { get; set; }
        public int AHashHammingDistance { get; set; }
        public string Sha256Checksum { get; set; }
        public int State { get; set; }
        public bool IsChecked { get; set; }
        
        public SerializableListViewDataItem() { }
        
        public SerializableListViewDataItem(MainWindow.ListViewDataItem item)
        {
            Text = item.text;
            Confidence = item.confidence;
            PHashHammingDistance = item.pHashHammingDistance;
            HdHashHammingDistance = item.hdHashHammingDistance;
            VdHashHammingDistance = item.vdHashHammingDistance;
            AHashHammingDistance = item.aHashHammingDistance;
            Sha256Checksum = item.sha256Checksum;
            State = item.state;
            IsChecked = item.isChecked;
        }
        
        public MainWindow.ListViewDataItem ToListViewDataItem()
        {
            var item = new MainWindow.ListViewDataItem(
                Text, Confidence, PHashHammingDistance, 
                HdHashHammingDistance, VdHashHammingDistance, 
                AHashHammingDistance, Sha256Checksum
            );
            item.state = State;
            item.isChecked = IsChecked;
            return item;
        }
    }
}
