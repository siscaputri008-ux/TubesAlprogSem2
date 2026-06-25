namespace SistemPrediksiKelelahan
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set MainPage ke MainPage dengan BlazorWebView
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}