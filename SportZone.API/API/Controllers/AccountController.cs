using SportZone.Application.Dtos;
using SportZone.Application.Interfaces.IService;
using SportZone.Application.Extensions;
using SportZone.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SportZone.API.Extensions;
using SportZone.Application.Dtos.SportZone.Application.Dtos;
using SportZone.Application.Dtos.EmailLoginDto;

namespace SportZone.API.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IEmailService emailService) : ControllerBase
    {

        //   !login  ->  register 
        [HttpPost("authenticate")]
        public async Task<ActionResult> Login(LoginDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            bool isNewUser = false;

            // TH1: email chưa có -> đki nhập password -> tạo tk và gửi otp 
            if (user == null)
            {
                user = new AppUser
                {
                    Email = loginDto.Email,
                    UserName = loginDto.Email,
                    FullName = loginDto.Email.Split("@")[0]
                };

                var result = await userManager.CreateAsync(user, loginDto.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("identity", error.Description);
                    return ValidationProblem();
                }

                await userManager.AddToRoleAsync(user, "Member");

                // Tạo mã OTP và Gửi Mail
                var otpToken = await userManager.GenerateUserTokenAsync(user, "Email", "EmailConfirmation");

                string subject = "Xác nhận tài khoản SportZone";
                string body = $@"
            <div style='font-family: Arial;'>
                <h2>Chào mừng bạn đến với SportZone!</h2>
                <p>Tài khoản của bạn vừa được tạo. Mã xác nhận là: <strong style='font-size: 24px; color: #d9534f;'>{otpToken}</strong></p>
                <p>Vui lòng nhập mã này vào ứng dụng để hoàn tất đăng ký.</p>
            </div>";

                await emailService.SendEmailAsync(user.Email, subject, body);

                return Ok(new
                {
                    Message = "Tài khoản mới đã được tạo. Vui lòng kiểm tra email để lấy mã xác thực!",
                    RequireOtp = true // Cờ báo cho Frontend biết cần chuyển sang màn hình nhập OTP
                });
            }

            // th2: email có rồi thì login như thường
            else
            {
                // 1. Kiểm tra mật khẩu
                var result = await userManager.CheckPasswordAsync(user, loginDto.Password);
                if (!result) return Unauthorized("Email hoặc mật khẩu không đúng!");

                // 2. Kiểm tra xem đã xác thực Email chưa
                if (!user.EmailConfirmed)
                {

                    return BadRequest(new
                    {
                        Message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email để lấy mã xác nhận.",
                        RequireOtp = true
                    });
                }

                // 3. Đăng nhập thành công -> Trả về Token
                await SetRefreshTokenCookie(user);
                var userDto = await user.ToDto(tokenService);

                return Ok(new
                {
                    user = userDto,
                    Message = "Đăng nhập thành công!",
                    RequireOtp = false
                });
            }
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult<UserDto>> VerifyEmail(VerifyOtpDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null) return NotFound("Không tìm thấy tài khoản.");

            if (user.EmailConfirmed) return BadRequest("Tài khoản này đã được xác thực rồi.");

            //  Kiểm tra mã OTP
            var isValid = await userManager.VerifyUserTokenAsync(user, "Email", "EmailConfirmation", request.OtpCode);

            if (!isValid) return BadRequest("Mã xác thực không chính xác hoặc đã hết hạn.");

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);


            // Tạo Refresh Token và lưu vào Cookie
            await SetRefreshTokenCookie(user);

            // Tạo JWT Token cho userDto
            var userDto = await user.ToDto(tokenService);

            return Ok(new
            {
                user = userDto,
                Message = "Xác thực email và đăng nhập thành công!"
            });
        }


        [HttpPost("refresh-token")]
        public async Task<ActionResult<UserDto>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null) return NoContent();

            var user = await userManager.Users
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken
                    && x.RefreshTokenExpiry > DateTime.UtcNow);

            if (user == null) return Unauthorized("Your login has expired, please log in again.");

            await SetRefreshTokenCookie(user);

            return await user.ToDto(tokenService);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            await userManager.Users
                .Where(x => x.Id == User.GetUserId())
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.RefreshToken, _ => null)
                    .SetProperty(x => x.RefreshTokenExpiry, _ => null)
                    );

            Response.Cookies.Delete("refreshToken");

            return Ok();
        }
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            var profile = await userManager.FindByIdAsync(userId);

            // var profileUser = UserService.GetProfile();

            if (profile == null) return Unauthorized();

            return Ok(new UserProfileDto
            {
                Id = profile.Id,
                UserName = profile.UserName,
                Email = profile.Email!,
                ImageUrl = profile.ImageUrl,
                FullName = profile.FullName,
                Address = profile.Address,
                Gender = profile.Gender,
                DateOfBirth = profile.DateOfBirth?.ToString("dd/MM/yyyy"),
                PhoneNumber = profile.PhoneNumber,
                Points = profile.Points
            });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto updateDto)
        {
            var userId = User.GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            if (user == null) return Unauthorized();

            user.FullName = updateDto.FullName;
            user.Address = updateDto.Address;
            user.Gender = updateDto.Gender;
            user.PhoneNumber = updateDto.PhoneNumber;
            user.ImageUrl = updateDto.ImageUrl;

            if (!string.IsNullOrEmpty(updateDto.DateOfBirth))
            {
                if (DateTime.TryParseExact(updateDto.DateOfBirth, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dob))
                {
                    user.DateOfBirth = DateTime.SpecifyKind(dob, DateTimeKind.Utc);
                }
            }

            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest("Không thể cập nhật hồ sơ cá nhân.");

            return Ok(new
            {
                Message = "Cập nhật hồ sơ thành công!",
                User = new UserProfileDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email!,
                    ImageUrl = user.ImageUrl,
                    FullName = user.FullName,
                    Address = user.Address,
                    Gender = user.Gender,
                    DateOfBirth = user.DateOfBirth?.ToString("dd/MM/yyyy"),
                    PhoneNumber = user.PhoneNumber
                }
            });
        }

        private async Task SetRefreshTokenCookie(AppUser user)
        {
            var refreshToken = tokenService.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,                 // JavaScript không đọc trộm được (Chống XSS)
                Secure = true,                   // Chỉ chạy trên HTTPS
                SameSite = SameSiteMode.None,  // Chỉ gửi cookie khi request từ chính trang web của bạn (Chống CSRF)
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
