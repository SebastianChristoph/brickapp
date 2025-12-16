using System;
using System.Threading.Tasks;

namespace Services
{
    public class LoadingService
    {
        public event Action<string?>? OnShow;
        public event Action? OnHide;
        public bool IsLoading { get; private set; }

        public void Show(string? message = null)
        {
            IsLoading = true;
            OnShow?.Invoke(message);
        }

        public void Hide()
        {
            IsLoading = false;
            OnHide?.Invoke();
        }
    }
}