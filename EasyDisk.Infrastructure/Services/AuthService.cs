using EasyDisk.Application.DTOs;
using EasyDisk.Application.DTOs.Auth;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces.Audit;
using EasyDisk.Application.Interfaces.Auth;
using EasyDisk.Application.Interfaces.EmailSender;
using EasyDisk.Infrastructure.Identity.Entities;
using EasyDisk.Infrastructure.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IAuditService _auditService;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailSenderService emailSenderService, IAuditService auditService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailSenderService = emailSenderService;
            _auditService = auditService;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await ValidateUserCredentialsAsync(loginDto.Email, loginDto.Password);
            return await FinalizeAuthenticationAsync(user, loginDto.Code, isGoogleAuth: false);
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            var email = await FetchEmailFromGoogleAsync(googleLoginDto.AccessToken);
            var user = await EnsureGoogleUserExistsAsync(email);
            return await FinalizeAuthenticationAsync(user, googleLoginDto.Code, isGoogleAuth: true);
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            await EnsureEmailIsUniqueAsync(registerDto.Email);
            var user = await CreateNewUserAsync(registerDto.Email, registerDto.Password);
            return new AuthResponseDto();
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                throw new ValidationException("Wrong confirmation link.");

            var user = await _userManager.FindByIdAsync(userId).EnsureExistsAsync(() => "User not found");
            var result = await _userManager.ConfirmEmailAsync(user, token);

            await LogAuditResultAsync("User.EmailConfirmation", user.Id, new { Email = user.Email }, result.Succeeded);

            if (!result.Succeeded)
                throw new ValidationException("Email verification failed. The link may be outdated or has already been used.");
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                await LogAuditResultAsync("User.PasswordResetRequested", null, new { AttemptedEmail = dto.Email, Reason = "Invalid user state" }, false);
                return;
            }

            await SendPasswordResetEmailAsync(user);
            await LogAuditResultAsync("User.PasswordResetRequested", user.Id, new { Email = user.Email }, true);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email) ?? throw new ValidationException("Invalid password recovery request.");
            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            await LogAuditResultAsync("User.PasswordReset", user.Id, new { Email = user.Email }, result.Succeeded);

            if (!result.Succeeded)
                throw new ValidationException($"Failed to reset password. The link may be out of date.");
        }

        public async Task<TwoFactorSetupResponseDto> Get2FaSetupInfoAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var email = user.Email ?? throw new ValidationException("User email is required for 2FA setup.");

            var authenticatorUri = $"otpauth://totp/EasyDisk:{email}?secret={unformattedKey}&issuer=EasyDisk&digits=6";

            return new TwoFactorSetupResponseDto
            {
                SharedKey = unformattedKey!,
                AuthenticatorUri = authenticatorUri
            };
        }

        public async Task Confirm2FaSetupAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                code).ValidateCodeAsync(() => "Invalid 2FA code.");

            await _userManager.SetTwoFactorEnabledAsync(user, true);
        }

        public async Task Disable2FaAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            await _userManager.SetTwoFactorEnabledAsync(user, false);   
        }

        public async Task ResendConfirmationEmail(ResendEmailDto resendEmailDto)
        {
            var user = await _userManager.FindByEmailAsync(resendEmailDto.Email);

            if (user!.EmailConfirmed)
            {
                throw new ValidationException("Email is already confirmed.");
            }

            await ProcessConfirmationEmail(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");

            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                UserId = user.Id,
                RequiresTwoFactor = user.TwoFactorEnabled
            };
        }

        private async Task<ApplicationUser> ValidateUserCredentialsAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                await LogAuditResultAsync("User.LoginFailed", user?.Id, new { AttemptedEmail = email, Reason = "Invalid credentials" }, false);
                throw new ValidationException("Invalid email or password.");
            }
            return user;
        }

        private async Task<string> FetchEmailFromGoogleAsync(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

            if (!response.IsSuccessStatusCode)
            {
                await LogAuditResultAsync("User.GoogleLoginFailed", null, new { Reason = "Invalid Google Access Token" }, false);
                throw new ValidationException("Invalid Google token.");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var email = doc.RootElement.GetProperty("email").GetString();

            return string.IsNullOrEmpty(email) ? throw new ValidationException("Google account does not have an email.") : email;
        }

        private async Task<ApplicationUser> EnsureGoogleUserExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = await CreateNewUserAsync(email);
                await _userManager.UpdateAsync(user);
            }
            return user;
        }

        private async Task<AuthResponseDto> FinalizeAuthenticationAsync(ApplicationUser user, string? code, bool isGoogleAuth)
        {
            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(code))
                {
                    return new AuthResponseDto { RequiresTwoFactor = true, Email = user.Email };
                }
                await VerifyTwoFactorCodeAsync(user, code);
            }

            var token = await GenerateJwtTokenAsync(user);
            var actionName = isGoogleAuth ? "User.GoogleLogin" : "User.Login";

            await LogAuditResultAsync(actionName, user.Id, new { Email = user.Email }, true);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                UserId = user.Id,
                RequiresTwoFactor = false
            };
        }

        private async Task VerifyTwoFactorCodeAsync(ApplicationUser user, string code)
        {
            await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                code).ValidateCodeAsync(() => "Wrong 2FA code.");
        }

        private async Task EnsureEmailIsUniqueAsync(string email)
        {
            var userExists = await _userManager.FindByEmailAsync(email);
            if (userExists != null)
            {
                await LogAuditResultAsync("User.RegisterFailed", userExists.Id, new { AttemptedEmail = email, Reason = "Email taken" }, false);
                throw new ValidationException($"User with email {email} already exists.");
            }
        }

        private async Task SendPasswordResetEmailAsync(ApplicationUser user)
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Uri.EscapeDataString(resetToken);
            var resetLink = $"http://localhost:5173/reset-password?email={user.Email}&token={encodedToken}";

            var emailBody = $@"
                <h2>Password Recovery - EasyDisk</h2>
                <p>To set a new password, click on the button below:</p>
                <a href='{resetLink}' style='padding: 10px 20px; background-color: #dc3545; color: white; border-radius: 5px; display: inline-block;'>Reset password</a>";

            await _emailSenderService.SendEmailAsync(user.Email!, "Password reset - EasyDisk", emailBody);
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("email_confirmed", user.EmailConfirmed.ToString().ToLower())
            };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:ValidIssuer"],
                audience: _configuration["JwtSettings:ValidAudience"],
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<ApplicationUser> CreateNewUserAsync(string email, string? password = null)
        {
            var user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var result = !string.IsNullOrEmpty(password)
                ? await _userManager.CreateAsync(user, password)
                : await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                await _auditService.LogAsync(
                    action: "User.CreateAccountFailed",
                    entityType: "User",
                    entityId: user?.Id,
                    details: new { AttemptedEmail = email, Reason = "Invalid credentials" },
                    isSuccess: false
                );

                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"User creation failed: {errors}");
            }

            await _auditService.LogAsync(
                action: "User.AccountCreated",
                entityType: "User",
                entityId: user?.Id,
                details: new { Email = user?.Email },
                isSuccess: true
            );

            await ProcessConfirmationEmail(user!);

            await _userManager.AddToRoleAsync(user!, "User");

            return user!;
        }

        private async Task ProcessConfirmationEmail(ApplicationUser user)
        {
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = System.Uri.EscapeDataString(emailToken);
            var confirmationLink = $"http://localhost:5173/confirm-email?userId={user.Id}&token={encodedToken}";

            var emailBody = $@"
                <h2>Welcome to EasyDisk!</h2>
                <p>To complete registration and activate your cloud storage, please confirm your email address by following the link below:</p>
                <a href='{confirmationLink}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>Confirm Email</a>
                <p>If the button doesn't work, copy this link into your browser: {confirmationLink}</p>
            ";

            await _emailSenderService.SendEmailAsync(user.Email!, "Registation confirmation - EasyDisk", emailBody);
        }

        private async Task LogAuditResultAsync(string action, string? entityId, object details, bool isSuccess)
        {
            await _auditService.LogAsync(action, "User", entityId, details, isSuccess);
        }
    }
}
