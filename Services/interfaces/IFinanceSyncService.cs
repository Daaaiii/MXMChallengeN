using System.Text.Json;
using MXMChallenge.DTOs;

namespace MXMChallenge.Services.interfaces
{
    public interface IFinanceSyncService
    {
        Task<FinanceStateResponseDTO> GetStateAsync(Guid userId);
        Task<FinanceOperationResult<FinanceStateResponseDTO>> SaveStateAsync(Guid userId, JsonElement state);
        Task<FinanceOperationResult<FinanceSyncResponseDTO>> SyncAsync(Guid userId, FinanceSyncRequestDTO request);
    }

    public class FinanceOperationResult<T>
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public T? Value { get; set; }

        public static FinanceOperationResult<T> Ok(T value)
        {
            return new FinanceOperationResult<T> { Success = true, Value = value };
        }

        public static FinanceOperationResult<T> Fail(string error)
        {
            return new FinanceOperationResult<T> { Success = false, Error = error };
        }
    }
}
