using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Dtos;
using Gauniv.Client.Services;
using System.Collections.ObjectModel;

namespace Gauniv.Client.ViewModel
{
    public partial class MyFriendsViewModel : ObservableObject
    {
        private readonly NetworkService _networkService;

        [ObservableProperty]
        private ObservableCollection<FriendDto> friends = new();

        [ObservableProperty]
        private ObservableCollection<FriendDto> incomingRequests = new();

        [ObservableProperty]
        private ObservableCollection<FriendDto> outgoingRequests = new();

        [ObservableProperty]
        private string friendSearchText = "";

        [ObservableProperty]
        private string statusMessage = "";

        [ObservableProperty]
        private bool isLoading = false;

        public MyFriendsViewModel()
        {
            _networkService = NetworkService.Instance;
            _networkService.OnFriendStatusChanged += OnFriendStatusChanged;
            if (!string.IsNullOrEmpty(_networkService.Token))
            {
                _ = LoadFriendsAsync();
            }
        }

        private void OnFriendStatusChanged(string userId, bool isOnline)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var local_friend = Friends.FirstOrDefault(f => f.UserId == userId);
                if (local_friend != null)
                {
                    local_friend.IsOnline = isOnline;
                }
            });
        }

        [RelayCommand]
        private async Task LoadFriendsAsync()
        {
            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CLIENT] LoadFriendsAsync called. Token: {(!string.IsNullOrEmpty(_networkService.Token) ? "Present" : "Missing")}");
                
                var local_list = await _networkService.GetFriendsAsync();
                System.Diagnostics.Debug.WriteLine($"[CLIENT] Received {local_list?.Count ?? 0} friends from API");
                
                Friends.Clear();
                IncomingRequests.Clear();
                OutgoingRequests.Clear();

                if (local_list == null || local_list.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[CLIENT] No friends returned from API");
                    StatusMessage = "Aucun ami pour le moment";
                    return;
                }

                foreach (var f in local_list)
                {
                    System.Diagnostics.Debug.WriteLine($"[CLIENT] Processing Friend: UserName={f.UserName}, Status='{f.Status}', IsOnline={f.IsOnline}");
                    
                    if (f.Status == "Accepted")
                    {
                        Friends.Add(f);
                        System.Diagnostics.Debug.WriteLine($"[CLIENT] Added {f.UserName} to Friends list");
                    }
                    else if (f.Status == "Received")
                    {
                        IncomingRequests.Add(f);
                        System.Diagnostics.Debug.WriteLine($"[CLIENT] Added {f.UserName} to IncomingRequests list");
                    }
                    else if (f.Status == "Sent")
                    {
                        OutgoingRequests.Add(f);
                        System.Diagnostics.Debug.WriteLine($"[CLIENT] Added {f.UserName} to OutgoingRequests list");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CLIENT] WARNING: Unknown status '{f.Status}' for {f.UserName}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CLIENT] FINAL COUNTS: Friends={Friends.Count}, Incoming={IncomingRequests.Count}, Outgoing={OutgoingRequests.Count}");
                StatusMessage = $"{Friends.Count} ami(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CLIENT] ERROR in LoadFriendsAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CLIENT] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddFriendAsync()
        {
            if (string.IsNullOrWhiteSpace(FriendSearchText)) return;

            StatusMessage = "Ajout en cours...";
            
            var (local_success, local_error) = await _networkService.AddFriendAsync(FriendSearchText);
            if (local_success)
            {
                StatusMessage = "Demande envoyée !";
                FriendSearchText = "";
                await LoadFriendsAsync();
            }
            else
            {
                StatusMessage = $"Erreur : {local_error}";
            }
        }

        [RelayCommand]
        private async Task AcceptRequestAsync(FriendDto request)
        {
            if (request == null) return;
            bool local_success = await _networkService.AcceptFriendAsync(request.UserId);
            if (local_success) await LoadFriendsAsync();
            else StatusMessage = "Erreur lors de l'acceptation.";
        }

        [RelayCommand]
        private async Task RemoveFriendAsync(FriendDto friend)
        {
            if (friend == null) return;

            string local_action = "Supprimer";
            if (friend.Status == "Received") local_action = "Refuser";
            else if (friend.Status == "Sent") local_action = "Annuler";

            bool local_confirm = await Application.Current!.MainPage!.DisplayAlert("Confirmation", $"{local_action} {friend.UserName} ?", "Oui", "Non");
            if (!local_confirm) return;

            bool local_success = await _networkService.RemoveFriendAsync(friend.UserId);
            if (local_success)
            {
                await LoadFriendsAsync();
            }
            else
            {
                StatusMessage = "Erreur lors de la suppression.";
            }
        }
    }
}
