using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class MyGamesViewModel: ObservableObject
    {
        private readonly NetworkService _networkService;
        private bool _hasLoadedOnce = false;
        private int _currentOffset = 0;
        private const int PageSize = 30;
        private int _totalCount = 0;
        private bool _isLoadingMore = false;

        // Collection observable pour afficher les jeux dans l'UI
        [ObservableProperty]
        private ObservableCollection<Dtos.GameDto> games = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = "";

        public MyGamesViewModel()
        {
            _networkService = NetworkService.Instance;
            
            // Écouter l'événement de connexion
            _networkService.OnConnected += OnUserConnected;
            
            // Écouter l'événement d'achat de jeu
            _networkService.OnGamePurchased += OnGamePurchased;
            
            // Charger les jeux si déjà connecté
            if (!string.IsNullOrEmpty(_networkService.Token))
            {
                _ = LoadMyGamesAsync();
            }
            else
            {
                StatusMessage = "Connectez-vous pour voir vos jeux";
            }
        }

        private void OnUserConnected()
        {
            // Charger seulement si pas déjà en cours de chargement et pas déjà chargé
            if (!IsLoading && !_hasLoadedOnce)
            {
                _ = LoadMyGamesAsync();
            }
        }

        private void OnGamePurchased()
        {
            // Recharger la bibliothèque après un achat
            _hasLoadedOnce = false; // Réinitialiser pour permettre le rechargement
            if (!IsLoading)
            {
                _ = LoadMyGamesAsync();
            }
        }

        [RelayCommand]
        private async Task LoadMyGamesAsync()
        {
            IsLoading = true;
            StatusMessage = "Connexion en cours...";

            try
            {
                // Vérifier si on est connecté
                if (string.IsNullOrEmpty(_networkService.Token))
                {
                    StatusMessage = "Veuillez vous connecter d'abord";
                    return;
                }

                StatusMessage = "Chargement de vos jeux...";

                // Récupérer MES jeux
                var local_result = await _networkService.GetMyGamesAsync(offset: 0, limit: 20);

                Games.Clear();
                foreach (var local_game in local_result.Items)
                {
                    Games.Add(local_game);
                }

                StatusMessage = $"Vous possédez {local_result.TotalCount} jeu(x)";
                _hasLoadedOnce = true;
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

        [RelayCommand]
        private async Task DownloadGameAsync(Dtos.GameDto game)
        {
            if (game == null) return;

            StatusMessage = $"Téléchargement de {game.Title}...";

            try
            {
                // Vérifier si on est connecté
                if (string.IsNullOrEmpty(_networkService.Token))
                {
                    StatusMessage = "Veuillez vous connecter pour télécharger";
                    return;
                }

                // Télécharger le jeu
                string local_savePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Gauniv",
                    "Games",
                    $"{game.Title}.bin"
                );

                bool local_success = await _networkService.DownloadGameAsync(game.Id, local_savePath);

                if (local_success)
                {
                    StatusMessage = $"{game.Title} téléchargé avec succès !";
                }
                else
                {
                    StatusMessage = "Échec du téléchargement";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
            }
        }
    }
}
