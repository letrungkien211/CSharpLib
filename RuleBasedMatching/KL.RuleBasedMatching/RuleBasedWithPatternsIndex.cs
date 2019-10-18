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

        /// <summary>
        /// Add matching rule
        /// </summary>
        /// <param name="matchingRuleItem"></param>
        public void Add(MatchingRuleItem matchingRuleItem)
        {
            var keywords = matchingRuleItem.KeyWords.SplitPatterns().ToList();

            switch (matchingRuleItem.Type)
            {
                case MatchingRuleType.Perfect:
                    var key = keywords.First();

                    if (!key.IsPattern())
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
                case MatchingRuleType.Pattern:
                case MatchingRuleType.Contain:
                    var indexKeyWord = keywords.First();
                    foreach (var keyword in keywords.Skip(1))
                    {
                        if (!keyword.IsPattern())
                        {
                            if (indexKeyWord.IsPattern())
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

                    if (!indexKeyWord.IsPattern())
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

            candidates = candidates.OrderBy(x => { return x.Type == MatchingRuleType.Pattern ? 0 : 1; }).ToList();

            // Further check if rule matches or not
            var matches = new List<MatchingRuleOutput>();

            foreach (var candidate in candidates)
            {
                var keywords = candidate.KeyWords.SplitPatterns().ToList();
                switch (candidate.Type)
                {
                    case MatchingRuleType.Contain:
                        var patternMatches1 = keywords.Select(keyword =>
                        {
                            if (!keyword.IsPattern())
                            {
                                return str.Contains(keyword) ? keyword : null;
                            }
                            else if (PatternToPhrases.TryGetValue(keyword, out var phrases))
                            {
                                return phrases.FirstOrDefault(x => str.Contains(x));
                            }
                            return null;
                        });
                        if (!patternMatches1.Any(x => x == null))
                        {
                            matches.Add(candidate.ToRuleOutput(patternMatches1));
                        }

                        break;
                    case MatchingRuleType.Pattern:
                        var patternMatches2 = PatternMatches(str, keywords);
                        if (patternMatches2.Any())
                            matches.Add(candidate.ToRuleOutput(patternMatches2));
                        break;
                    default:
                        break;
                }
            }
            return matches;
        }

        public IEnumerable<string> PatternMatches(string str, List<string> keywords)
        {
            var stack = new Stack<Tuple<int, int, IEnumerable<string>>>();
            stack.Push(new Tuple<int, int, IEnumerable<string>>(0, 0, new List<string>()));

            while (stack.Any())
            {
                var temp = stack.Pop();
                if (temp.Item1 >= str.Length)
                    return temp.Item3;

                if (temp.Item2 >= keywords.Count)
                    continue;

                var keyword = keywords[temp.Item2];

                if (!keyword.IsPattern())
                {

                    if (str.StartsWith(temp.Item1, keyword))
                    {
                        stack.Push(new Tuple<int, int, IEnumerable<string>>(temp.Item1 + keyword.Length, temp.Item2 + 1, temp.Item3.Append(keyword)));
                    }
                }
                else
                {

                    if (PatternToPhrases.TryGetValue(keyword, out var phrases))
                    {

                        foreach (var phrase in phrases)
                        {
                            if (str.StartsWith(temp.Item1, phrase))
                            {
                                stack.Push(new Tuple<int, int, IEnumerable<string>>(temp.Item1 + phrase.Length, temp.Item2 + 1, temp.Item3.Append(phrase)));
                            }
                        }
                    }
                }
            }
            return new List<string>();
        }
    }
}
