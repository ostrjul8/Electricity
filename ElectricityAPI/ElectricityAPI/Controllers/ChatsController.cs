using BLL.Models;
using BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectricityAPI.Controllers
{
    [ApiController]
    [Route("api/chats")]
    [Authorize(Roles = "User,Admin")]
    public class ChatsController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatsController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequestDTO request)
        {
            try
            {
                if (!TryGetCurrentUser(out int userId, out bool isAdmin))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }

                if (isAdmin)
                {
                    return Forbid();
                }

                OpenChatResponseDTO result = await _chatService.CreateChatWithFirstMessageAsync(userId, request.Text);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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
        }

        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            if (!TryGetCurrentUser(out int userId, out bool isAdmin))
            {
                return Unauthorized(new { error = "Invalid user token." });
            }

            List<ChatDTO> chats = await _chatService.GetChatsAsync(userId, isAdmin);
            return Ok(chats);
        }

        [HttpGet("{chatId:int}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            try
            {
                if (!TryGetCurrentUser(out int userId, out bool isAdmin))
                {
                    return Unauthorized(new { error = "Invalid user token." });
                }

                List<MessageDTO> messages = await _chatService.GetMessagesAsync(userId, isAdmin, chatId);
                return Ok(messages);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
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

            isAdmin = User.IsInRole("Admin");
            return true;
        }
    }
}
