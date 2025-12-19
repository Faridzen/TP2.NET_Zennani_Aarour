using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gauniv.WebServer.Data
{
    public class UserFriend
    {
        public string SourceUserId { get; set; }
        public User SourceUser { get; set; }

        public string TargetUserId { get; set; }
        public User TargetUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsAccepted { get; set; } = false;
        public DateTime? AcceptedAt { get; set; }
    }
}
