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
        private string firstName = "";

        [ObservableProperty]
        private string lastName = "";

        [ObservableProperty]
        private string confirmPassword = "";

        [ObservableProperty]
        private bool showRegisterForm = false;

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
                    var profile = await _networkService.GetProfileAsync();
                    if (profile != null)
                    {
                        FirstName = profile.FirstName ?? "";
                        LastName = profile.LastName ?? "";
                        Email = profile.Email ?? Email;
                    }
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

        [RelayCommand]
        private void ToggleRegisterForm()
        {
            ShowRegisterForm = !ShowRegisterForm;
            StatusMessage = ShowRegisterForm ? "Créer un compte" : (IsConnected ? "✅ Connecté" : "❌ Non connecté");
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "⚠️ Email et mot de passe requis";
                return;
            }

            if (Password != ConfirmPassword)
            {
                StatusMessage = "⚠️ Les mots de passe ne correspondent pas";
                return;
            }

            IsLoading = true;
            StatusMessage = "Création du compte...";

            try
            {
                bool success = await _networkService.RegisterAsync(Email, Password, FirstName, LastName);

                if (success)
                {
                    StatusMessage = "✅ Compte créé ! Connexion...";
                    await LoginAsync();
                }
                else
                {
                    StatusMessage = "❌ Échec de la création du compte";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task UpdateProfileAsync()
        {
            IsLoading = true;
            StatusMessage = "Mise à jour du profil...";

            try
            {
                bool success = await _networkService.UpdateProfileAsync(FirstName, LastName);

                if (success)
                {
                    StatusMessage = "✅ Profil mis à jour !";
                }
                else
                {
                    StatusMessage = "❌ Échec de la mise à jour";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
