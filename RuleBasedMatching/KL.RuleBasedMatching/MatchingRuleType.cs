using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KL.RuleBasedMatching
{
    /// <summary>
    /// Matching rule type. Perfect or Contain
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchingRuleType
    {
        /// <summary>
        /// Perfectly match a keyword
        /// </summary>
        Perfect = 0,

        /// <summary>
        /// Contain keywords
        /// </summary>
        Contain = 1,

        /// <summary>
        /// Perfectly match a keyword with patterns supported
        /// </summary>
        Pattern = 2
    }
}