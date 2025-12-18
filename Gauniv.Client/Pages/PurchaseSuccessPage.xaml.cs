using Gauniv.Client.ViewModel;

namespace Gauniv.Client.Pages;

public partial class PurchaseSuccessPage : ContentPage
{
	public PurchaseSuccessPage(PurchaseSuccessViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
