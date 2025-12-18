#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Websocket;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text;

namespace Gauniv.WebServer.Services
{
    public class SetupService : IHostedService
    {
        private ApplicationDbContext? applicationDbContext;
        private readonly IServiceProvider serviceProvider;
        private Task? task;

        public SetupService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceProvider.CreateScope()) // this will use `IServiceScopeFactory` internally
            {
                applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var userSignInManager = scope.ServiceProvider.GetService<UserManager<User>>();
                var signInManager = scope.ServiceProvider.GetService<SignInManager<User>>();

                if (applicationDbContext is null)
                {
                    throw new Exception("ApplicationDbContext is null");
                }

                // Create Admin account
                if (userSignInManager?.FindByNameAsync("admin").Result == null)
                {
                    var local_adminUser = new User()
                    {
                        UserName = "admin",
                        Email = "admin",
                        EmailConfirmed = true,
                        IsAdmin = true,
                        FirstName = "Admin",
                        LastName = "Admin"
                    };
                    var local_res = userSignInManager.CreateAsync(local_adminUser, "admin").Result;
                }

                // Create User account
                if (userSignInManager?.FindByNameAsync("user").Result == null)
                {
                    var local_normalUser = new User()
                    {
                        UserName = "user",
                        Email = "user",
                        EmailConfirmed = true,
                        IsAdmin = false,
                        FirstName = "User",
                        LastName = "User"
                    };
                    var local_res = userSignInManager.CreateAsync(local_normalUser, "user").Result;
                }

                // Add sample games for testing
                if (!applicationDbContext.Games.Any())
                {
                    var local_sampleGames = new List<Game>();
                    var local_csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "games.csv");
                    
                    if (File.Exists(local_csvPath))
                    {
                        var local_lines = File.ReadAllLines(local_csvPath);
                        // Skip header line
                        for (int i = 1; i < local_lines.Length; i++)
                        {
                            var local_parts = local_lines[i].Split(',');
                            if (local_parts.Length >= 5)
                            {
                                var local_title = local_parts[0];
                                var local_description = local_parts[1];
                                var local_price = decimal.Parse(local_parts[2]);
                                var local_categories = local_parts[3].Trim('"').Split(',').Select(c => c.Trim()).ToList();
                                var local_imageUrl = local_parts[4];
                                
                                local_sampleGames.Add(new Game
                                {
                                    Title = local_title,
                                    Description = local_description,
                                    Price = local_price,
                                    Categories = local_categories,
                                    ImageUrl = local_imageUrl,
                                    payload = Encoding.UTF8.GetBytes($"Sample game binary for {local_title}")
                                });
                            }
                        }
                    }
                    
                    applicationDbContext.Games.AddRange(local_sampleGames);

                    // Populate Categories table from the unique categories found in games
                    var local_uniqueCategories = local_sampleGames
                        .SelectMany(g => g.Categories)
                        .Distinct()
                        .Select(name => new Category { Name = name })
                        .ToList();
                    
                    applicationDbContext.Categories.AddRange(local_uniqueCategories);
                }

                applicationDbContext.SaveChanges();

                return Task.CompletedTask;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
