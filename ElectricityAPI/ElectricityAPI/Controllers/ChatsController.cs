using BLL.Models;
using BLL.Services;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/chats")]
    [Authorize(Roles = AppRoles.UserAndAbove)]
    public class ChatsController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatsController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequestDTO request)
        {
            try
            {
                bool hasCurrentUser = TryGetCurrentUser(out int userId, out bool isAdmin);
                
                if (hasCurrentUser && isAdmin)
                {
                    return Forbid();
                }

                int? senderUserId = hasCurrentUser ? userId : null;
                OpenChatResponseDTO result = await _chatService.CreateChatWithFirstMessageAsync(senderUserId, request.Text);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{chatId:int}/messages")]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendMessageRequestDTO request)
        {
            try
            {
                if (!TryGetCurrentUser(out int userId, out bool isAdmin))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }

                MessageDTO result = await _chatService.SendMessageAsync(userId, isAdmin, chatId, request.Text);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("lazy")]
        public async Task<IActionResult> GetChatsLazy(
            [FromQuery] bool onlyUnread = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            pageSize = pageSize > 100 ? 100 : pageSize;

            try
            {
                if (!TryGetCurrentUser(out int userId, out bool isAdmin))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }

                PagedResultDTO<ChatDTO> chats = await _chatService.GetChatsPagedAsync(userId, isAdmin, onlyUnread, page, pageSize);
                return Ok(chats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{chatId:int}/messages/lazy")]
        public async Task<IActionResult> GetMessagesLazy(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 30 : pageSize;
            pageSize = pageSize > 200 ? 200 : pageSize;

            try
            {
                if (!TryGetCurrentUser(out int userId, out bool isAdmin))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }

                PagedResultDTO<MessageDTO> messages = await _chatService.GetMessagesPagedAsync(userId, isAdmin, chatId, page, pageSize);
                return Ok(messages);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private bool TryGetCurrentUser(out int userId, out bool isAdmin)
        {
            userId = 0;
            isAdmin = false;

            string? userIdClaim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out userId))
            {
                return false;
            }

            isAdmin = User.IsInRole(AppRoles.Admin);
            return true;
        }
    }
}
