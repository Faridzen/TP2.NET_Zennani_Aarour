using System.Globalization;

namespace Gauniv.Client.Converters
{
    public class GameOwnedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int gameId && parameter is ViewModel.IndexViewModel viewModel)
            {
                return viewModel.IsGameOwned(gameId);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
