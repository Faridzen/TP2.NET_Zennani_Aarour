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
        private bool isLoading = false;

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
            
            _ = LoadInitialDataAsync();
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

        private async Task LoadInitialDataAsync()
        {
            await LoadCategoriesAsync();
            await LoadGamesAsync();
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
        private async Task LoadGamesAsync()
        {
            IsLoading = true;
            StatusMessage = "Chargement des jeux...";
            _currentOffset = 0;

            try
            {
                var local_result = await _networkService.GetGamesAsync(offset: 0, limit: PageSize);
                _totalCount = local_result.TotalCount;

                _allGames.Clear();
                foreach (var local_game in local_result.Items)
                {
                    local_game.IsOwned = _ownedGameIds.Contains(local_game.Id);
                    _allGames.Add(local_game);
                }

                _currentOffset = local_result.Items.Count;
                ApplyFilters();
                StatusMessage = $"{_totalCount} jeu(x) disponible(s)";
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
        private async Task LoadMoreGamesAsync()
        {
            if (_isLoadingMore || _currentOffset >= _totalCount)
                return;

            _isLoadingMore = true;

            try
            {
                var local_result = await _networkService.GetGamesAsync(offset: _currentOffset, limit: PageSize);

                foreach (var local_game in local_result.Items)
                {
                    local_game.IsOwned = _ownedGameIds.Contains(local_game.Id);
                    _allGames.Add(local_game);
                    
                    // Appliquer les filtres au nouveau jeu avant de l'ajouter
                    bool local_passesFilters = true;
                    
                    // Filtre par catégorie (multi-sélection)
                    var local_selectedCategories = CategoryItems.Where(c => c.IsSelected).Select(c => c.Name).ToList();
                    if (local_selectedCategories.Any())
                    {
                        local_passesFilters = local_game.Categories != null && 
                            local_game.Categories.Any(c => local_selectedCategories.Contains(c, StringComparer.OrdinalIgnoreCase));
                    }
                    
                    // Filtre par prix
                    if (local_passesFilters)
                    {
                        local_passesFilters = local_game.Price >= MinPrice && local_game.Price <= MaxPrice;
                    }
                    
                    // Filtre par recherche
                    if (local_passesFilters && !string.IsNullOrWhiteSpace(SearchText))
                    {
                        local_passesFilters = local_game.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            local_game.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                    }
                    
                    // Ajouter seulement si passe tous les filtres
                    if (local_passesFilters)
                    {
                        Games.Add(local_game);
                    }
                }

                _currentOffset += local_result.Items.Count;
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

        private void ApplyFilters()
        {
            var local_filtered = _allGames.AsEnumerable();

            // Filtre par catégorie (multi-sélection)
            var local_selectedCategories = CategoryItems.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            if (local_selectedCategories.Any())
            {
                local_filtered = local_filtered.Where(g => 
                    g.Categories != null && g.Categories.Any(c => local_selectedCategories.Contains(c, StringComparer.OrdinalIgnoreCase)));
            }

            // Filtre par prix
            local_filtered = local_filtered.Where(g => g.Price >= MinPrice && g.Price <= MaxPrice);

            // Filtre par recherche
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                local_filtered = local_filtered.Where(g => 
                    g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    g.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Games.Clear();
            foreach (var local_game in local_filtered)
            {
                Games.Add(local_game);
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
                    await Shell.Current.GoToAsync("///purchasesuccess", local_navParam);
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
                await Shell.Current.GoToAsync($"///gamedetails?gameId={game.Id}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
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
