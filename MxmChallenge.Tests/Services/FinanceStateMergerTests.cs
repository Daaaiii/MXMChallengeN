using System.Text.Json;
using MXMChallenge.Services;
using Xunit;

namespace MxmChallenge.Tests.Services
{
    public class FinanceStateMergerTests
    {
        private readonly FinanceStateMerger _merger = new();

        [Fact]
        public void MergePreservesLocalAndRemoteOnlyItems()
        {
            using var local = ParseState("""
            {
              "incomes": [{ "id": "income-local", "amount": 100 }],
              "expenses": [],
              "cards": [],
              "goals": [],
              "accounts": [],
              "investments": []
            }
            """);
            using var remote = ParseState("""
            {
              "incomes": [],
              "expenses": [{ "id": "expense-remote", "amount": 50 }],
              "cards": [],
              "goals": [],
              "accounts": [],
              "investments": []
            }
            """);

            var result = _merger.Merge(local.RootElement, remote.RootElement);
            using var merged = JsonDocument.Parse(result.MergedStateJson);

            Assert.Equal("income-local", merged.RootElement.GetProperty("incomes")[0].GetProperty("id").GetString());
            Assert.Equal("expense-remote", merged.RootElement.GetProperty("expenses")[0].GetProperty("id").GetString());
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void MergeChoosesHigherVersionWhenVersionsDiffer()
        {
            using var local = StateWithIncome("""{ "id": "income-1", "amount": 200, "version": 3, "updatedAt": "2026-06-12T10:00:00Z" }""");
            using var remote = StateWithIncome("""{ "id": "income-1", "amount": 100, "version": 2, "updatedAt": "2026-06-12T11:00:00Z" }""");

            var result = _merger.Merge(local.RootElement, remote.RootElement);
            using var merged = JsonDocument.Parse(result.MergedStateJson);

            Assert.Equal(200, merged.RootElement.GetProperty("incomes")[0].GetProperty("amount").GetInt32());
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void MergeChoosesNewestUpdatedAtWhenVersionsTie()
        {
            using var local = StateWithIncome("""{ "id": "income-1", "amount": 100, "version": 2, "updatedAt": "2026-06-12T10:00:00Z" }""");
            using var remote = StateWithIncome("""{ "id": "income-1", "amount": 150, "version": 2, "updatedAt": "2026-06-12T11:00:00Z" }""");

            var result = _merger.Merge(local.RootElement, remote.RootElement);
            using var merged = JsonDocument.Parse(result.MergedStateJson);

            Assert.Equal(150, merged.RootElement.GetProperty("incomes")[0].GetProperty("amount").GetInt32());
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void MergeChoosesNewestDeletedAtDecision()
        {
            using var local = StateWithIncome("""{ "id": "income-1", "amount": 100, "updatedAt": "2026-06-12T10:00:00Z" }""");
            using var remote = StateWithIncome("""{ "id": "income-1", "amount": 100, "deletedAt": "2026-06-12T11:00:00Z" }""");

            var result = _merger.Merge(local.RootElement, remote.RootElement);
            using var merged = JsonDocument.Parse(result.MergedStateJson);

            Assert.True(merged.RootElement.GetProperty("incomes")[0].TryGetProperty("deletedAt", out _));
            Assert.Empty(result.Conflicts);
        }

        [Fact]
        public void MergeFallsBackToRemoteAndRecordsConflictsWhenMetadataCannotDecide()
        {
            using var local = StateWithIncome("""{ "id": "income-1", "amount": 200, "description": "Local" }""");
            using var remote = StateWithIncome("""{ "id": "income-1", "amount": 100, "description": "Remote" }""");

            var result = _merger.Merge(local.RootElement, remote.RootElement);
            using var merged = JsonDocument.Parse(result.MergedStateJson);

            Assert.Equal(100, merged.RootElement.GetProperty("incomes")[0].GetProperty("amount").GetInt32());
            Assert.Contains(result.Conflicts, conflict => conflict.Field == "amount");
            Assert.Contains(result.Conflicts, conflict => conflict.Field == "description");
        }

        private static JsonDocument StateWithIncome(string incomeJson)
        {
            return ParseState($$"""
            {
              "incomes": [{{incomeJson}}],
              "expenses": [],
              "cards": [],
              "goals": [],
              "accounts": [],
              "investments": []
            }
            """);
        }

        private static JsonDocument ParseState(string json)
        {
            return JsonDocument.Parse(json);
        }
    }
}
