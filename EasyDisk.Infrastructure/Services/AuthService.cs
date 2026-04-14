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
using EasyDisk.Application.Exceptions;

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
                throw new ValidationException("Invalid email or password.");
            }
            
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                UserId = user.Id
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                throw new ValidationException($"User with email {registerDto.Email} already exists.");
            }

            var user = await CreateNewUserAsync(registerDto.Email, registerDto.Password);

            return new AuthResponseDto {};
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
                    user = await CreateNewUserAsync(payload.Email);
                }

                var token = await GenerateJwtTokenAsync(user);

                return new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    UserId = user.Id
                };
            }
            catch (InvalidJwtException)
            {
               throw new ValidationException("Invalid Google token.");
            }
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"User creation failed: {errors}");
            }
            await _userManager.AddToRoleAsync(user, "User");
            return user;
        }
    }
}
