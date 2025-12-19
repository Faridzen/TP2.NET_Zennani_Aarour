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

        [ObservableProperty]
        private bool isDownloaded = false;

        [ObservableProperty]
        private bool isRunning = false;

        private Process? _gameProcess = null;

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
                    
                    // Vérifier si le jeu est téléchargé
                    CheckIfDownloaded();
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

        private void CheckIfDownloaded()
        {
            if (Game == null) return;

            string local_savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Gauniv",
                "Games",
                $"{Game.Title}.exe"
            );

            IsDownloaded = File.Exists(local_savePath);
            if (IsDownloaded && Game != null)
            {
                Game.LocalPath = local_savePath;
                Game.IsDownloaded = true;
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
                    
                    // Recharger pour être sûr
                    await CheckOwnershipAsync();

                    // Naviguer vers la page de succès
                    var local_navParam = new Dictionary<string, object>
                    {
                        { "Game", Game }
                    };
                    await Shell.Current.GoToAsync("///purchasesuccess", local_navParam);
                }
                else
                {
                    StatusMessage = "❌ Échec de l'achat. Veuillez vérifier votre solde ou votre connexion.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur d'achat: {ex.Message}";
                Debug.WriteLine($"Purchase error: {ex}");
            }
        }

        [ObservableProperty]
        private double downloadProgress = 0;

        [ObservableProperty]
        private bool isDownloading = false;

        [RelayCommand]
        private async Task DownloadAsync()
        {
            if (Game == null) return;

            StatusMessage = "Téléchargement en cours...";
            IsDownloading = true;
            DownloadProgress = 0;

            try
            {
                string local_savePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Gauniv",
                    "Games",
                    $"{Game.Title}.exe"
                );

                // Créer le dossier si nécessaire
                Directory.CreateDirectory(Path.GetDirectoryName(local_savePath)!);

                var local_progress = new Progress<double>(p => DownloadProgress = p);
                bool local_success = await _networkService.DownloadGameAsync(Game.Id, local_savePath, local_progress);

                if (local_success)
                {
                    Game.LocalPath = local_savePath;
                    Game.IsDownloaded = true;
                    IsDownloaded = true;
                    StatusMessage = $"✅ Téléchargé avec succès";
                }
                else
                {
                    StatusMessage = "❌ Échec du téléchargement. Le serveur est peut-être indisponible.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur de téléchargement: {ex.Message}";
                Debug.WriteLine($"Download error: {ex}");
            }
            finally
            {
                IsDownloading = false;
            }
        }

        [RelayCommand]
        private async Task PlayAsync()
        {
            if (Game == null || string.IsNullOrEmpty(Game.LocalPath)) return;

            try
            {
                if (IsRunning)
                {
                    StatusMessage = "🛑 Arrêt du jeu...";
                    try 
                    {
                        if (_gameProcess != null && !_gameProcess.HasExited)
                        {
                            _gameProcess.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error killing process: {ex.Message}");
                    }
                    
                    IsRunning = false;
                    StatusMessage = "✅ Jeu arrêté de force";
                    return;
                }

                StatusMessage = "🎮 Lancement du jeu...";
                IsRunning = true;

                // Lancer l'exécutable
                _gameProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Game.LocalPath,
                        UseShellExecute = true
                    },
                    EnableRaisingEvents = true
                };

                _gameProcess.Exited += (sender, args) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (IsRunning) // Si on n'a pas arrêté de force
                        {
                            IsRunning = false;
                            StatusMessage = "✅ Le jeu s'est terminé";
                        }
                        _gameProcess?.Dispose();
                        _gameProcess = null;
                    });
                };

                _gameProcess.Start();
                StatusMessage = "✅ Jeu lancé !";
            }
            catch (Exception ex)
            {
                IsRunning = false;
                StatusMessage = $"❌ Erreur: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (Game == null || string.IsNullOrEmpty(Game.LocalPath)) return;

            try
            {
                if (IsRunning)
                {
                    StatusMessage = "⚠️ Impossible de supprimer: le jeu est en cours d'exécution";
                    return;
                }

                bool local_confirm = await Application.Current!.MainPage!.DisplayAlert(
                    "Confirmation",
                    $"Voulez-vous vraiment supprimer {Game.Title} ?",
                    "Oui",
                    "Non"
                );

                if (!local_confirm) return;

                StatusMessage = "Suppression en cours...";

                if (File.Exists(Game.LocalPath))
                {
                    File.Delete(Game.LocalPath);
                    Game.LocalPath = null;
                    Game.IsDownloaded = false;
                    IsDownloaded = false;
                    StatusMessage = "✅ Jeu supprimé avec succès";
                }
                else
                {
                    StatusMessage = "⚠️ Fichier introuvable";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur lors de la suppression: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("///games");
        }
    }
}
