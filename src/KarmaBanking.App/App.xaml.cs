using Microsoft.UI.Xaml;

namespace KarmaBanking.App
{
    public partial class App : Application
    {
        private Window? _window;

        public static Window MainAppWindow { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainAppWindow = _window;
            _window.Activate();
        }
    }
}