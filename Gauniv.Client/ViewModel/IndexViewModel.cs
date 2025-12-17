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
   
    public partial class IndexViewModel: ObservableObject
    {
        private readonly NetworkService _networkService;

        // Collection observable pour afficher les jeux dans l'UI
        [ObservableProperty]
        private ObservableCollection<Dtos.GameDto> games = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string statusMessage = "Cliquez sur 'Charger les jeux' pour commencer";

        public IndexViewModel()
        {
            _networkService = NetworkService.Instance;
        }

       
        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            IsLoading = true;
            StatusMessage = "Chargement des jeux...";

            try
            {
                // Récupérer les jeux depuis l'API
                var local_result = await _networkService.GetGamesAsync(offset: 0, limit: 20);

                // Mettre à jour la collection
                Games.Clear();
                foreach (var local_game in local_result.Items)
                {
                    Games.Add(local_game);
                }

                StatusMessage = $"{local_result.TotalCount} jeu(x) chargé(s) avec succès !";
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
        private async Task LoadMyGamesAsync()
        {
            IsLoading = true;
            StatusMessage = "Connexion en cours...";

            try
            {
                // D'abord se connecter
                bool local_loginSuccess = await _networkService.LoginAsync("test@test.com", "password");

                if (!local_loginSuccess)
                {
                    StatusMessage = "Échec de la connexion";
                    return;
                }

                StatusMessage = "Connecté ! Chargement de vos jeux...";

                // Ensuite récupérer MES jeux
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
        private async Task PurchaseGameAsync(Dtos.GameDto game)
        {
            if (game == null) return;

            StatusMessage = $"Achat de {game.Title}...";

            try
            {
                // Vérifier si on est connecté
                if (string.IsNullOrEmpty(_networkService.Token))
                {
                    // Se connecter d'abord
                    await _networkService.LoginAsync("test@test.com", "password");
                }

                // Acheter le jeu
                bool local_success = await _networkService.PurchaseGameAsync(game.Id);

                if (local_success)
                {
                    StatusMessage = $"{game.Title} acheté avec succès !";
                }
                else
                {
                    StatusMessage = "Échec de l'achat";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
            }
        }
    }
}
