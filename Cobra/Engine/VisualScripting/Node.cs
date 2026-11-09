namespace Cobra.Engine.VisualScripting
{
    public class Node
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Category { get; set; } = "Events";
        public string Type { get; set; } = "OnStart";
        public string Title { get; set; } = "On Start";

        public double X { get; set; }
        public double Y { get; set; }

        public Dictionary<string, string> Properties { get; set; } = [];
        public List<string> Outputs { get; set; } = [];
    }
}
