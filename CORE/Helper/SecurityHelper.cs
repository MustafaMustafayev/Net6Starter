using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CORE.Config;
using DTO.User;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;

namespace CORE.Helper;

public class SecurityHelper
{
    private readonly ConfigSettings _configSettings;

    public SecurityHelper(ConfigSettings configSettings)
    {
        _configSettings = configSettings;
    }

    public static string GenerateSalt()
    {
        var saltBytes = new byte[16];

        using (var provider = new RNGCryptoServiceProvider())
        {
            provider.GetNonZeroBytes(saltBytes);
        }

        return Convert.ToBase64String(saltBytes);
    }

    public static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);

        var hashed = KeyDerivation.Pbkdf2(
            password,
            saltBytes,
            KeyDerivationPrf.HMACSHA512,
            100000,
            512 / 8);

        return Convert.ToBase64String(hashed);
    }

    public string CreateTokenForUser(UserToListDto userDto, DateTime expirationDate)
    {
        var claims = new List<Claim>
        {
            new(_configSettings.AuthSettings.TokenUserIdKey, userDto.UserId.ToString()),
            new(ClaimTypes.Name, userDto.Username),
            new(ClaimTypes.Expiration, expirationDate.ToString(CultureInfo.InvariantCulture))
        };
        //claims.Add(new Claim(_configSettings.AuthSettings.TokenCompanyIdKey, userDto.CompanyId.ToString()!));
        //claims.Add(new Claim(_configSettings.AuthSettings.TokenUserTypeKey, userDto.Type.ToString()));

        var key = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(_configSettings.AuthSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expirationDate,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}