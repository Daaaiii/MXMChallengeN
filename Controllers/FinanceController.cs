using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;

namespace MxmChallenge.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/finance")]
    public class FinanceController : ControllerBase
    {
        private readonly IFinanceSyncService _financeSyncService;
        private readonly IAuthService _authService;

        public FinanceController(IFinanceSyncService financeSyncService, IAuthService authService)
        {
            _financeSyncService = financeSyncService;
            _authService = authService;
        }

        /// <summary>
        /// Retorna o snapshot financeiro do usuario autenticado.
        /// </summary>
        [HttpGet("state")]
        public async Task<ActionResult<FinanceStateResponseDTO>> GetState()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            return Ok(await _financeSyncService.GetStateAsync(userId));
        }

        /// <summary>
        /// Sobrescreve o snapshot financeiro completo do usuario autenticado.
        /// </summary>
        [HttpPut("state")]
        public async Task<ActionResult<FinanceStateResponseDTO>> PutState([FromBody] JsonElement state)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _financeSyncService.SaveStateAsync(userId, state);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Sincroniza o estado local com o snapshot remoto do usuario autenticado.
        /// </summary>
        [HttpPost("sync")]
        public async Task<ActionResult<FinanceSyncResponseDTO>> Sync([FromBody] FinanceSyncRequestDTO request)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized();
            }

            var result = await _financeSyncService.SyncAsync(userId, request);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Error });
            }

            return Ok(result.Value);
        }

        private Guid GetAuthenticatedUserId()
        {
            try
            {
                return _authService.GetTokenDateByHtppContext(HttpContext).UserId;
            }
            catch
            {
                return Guid.Empty;
            }
        }
    }
}
