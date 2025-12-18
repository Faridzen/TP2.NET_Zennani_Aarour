using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Dtos;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class GameDetailsViewModel : ObservableObject
    {
        private readonly NetworkService _networkService;

        [ObservableProperty]
        private GameDto? game;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isOwned = false;

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private bool isAdmin = false;

        public bool IsNotAdmin => !IsAdmin;

        public GameDetailsViewModel()
        {
            _networkService = NetworkService.Instance;
            UpdateAdminStatus();
        }

        private void UpdateAdminStatus()
        {
            IsAdmin = _networkService.IsAdmin;
            OnPropertyChanged(nameof(IsNotAdmin));
        }

        public async Task LoadGameAsync(int gameId)
        {
            IsLoading = true;
            StatusMessage = "Chargement...";

            try
            {
                // Charger les détails du jeu
                Game = await _networkService.GetGameDetailsAsync(gameId);

                if (Game != null)
                {
                    StatusMessage = "";
                    
                    // Vérifier si le jeu est possédé
                    if (!string.IsNullOrEmpty(_networkService.Token))
                    {
                        await CheckOwnershipAsync();
                    }
                }
                else
                {
                    StatusMessage = "Jeu introuvable";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckOwnershipAsync()
        {
            try
            {
                var local_myGames = await _networkService.GetMyGamesAsync(0, 100);
                IsOwned = local_myGames.Items.Any(g => g.Id == Game?.Id);
            }
            catch
            {
                IsOwned = false;
            }
        }

        [RelayCommand]
        private async Task PurchaseAsync()
        {
            if (Game == null) return;

            StatusMessage = "Achat en cours...";

            try
            {
                // Se connecter si nécessaire
                if (string.IsNullOrEmpty(_networkService.Token))
                {
                    StatusMessage = "Connexion requise pour acheter";
                    return;
                }

                bool local_success = await _networkService.PurchaseGameAsync(Game.Id);

                if (local_success)
                {
                    IsOwned = true;
                    StatusMessage = "✅ Jeu acheté avec succès !";
                }
                else
                {
                    StatusMessage = "❌ Échec de l'achat";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DownloadAsync()
        {
            if (Game == null) return;

            StatusMessage = "Téléchargement en cours...";

            try
            {
                string local_savePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Gauniv",
                    "Games",
                    $"{Game.Title}.bin"
                );

                bool local_success = await _networkService.DownloadGameAsync(Game.Id, local_savePath);

                if (local_success)
                {
                    StatusMessage = $"✅ Téléchargé dans: {local_savePath}";
                }
                else
                {
                    StatusMessage = "❌ Échec du téléchargement";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("///games");
        }
    }
}
