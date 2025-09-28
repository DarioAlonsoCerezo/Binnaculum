using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Service for discovering and managing test scenarios with metadata and tagging support
    /// </summary>
    public class TestDiscoveryService
    {
        private readonly Dictionary<string, TestScenario> _registeredTests = new();
        private readonly Dictionary<string, List<string>> _taggedTests = new();
        
        /// <summary>
        /// Register a test scenario for discovery
        /// </summary>
        public void RegisterTest(TestScenario scenario)
        {
            _registeredTests[scenario.Name] = scenario;
            
            // Index by tags for fast lookup
            foreach (var tag in scenario.Tags)
            {
                if (!_taggedTests.ContainsKey(tag))
                    _taggedTests[tag] = new List<string>();
                _taggedTests[tag].Add(scenario.Name);
            }
        }

        /// <summary>
        /// Register a test using the fluent builder pattern
        /// </summary>
        public void RegisterTest(Func<TestScenarioBuilder> builderFactory)
        {
            var scenario = builderFactory().Build();
            RegisterTest(scenario);
        }

        /// <summary>
        /// Get all registered test scenarios
        /// </summary>
        public IEnumerable<TestScenario> GetAllTests()
        {
            return _registeredTests.Values;
        }

        /// <summary>
        /// Get test scenarios by name
        /// </summary>
        public TestScenario? GetTestByName(string name)
        {
            _registeredTests.TryGetValue(name, out var scenario);
            return scenario;
        }

        /// <summary>
        /// Get test scenarios by tag
        /// </summary>
        public IEnumerable<TestScenario> GetTestsByTag(string tag)
        {
            if (!_taggedTests.TryGetValue(tag, out var testNames))
                return Enumerable.Empty<TestScenario>();

            return testNames.Where(name => _registeredTests.ContainsKey(name))
                           .Select(name => _registeredTests[name]);
        }

        /// <summary>
        /// Get test scenarios by multiple tags (AND logic - must have all tags)
        /// </summary>
        public IEnumerable<TestScenario> GetTestsByTags(params string[] tags)
        {
            if (tags.Length == 0) return GetAllTests();
            
            return GetAllTests().Where(test => tags.All(tag => test.Tags.Contains(tag)));
        }

        /// <summary>
        /// Get test scenarios by any of the provided tags (OR logic - must have at least one tag)
        /// </summary>
        public IEnumerable<TestScenario> GetTestsByAnyTag(params string[] tags)
        {
            if (tags.Length == 0) return GetAllTests();
            
            return GetAllTests().Where(test => tags.Any(tag => test.Tags.Contains(tag)));
        }

        /// <summary>
        /// Get all unique tags used by registered tests
        /// </summary>
        public IEnumerable<string> GetAllTags()
        {
            return _taggedTests.Keys;
        }

        /// <summary>
        /// Get test scenarios filtered by name pattern (supports wildcards)
        /// </summary>
        public IEnumerable<TestScenario> GetTestsByNamePattern(string pattern)
        {
            // Simple wildcard support (* and ?)
            var regexPattern = "^" + pattern.Replace("*", ".*").Replace("?", ".") + "$";
            var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return _registeredTests.Values.Where(test => regex.IsMatch(test.Name));
        }

        /// <summary>
        /// Get test execution summary grouped by tags
        /// </summary>
        public Dictionary<string, TestExecutionSummary> GetTestSummaryByTag()
        {
            var summary = new Dictionary<string, TestExecutionSummary>();

            foreach (var tag in GetAllTags())
            {
                var testsWithTag = GetTestsByTag(tag).ToList();
                summary[tag] = new TestExecutionSummary
                {
                    Tag = tag,
                    TotalTests = testsWithTag.Count,
                    TestNames = testsWithTag.Select(t => t.Name).ToList()
                };
            }

            return summary;
        }

        /// <summary>
        /// Clear all registered tests
        /// </summary>
        public void Clear()
        {
            _registeredTests.Clear();
            _taggedTests.Clear();
        }

        /// <summary>
        /// Get count of registered tests
        /// </summary>
        public int TestCount => _registeredTests.Count;

        /// <summary>
        /// Check if a test is registered
        /// </summary>
        public bool IsTestRegistered(string name) => _registeredTests.ContainsKey(name);
    }

    /// <summary>
    /// Summary information for tests grouped by tag
    /// </summary>
    public class TestExecutionSummary
    {
        public string Tag { get; set; } = "";
        public int TotalTests { get; set; }
        public List<string> TestNames { get; set; } = new();
    }

    /// <summary>
    /// Common test tags used throughout the system
    /// </summary>
    public static class TestTags
    {
        public const string Overview = "overview";
        public const string BrokerAccount = "broker-account";
        public const string Database = "database";
        public const string Collection = "collection";
        public const string Financial = "financial";
        public const string Movement = "movement";
        public const string Verification = "verification";
        public const string Setup = "setup";
        public const string Integration = "integration";
        public const string Performance = "performance";
        public const string Smoke = "smoke";
        public const string Regression = "regression";
        public const string Import = "import";
        public const string Options = "options";
        public const string SPX = "spx";
    }
}