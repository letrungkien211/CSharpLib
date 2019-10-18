using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("KL.RuleBasedMatching.Tests")]

namespace KL.RuleBasedMatching
{
    /// <summary>
    /// Indexing matching rules
    /// </summary>
    internal class RuleBasedIndex : IRuleBasedIndex
    {
        private int MaxIndexLen { get; }
        internal IDictionary<string, List<MatchingRuleItem>> RuleItems { get; } = new Dictionary<string, List<MatchingRuleItem>>();
        internal IDictionary<string, List<MatchingRuleItem>> Perfect { get; set; } = new Dictionary<string, List<MatchingRuleItem>>();

        /// <summary>
        /// Indexing matching rules
        /// </summary>
        /// <param name="maxIndexLen"></param>
        public RuleBasedIndex(int maxIndexLen)
        {
            MaxIndexLen = maxIndexLen;
        }

        /// <summary>
        /// Add matching rule
        /// </summary>
        /// <param name="matchingRuleItem"></param>
        public void Add(MatchingRuleItem matchingRuleItem)
        {
            switch (matchingRuleItem.Type)
            {
                case MatchingRuleType.Perfect:
                    var key = matchingRuleItem.KeyWords[0];
                    if (!Perfect.TryGetValue(key, out var list))
                    {
                        list = new List<MatchingRuleItem>();
                        Perfect[key] = list;
                    }
                    list.Add(matchingRuleItem);
                    break;
                case MatchingRuleType.Contain:
                    var indexKeyWord = "";
                    foreach (var keyword in matchingRuleItem.KeyWords)
                    {
                        if (indexKeyWord.Length < keyword.Length)
                        {
                            indexKeyWord = keyword;
                        }
                    }
                    var indexKey = indexKeyWord.Substring(0, Math.Min(indexKeyWord.Length, MaxIndexLen));
                    if (!RuleItems.TryGetValue(indexKey, out var containRuleItems))
                    {
                        containRuleItems = new List<MatchingRuleItem>();
                        RuleItems[indexKey] = containRuleItems;
                    }
                    containRuleItems.Add(matchingRuleItem);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Retrieve all rules that match this string
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>list of rule items</returns>
        public List<MatchingRuleOutput> Retrieve(string str)
        {
            if (string.IsNullOrEmpty(str)) throw new ArgumentNullException(nameof(str));

            if (Perfect.TryGetValue(str, out var ret))
            {
                return ret.ToRuleOutputs();
            }

            // Fetch rule candidates
            var candidates = new List<MatchingRuleItem>();
            for (var i = 0; i < str.Length; i++)
            {
                for (var j = 0; j < Math.Min(MaxIndexLen, str.Length - i); j++)
                {
                    var key = str.Substring(i, j + 1);
                    if (RuleItems.TryGetValue(key, out var rules))
                    {
                        candidates.AddRange(rules);
                    }
                }
            }

            // Further check if rule matches or not
            var matches = new List<MatchingRuleItem>();
            foreach (var candidate in candidates)
            {
                if (candidate.KeyWords.All(str.Contains))
                {
                    matches.Add(candidate);
                }
            }

            return matches.ToRuleOutputs();
        }
    }
}
