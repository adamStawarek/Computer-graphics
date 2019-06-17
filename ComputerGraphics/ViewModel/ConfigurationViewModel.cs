namespace ImageEditor.ViewModel
{
    public class ConfigurationViewModel
    {
        public string Description { get; }
        public double Value { get; set; }

        public ConfigurationViewModel(string description, double value)
        {
            Description = description;
            Value = value;
        }
    }
}