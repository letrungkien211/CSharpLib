using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KL.RuleBasedMatching
{
    public static class Extensions
    {
        /// Split patterns inside string
        public static IEnumerable<string> SplitPatterns(this string str)
        {
            if (str.Length <= 1)
            {
                yield return str;
            }
            else
            {

                var prevIndex = 0;
                var prevChar = str[0];

                for (var i = 1; i < str.Length; i++)
                {
                    switch (str[i])
                    {
                        case '{':
                            if (prevChar != '{')
                            {
                                yield return str.Substring(prevIndex, i - prevIndex);
                                prevIndex = i;
                                prevChar = '{';
                            }
                            break;
                        case '}':
                            if (prevIndex != -1 && prevChar == '{')
                            {
                                if (i > prevIndex)
                                {
                                    yield return str.Substring(prevIndex, i - prevIndex + 1);
                                }
                                if (i < str.Length - 1)
                                {
                                    prevIndex = i + 1;
                                    prevChar = str[i + 1];
                                }
                            }
                            break;
                        default:
                            if (i == str.Length - 1)
                            {
                                yield return str.Substring(prevIndex, i - prevIndex + 1);
                            }
                            break;
                    }
                }
            }
        }

        /// Split pattern in each string and join
        public static IEnumerable<string> SplitPatterns(this IEnumerable<string> strs)
        {
            return strs.Select(SplitPatterns).SelectMany(y => y);
        }

        public static bool IsPattern(this string str)
        {
            return str.StartsWith("{") && str.EndsWith("}");
        }

        public static bool StartsWith(this string str, int startIndex, string substr)
        {
            for (var i = 0; i < substr.Length; i++)
            {
                if (i + startIndex > str.Length - 1 || str[i + startIndex] != substr[i])
                    return false;
            }
            return true;
        }

        public static MatchingRuleOutput ToRuleOutput(this MatchingRuleItem item, IEnumerable<string> patternKeywords = null)
        {
            var ret = JsonConvert.DeserializeObject<MatchingRuleOutput>(JsonConvert.SerializeObject(item));
            ret.Matches = patternKeywords?.ToList() ?? new List<string>();
            return ret;
        }


        public static List<MatchingRuleOutput> ToRuleOutputs(this IEnumerable<MatchingRuleItem> items)
        {
            return items.Select(x => x.ToRuleOutput()).ToList();
        }
    }
}