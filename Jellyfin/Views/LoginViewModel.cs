using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

internal sealed partial class LoginViewModel : ObservableValidator
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinSdkSettings _sdkClientSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private bool _isInteractable;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _showErrorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    [Required(AllowEmptyStrings = false)]
    [NotifyDataErrorInfo]
    private string _userName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    [Required(AllowEmptyStrings = false)]
    [NotifyDataErrorInfo]
    private string _password;

    public LoginViewModel(
        AppSettings appSettings,
        JellyfinSdkSettings sdkClientSettings,
        JellyfinApiClient jellyfinApiClient,
        NavigationManager navigationManager)
    {
        _appSettings = appSettings;
        _sdkClientSettings = sdkClientSettings;
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        IsInteractable = true;
    }

    private bool CanSignIn() => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync(CancellationToken cancellationToken)
    {
        IsInteractable = false;
        ShowErrorMessage = false;

        try
        {
            ValidateAllProperties();
            if (HasErrors)
            {
                UpdateErrorMessage("Username and password are required");
                return;
            }

            Console.WriteLine($"Logging into {_sdkClientSettings.ServerUrl}");

            AuthenticateUserByName request = new()
            {
                Username = UserName,
                Pw = Password,
            };
            AuthenticationResult authenticationResult = await _jellyfinApiClient.Users.AuthenticateByName.PostAsync(request, cancellationToken: cancellationToken);

            string accessToken = authenticationResult.AccessToken;

            // TODO: Save creds separately for each server
            _appSettings.AccessToken = accessToken;

            _sdkClientSettings.SetAccessToken(accessToken);

            Console.WriteLine("Authentication success.");

            _navigationManager.NavigateToHome();

            // After signing in, disallow accidentally coming back here.
            _navigationManager.ClearHistory();
        }
        catch (Exception ex)
        {
            // TODO: Need a friendlier message.
            UpdateErrorMessage(ex.Message);
            return;
        }
        finally
        {
            IsInteractable = true;
        }
    }

    [RelayCommand]
    private void ChangeServer()
    {
        _navigationManager.NavigateToServerSelection();
        _navigationManager.ClearHistory();
    }

    private void UpdateErrorMessage(string message)
    {
        ShowErrorMessage = true;
        ErrorMessage = message;
    }
}
