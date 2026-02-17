using F1Predictions.Data;
using F1Predictions.Models;
using F1Predictions.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace F1Predictions.Services
{
    public class AdminAuthService
    {
        private readonly AppDbContext _context;

        public AdminAuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AnyAdminExistsAsync()
        {
            return await _context.AdminUsers.AnyAsync();
        }

        public async Task<ServiceResult<bool>> CreateAdminAsync(CreateAdminDto dto)
        {
            // Check if an admin already exists
            if (await AnyAdminExistsAsync())
            {
                return ServiceResult<bool>.Fail("An admin user already exists. Setup is locked.");
            }

            // Check for duplicate username
            var existingUser = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (existingUser != null)
            {
                return ServiceResult<bool>.Fail("Username already taken.");
            }

            // Hash password with BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var adminUser = new AdminUser
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AdminUsers.Add(adminUser);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Succeed(true, "Admin user created successfully.");
        }

        public async Task<ServiceResult<AdminUser>> LoginAsync(AdminLoginDto dto)
        {
            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (admin == null)
            {
                return ServiceResult<AdminUser>.Fail("Invalid username or password.");
            }

            if (!admin.IsActive)
            {
                return ServiceResult<AdminUser>.Fail("This account has been deactivated.");
            }

            // Verify BCrypt hash
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
            {
                return ServiceResult<AdminUser>.Fail("Invalid username or password.");
            }

            // Update last login timestamp
            admin.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<AdminUser>.Succeed(admin, "Login successful.");
        }
    }
}
