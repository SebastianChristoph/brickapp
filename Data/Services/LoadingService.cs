using System;
using System.Threading.Tasks;

namespace Services
{
    public class LoadingService
    {
        public event Action? OnShow;
        public event Action? OnHide;
        public bool IsLoading { get; private set; }

        public void Show()
        {
            IsLoading = true;
            OnShow?.Invoke();
        }

        public void Hide()
        {
            IsLoading = false;
            OnHide?.Invoke();
        }
    }
}