using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MxmChallenge.Models;
using MXMChallenge.Services;
using Xunit;

namespace MxmChallenge.Tests.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public void GenerateTokenIncludesFinanceCompatibleUserClaims()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "finance-token@example.com",
                Fullname = "Finance Token User",
                Password = "hash",
                cpf_cnpj = "12345678901"
            };
            var service = CreateService();

            var token = service.GenerateToken(user);
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Contains(jwtToken.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, claim => claim.Type == "id" && claim.Value == user.Id.ToString());
            Assert.Contains(jwtToken.Claims, claim => claim.Type == "email" && claim.Value == user.Email);
            Assert.Contains(jwtToken.Claims, claim => claim.Type == ClaimTypes.Email && claim.Value == user.Email);
        }

        [Fact]
        public void GetTokenDataReadsUserIdFromSubClaim()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "finance-sub@example.com",
                Fullname = "Finance Sub User",
                Password = "hash",
                cpf_cnpj = "12345678902"
            };
            var service = CreateService();
            var context = ContextWithBearerToken(service.GenerateToken(user));

            var tokenInfo = service.GetTokenDateByHtppContext(context);

            Assert.Equal(user.Id, tokenInfo.UserId);
            Assert.Equal(user.Email, tokenInfo.Email);
        }

        [Fact]
        public void GetTokenDataReadsUserIdFromLegacyIdClaim()
        {
            var userId = Guid.NewGuid();
            var service = CreateService();
            var token = CreateToken([
                new Claim("id", userId.ToString()),
                new Claim("email", "legacy-id@example.com")
            ]);
            var context = ContextWithBearerToken(token);

            var tokenInfo = service.GetTokenDateByHtppContext(context);

            Assert.Equal(userId, tokenInfo.UserId);
            Assert.Equal("legacy-id@example.com", tokenInfo.Email);
        }

        private static AuthService CreateService()
        {
            return new AuthService(null!, null!, CreateConfiguration());
        }

        private static IConfiguration CreateConfiguration()
        {
            var configuration = new ConfigurationManager();
            configuration["jwt:issuer"] = "mxm-tests";
            configuration["jwt:audience"] = "mxm-tests";
            configuration["jwt:secretKey"] = "0123456789ABCDEF0123456789ABCDEF";

            return configuration;
        }

        private static DefaultHttpContext ContextWithBearerToken(string token)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Authorization = $"Bearer {token}";

            return context;
        }

        private static string CreateToken(IEnumerable<Claim> claims)
        {
            var configuration = CreateConfiguration();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwt:secretKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["jwt:issuer"],
                audience: configuration["jwt:audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
