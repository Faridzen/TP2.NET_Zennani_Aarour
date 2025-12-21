
namespace Gauniv.Client.Dtos
{
    public partial class FriendDto : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private bool isOnline;

        // Status must be public for JSON deserialization
        private string _status = "Accepted";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        
        public DateTime AddedAt { get; set; }
    }
}
