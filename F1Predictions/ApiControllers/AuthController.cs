using F1Predictions.Models.DTOs;
using F1Predictions.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1Predictions.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> Signup([FromBody] SignupRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = GetModelStateErrors()
                });
            }

            var result = await _authService.SignupAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("verify-phone")]
        public async Task<ActionResult<AuthResponseDto>> VerifyPhone([FromBody] VerifyPhoneRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = GetModelStateErrors()
                });
            }

            var result = await _authService.VerifyPhoneAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = GetModelStateErrors()
                });
            }

            var result = await _authService.LoginAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("logout")]
        public ActionResult<AuthResponseDto> Logout()
        {
            // JWT is stateless - client should discard the token
            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Logged out successfully"
            });
        }

        [HttpPost("request-password-reset")]
        public async Task<ActionResult<AuthResponseDto>> RequestPasswordReset([FromBody] RequestPasswordResetDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = GetModelStateErrors()
                });
            }

            var result = await _authService.RequestPasswordResetAsync(request);
            return Ok(result); // Always return OK to not reveal if user exists
        }

        [HttpPost("verify-reset-otp")]
        public async Task<ActionResult<AuthResponseDto>> VerifyResetOtp([FromBody] VerifyResetOtpRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = GetModelStateErrors()
                });
            }

            var result = await _authService.VerifyResetOtpAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponseDto>> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = GetModelStateErrors()
                });
            }

            var result = await _authService.ResetPasswordAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private string GetModelStateErrors()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }
    }
}
