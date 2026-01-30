namespace ImageComparator.Models
{
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
