using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace F1Predictions.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(AppDbContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResponseDto> SignupAsync(SignupRequestDto request)
        {
            // Check if phone already exists
            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "A user with this phone number already exists"
                };
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var existingEmail = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingEmail != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "A user with this email already exists"
                    };
                }
            }

            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = request.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                SecurityStamp = Guid.NewGuid()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate OTP for phone verification
            string code = new Random().Next(100000, 999999).ToString();
            var otp = new UserOtp
            {
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };
            _context.UserOtps.Add(otp);
            await _context.SaveChangesAsync();

            // Send OTP (console for now)
            Console.WriteLine($"[SIGNUP OTP] Phone: {user.Phone}, Code: {code}");

            return new AuthResponseDto
            {
                Success = true,
                Message = "User registered successfully. Please verify your phone number.",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive,
                    IsPhoneVerified = user.PhoneVerifiedAt.HasValue,
                    IsEmailVerified = user.EmailVerifiedAt.HasValue
                }
            };
        }

        public async Task<AuthResponseDto> VerifyPhoneAsync(VerifyPhoneRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            if (user.PhoneVerifiedAt.HasValue)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Phone number is already verified"
                };
            }

            // Find valid OTP
            var otp = await _context.UserOtps
                .Where(o => o.UserId == user.Id && o.Code == request.Code && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired OTP"
                };
            }

            // Mark OTP as used
            otp.IsUsed = true;
            user.PhoneVerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens and set cookies
            await SetAuthCookiesAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Phone verified successfully",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive,
                    IsPhoneVerified = true,
                    IsEmailVerified = user.EmailVerifiedAt.HasValue
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid phone number or password"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Account is deactivated"
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid phone number or password"
                };
            }

            // Check if phone is verified
            if (!user.PhoneVerifiedAt.HasValue)
            {
                // Send new OTP
                string code = new Random().Next(100000, 999999).ToString();
                var otp = new UserOtp
                {
                    UserId = user.Id,
                    Code = code,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false
                };
                _context.UserOtps.Add(otp);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[LOGIN OTP] Phone: {user.Phone}, Code: {code}");

                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Phone number not verified. OTP sent."
                };
            }

            // Generate tokens and set cookies
            await SetAuthCookiesAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive,
                    IsPhoneVerified = true,
                    IsEmailVerified = user.EmailVerifiedAt.HasValue
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return new AuthResponseDto { Success = false, Message = "No HTTP context" };
            }

            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return new AuthResponseDto { Success = false, Message = "No refresh token" };
            }

            var tokenRecord = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow);

            if (tokenRecord == null)
            {
                return new AuthResponseDto { Success = false, Message = "Invalid or expired refresh token" };
            }

            // Revoke old token
            tokenRecord.IsRevoked = true;

            // Generate new tokens
            var accessToken = GenerateAccessToken(tokenRecord.User);
            var newRefreshToken = GenerateRefreshToken();

            // Store new refresh token
            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = tokenRecord.UserId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _context.SaveChangesAsync();

            // Set cookies
            SetTokenCookies(httpContext.Response, accessToken, newRefreshToken);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Token refreshed",
                User = new UserDto
                {
                    Id = tokenRecord.User.Id,
                    FirstName = tokenRecord.User.FirstName,
                    LastName = tokenRecord.User.LastName,
                    Phone = tokenRecord.User.Phone,
                    Email = tokenRecord.User.Email,
                    CreatedAt = tokenRecord.User.CreatedAt,
                    IsActive = tokenRecord.User.IsActive,
                    IsPhoneVerified = tokenRecord.User.PhoneVerifiedAt.HasValue,
                    IsEmailVerified = tokenRecord.User.EmailVerifiedAt.HasValue
                }
            };
        }

        public async Task<AuthResponseDto> LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return new AuthResponseDto { Success = false, Message = "No HTTP context" };
            }

            var refreshToken = httpContext.Request.Cookies["refresh_token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenRecord = await _context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == refreshToken);
                if (tokenRecord != null)
                {
                    tokenRecord.IsRevoked = true;
                    await _context.SaveChangesAsync();
                }
            }

            // Clear cookies
            httpContext.Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
            httpContext.Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });

            return new AuthResponseDto
            {
                Success = true,
                Message = "Logged out successfully"
            };
        }

        public async Task<AuthResponseDto> RequestPasswordResetAsync(RequestPasswordResetDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (user == null)
            {
                // Don't reveal if user exists
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "If this phone number is registered, you will receive an OTP"
                };
            }

            // Generate OTP
            string code = new Random().Next(100000, 999999).ToString();
            var otp = new UserOtp
            {
                UserId = user.Id,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };
            _context.UserOtps.Add(otp);
            await _context.SaveChangesAsync();

            // Send OTP
            Console.WriteLine($"[PASSWORD RESET OTP] Phone: {user.Phone}, Code: {code}");

            return new AuthResponseDto
            {
                Success = true,
                Message = "If this phone number is registered, you will receive an OTP"
            };
        }

        public async Task<AuthResponseDto> VerifyResetOtpAsync(VerifyResetOtpRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid OTP"
                };
            }

            // Find valid OTP
            var otp = await _context.UserOtps
                .Where(o => o.UserId == user.Id && o.Code == request.Code && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired OTP"
                };
            }

            return new AuthResponseDto
            {
                Success = true,
                Message = "OTP verified. You can now reset your password."
            };
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == request.Phone);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request"
                };
            }

            // Find and validate OTP
            var otp = await _context.UserOtps
                .Where(o => o.UserId == user.Id && o.Code == request.Code && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired OTP"
                };
            }

            // Mark OTP as used
            otp.IsUsed = true;

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.SecurityStamp = Guid.NewGuid();
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Success = true,
                Message = "Password reset successfully"
            };
        }

        #region Private Helper Methods

        private async Task SetAuthCookiesAsync(User user)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();

            // Store refresh token in database
            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await _context.SaveChangesAsync();

            // Set cookies
            SetTokenCookies(httpContext.Response, accessToken, refreshToken);
        }

        private string GenerateAccessToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("auth_type", "user"),
                new Claim("id", user.Id.ToString()),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim("phone", user.Phone)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private void SetTokenCookies(HttpResponse response, string accessToken, string refreshToken)
        {
            var isProduction = !(_config["ASPNETCORE_ENVIRONMENT"] == "Development");

            var accessCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction, // HTTPS only in production
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddMinutes(15)
            };
            response.Cookies.Append("access_token", accessToken, accessCookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isProduction,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(7)
            };
            response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
        }

        #endregion
    }
}
