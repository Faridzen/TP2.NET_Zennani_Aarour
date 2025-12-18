using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Dtos;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    [QueryProperty(nameof(Game), "Game")]
    public partial class PurchaseSuccessViewModel : ObservableObject
    {
        [ObservableProperty]
        private GameDto? game;

        [RelayCommand]
        private async Task GoToLibraryAsync()
        {
            await Shell.Current.GoToAsync("///mygames");
        }

        [RelayCommand]
        private async Task ContinueShoppingAsync()
        {
            await Shell.Current.GoToAsync("///games");
        }
    }
}
