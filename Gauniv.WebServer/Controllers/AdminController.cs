using Gauniv.WebServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var local_games = await _db.Games.OrderBy(g => g.Title).ToListAsync();
            var local_categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            
            ViewBag.Categories = local_categories;
            return View(local_games);
        }

        [HttpGet]
        public async Task<IActionResult> CreateGame()
        {
            ViewBag.AvailableCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGame(Game game, string categoriesCsv, IFormFile? executable)
        {
            try
            {
                if (executable != null && executable.Length > 0)
                {
                    using var local_ms = new MemoryStream();
                    await executable.CopyToAsync(local_ms);
                    game.payload = local_ms.ToArray();
                }

                game.Categories = string.IsNullOrWhiteSpace(categoriesCsv) 
                    ? new List<string>() 
                    : categoriesCsv.Split(',').Select(c => c.Trim()).ToList();

                _db.Games.Add(game);
                await _db.SaveChangesAsync();
                
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ViewBag.AvailableCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
                return View(game);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var local_game = await _db.Games.FindAsync(id);
            if (local_game != null)
            {
                _db.Games.Remove(local_game);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!await _db.Categories.AnyAsync(c => c.Name == name))
                {
                    _db.Categories.Add(new Category { Name = name });
                    await _db.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var local_cat = await _db.Categories.FindAsync(id);
            if (local_cat != null)
            {
                // Note: Reusing logic from Api.AdminController logic to sync games would be better 
                // but let's keep it simple for now or implement if needed.
                _db.Categories.Remove(local_cat);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
