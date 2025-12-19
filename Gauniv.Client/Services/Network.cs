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
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Gauniv.Client.Dtos;
using Microsoft.AspNetCore.SignalR.Client;

namespace Gauniv.Client.Services
{
    internal partial class NetworkService : ObservableObject
    {

        public static NetworkService Instance { get; private set; } = new NetworkService();
        
        [ObservableProperty]
        private string token;

        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private bool isAnyGameRunning;

        [ObservableProperty]
        private int? runningGameId;

        [ObservableProperty]
        private int? runningProcessId;
        
        public HttpClient httpClient;
        private HubConnection? hubConnection;

        private Process? _runningProcess;

        private const string BaseUrl = "http://localhost:5231/api/1.0.0/Games/";
        private const string AdminUrl = "http://localhost:5231/api/1.0.0/Admin/";
        private const string AuthUrl = "http://localhost:5231/Bearer/login";
        private const string HubUrl = "http://localhost:5231/online";

        public NetworkService() {
            var local_handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            httpClient = new HttpClient(local_handler);
            Token = null;
        }

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnGamePurchased;
        public event Action<string, bool> OnFriendStatusChanged;


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
                        
                        // Détecter si l'utilisateur est admin
                        await GetProfileAsync();
                        
                        // Start SignalR
                        await InitializeSignalR();

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

        private async Task InitializeSignalR()
        {
            try
            {
                if (hubConnection != null) await hubConnection.DisposeAsync();

                hubConnection = new HubConnectionBuilder()
                    .WithUrl(HubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(Token);
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            if (handler is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                            }
                            return handler;
                        };
                    })
                    .WithAutomaticReconnect()
                    .Build();

                hubConnection.On<string>("FriendOnline", (userId) =>
                {
                    OnFriendStatusChanged?.Invoke(userId, true);
                });

