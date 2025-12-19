using Gauniv.Client.ViewModel;

namespace Gauniv.Client.Pages;

public partial class MyFriends : ContentPage
{
	public MyFriends()
	{
		InitializeComponent();
		BindingContext = new MyFriendsViewModel();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MyFriendsViewModel vm)
        {
            await vm.LoadFriendsCommand.ExecuteAsync(null);
        }
    }
}
