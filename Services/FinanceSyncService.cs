using System.Text;
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
            var conflicts = new List<FinanceSyncConflictDTO>();
            var mergedStateJson = MergeStates(userId, request.LocalState, remoteDocument.RootElement, conflicts);

            snapshot = await SaveSnapshotAsync(userId, mergedStateJson, snapshot);
            return FinanceOperationResult<FinanceSyncResponseDTO>.Ok(ToSyncResponse("merged", snapshot, conflicts));
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

        private string MergeStates(Guid userId, JsonElement localState, JsonElement remoteState, List<FinanceSyncConflictDTO> conflicts)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();

                foreach (var collectionName in StateCollections)
                {
                    writer.WritePropertyName(collectionName);
                    writer.WriteStartArray();

                    var localItems = IndexById(localState.GetProperty(collectionName));
                    var remoteItems = IndexById(remoteState.GetProperty(collectionName));
                    var ids = localItems.Keys.Union(remoteItems.Keys).OrderBy(id => id, StringComparer.Ordinal);

                    foreach (var id in ids)
                    {
                        var hasLocal = localItems.TryGetValue(id, out var localItem);
                        var hasRemote = remoteItems.TryGetValue(id, out var remoteItem);

                        if (hasLocal && hasRemote)
                        {
                            var selected = ChooseItem(userId, collectionName, id, localItem, remoteItem, conflicts);
                            selected.WriteTo(writer);
                        }
                        else if (hasLocal)
                        {
                            localItem.WriteTo(writer);
                        }
                        else if (hasRemote)
                        {
                            remoteItem.WriteTo(writer);
                        }
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private JsonElement ChooseItem(
            Guid userId,
            string entity,
            string entityId,
            JsonElement localItem,
            JsonElement remoteItem,
            List<FinanceSyncConflictDTO> conflicts)
        {
            var localDeletedAt = GetDateTime(localItem, "deletedAt");
            var remoteDeletedAt = GetDateTime(remoteItem, "deletedAt");
            var localUpdatedAt = GetDateTime(localItem, "updatedAt");
            var remoteUpdatedAt = GetDateTime(remoteItem, "updatedAt");

            if (localDeletedAt.HasValue || remoteDeletedAt.HasValue)
            {
                var localDecisionAt = localDeletedAt ?? localUpdatedAt;
                var remoteDecisionAt = remoteDeletedAt ?? remoteUpdatedAt;

                if (localDecisionAt.HasValue && remoteDecisionAt.HasValue && localDecisionAt.Value != remoteDecisionAt.Value)
                {
                    return localDecisionAt.Value > remoteDecisionAt.Value ? localItem : remoteItem;
                }

                if (localDeletedAt.HasValue && !remoteDeletedAt.HasValue)
                {
                    return localItem;
                }

                if (remoteDeletedAt.HasValue && !localDeletedAt.HasValue)
                {
                    return remoteItem;
                }
            }

            var localVersion = GetInt(localItem, "version");
            var remoteVersion = GetInt(remoteItem, "version");
            if (localVersion.HasValue && remoteVersion.HasValue && localVersion.Value != remoteVersion.Value)
            {
                return localVersion.Value > remoteVersion.Value ? localItem : remoteItem;
            }

            if (localUpdatedAt.HasValue && remoteUpdatedAt.HasValue && localUpdatedAt.Value != remoteUpdatedAt.Value)
            {
                return localUpdatedAt.Value > remoteUpdatedAt.Value ? localItem : remoteItem;
            }

            if (JsonEquals(localItem, remoteItem))
            {
                return remoteItem;
            }

            RegisterConflicts(userId, entity, entityId, localItem, remoteItem, conflicts);
            return remoteItem;
        }

        private void RegisterConflicts(
            Guid userId,
            string entity,
            string entityId,
            JsonElement localItem,
            JsonElement remoteItem,
            List<FinanceSyncConflictDTO> conflicts)
        {
            var localProperties = localItem.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.Clone());
            var remoteProperties = remoteItem.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.Clone());
            var propertyNames = localProperties.Keys.Union(remoteProperties.Keys).OrderBy(name => name, StringComparer.Ordinal);

            foreach (var propertyName in propertyNames)
            {
                localProperties.TryGetValue(propertyName, out var localValue);
                remoteProperties.TryGetValue(propertyName, out var remoteValue);

                if (JsonEquals(localValue, remoteValue))
                {
                    continue;
                }

                var localResponseValue = localValue.ValueKind == JsonValueKind.Undefined ? NullElement() : localValue.Clone();
                var remoteResponseValue = remoteValue.ValueKind == JsonValueKind.Undefined ? NullElement() : remoteValue.Clone();

                conflicts.Add(new FinanceSyncConflictDTO
                {
                    Entity = entity,
                    EntityId = entityId,
                    Field = propertyName,
                    LocalValue = localResponseValue,
                    RemoteValue = remoteResponseValue
                });

                _context.FinanceSyncConflicts.Add(new FinanceSyncConflict
                {
                    UserId = userId,
                    Entity = entity,
                    EntityId = entityId,
                    Field = propertyName,
                    LocalValueJson = localValue.ValueKind == JsonValueKind.Undefined ? "null" : localValue.GetRawText(),
                    RemoteValueJson = remoteValue.ValueKind == JsonValueKind.Undefined ? "null" : remoteValue.GetRawText(),
                    Resolved = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private static Dictionary<string, JsonElement> IndexById(JsonElement collection)
        {
            var result = new Dictionary<string, JsonElement>();
            foreach (var item in collection.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("id", out var idProperty) &&
                    idProperty.ValueKind == JsonValueKind.String)
                {
                    var id = idProperty.GetString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        result[id] = item.Clone();
                    }
                }
            }

            return result;
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

        private static JsonElement NullElement()
        {
            return ToJsonElement("null");
        }

        private static JsonElement ToJsonElement(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static DateTime? GetDateTime(JsonElement item, string propertyName)
        {
            if (!item.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return DateTime.TryParse(property.GetString(), out var value) ? value.ToUniversalTime() : null;
        }

        private static int? GetInt(JsonElement item, string propertyName)
        {
            if (!item.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            return property.TryGetInt32(out var value) ? value : null;
        }

        private static bool JsonEquals(JsonElement left, JsonElement right)
        {
            if (left.ValueKind == JsonValueKind.Undefined && right.ValueKind == JsonValueKind.Undefined)
            {
                return true;
            }

            if (left.ValueKind == JsonValueKind.Undefined || right.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            return left.GetRawText() == right.GetRawText();
        }
    }
}
