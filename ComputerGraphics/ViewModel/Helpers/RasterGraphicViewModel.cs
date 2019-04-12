namespace ImageEditor.ViewModel.Helpers
{
    public class RasterGraphicViewModel
    {
        public RasterGraphicViewModel(string type, bool isSelected)
        {
            Type = type;
            IsSelected = isSelected;
        }

        public string Type { get; set; }
        public bool IsSelected { get; set; }
    }
}