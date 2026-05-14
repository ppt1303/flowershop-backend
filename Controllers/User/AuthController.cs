using Microsoft.AspNetCore.Mvc;
using FlowerShop.Data;
using FlowerShop.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace FlowerShop.Controllers.User
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly FlowerContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(FlowerContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = UserClaimsHelper.GetUserId(User);
            if (userId == null) return Unauthorized();

            var u = await _context.Users.FindAsync(userId);
            if (u == null || u.IsActive != true) return Unauthorized();

            return Ok(new
            {
                userId = u.UserId,
                fullName = u.FullName,
                email = u.Email,
                role = u.Role,
                phone = u.Phone,
                avatar = u.Avatar
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Vui lòng nhập email và mật khẩu" });

            var email = request.Email.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
            if (user.IsActive != true)
                return Unauthorized(new { message = "Tài khoản đã bị khóa" });

            var parts = user.PasswordHash.Split('.');
            if (parts.Length != 2)
                return Unauthorized(new { message = "Lỗi dữ liệu" });

            byte[] salt;
            try
            {
                salt = Convert.FromBase64String(parts[0]);
            }
            catch
            {
                return Unauthorized(new { message = "Lỗi dữ liệu" });
            }

            if (!TokenHelper.IsValidPassword(request.Password, salt, parts[1]))
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });

            return Ok(new
            {
                token = CreateToken(user),
                user = ToUserDto(user)
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Password)
                || string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin" });
            }

            var email = request.Email.Trim();
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return BadRequest(new { message = "Email đã tồn tại" });

            string hash = TokenHelper.HashPassword(request.Password, out byte[] salt);
            var newUser = new FlowerShop.Data.User
            {
                Email = email,
                FullName = request.FullName.Trim(),
                PasswordHash = Convert.ToBase64String(salt) + "." + hash,
                Phone = request.Phone?.Trim(),
                Role = "Customer",
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                token = CreateToken(newUser),
                user = ToUserDto(newUser)
            });
        }

        private string CreateToken(FlowerShop.Data.User user)
        {
            string secretKey = _configuration["Jwt:Key"] ?? "Chuoi_Secret_Key_Mac_Dinh_Sieu_Bao_Mat_123";
            return TokenHelper.GenerateToken(secretKey, 480, user.UserId.ToString(), user.FullName, user.Role ?? "Customer");
        }

        private static object ToUserDto(FlowerShop.Data.User user)
        {
            return new
            {
                userId = user.UserId,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role,
                phone = user.Phone,
                avatar = user.Avatar
            };
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}
