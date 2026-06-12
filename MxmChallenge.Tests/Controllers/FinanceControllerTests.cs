using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MxmChallenge.Controllers;
using MxmChallenge.Models;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;
using Xunit;

namespace MxmChallenge.Tests.Controllers
{
    public class FinanceControllerTests
    {
        [Fact]
        public async Task GetStateReturnsUnauthorizedWhenAuthenticatedUserCannotBeResolved()
        {
            var financeService = new RecordingFinanceSyncService();
            var controller = CreateController(financeService, new ThrowingAuthService());

            var result = await controller.GetState();

            Assert.IsType<UnauthorizedResult>(result.Result);
            Assert.Null(financeService.LastUserId);
        }

        [Fact]
        public async Task GetStateUsesAuthenticatedUserId()
        {
            var userId = Guid.NewGuid();
            var financeService = new RecordingFinanceSyncService();
            var controller = CreateController(financeService, new StaticAuthService(userId));

            var result = await controller.GetState();

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(userId, financeService.LastUserId);
        }

        [Fact]
        public async Task PutStateUsesAuthenticatedUserId()
        {
            var userId = Guid.NewGuid();
            var financeService = new RecordingFinanceSyncService();
            var controller = CreateController(financeService, new StaticAuthService(userId));
            using var state = EmptyState();

            var result = await controller.PutState(state.RootElement);

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(userId, financeService.LastUserId);
            Assert.Equal("save", financeService.LastOperation);
        }

        [Fact]
        public async Task SyncUsesAuthenticatedUserId()
        {
            var userId = Guid.NewGuid();
            var financeService = new RecordingFinanceSyncService();
            var controller = CreateController(financeService, new StaticAuthService(userId));
            using var state = EmptyState();

            var result = await controller.Sync(new FinanceSyncRequestDTO
            {
                BaseVersion = 1,
                LocalState = state.RootElement
            });

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(userId, financeService.LastUserId);
            Assert.Equal("sync", financeService.LastOperation);
        }

        [Fact]
        public async Task PutStateReturnsBadRequestWhenValidationFails()
        {
            var financeService = new RecordingFinanceSyncService { ValidationError = "Estado invalido." };
            var controller = CreateController(financeService, new StaticAuthService(Guid.NewGuid()));
            using var state = EmptyState();

            var result = await controller.PutState(state.RootElement);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Estado invalido", JsonSerializer.Serialize(badRequest.Value));
        }

        private static FinanceController CreateController(
            IFinanceSyncService financeService,
            IAuthService authService)
        {
            return new FinanceController(financeService, authService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private static JsonDocument EmptyState()
        {
            return JsonDocument.Parse("""{"incomes":[],"expenses":[],"cards":[],"goals":[],"accounts":[],"investments":[]}""");
        }

        private class RecordingFinanceSyncService : IFinanceSyncService
        {
            public Guid? LastUserId { get; private set; }
            public string? LastOperation { get; private set; }
            public string? ValidationError { get; set; }

            public Task<FinanceStateResponseDTO> GetStateAsync(Guid userId)
            {
                LastUserId = userId;
                LastOperation = "get";
                using var state = EmptyState();

                return Task.FromResult(new FinanceStateResponseDTO
                {
                    Exists = false,
                    ServerVersion = 0,
                    State = state.RootElement.Clone()
                });
            }

            public Task<FinanceOperationResult<FinanceStateResponseDTO>> SaveStateAsync(Guid userId, JsonElement state)
            {
                LastUserId = userId;
                LastOperation = "save";

                if (ValidationError != null)
                {
                    return Task.FromResult(FinanceOperationResult<FinanceStateResponseDTO>.Fail(ValidationError));
                }

                return Task.FromResult(FinanceOperationResult<FinanceStateResponseDTO>.Ok(new FinanceStateResponseDTO
                {
                    Exists = true,
                    ServerVersion = 1,
                    State = state.Clone()
                }));
            }

            public Task<FinanceOperationResult<FinanceSyncResponseDTO>> SyncAsync(Guid userId, FinanceSyncRequestDTO request)
            {
                LastUserId = userId;
                LastOperation = "sync";

                return Task.FromResult(FinanceOperationResult<FinanceSyncResponseDTO>.Ok(new FinanceSyncResponseDTO
                {
                    Source = "local",
                    ServerVersion = 1,
                    State = request.LocalState.Clone(),
                    Conflicts = []
                }));
            }
        }

        private class StaticAuthService(Guid userId) : IAuthService
        {
            public Task<bool> AuthenticateAsync(string email, string senha) => Task.FromResult(true);
            public string GenerateToken(User user) => string.Empty;
            public Task<User> FoundUserByEmail(string email) => Task.FromResult(new User { Email = email });
            public TokenReturnDTO ResponseTokenData(string token, string fullName) => new() { token = token, userName = fullName };
            public TokenInfoDTO GetTokenDateByHtppContext(HttpContext httpContext) => new() { UserId = userId };
        }

        private class ThrowingAuthService : IAuthService
        {
            public Task<bool> AuthenticateAsync(string email, string senha) => Task.FromResult(false);
            public string GenerateToken(User user) => string.Empty;
            public Task<User> FoundUserByEmail(string email) => Task.FromResult(new User { Email = email });
            public TokenReturnDTO ResponseTokenData(string token, string fullName) => new() { token = token, userName = fullName };
            public TokenInfoDTO GetTokenDateByHtppContext(HttpContext httpContext)
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}
