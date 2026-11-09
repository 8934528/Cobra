using System.Collections.ObjectModel;

namespace Cobra.Editor
{
    public class FileNode : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _iconKind = "File";
        private bool _isExpanded;
        private bool _isSelected;

        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(); } 
        }

        public string IconKind 
        { 
            get => _iconKind; 
            set { _iconKind = value; OnPropertyChanged(); } 
        }

        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public ObservableCollection<FileNode> Children { get; set; } = [];

        public bool IsExpanded 
        { 
            get => _isExpanded; 
            set { _isExpanded = value; OnPropertyChanged(); } 
        }

        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; OnPropertyChanged(); } 
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
