using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;

namespace EasyDisk.Infrastructure.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        
        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return new AuthResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Invalid email or password."
                };
            }
            
            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                IsSuccessful = true,
                Token = token,
                Email = user.Email,
                UserId = user.Id
            };
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return new AuthResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "User with this email already exists."
                };
            }

            var user = new ApplicationUser
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return new AuthResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = $"User creation failed: {errors}"
                };
            }

            return new AuthResponseDto { IsSuccessful = true };
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string>() { _configuration["Authentication:Google:ClientId"]! }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken, settings);

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        SecurityStamp = Guid.NewGuid().ToString(),
                    };

                    var result = await _userManager.CreateAsync(user);

                    if (!result.Succeeded)
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        return new AuthResponseDto
                        {
                            IsSuccessful = false,
                            ErrorMessage = $"User creation failed: {errors}"
                        };
                    }
                }

                var token = GenerateJwtToken(user);

                return new AuthResponseDto
                {
                    IsSuccessful = true,
                    Token = token,
                    Email = user.Email,
                    UserId = user.Id
                };
            }
            catch (InvalidJwtException)
            {
                return new AuthResponseDto
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Недійсний токен Google"
                };
            }
        }
    }
}
