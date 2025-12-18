using System.Globalization;

namespace Gauniv.Client.Converters
{
    public class CategoryColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string category)
            {
                // Couleur différente selon si c'est la catégorie sélectionnée
                // Pour l'instant, on retourne une couleur Steam par défaut
                return Color.FromArgb("#5c7e10");
            }
            return Color.FromArgb("#3c4e64");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
