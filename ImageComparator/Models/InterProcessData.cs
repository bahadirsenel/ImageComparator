using System.Collections.Generic;

namespace ImageComparator.Models
{
    /// <summary>
    /// Data structure for Directories.json inter-process communication
    /// </summary>
    public class DirectoriesData
    {
        public List<string> Directories { get; set; }
    }

    /// <summary>
    /// Data structure for Filters.json inter-process communication
    /// </summary>
    public class FiltersData
    {
        public bool IncludeSubfolders { get; set; }
        public bool JpegFiles { get; set; }
        public bool GifFiles { get; set; }
        public bool PngFiles { get; set; }
        public bool BmpFiles { get; set; }
        public bool TiffFiles { get; set; }
        public bool IcoFiles { get; set; }
    }

    /// <summary>
    /// Data structure for Results.json inter-process communication
    /// </summary>
    public class ResultsData
    {
        public bool GotException { get; set; }
        public List<string> Files { get; set; }
    }
}
