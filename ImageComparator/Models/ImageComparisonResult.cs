using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace ImageComparator.Models
{
    /// <summary>
    /// Represents the result of an image comparison operation.
    /// Contains file path, similarity metrics, and UI state.
    /// </summary>
    [Serializable]
    public class ImageComparisonResult : INotifyPropertyChanged
    {
        private bool _selected;
        private int _state;
        private bool _isChecked;
        private bool _checkboxEnabled;

        public string Text { get; set; }
        public int Confidence { get; set; }
        public int PHashHammingDistance { get; set; }
        public int HdHashHammingDistance { get; set; }
        public int VdHashHammingDistance { get; set; }
        public int AHashHammingDistance { get; set; }
        public string Sha256Checksum { get; set; }

        public bool IsSelected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public int State
        {
            get { return _state; }
            set
            {
                _state = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
            }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public bool CheckboxEnabled
        {
            get { return _checkboxEnabled; }
            set
            {
                _checkboxEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckboxEnabled)));
            }
        }

        public ImageComparisonResult(string text, int confidence, int pHashHammingDistance, 
            int hdHashHammingDistance, int vdHashHammingDistance, int aHashHammingDistance, 
            string sha256Checksum)
        {
            Text = text;
            Confidence = confidence;
            PHashHammingDistance = pHashHammingDistance;
            HdHashHammingDistance = hdHashHammingDistance;
            VdHashHammingDistance = vdHashHammingDistance;
            AHashHammingDistance = aHashHammingDistance;
            Sha256Checksum = sha256Checksum;
            IsSelected = false;
            State = 0; // Normal state
            IsChecked = false;
            CheckboxEnabled = true;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
