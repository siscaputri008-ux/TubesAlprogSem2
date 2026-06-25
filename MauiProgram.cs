using Microsoft.Extensions.Logging;
using SistemPrediksiKelelahan.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace SistemPrediksiKelelahan
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // HAPUS: .UseMauiCameraView()  ← INI DIHAPUS!
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Services
            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<FaceRecognitionService>();
            builder.Services.AddSingleton<FatigueService>();
            builder.Services.AddSingleton<ChatbotService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}