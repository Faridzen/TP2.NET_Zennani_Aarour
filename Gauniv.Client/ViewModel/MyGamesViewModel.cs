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

        [ObservableProperty]
        private ObservableCollection<Dtos.GameDto> games = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = "Connectez-vous pour voir vos jeux";

        public MyGamesViewModel()
        {
            _networkService = NetworkService.Instance;
        }

        [RelayCommand]
        private async Task LoadMyGamesAsync()
        {
            IsLoading = true;
            StatusMessage = "Connexion en cours...";

            try
            {
                // Se connecter d'abord
                bool local_loginSuccess = await _networkService.LoginAsync("test@test.com", "password");

                if (!local_loginSuccess)
                {
                    StatusMessage = "Échec de la connexion";
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
                    await _networkService.LoginAsync("test@test.com", "password");
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
