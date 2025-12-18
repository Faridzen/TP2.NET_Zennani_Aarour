using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Dtos;
using Gauniv.Client.Services;
using System.Collections.ObjectModel;

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

        // Form fields for Category
        [ObservableProperty]
        private string categoryName = "";

        public AdminViewModel()
        {
            _networkService = NetworkService.Instance;
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
                var local_game = new GameDto
                {
                    Id = SelectedGame?.Id ?? 0,
                    Title = GameTitle,
                    Description = GameDescription,
                    Price = GamePrice,
                    ImageUrl = GameImageUrl,
                    Categories = GameCategoriesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(c => c.Trim())
                                                 .ToList()
                };

                bool local_success;
                if (local_game.Id == 0)
                    local_success = await _networkService.AddGameAsync(local_game);
                else
                    local_success = await _networkService.UpdateGameAsync(local_game);

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
