using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly NetworkService _networkService;

        [ObservableProperty]
        private string email = "";

        [ObservableProperty]
        private string password = "";

        [ObservableProperty]
        private string statusMessage = "Non connecté";

        [ObservableProperty]
        private bool isConnected = false;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isAdmin = false;

        public ProfileViewModel()
        {
            _networkService = NetworkService.Instance;
            CheckConnectionStatus();
        }

        private void CheckConnectionStatus()
        {
            IsConnected = !string.IsNullOrEmpty(_networkService.Token);
            IsAdmin = _networkService.IsAdmin;
            StatusMessage = IsConnected ? "✅ Connecté" : "❌ Non connecté";
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "⚠️ Email et mot de passe requis";
                return;
            }

            IsLoading = true;
            StatusMessage = "Connexion en cours...";

            try
            {
                bool local_success = await _networkService.LoginAsync(Email, Password);

                if (local_success)
                {
                    await _networkService.GetProfileAsync();
                    IsConnected = true;
                    IsAdmin = _networkService.IsAdmin;
                    StatusMessage = "✅ Connexion réussie !";
                }
                else
                {
                    IsConnected = false;
                    StatusMessage = "❌ Échec de la connexion";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = $"❌ Erreur: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            _networkService.Logout();
            IsConnected = false;
            IsAdmin = false;
            StatusMessage = "Vous n'êtes plus connecté";
            Email = "";
            Password = "";
        }

        [RelayCommand]
        private async Task GoToAdminAsync()
        {
            await Shell.Current.GoToAsync("///admin");
        }
    }
}
