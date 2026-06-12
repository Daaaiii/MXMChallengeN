using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MxmChallenge.Data;
using MxmChallenge.Models;
using MXMChallenge.DTOs;
using MXMChallenge.Services.interfaces;

namespace MXMChallenge.Services
{
    public class FinanceSyncService(ApplicationDbContext context) : IFinanceSyncService
    {
        private static readonly string[] StateCollections =
        [
            "incomes",
            "expenses",
            "cards",
            "goals",
            "accounts",
            "investments"
        ];

        private readonly ApplicationDbContext _context = context;
        private readonly FinanceStateMerger _merger = new();

        public async Task<FinanceStateResponseDTO> GetStateAsync(Guid userId)
        {
            var snapshot = await _context.FinanceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId);

            if (snapshot == null)
            {
                return new FinanceStateResponseDTO
                {
                    Exists = false,
                    ServerVersion = 0,
                    UpdatedAt = null,
                    State = EmptyState()
                };
            }

            return ToStateResponse(snapshot, exists: true);
        }

        public async Task<FinanceOperationResult<FinanceStateResponseDTO>> SaveStateAsync(Guid userId, JsonElement state)
        {
            var validationError = ValidateState(state);
            if (validationError != null)
            {
                return FinanceOperationResult<FinanceStateResponseDTO>.Fail(validationError);
            }

            var snapshot = await SaveSnapshotAsync(userId, state.GetRawText());
            return FinanceOperationResult<FinanceStateResponseDTO>.Ok(ToStateResponse(snapshot, exists: true));
        }

        public async Task<FinanceOperationResult<FinanceSyncResponseDTO>> SyncAsync(Guid userId, FinanceSyncRequestDTO request)
        {
            var validationError = ValidateState(request.LocalState);
            if (validationError != null)
            {
                return FinanceOperationResult<FinanceSyncResponseDTO>.Fail(validationError);
            }

            var snapshot = await _context.FinanceSnapshots
                .FirstOrDefaultAsync(item => item.UserId == userId);

            if (snapshot == null)
            {
                snapshot = await SaveSnapshotAsync(userId, request.LocalState.GetRawText());
                return FinanceOperationResult<FinanceSyncResponseDTO>.Ok(ToSyncResponse("local", snapshot, []));
            }

            if (request.BaseVersion == snapshot.Version)
            {
                snapshot = await SaveSnapshotAsync(userId, request.LocalState.GetRawText(), snapshot);
                return FinanceOperationResult<FinanceSyncResponseDTO>.Ok(ToSyncResponse("local", snapshot, []));
            }

            using var remoteDocument = JsonDocument.Parse(snapshot.StateJson);
            var mergeResult = _merger.Merge(request.LocalState, remoteDocument.RootElement);
            PersistConflicts(userId, mergeResult.Conflicts);

            snapshot = await SaveSnapshotAsync(userId, mergeResult.MergedStateJson, snapshot);
            return FinanceOperationResult<FinanceSyncResponseDTO>.Ok(ToSyncResponse("merged", snapshot, mergeResult.Conflicts));
        }

        private async Task<FinanceSnapshot> SaveSnapshotAsync(Guid userId, string stateJson, FinanceSnapshot? snapshot = null)
        {
            var now = DateTime.UtcNow;
            snapshot ??= await _context.FinanceSnapshots.FirstOrDefaultAsync(item => item.UserId == userId);

            if (snapshot == null)
            {
                snapshot = new FinanceSnapshot
                {
                    UserId = userId,
                    StateJson = stateJson,
                    Version = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.FinanceSnapshots.Add(snapshot);
            }
            else
            {
                snapshot.StateJson = stateJson;
                snapshot.Version += 1;
                snapshot.UpdatedAt = now;
                _context.FinanceSnapshots.Update(snapshot);
            }

            await _context.SaveChangesAsync();
            return snapshot;
        }

        private void PersistConflicts(Guid userId, List<FinanceSyncConflictDTO> conflicts)
        {
            foreach (var conflict in conflicts)
            {
                _context.FinanceSyncConflicts.Add(new FinanceSyncConflict
                {
                    UserId = userId,
                    Entity = conflict.Entity,
                    EntityId = conflict.EntityId,
                    Field = conflict.Field,
                    LocalValueJson = conflict.LocalValue.GetRawText(),
                    RemoteValueJson = conflict.RemoteValue.GetRawText(),
                    Resolved = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private static string? ValidateState(JsonElement state)
        {
            if (state.ValueKind != JsonValueKind.Object)
            {
                return "Estado financeiro deve ser um objeto JSON.";
            }

            foreach (var collectionName in StateCollections)
            {
                if (!state.TryGetProperty(collectionName, out var collection) || collection.ValueKind != JsonValueKind.Array)
                {
                    return $"Estado financeiro deve conter a lista '{collectionName}'.";
                }

                var ids = new HashSet<string>();
                foreach (var item in collection.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        return $"Todos os itens de '{collectionName}' devem ser objetos.";
                    }

                    if (!item.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.String)
                    {
                        return $"Todos os itens de '{collectionName}' devem conter id string.";
                    }

                    var id = idProperty.GetString();
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        return $"Todos os itens de '{collectionName}' devem conter id preenchido.";
                    }

                    if (!ids.Add(id))
                    {
                        return $"ID duplicado em '{collectionName}': {id}.";
                    }
                }
            }

            return null;
        }

        private static FinanceStateResponseDTO ToStateResponse(FinanceSnapshot snapshot, bool exists)
        {
            return new FinanceStateResponseDTO
            {
                Exists = exists,
                ServerVersion = snapshot.Version,
                UpdatedAt = snapshot.UpdatedAt,
                State = ToJsonElement(snapshot.StateJson)
            };
        }

        private static FinanceSyncResponseDTO ToSyncResponse(
            string source,
            FinanceSnapshot snapshot,
            List<FinanceSyncConflictDTO> conflicts)
        {
            return new FinanceSyncResponseDTO
            {
                Source = source,
                ServerVersion = snapshot.Version,
                State = ToJsonElement(snapshot.StateJson),
                Conflicts = conflicts
            };
        }

        private static JsonElement EmptyState()
        {
            return ToJsonElement("""{"incomes":[],"expenses":[],"cards":[],"goals":[],"accounts":[],"investments":[]}""");
        }

        private static JsonElement ToJsonElement(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }
}
