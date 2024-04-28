using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MXMChallenge.DTOs;
using MxmChallenge.Repositories.Interfaces;
using MXMChallenge.Services.interfaces;

namespace MxmChallenge.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;

        public UserController(IUserRepository userRepository, IAuthService authService)
        {
            _userRepository = userRepository;
            _authService = authService;

        }

        /// <summary>
        /// Endpoint utilizado para criar um novo usuário.
        /// </summary>
        /// <param name="createUserDTO">Dados necessários para criar um novo usuário.</param>
        /// <returns>Retorna um objeto ActionResult contendo um CreateUserDTO em caso de sucesso, ou uma mensagem de erro em caso de falha na criação do usuário.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<CreateUserDTO>> CreateUser(CreateUserDTO createUserDTO)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var user = await _userRepository.CreateUser(createUserDTO);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message }); //modificando retorno pra voltar um objeto
            }
        }


        /// <summary>
        /// Endpoint utilizado para obter detalhes de um usuário pelo seu ID.
        /// </summary>
        /// <returns>Retorna um objeto ActionResult contendo os detalhes do usuário em caso de sucesso, ou uma mensagem de erro em caso de usuário não encontrado.</returns>
        [HttpGet()]
        public async Task<ActionResult<UserDetailsDTO>> GetUser()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid id = _authService.GetTokenDateByHtppContext(HttpContext).UserId;

            if(id == Guid.Empty)
            {
                return BadRequest("User not found.");
            }

            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok(user);
        }



        /// <summary>
        /// Endpoint utilizado para atualizar informações de um usuário autenticado.
        /// </summary>
        /// <param name="updateUserDTO">Dados necessários para atualizar as informações do usuário.</param>
        /// <returns>Retorna um objeto ActionResult contendo os detalhes atualizados do usuário em caso de sucesso, ou uma mensagem de erro em caso de falha na atualização.</returns>
        [Authorize]
        [HttpPut()]
        public async Task<ActionResult<UserDetailsDTO>> UpdateUser(
            UpdateUserDTO updateUserDTO
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid id = _authService.GetTokenDateByHtppContext(HttpContext).UserId;
            if (id != updateUserDTO.UserId)
            {
                return BadRequest("User not found.");
            }

            try
            {
                var updatedUser = await _userRepository.UpdateUser(id, updateUserDTO);
                if (updatedUser == null)
                {
                    return NotFound("User not found.");
                }
                return CreatedAtAction(nameof(GetUser), new { id = updatedUser.Id }, updatedUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Endpoint utilizado para excluir um usuário autenticado.
        /// </summary>
        /// <returns>Retorna um objeto ActionResult indicando o resultado da operação de exclusão. Em caso de sucesso, retorna um status 200 OK com uma mensagem indicando que o usuário foi excluído com sucesso. Em caso de falha, retorna um status 404 Not Found com uma mensagem indicando que o usuário não foi encontrado.</returns>
        [Authorize]
        [HttpDelete()]
        public async Task<ActionResult> DeleteUser()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid id = _authService.GetTokenDateByHtppContext(HttpContext).UserId;

            var user = await _userRepository.DeleteUser(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok("User deleted successfully");
        }

    }
}
