namespace Cobra.Engine.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Version => "1.0.0";
        string Category => "Editor Tools";
        string Description => string.Empty;
        void Initialize();
        void Update();
    }
}
