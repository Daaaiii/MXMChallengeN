using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MxmChallenge.Data;
using MxmChallenge.Models;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MXMChallenge.Services
{
    public class AuthService: IAuthService
    {
       private ApplicationDbContext _context;
        private IHashService _hashService;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IHashService hashService, IConfiguration configuration)
        {
            _context = context;
            _hashService = hashService;
            _configuration = configuration;
        }

        public async Task<bool> AuthenticateAsync(string email, string senha)
        {
            string hashPassword = _hashService.HashPassword(senha);

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == hashPassword);
            if (user != null)
            {
                return true;
            }
            return false;
        }


        public string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var privateKey = new SymmetricSecurityKey(Encoding.UTF8.
            GetBytes(_configuration["jwt:secretKey"]!));

            var credentials = new SigningCredentials(privateKey, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddMinutes(15).AddDays(2);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _configuration["jwt:issuer"],
                audience: _configuration["jwt:audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User> FoundUserByEmail(string email)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new Exception("Email não cadastrado");
            }
            return user;
        }

        public TokenReturnDTO ResponseTokenData(string token, string userName)
        {
            TokenReturnDTO responseUserData = new()
            {
                token = token,
                userName = userName
            };
            return responseUserData;
        }

        public TokenInfoDTO GetTokenDateByHtppContext(HttpContext httpContext)
        {
            string token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last() ?? throw new Exception("Não autorizado");



            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            var claims = jwtToken!.Claims;


            var tokenInfo = new TokenInfoDTO();

            var userIdClaim = claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type == "nameidentifier" ||
                c.Type == "id")?.Value;
            var emailClaim = claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Email ||
                c.Type == JwtRegisteredClaimNames.Email ||
                c.Type == "email")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                tokenInfo.UserId = userId;
            }
            else if (!string.IsNullOrWhiteSpace(emailClaim))
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == emailClaim);
                tokenInfo.UserId = user?.Id ?? Guid.Empty;
            }

            if (tokenInfo.UserId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Token sem usuario valido.");
            }

            tokenInfo.Email = emailClaim ?? string.Empty;


            return tokenInfo;

        }
    }
}
