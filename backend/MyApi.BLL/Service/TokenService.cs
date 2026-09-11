using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApi.DAL.Models;

namespace MyApi.BLL.Service;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    public TokenService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }
    public async Task<string> GenerateAccessToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        //payload - Claims
        var UserClaims = new List<Claim>()
            {

                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Role,string.Join(',',roles))
            };
        foreach (var role in roles)
        {
            UserClaims.Add(new Claim(ClaimTypes.Role, role));
        }
        // we need secret key to check
        var secret = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: UserClaims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
{
    var secret = _configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");

    var tokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidAudience = _configuration["Jwt:Audience"],

        ValidateIssuer = true,
        ValidIssuer = _configuration["Jwt:Issuer"],

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),

        ValidateLifetime = false
    };

    var tokenHandler = new JwtSecurityTokenHandler();

    var principal = tokenHandler.ValidateToken(
        token,
        tokenValidationParameters,
        out SecurityToken securityToken
    );

    if (securityToken is not JwtSecurityToken jwtSecurityToken ||
        !jwtSecurityToken.Header.Alg.Equals(
            SecurityAlgorithms.HmacSha256,
            StringComparison.InvariantCultureIgnoreCase))
    {
        throw new SecurityTokenException("Invalid token");
    }

    return principal;
}
}
