using Microsoft.AspNetCore.Mvc;
using FlowerShop.Data;

namespace FlowerShop.Controllers.User
{
    [Route("api/contacts")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly FlowerContext _context;

        public ContactController(FlowerContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ContactRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName)
                || string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin bắt buộc" });
            }

            var contact = new Contact
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone?.Trim(),
                Subject = request.Subject?.Trim(),
                Message = request.Message.Trim(),
                IsRead = false,
                CreatedDate = DateTime.Now
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    public class ContactRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }
}
