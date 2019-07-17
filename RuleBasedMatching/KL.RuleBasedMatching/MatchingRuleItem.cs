using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KL.RuleBasedMatching
{
    /// <summary>
    /// Rule Item
    /// </summary>
    public class MatchingRuleItem
    {
        [JsonConstructor]
        private MatchingRuleItem()
        {
        }

        private MatchingRuleItem(IEnumerable<string> keyWords, object val, MatchingRuleType matchingRuleType)
        {
            Value = val ?? throw new ArgumentNullException(nameof(keyWords));
            KeyWords = keyWords?.Where(x => !string.IsNullOrEmpty(x)).ToList() ?? throw new ArgumentNullException(nameof(keyWords));
            if (!KeyWords.Any()) throw new ArgumentException($"{nameof(keyWords)} cannot be empty", nameof(keyWords));

            Type = matchingRuleType;
        }

        /// <summary>
        /// Create new rule item
        /// </summary>
        /// <param name="keyWords">keywords</param>
        /// <param name="val">value that this rule holds</param>
        /// <param name="matchingRuleType">type of matching</param>
        /// <returns></returns>
        public static MatchingRuleItem Create(IEnumerable<string> keyWords, object val, MatchingRuleType matchingRuleType)
        {
            return new MatchingRuleItem(keyWords, val, matchingRuleType);
        }

        /// <summary>
        /// Key words for matching
        /// </summary>
        [JsonProperty]
        public List<string> KeyWords { get; private set; }

        /// <summary>
        /// Value that this rule holds
        /// </summary>
        [JsonProperty]
        public object Value { get; private set; }

        /// <summary>
        /// Matching rule type.
        /// </summary>
        [JsonProperty]
        public MatchingRuleType Type { get; private set; }
    }
}
