using FinanceManager.UserAccounts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinanceManager.Crosscutting.Auth
{
    public interface ITokenProviderService
    {
        string GenerateToken(UserAccount userAccount);
        string GeneratePermanentToken(UserAccount userAccount);
        string GenerateRefreshToken();

        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public class TokenProviderService : ITokenProviderService
    {
        private readonly BearerAuthenticationOptions _authenticationOptions;

        public TokenProviderService(IOptions<BearerAuthenticationOptions> authOptions)
        {
            _authenticationOptions = authOptions.Value;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string GenerateToken(UserAccount userAccount)
        {
            var utcNow = DateTime.UtcNow;

            var claims = new Claim[]
            {
                new(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, userAccount.Name),
                new(JwtRegisteredClaimNames.Email, userAccount.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(utcNow).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                new ("Role", userAccount.Role)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationOptions.Secret));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims,
                notBefore: utcNow,
                expires: utcNow.AddYears(1),
                // expires: utcNow.AddSeconds(_authenticationOptions.ExpiresInSeconds),
                audience: _authenticationOptions.Audience,
                issuer: _authenticationOptions.Issuer
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public string GeneratePermanentToken(UserAccount userAccount)
        {
            var utcNow = DateTime.UtcNow;

            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, userAccount.Name),
                new Claim(JwtRegisteredClaimNames.Email, userAccount.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, utcNow.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationOptions.Secret));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims,
                notBefore: utcNow,
                //expires: utcNow.AddSeconds(_authenticationOptions.ExpiresInSeconds),
                expires: utcNow.AddYears(10),
                audience: _authenticationOptions.Audience,
                issuer: _authenticationOptions.Issuer
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationOptions.Secret)),
                ValidIssuer = _authenticationOptions.Issuer,
                ValidAudience = _authenticationOptions.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
