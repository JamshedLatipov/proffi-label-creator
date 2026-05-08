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
            var settingsService  = new SettingsService();
            var authService      = new AuthService(settingsService);
            var warehouseService = new WarehouseService(settingsService);

            var mainVm = new MainWindowViewModel(settingsService, warehouseService);

            void ShowLogin(string? message = null)
            {
                var loginVm = new LoginViewModel(authService);

                if (!string.IsNullOrEmpty(message))
                    loginVm.ErrorMessage = message;

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

                mainVm.IsAuthenticated = false;
                mainVm.AuthContent = loginVm;
            }

            mainVm.LogoutRequested += (_, _) =>
            {
                authService.Logout();
                ShowLogin();
            };

            warehouseService.SessionExpired += (_, _) =>
            {
                authService.Logout();
                ShowLogin("Сессия истекла. Пожалуйста, войдите снова.");
            };

            // If no token saved, require login; otherwise go straight to main app.
            if (string.IsNullOrWhiteSpace(settingsService.AuthToken))
            {
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

