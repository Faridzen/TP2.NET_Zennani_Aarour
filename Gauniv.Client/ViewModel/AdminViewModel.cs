using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Dtos;
using Gauniv.Client.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;

namespace Gauniv.Client.ViewModel
{
    public partial class AdminViewModel : ObservableObject
    {
        private readonly NetworkService _networkService;

        [ObservableProperty]
        private ObservableCollection<GameDto> games = new();

        [ObservableProperty]
        private ObservableCollection<CategoryDto> categories = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = "";

        // Selection for editing
        [ObservableProperty]
        private GameDto? selectedGame;

        [ObservableProperty]
        private CategoryDto? selectedCategory;

        // Form fields for Game
        [ObservableProperty]
        private string gameTitle = "";

        [ObservableProperty]
        private string gameDescription = "";

        [ObservableProperty]
        private decimal gamePrice;

        [ObservableProperty]
        private string gameImageUrl = "";

        [ObservableProperty]
        private string gameCategoriesCsv = ""; // Comma separated categories for the game

        [ObservableProperty]
        private string categoryName = "";

        // Executable upload
        [ObservableProperty]
        private string executablePath = "";

        [ObservableProperty]
        private string executableFileName = "Aucun fichier sélectionné";

        [ObservableProperty]
        private ObservableCollection<decimal> availablePrices = new();

        [ObservableProperty]
        private CategoryDto? selectedCategoryToAdd;

        public bool HasSelectedExecutable => !string.IsNullOrEmpty(ExecutablePath);

        public AdminViewModel()
        {
            _networkService = NetworkService.Instance;
            InitializePrices();
        }

        private void InitializePrices()
        {
            AvailablePrices = new ObservableCollection<decimal>
            {
                0m, 4.99m, 9.99m, 14.99m, 19.99m, 29.99m, 39.99m, 49.99m, 59.99m, 69.99m
            };
        }

        [RelayCommand]
        private void AddCategoryToGame()
        {
            if (SelectedCategoryToAdd == null) return;

            var local_catName = SelectedCategoryToAdd.Name;
            var local_currentCats = GameCategoriesCsv?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(c => c.Trim())
                                                     .ToList() ?? new List<string>();

            if (!local_currentCats.Contains(local_catName, StringComparer.OrdinalIgnoreCase))
            {
                local_currentCats.Add(local_catName);
                GameCategoriesCsv = string.Join(", ", local_currentCats);
            }
            
            SelectedCategoryToAdd = null; // Reset selection
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var local_gamesResult = await _networkService.GetGamesAsync(0, 100);
                Games = new ObservableCollection<GameDto>(local_gamesResult.Items);

                var local_categoriesResult = await _networkService.GetCategoriesAsync();
                Categories = new ObservableCollection<CategoryDto>(local_categoriesResult);
                
                StatusMessage = "Données chargées";
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

        #region Game Commands

        [RelayCommand]
        private void PrepareNewGame()
        {
            SelectedGame = null;
            GameTitle = "";
            GameDescription = "";
            GamePrice = 0;
            GameImageUrl = "";
            GameCategoriesCsv = "";
            ExecutablePath = "";
            ExecutableFileName = "Aucun fichier sélectionné";
            OnPropertyChanged(nameof(HasSelectedExecutable));
        }

        [RelayCommand]
        private async Task PickExecutableAsync()
        {
            try
            {
                var local_result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Sélectionnez l'exécutable du jeu (.exe)",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".exe" } }
                    })
                });

                if (local_result != null)
                {
                    ExecutablePath = local_result.FullPath;
                    ExecutableFileName = local_result.FileName;
                    OnPropertyChanged(nameof(HasSelectedExecutable));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors de la sélection: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SelectGame(GameDto game)
        {
            SelectedGame = game;
            GameTitle = game.Title;
            GameDescription = game.Description;
            GamePrice = game.Price;
            GameImageUrl = game.ImageUrl ?? "";
            GameCategoriesCsv = string.Join(", ", game.Categories);
        }

        [RelayCommand]
        private async Task SaveGameAsync()
        {
            if (string.IsNullOrWhiteSpace(GameTitle)) return;

            IsLoading = true;
            try
            {
                bool local_success;
                if (SelectedGame == null)
                {
                    // Upload new game with optional executable
                    local_success = await _networkService.UploadGameAsync(
                        GameTitle, 
                        GameDescription, 
                        GamePrice, 
                        GameCategoriesCsv, 
                        ExecutablePath);
                }
                else
                {
                    // Update existing game (no executable update in this view yet)
                    var local_game = new GameDto
                    {
                        Id = SelectedGame.Id,
                        Title = GameTitle,
                        Description = GameDescription,
                        Price = GamePrice,
                        ImageUrl = GameImageUrl,
                        Categories = GameCategoriesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                     .Select(c => c.Trim())
                                                     .ToList()
                    };
                    local_success = await _networkService.UpdateGameAsync(local_game);
                }

                if (local_success)
                {
                    StatusMessage = "Jeu enregistré avec succès";
                    await LoadDataAsync();
                    PrepareNewGame();
                }
                else
                {
                    StatusMessage = "Échec de l'enregistrement du jeu";
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

        [RelayCommand]
        private async Task DeleteGameAsync(GameDto game)
        {
            if (game == null) return;

            bool local_confirm = await Shell.Current.DisplayAlert("Confirmation", $"Supprimer {game.Title} ?", "Oui", "Non");
            if (!local_confirm) return;

            IsLoading = true;
            try
            {
                if (await _networkService.DeleteGameAsync(game.Id))
                {
                    StatusMessage = "Jeu supprimé";
                    await LoadDataAsync();
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

        #endregion

        #region Category Commands

        [RelayCommand]
        private void PrepareNewCategory()
        {
            SelectedCategory = null;
            CategoryName = "";
        }

        [RelayCommand]
        private void SelectCategory(CategoryDto category)
        {
            SelectedCategory = category;
            CategoryName = category.Name;
        }

        [RelayCommand]
        private async Task SaveCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(CategoryName)) return;

            IsLoading = true;
            try
            {
                bool local_success;
                if (SelectedCategory == null)
                    local_success = await _networkService.AddCategoryAsync(CategoryName);
                else
                    local_success = await _networkService.UpdateCategoryAsync(SelectedCategory.Id, CategoryName);

                if (local_success)
                {
                    StatusMessage = "Catégorie enregistrée";
                    await LoadDataAsync();
                    PrepareNewCategory();
                }
                else
                {
                    StatusMessage = "Échec de l'enregistrement";
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

        [RelayCommand]
        private async Task DeleteCategoryAsync(CategoryDto category)
        {
            if (category == null) return;

            bool local_confirm = await Shell.Current.DisplayAlert("Confirmation", $"Supprimer {category.Name} ?", "Oui", "Non");
            if (!local_confirm) return;

            IsLoading = true;
            try
            {
                if (await _networkService.DeleteCategoryAsync(category.Id))
                {
                    StatusMessage = "Catégorie supprimée";
                    await LoadDataAsync();
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

        #endregion
    }
}
