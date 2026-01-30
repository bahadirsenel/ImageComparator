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
        public int Version { get; set; } = 1;
        
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
}
