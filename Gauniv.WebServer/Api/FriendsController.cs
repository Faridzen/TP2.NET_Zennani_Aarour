using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gauniv.WebServer.Api
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class FriendsController(ApplicationDbContext _db, UserManager<User> _userManager, OnlineService _onlineService) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<List<FriendDto>>> List()
        {
            try
            {
                var local_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (local_userId == null) return Unauthorized();

                var local_allLinks = await _db.UserFriends
                    .Include(f => f.SourceUser)
                    .Include(f => f.TargetUser)
                    .Where(f => f.SourceUserId == local_userId || f.TargetUserId == local_userId)
                    .ToListAsync();

                var local_connectedUserIds = _onlineService?.GetConnectedUserIds() ?? new List<string>();

                var local_result = local_allLinks.Select(f =>
                {
                    bool local_isSource = f.SourceUserId == local_userId;
                    var local_otherUser = local_isSource ? f.TargetUser : f.SourceUser;
                    var local_otherUserId = local_isSource ? f.TargetUserId : f.SourceUserId;

                    string local_status;
                    if (f.IsAccepted)
                    {
                        local_status = "Accepted";
                    }
                    else
                    {
                        local_status = local_isSource ? "Sent" : "Received";
                    }

                    return new FriendDto
                    {
                        UserId = local_otherUserId,
                        UserName = local_otherUser.UserName ?? "Unknown",
                        IsOnline = local_connectedUserIds.Contains(local_otherUserId),
                        Status = local_status,
                        AddedAt = f.CreatedAt
                    };
                }).ToList();

                return Ok(local_result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> Add(string username)
        {
            try
            {
                var local_currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (local_currentUserId == null) return Unauthorized();

                var local_targetUser = await _userManager.FindByNameAsync(username);
                if (local_targetUser == null) return NotFound("User not found");

                if (local_targetUser.Id == local_currentUserId)
                    return BadRequest("Cannot add yourself");

                // Check existing link (any direction)
                var local_existing = await _db.UserFriends
                    .FirstOrDefaultAsync(f => 
                        (f.SourceUserId == local_currentUserId && f.TargetUserId == local_targetUser.Id) ||
                        (f.SourceUserId == local_targetUser.Id && f.TargetUserId == local_currentUserId));

                if (local_existing != null)
                {
                    if (local_existing.IsAccepted) return BadRequest("Already friends");
                    return BadRequest("Request already pending");
                }

                var local_request = new UserFriend
                {
                    SourceUserId = local_currentUserId,
                    TargetUserId = local_targetUser.Id,
                    IsAccepted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.UserFriends.Add(local_request);
                await _db.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{userId}")]
        public async Task<ActionResult> Accept(string userId)
        {
            try
            {
                var local_currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (local_currentUserId == null) return Unauthorized();

                // Find the request where Current User is the Target
                var local_friendship = await _db.UserFriends
                    .FirstOrDefaultAsync(f => f.SourceUserId == userId && f.TargetUserId == local_currentUserId);

                if (local_friendship == null) return NotFound("Request not found");

                local_friendship.IsAccepted = true;
                local_friendship.AcceptedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{userId}")]
        public async Task<ActionResult> Remove(string userId)
        {
            // Handles Reject, Cancel, and Remove Friend
            try
            {
                var local_currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (local_currentUserId == null) return Unauthorized();

                var local_friendship = await _db.UserFriends
                    .FirstOrDefaultAsync(f => 
                        (f.SourceUserId == local_currentUserId && f.TargetUserId == userId) ||
                        (f.SourceUserId == userId && f.TargetUserId == local_currentUserId));

                if (local_friendship != null)
                {
                    _db.UserFriends.Remove(local_friendship);
                    await _db.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class FriendDto
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public bool IsOnline { get; set; }
        public string Status { get; set; } = "Accepted"; // Accepted, Sent, Received
        public DateTime AddedAt { get; set; }
    }
}
