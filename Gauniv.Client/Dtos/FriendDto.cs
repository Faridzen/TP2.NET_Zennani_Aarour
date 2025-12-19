
namespace Gauniv.Client.Dtos
{
    public partial class FriendDto : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private bool isOnline;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private string status = "Accepted"; // Accepted, Sent, Received
        
        public DateTime AddedAt { get; set; }
    }
}
