using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Models
{
    public class CatalogViewModel
    {
        public List<Game> Games { get; set; } = new();
        public List<string> AllCategories { get; set; } = new();
        public string Search { get; set; } = "";
        public decimal? MaxPrice { get; set; }
        public List<string> SelectedCategories { get; set; } = new();
        public bool IsLibrary { get; set; } = false;
    }
}
