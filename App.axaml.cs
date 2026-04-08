using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LabelStudio.Services;
using LabelStudio.ViewModels;
using LabelStudio.Views;

namespace LabelStudio;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsService = new SettingsService();
            var authService = new AuthService(settingsService);

            var mainVm = new MainWindowViewModel(settingsService);

            void ShowLogin()
            {
                var loginVm = new LoginViewModel(authService);

                loginVm.SettingsRequested += (_, _) =>
                {
                    var settingsVm = new AuthSettingsViewModel(settingsService);
                    settingsVm.BackRequested += (_, _) => ShowLogin();
                    mainVm.AuthContent = settingsVm;
                };

                loginVm.LoginSuccessful += (_, _) =>
                {
                    mainVm.IsAuthenticated = true;
                    mainVm.AuthContent = null;
                };

                mainVm.AuthContent = loginVm;
            }

            // If no token saved, require login; otherwise go straight to main app.
            if (string.IsNullOrWhiteSpace(settingsService.AuthToken))
            {
                mainVm.IsAuthenticated = false;
                ShowLogin();
            }
            else
            {
                mainVm.IsAuthenticated = true;
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

