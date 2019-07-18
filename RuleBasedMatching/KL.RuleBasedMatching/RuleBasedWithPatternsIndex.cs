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
    internal class RuleBasedWithPatternsIndex : IRuleBasedIndex
    {
        private int MaxIndexLen { get; }
        internal IDictionary<string, List<string>> PatternToPhrases { get; set; }
        internal IDictionary<string, List<string>> PhraseToPatterns { get; set; }
        internal IDictionary<string, List<MatchingRuleItem>> RuleItems { get; } = new Dictionary<string, List<MatchingRuleItem>>();
        internal IDictionary<string, List<MatchingRuleItem>> Perfect { get; set; } = new Dictionary<string, List<MatchingRuleItem>>();

        /// <summary>
        /// Indexing matching rules
        /// </summary>
        /// <param name="maxIndexLen"></param>
        /// <param name="patternToPhrases"></param>
        public RuleBasedWithPatternsIndex(int maxIndexLen, IDictionary<string, List<string>> patternToPhrases)
        {
            MaxIndexLen = maxIndexLen;
            PatternToPhrases = patternToPhrases.ToDictionary(kv => "{" + kv.Key + "}", kv => kv.Value);
            PhraseToPatterns = new Dictionary<string, List<string>>();
            foreach (var kv in patternToPhrases)
            {
                foreach (var phrase in kv.Value)
                {
                    if (!PhraseToPatterns.TryGetValue(phrase, out var patterns))
                    {
                        patterns = new List<string>();
                        PhraseToPatterns[phrase] = patterns;
                    }
                    patterns.Add(kv.Key);
                }
            }
        }

        internal static bool IsPattern(string str)
        {
            return str.StartsWith("{") && str.EndsWith("}");
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
                    var key = matchingRuleItem.KeyWords.First();

                    if (!IsPattern(key))
                    {
                        if (!Perfect.TryGetValue(key, out var list))
                        {
                            list = new List<MatchingRuleItem>();
                            Perfect[key] = list;
                        }
                        list.Add(matchingRuleItem);
                    }
                    else if (PatternToPhrases.TryGetValue(key, out var phrases))
                    {
                        foreach (var phrase in phrases)
                        {
                            if (!Perfect.TryGetValue(phrase, out var list))
                            {
                                list = new List<MatchingRuleItem>();
                                Perfect[phrase] = list;
                            }
                            list.Add(matchingRuleItem);
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Pattern={key} is not found");
                    }
                    break;
                case MatchingRuleType.Contain:
                    var indexKeyWord = matchingRuleItem.KeyWords.First();
                    foreach (var keyword in matchingRuleItem.KeyWords.Skip(1))
                    {
                        if (!IsPattern(keyword))
                        {
                            if (IsPattern(indexKeyWord))
                            {
                                indexKeyWord = keyword;
                            }
                            else
                            {
                                if (indexKeyWord.Length < keyword.Length)
                                {
                                    indexKeyWord = keyword;
                                }
                            }
                        }
                    }

                    if (!IsPattern(indexKeyWord))
                    {
                        var indexKey = indexKeyWord.Substring(0, Math.Min(indexKeyWord.Length, MaxIndexLen));
                        if (!RuleItems.TryGetValue(indexKey, out var containRuleItems))
                        {
                            containRuleItems = new List<MatchingRuleItem>();
                            RuleItems[indexKey] = containRuleItems;
                        }
                        containRuleItems.Add(matchingRuleItem);
                    }
                    else if (PatternToPhrases.TryGetValue(indexKeyWord, out var phrases))
                    {
                        foreach (var phrase in phrases)
                        {
                            var indexKey = phrase.Substring(0, Math.Min(phrase.Length, MaxIndexLen));
                            if (!RuleItems.TryGetValue(indexKey, out var list))
                            {
                                list = new List<MatchingRuleItem>();
                                RuleItems[indexKey] = list;
                            }
                            list.Add(matchingRuleItem);
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Pattern={indexKeyWord} is not found");
                    }
                    break;
            }
        }

        /// <summary>
        /// Retrieve all rules that match this string
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>list of rule items</returns>
        public List<MatchingRuleItem> Retrieve(string str)
        {
            if (string.IsNullOrEmpty(str)) throw new ArgumentNullException(nameof(str));

            if (Perfect.TryGetValue(str, out var ret))
            {
                return ret;
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
                if (candidate.KeyWords.All(keyword =>
                {
                    if (!IsPattern(keyword))
                    {
                        return str.Contains(keyword);
                    }
                    else if(PatternToPhrases.TryGetValue(keyword, out var phrases))
                    {
                        return phrases.Any(str.Contains);
                    }
                    else
                    {
                        return false;
                    }
                }))
                {
                    matches.Add(candidate);
                }
            }

            return matches;
        }
    }
}
