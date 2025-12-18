using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class MenuViewModel : ObservableObject
    {
        [RelayCommand]
        public void GoToProfile() => NavigationService.Instance.Navigate<Profile>([]);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotAdmin))]
        private bool isConnected = NetworkService.Instance.Token != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotAdmin))]
        private bool isAdmin = NetworkService.Instance.IsAdmin;

        public bool IsNotAdmin => IsConnected && !IsAdmin;

        public MenuViewModel()
        {
            NetworkService.Instance.OnConnected += Instance_OnConnected;
            NetworkService.Instance.OnDisconnected += Instance_OnDisconnected;
            NetworkService.Instance.PropertyChanged += Instance_PropertyChanged;
        }

        private void Instance_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(NetworkService.IsAdmin))
            {
                IsAdmin = NetworkService.Instance.IsAdmin;
                OnPropertyChanged(nameof(IsNotAdmin));
            }
        }

        private void Instance_OnConnected()
        {
            IsConnected = true;
            IsAdmin = NetworkService.Instance.IsAdmin;
            OnPropertyChanged(nameof(IsNotAdmin));
        }

        private void Instance_OnDisconnected()
        {
            IsConnected = false;
            IsAdmin = false;
            OnPropertyChanged(nameof(IsNotAdmin));
        }
    }
}