                hubConnection.On<string>("FriendOffline", (userId) =>
                {
                    OnFriendStatusChanged?.Invoke(userId, false);
                });

                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Error: {ex.Message}");
            }
        }

       
        public async Task<PagedResultDto<GameDto>> GetGamesAsync(int offset = 0, int limit = 10, string[]? categories = null, string? search = null)
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

                if (!string.IsNullOrWhiteSpace(search))
                {
                    local_queryParams += $"&search={Uri.EscapeDataString(search)}";
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

       
        public async Task<UserDto?> GetProfileAsync()
        {
            try
            {
                var local_response = await httpClient.GetAsync($"{BaseUrl}Profile");
                if (local_response.IsSuccessStatusCode)
                {
                    var local_json = await local_response.Content.ReadAsStringAsync();
                    var local_user = JsonSerializer.Deserialize<UserDto>(local_json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (local_user != null)
                    {
                        IsAdmin = local_user.IsAdmin;
                    }
                    return local_user;
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> PurchaseGameAsync(int gameId)
        {
            try
            {
                var local_response = await httpClient.PostAsync($"{BaseUrl}Purchase/{gameId}", null);
                
                if (local_response.IsSuccessStatusCode)
                {
                    OnGamePurchased?.Invoke();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PurchaseGame error: {ex.Message}");
                return false;
            }
        }

        
        public async Task<bool> DownloadGameAsync(int gameId, string savePath, IProgress<double>? progress = null)
        {
            try
            {
                // Utiliser ResponseHeadersRead pour ne pas charger tout en mémoire immédiatement
                using var local_response = await httpClient.GetAsync($"{BaseUrl}Download/{gameId}", HttpCompletionOption.ResponseHeadersRead);

                if (local_response.IsSuccessStatusCode)
                {
                    var local_totalBytes = local_response.Content.Headers.ContentLength;
                    using var local_contentStream = await local_response.Content.ReadAsStreamAsync();
                    using var local_fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    var local_buffer = new byte[8192];
                    long local_totalBytesRead = 0;
                    int local_bytesRead;

                    while ((local_bytesRead = await local_contentStream.ReadAsync(local_buffer, 0, local_buffer.Length)) != 0)
                    {
                        await local_fileStream.WriteAsync(local_buffer, 0, local_bytesRead);
                        local_totalBytesRead += local_bytesRead;

                        if (local_totalBytes.HasValue)
                        {
                            progress?.Report((double)local_totalBytesRead / local_totalBytes.Value);
                        }
                    }

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

        #region Admin Operations

        public async Task<bool> AddGameAsync(GameDto game)
        {
            try
            {
                var local_json = JsonSerializer.Serialize(game);
                var local_content = new StringContent(local_json, Encoding.UTF8, "application/json");
                var local_response = await httpClient.PostAsync($"{AdminUrl}AddGame", local_content);
                
                if (!local_response.IsSuccessStatusCode)
                {
                    var local_error = await local_response.Content.ReadAsStringAsync();
                    throw new Exception(local_error);
                }
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Serveur:")) throw;
                throw new Exception($"Erreur réseau: {ex.Message}");
            }
        }

        public async Task<bool> UploadGameAsync(string title, string description, decimal price, string categoriesCsv, string executablePath, string imageUrl)
        {
            try
            {
                using var local_content = new MultipartFormDataContent();
                
                local_content.Add(new StringContent(title), "title");
                local_content.Add(new StringContent(description ?? ""), "description");
                local_content.Add(new StringContent(price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "price");
                local_content.Add(new StringContent(categoriesCsv ?? ""), "categories");
                local_content.Add(new StringContent(imageUrl ?? ""), "imageUrl");

                if (!string.IsNullOrEmpty(executablePath) && File.Exists(executablePath))
                {
                    var local_fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(executablePath));
                    local_fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    local_content.Add(local_fileContent, "executable", Path.GetFileName(executablePath));
                }

                var local_response = await httpClient.PostAsync($"{AdminUrl}UploadGame", local_content);
                
                if (!local_response.IsSuccessStatusCode)
                {
                    var local_error = await local_response.Content.ReadAsStringAsync();
                    throw new Exception($"Échec upload: {local_error}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadGame error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateGameAsync(GameDto game)
        {
            try
            {
                var local_json = JsonSerializer.Serialize(game);
                var local_content = new StringContent(local_json, Encoding.UTF8, "application/json");
                var local_response = await httpClient.PutAsync($"{AdminUrl}UpdateGame/{game.Id}", local_content);
                
                if (!local_response.IsSuccessStatusCode)
                {
                    var local_error = await local_response.Content.ReadAsStringAsync();
                    throw new Exception(local_error);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur mise à jour: {ex.Message}");
            }
        }

        public async Task<bool> DeleteGameAsync(int gameId)
        {
            try
            {
                var local_response = await httpClient.DeleteAsync($"{AdminUrl}DeleteGame/{gameId}");
                return local_response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> AddCategoryAsync(string name)
        {
            try
            {
                var local_cat = new CategoryDto { Name = name };
                var local_json = JsonSerializer.Serialize(local_cat);
                var local_content = new StringContent(local_json, Encoding.UTF8, "application/json");
                var local_response = await httpClient.PostAsync($"{AdminUrl}AddCategory", local_content);
                
                if (!local_response.IsSuccessStatusCode)
                {
                    var local_error = await local_response.Content.ReadAsStringAsync();
                    throw new Exception(local_error);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur catégorie: {ex.Message}");
            }
        }

        public async Task<bool> UpdateCategoryAsync(int id, string newName)
        {
            try
            {
                var local_cat = new CategoryDto { Id = id, Name = newName };
                var local_json = JsonSerializer.Serialize(local_cat);
                var local_content = new StringContent(local_json, Encoding.UTF8, "application/json");
                var local_response = await httpClient.PutAsync($"{AdminUrl}UpdateCategory/{id}", local_content);
                
                if (!local_response.IsSuccessStatusCode)
                {
                    var local_error = await local_response.Content.ReadAsStringAsync();
                    throw new Exception(local_error);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur catégorie: {ex.Message}");
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var local_response = await httpClient.DeleteAsync($"{AdminUrl}DeleteCategory/{id}");
                return local_response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        #endregion

        #region Friend Operations

        public async Task<List<FriendDto>> GetFriendsAsync()
        {
            try
            {
                var local_response = await httpClient.GetAsync("http://localhost:5231/api/Friends/List");
                if (local_response.IsSuccessStatusCode)
                {
                    var local_json = await local_response.Content.ReadAsStringAsync();
                    var local_result = JsonSerializer.Deserialize<List<FriendDto>>(local_json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return local_result ?? new List<FriendDto>();
                }
                return new List<FriendDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFriends error: {ex.Message}");
                return new List<FriendDto>();
            }
        }

        public async Task<(bool, string)> AddFriendAsync(string username)
        {
            try
            {
                var local_response = await httpClient.PostAsync($"http://localhost:5231/api/Friends/Add/{username}", null);
                if (local_response.IsSuccessStatusCode) return (true, "");
                
                var local_error = await local_response.Content.ReadAsStringAsync();
                return (false, local_error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddFriend error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<bool> AcceptFriendAsync(string userId)
        {
            try
            {
                var local_response = await httpClient.PostAsync($"http://localhost:5231/api/Friends/Accept/{userId}", null);
                return local_response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AcceptFriend error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveFriendAsync(string userId)
        {
            try
            {
                var local_response = await httpClient.PostAsync($"http://localhost:5231/api/Friends/Remove/{userId}", null);
                return local_response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoveFriend error: {ex.Message}");
                return false;
            }
        }

        #endregion



        public void StartGame(string localPath, int gameId)
        {
            if (IsAnyGameRunning) StopActiveGame();

            try
            {
                _runningProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = localPath,
                        UseShellExecute = true
                    },
                    EnableRaisingEvents = true
                };

                _runningProcess.Exited += (s, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _runningProcess?.Dispose();
                        _runningProcess = null;
                        IsAnyGameRunning = false;
                        RunningGameId = null;
                        RunningProcessId = null;
                    });
                };

                _runningProcess.Start();
                
                IsAnyGameRunning = true;
                RunningGameId = gameId;
                RunningProcessId = _runningProcess.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting game: {ex.Message}");
                IsAnyGameRunning = false;
                RunningGameId = null;
                RunningProcessId = null;
                throw;
            }
        }

        public void StopActiveGame()
        {
            try
            {
                if (_runningProcess != null && !_runningProcess.HasExited)
                {
                    _runningProcess.Kill(true);
                }
                else if (RunningProcessId.HasValue)
                {
                    try
                    {
                        var local_p = Process.GetProcessById(RunningProcessId.Value);
                        if (local_p != null && !local_p.HasExited)
                        {
                            local_p.Kill(true);
                        }
                    }
                    catch { /* Handle already exited */ }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping game: {ex.Message}");
            }
            finally
            {
                _runningProcess?.Dispose();
                _runningProcess = null;
                IsAnyGameRunning = false;
                RunningGameId = null;
                RunningProcessId = null;
            }
        }

        public void Logout()
        {
            StopActiveGame();
            Token = null;
            IsAdmin = false;
            httpClient.DefaultRequestHeaders.Authorization = null;
            OnDisconnected?.Invoke();
        }
    }
}
