using MudBlazor;

namespace brickapp.Data.Services
{
    public class NotificationService(ISnackbar snackbar)
    {
        public void Success(string message)
        {
            snackbar.Add(message, Severity.Success);
        }

        public void Error(string message)
        {
            snackbar.Add(message, Severity.Error);
        }

        public void Warning(string message)
        {
            snackbar.Add(message, Severity.Warning);
        }

        public void Info(string message)
        {
            snackbar.Add(message, Severity.Info);
        }
    }
}
