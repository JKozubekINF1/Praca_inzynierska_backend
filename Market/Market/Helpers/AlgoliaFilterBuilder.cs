namespace Market.Helpers
{
    public class AlgoliaFilterBuilder
    {
        private readonly List<string> _filters = new();

        public AlgoliaFilterBuilder AddFacet(string field, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _filters.Add($"{field}:'{value}'");
            }
            return this;
        }

        public AlgoliaFilterBuilder AddRange(string field, decimal? min, decimal? max)
        {
            if (min.HasValue) _filters.Add($"{field} >= {min.Value}");
            if (max.HasValue) _filters.Add($"{field} <= {max.Value}");
            return this;
        }

        public AlgoliaFilterBuilder AddRange(string field, int? min, int? max)
        {
            if (min.HasValue) _filters.Add($"{field} >= {min.Value}");
            if (max.HasValue) _filters.Add($"{field} <= {max.Value}");
            return this;
        }

        public AlgoliaFilterBuilder AddCondition(string condition)
        {
            if (!string.IsNullOrWhiteSpace(condition))
            {
                _filters.Add(condition);
            }
            return this;
        }

        public AlgoliaFilterBuilder AddExpirationCheck()
        {
            long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _filters.Add($"expiresAt > {nowTimestamp}");
            return this;
        }

        public string Build()
        {
            return string.Join(" AND ", _filters);
        }
    }
}