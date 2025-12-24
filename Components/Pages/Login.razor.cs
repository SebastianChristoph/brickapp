using brickapp.Components.Dialogs;
using MudBlazor;

namespace brickapp.Components.Pages;

public partial class Login
{
    private readonly DialogOptions _dialogOptions = new()
    {
        BackdropClick = false,
        CloseButton = true,
        CloseOnEscapeKey = true
    };

    private bool _acceptTerms;
    private string? _error;
    private string _newUsername = string.Empty;
    private string? _signupError;
    private string _token = string.Empty;

    private async Task LoginWithToken()
    {
        _error = null;

        var success = await UserService.LoginWithTokenAsync(_token);

        if (!success)
        {
            _error = "Invalid token. Please check.";
            return;
        }

        // Track login
        await TrackingService.TrackAsync("UserLogin", null, "/login");

        // After successful login, navigate somewhere
        Nav.NavigateTo("/", true); // forceLoad: true
    }

    private async Task SignUp()
    {
        _signupError = null;

        if (string.IsNullOrWhiteSpace(_newUsername))
        {
            _signupError = "Please enter a username.";
            return;
        }

        var user = await UserService.AddUserAsync(_newUsername);

        if (user == null)
        {
            _signupError = "Error creating user. Please try again.";
            return;
        }

        // Track signup (will use the new user's token)
        await TrackingService.TrackAsync("UserSignup", $"Username: {user.Name}", "/login");

        // Show dialog with token
        var parameters = new DialogParameters
        {
            { "Token", user.Uuid },
            { "Username", user.Name }
        };


        await DialogService.ShowAsync<TokenDisplayDialog>("Your Access Token", parameters, _dialogOptions);

        // Clear form
        _newUsername = string.Empty;
    }
}