using MxmChallenge.Models;
using MXMChallenge.DTOs;

namespace MXMChallenge.Services.interfaces
{
    public interface IAuthService
    {
        
        Task<bool> AuthenticateAsync(string email, string senha);
        public string GenerateToken(User user);
        public Task<User> FoundUserByEmail(string email);
        public TokenReturnDTO ResponseTokenData(string token, string fullName);
        public TokenInfoDTO GetTokenDateByHtppContext(HttpContext httpContext);
    }
}
