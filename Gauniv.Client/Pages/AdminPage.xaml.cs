using Gauniv.Client.ViewModel;

namespace Gauniv.Client.Pages;

public partial class AdminPage : ContentPage
{
	public AdminPage()
	{
		InitializeComponent();
		BindingContext = new AdminViewModel();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AdminViewModel viewModel)
        {
            await viewModel.LoadDataAsync();
        }
    }
}
