using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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

        //verify phone 
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

            // Generate JWT token
            var token = GenerateJwt(user.Id, user.FirstName, user.LastName, user.Phone);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Phone verified successfully",
                Token = token,
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

        //login 
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

            // Generate JWT token
            var token = GenerateJwt(user.Id, user.FirstName, user.LastName, user.Phone);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
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

        //request otp
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

        //verify otp
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

        //reset password
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

        //generate jwt
        private string GenerateJwt(int id, string firstName, string lastName, string phone)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("auth_type", "user"),
                new Claim("id", id.ToString()),
                new Claim("firstName", firstName),
                new Claim("lastName", lastName),
                new Claim("phone", phone)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
