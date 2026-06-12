using System.Text;
using System.Text.Json;
using MXMChallenge.DTOs;

namespace MXMChallenge.Services
{
    public class FinanceStateMerger
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

        public FinanceMergeResult Merge(JsonElement localState, JsonElement remoteState)
        {
            var conflicts = new List<FinanceSyncConflictDTO>();

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
                            var selected = ChooseItem(collectionName, id, localItem, remoteItem, conflicts);
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

            return new FinanceMergeResult(Encoding.UTF8.GetString(stream.ToArray()), conflicts);
        }

        private static JsonElement ChooseItem(
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

            RegisterConflicts(entity, entityId, localItem, remoteItem, conflicts);
            return remoteItem;
        }

        private static void RegisterConflicts(
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

                conflicts.Add(new FinanceSyncConflictDTO
                {
                    Entity = entity,
                    EntityId = entityId,
                    Field = propertyName,
                    LocalValue = localValue.ValueKind == JsonValueKind.Undefined ? NullElement() : localValue.Clone(),
                    RemoteValue = remoteValue.ValueKind == JsonValueKind.Undefined ? NullElement() : remoteValue.Clone()
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

        private static JsonElement NullElement()
        {
            using var document = JsonDocument.Parse("null");
            return document.RootElement.Clone();
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

    public record FinanceMergeResult(string MergedStateJson, List<FinanceSyncConflictDTO> Conflicts);
}
