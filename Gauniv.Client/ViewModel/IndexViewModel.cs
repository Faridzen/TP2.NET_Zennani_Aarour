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
        private ObservableCollection<Dtos.GameDto> _allGames = new();
        private HashSet<int> _ownedGameIds = new();
        private int _currentOffset = 0;
        private const int PageSize = 30;
        private int _totalCount = 0;
        private bool _isLoadingMore = false;

        // Collection observable pour afficher les jeux dans l'UI
        [ObservableProperty]
        private ObservableCollection<Dtos.GameDto> games = new();

        [ObservableProperty]
        private ObservableCollection<CategoryItem> categoryItems = new();

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private string? selectedCategory;

        [ObservableProperty]
        private decimal minPrice = 0;

        [ObservableProperty]
        private decimal maxPrice = 100;

        // Properties pour les sliders (double requis par MAUI)
        [ObservableProperty]
        private double minPriceSlider = 0;

        [ObservableProperty]
        private double maxPriceSlider = 100;

        [ObservableProperty]
        private bool isConnected = false;

        [ObservableProperty]
        private bool isNoGamesFound = false;

        private void UpdateNoGamesFound()
        {
            IsNoGamesFound = !IsLoading && (Games == null || Games.Count == 0);
        }

        [ObservableProperty]
        private bool isLoading = false;

        partial void OnIsLoadingChanged(bool value)
        {
            UpdateNoGamesFound();
        }

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private bool isAdmin = false;

        public bool IsNotAdmin => !IsAdmin;

        public IndexViewModel()
        {
            _networkService = NetworkService.Instance;
            
            // Écouter les événements de connexion/déconnexion
            _networkService.OnConnected += UpdateConnectionStatus;
            _networkService.OnDisconnected += UpdateConnectionStatus;
            _networkService.OnGamePurchased += OnGamePurchased;
            
            // Vérifier l'état initial
            UpdateConnectionStatus();
            UpdateAdminStatus();
            UpdateNoGamesFound();
        }

        private void UpdateAdminStatus()
        {
            IsAdmin = _networkService.IsAdmin;
            OnPropertyChanged(nameof(IsNotAdmin));
        }

        private void OnGamePurchased()
        {
            // Recharger les jeux possédés après un achat
            _ = LoadOwnedGamesAsync();
        }

        private void UpdateConnectionStatus()
        {
            IsConnected = !string.IsNullOrEmpty(_networkService.Token);
            UpdateAdminStatus();
            if (IsConnected)
            {
                _ = LoadOwnedGamesAsync();
            }
            else
            {
                _ownedGameIds.Clear();
            }
        }



       
        private async Task LoadCategoriesAsync()
        {
            try
            {
                Console.WriteLine("LoadCategoriesAsync: Starting...");
                var local_cats = await _networkService.GetCategoriesAsync();
                Console.WriteLine($"LoadCategoriesAsync: Received {local_cats.Count} categories");
                
                CategoryItems.Clear();
                foreach (var local_cat in local_cats)
                {
                    var local_item = new CategoryItem { Name = local_cat.Name };
                    local_item.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(CategoryItem.IsSelected))
                        {
                            ApplyFilters();
                        }
                    };
                    CategoryItems.Add(local_item);
                }
                Console.WriteLine($"LoadCategoriesAsync: Total categories in list: {CategoryItems.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadCategoriesAsync ERROR: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadInitialDataAsync()
        {
            if (IsLoading) return;
            
            try 
            {
                IsLoading = true;
                _allGames.Clear();
                Games.Clear();
                _currentOffset = 0;

                await LoadCategoriesAsync();
                await LoadGamesAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadGamesAsync()
        {
            try
            {
                var local_selectedCategories = CategoryItems.Where(c => c.IsSelected).Select(c => c.Name).ToArray();
                var local_result = await _networkService.GetGamesAsync(
                    offset: _currentOffset, 
                    limit: PageSize, 
                    categories: local_selectedCategories,
                    search: SearchText);

                if (local_result != null)
                {
                    _totalCount = local_result.TotalCount;
                    
                    foreach (var local_game in local_result.Items)
                    {
                        local_game.IsOwned = _ownedGameIds.Contains(local_game.Id);
                        
                        // Apply client-side price filter
                        if (local_game.Price >= MinPrice && local_game.Price <= MaxPrice)
                        {
                            _allGames.Add(local_game);
                            Games.Add(local_game);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadGames error: {ex.Message}");
                StatusMessage = "Erreur lors du chargement des jeux";
            }
        }

        [RelayCommand]
        private async Task LoadMoreGamesAsync()
        {
            if (_isLoadingMore || IsLoading || _currentOffset >= _totalCount)
                return;

            _isLoadingMore = true;
            StatusMessage = "Chargement de la suite...";

            try
            {
                var local_selectedCategories = CategoryItems.Where(c => c.IsSelected).Select(c => c.Name).ToArray();
                var local_result = await _networkService.GetGamesAsync(
                    offset: _currentOffset, 
                    limit: PageSize,
                    categories: local_selectedCategories,
                    search: SearchText);

                if (local_result != null)
                {
                    foreach (var local_game in local_result.Items)
                    {
                        local_game.IsOwned = _ownedGameIds.Contains(local_game.Id);
                        
                        // Apply client-side price filter
                        if (local_game.Price >= MinPrice && local_game.Price <= MaxPrice)
                        {
                            _allGames.Add(local_game);
                            Games.Add(local_game);
                        }
                    }

                    _currentOffset += local_result.Items.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading more games: {ex.Message}");
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedCategoryChanged(string? value)
        {
            ApplyFilters();
        }

        partial void OnMinPriceSliderChanged(double value)
        {
            MinPrice = (decimal)value;
            ApplyFilters();
        }

        partial void OnMaxPriceSliderChanged(double value)
        {
            MaxPrice = (decimal)value;
            ApplyFilters();
        }

        private async Task LoadOwnedGamesAsync()
        {
            if (string.IsNullOrEmpty(_networkService.Token))
                return;

            try
            {
                var local_ownedGames = await _networkService.GetMyGamesAsync(offset: 0, limit: 1000000);
                _ownedGameIds.Clear();
                foreach (var local_game in local_ownedGames.Items)
                {
                    _ownedGameIds.Add(local_game.Id);
                }
                
                // Mettre à jour le statut IsOwned pour tous les jeux déjà chargés
                foreach (var local_game in _allGames)
                {
                    local_game.IsOwned = _ownedGameIds.Contains(local_game.Id);
                }
                
                // Forcer la mise à jour de la liste filtrée pour rafraîchir l'UI
                ApplyFilters();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading owned games: {ex.Message}");
            }
        }

        private CancellationTokenSource? _searchCts;

        private async void ApplyFilters()
        {
            // Annuler la recherche précédente si elle est encore en cours (debounce)
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var local_token = _searchCts.Token;

            try
            {
                // Attendre un peu pour éviter de surcharger le serveur si l'utilisateur tape vite
                await Task.Delay(500, local_token);

                if (local_token.IsCancellationRequested) return;

                _currentOffset = 0;
                _allGames.Clear();
                Games.Clear();
                
                IsLoading = true;
                await LoadGamesAsync();
            }
            catch (TaskCanceledException)
            {
                // Normal si on tape vite
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyFilters error: {ex.Message}");
            }
            finally
            {
                if (!local_token.IsCancellationRequested)
                    IsLoading = false;
            }
        }

        private bool PassesFilters(Dtos.GameDto game)
        {
            if (game == null) return false;

            // Filtre par catégorie (multi-sélection)
            var local_selectedCategories = CategoryItems.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            if (local_selectedCategories.Any())
            {
                bool local_hasCategory = game.Categories != null && 
                    game.Categories.Any(c => local_selectedCategories.Contains(c, StringComparer.OrdinalIgnoreCase));
                
                if (!local_hasCategory) return false;
            }

            // Filtre par prix
            if (game.Price < MinPrice || game.Price > MaxPrice)
                return false;

            // Filtre par recherche
            var local_searchText = SearchText?.Trim();
            if (!string.IsNullOrWhiteSpace(local_searchText))
            {
                bool local_titleMatch = game.Title != null && game.Title.Contains(local_searchText, StringComparison.OrdinalIgnoreCase);
                bool local_descMatch = game.Description != null && game.Description.Contains(local_searchText, StringComparison.OrdinalIgnoreCase);
                
                if (!local_titleMatch && !local_descMatch)
                    return false;
            }

            return true;
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
                    StatusMessage = "Veuillez vous connecter pour acheter";
                    return;
                }

                // Acheter le jeu
                bool local_success = await _networkService.PurchaseGameAsync(game.Id);

                if (local_success)
                {
                    StatusMessage = $"{game.Title} acheté avec succès !";
                    
                    // Naviguer vers la page de succès
                    var local_navParam = new Dictionary<string, object>
                    {
                        { "Game", game }
                    };
                    await Shell.Current.GoToAsync("purchasesuccess", local_navParam);
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

        [RelayCommand]
        private async Task ViewGameDetailsAsync(Dtos.GameDto game)
        {
            if (game == null) return;

            try
            {
                // Navigation relative (enregistrée dans AppShell.xaml.cs)
                await Shell.Current.GoToAsync($"gamedetails?gameId={game.Id}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur navigation: {ex.Message}";
                Debug.WriteLine($"Navigation error: {ex}");
            }
        }

        public bool IsGameOwned(int gameId)
        {
            return _ownedGameIds.Contains(gameId);
        }
    }

    public partial class CategoryItem : ObservableObject
    {
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private bool isSelected = false;
    }
}
