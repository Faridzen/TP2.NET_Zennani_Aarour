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
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gauniv.Client.Dtos;

namespace Gauniv.Client.Services
{
    internal partial class NetworkService : ObservableObject
    {

        public static NetworkService Instance { get; private set; } = new NetworkService();
        
        [ObservableProperty]
        private string token;
        
        public HttpClient httpClient;

        private const string BaseUrl = "http://localhost:5231/api/1.0.0/Games/";
        private const string AuthUrl = "http://localhost:5231/Bearer/login";

        public NetworkService() {
            httpClient = new HttpClient();
            Token = null;
        }

        public event Action OnConnected;

        /// <summary>
        /// Login to the server and retrieve authentication token
        /// </summary>
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var local_loginRequest = new LoginRequest
                {
                    Email = email,
                    Password = password
                };

                var local_json = JsonSerializer.Serialize(local_loginRequest);
                var local_content = new StringContent(local_json, Encoding.UTF8, "application/json");

                var local_response = await httpClient.PostAsync(AuthUrl, local_content);

                if (local_response.IsSuccessStatusCode)
                {
                    var local_responseContent = await local_response.Content.ReadAsStringAsync();
                    var local_tokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(local_responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (local_tokenResponse != null && !string.IsNullOrEmpty(local_tokenResponse.AccessToken))
                    {
                        Token = local_tokenResponse.AccessToken;
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                        OnConnected?.Invoke();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get paginated list of all games with optional category filter
        /// </summary>
        public async Task<PagedResultDto<GameDto>> GetGamesAsync(int offset = 0, int limit = 10, string[]? categories = null)
        {
            try
            {
                var local_queryParams = $"?offset={offset}&limit={limit}";
                
                if (categories != null && categories.Length > 0)
                {
                    foreach (var local_category in categories)
                    {
                        local_queryParams += $"&category={Uri.EscapeDataString(local_category)}";
                    }
                }

                var local_response = await httpClient.GetAsync($"{BaseUrl}List{local_queryParams}");

                if (local_response.IsSuccessStatusCode)
                {
                    var local_json = await local_response.Content.ReadAsStringAsync();
                    var local_result = JsonSerializer.Deserialize<PagedResultDto<GameDto>>(local_json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return local_result ?? new PagedResultDto<GameDto>();
                }

                return new PagedResultDto<GameDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetGames error: {ex.Message}");
                return new PagedResultDto<GameDto>();
            }
        }

        /// <summary>
        /// Get paginated list of games owned by the authenticated user
        /// </summary>
        public async Task<PagedResultDto<GameDto>> GetMyGamesAsync(int offset = 0, int limit = 10)
        {
            try
            {
                var local_queryParams = $"?offset={offset}&limit={limit}";
                var local_response = await httpClient.GetAsync($"{BaseUrl}MyGames{local_queryParams}");

                if (local_response.IsSuccessStatusCode)
                {
                    var local_json = await local_response.Content.ReadAsStringAsync();
                    var local_result = JsonSerializer.Deserialize<PagedResultDto<GameDto>>(local_json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return local_result ?? new PagedResultDto<GameDto>();
                }

                return new PagedResultDto<GameDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMyGames error: {ex.Message}");
                return new PagedResultDto<GameDto>();
            }
        }

        /// <summary>
        /// Get details of a specific game
        /// </summary>
        public async Task<GameDto?> GetGameDetailsAsync(int gameId)
        {
            try
            {
                var local_response = await httpClient.GetAsync($"{BaseUrl}Details/{gameId}");

                if (local_response.IsSuccessStatusCode)
                {
                    var local_json = await local_response.Content.ReadAsStringAsync();
                    var local_game = JsonSerializer.Deserialize<GameDto>(local_json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return local_game;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetGameDetails error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all available categories
        /// </summary>
        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var local_response = await httpClient.GetAsync($"{BaseUrl}Categories");

                if (local_response.IsSuccessStatusCode)
                {
                    var local_json = await local_response.Content.ReadAsStringAsync();
                    var local_categories = JsonSerializer.Deserialize<List<CategoryDto>>(local_json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return local_categories ?? new List<CategoryDto>();
                }

                return new List<CategoryDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCategories error: {ex.Message}");
                return new List<CategoryDto>();
            }
        }

        /// <summary>
        /// Purchase a game
        /// </summary>
        public async Task<bool> PurchaseGameAsync(int gameId)
        {
            try
            {
                var local_response = await httpClient.PostAsync($"{BaseUrl}Purchase/{gameId}", null);
                return local_response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PurchaseGame error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Download game binary to specified path
        /// </summary>
        public async Task<bool> DownloadGameAsync(int gameId, string savePath)
        {
            try
            {
                var local_response = await httpClient.GetAsync($"{BaseUrl}Download/{gameId}");

                if (local_response.IsSuccessStatusCode)
                {
                    var local_fileBytes = await local_response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(savePath, local_fileBytes);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DownloadGame error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logout and clear authentication token
        /// </summary>
        public void Logout()
        {
            Token = null;
            httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
