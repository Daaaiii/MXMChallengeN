using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;

namespace MxmChallenge.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        /// <summary>
        /// Endpoint utilizado para autenticar um usuário.
        /// </summary>
        /// <param name="data">Dados de entrada do usuário (email e senha).</param>
        /// <returns>Retorna um token de autenticação em caso de sucesso, ou mensagens de erro em caso de falha na autenticação.</returns>
        [AllowAnonymous]
        [HttpPost()]
        public async Task<ActionResult> Authenticate([FromBody] UserRequestDTO loginData)
        {
            if (loginData == null)
            {
                return BadRequest("Não foi possível completar o login pois existem parâmetros nulos.");
            }

            var user = await _authService.FoundUserByEmail(loginData.email);

            if (user == null)
            {
                return NotFound("Usuário não encontrado");
            }

            bool isAuthenticated = await _authService.AuthenticateAsync(loginData.email, loginData.password);

            if (!isAuthenticated)
            {
                return Unauthorized("Credenciais inválidas.");
            }

            var token = _authService.GenerateToken(user);
            var tokenInfoDTO = new TokenInfoDTO
            {
                UserId = user.Id,
                Email = user.Email,
                Fullname = user.Fullname,
                Token = token
            };


            return Ok(tokenInfoDTO);
        }
    }
}
