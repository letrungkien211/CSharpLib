using System.Collections.Generic;
using System.Linq;

namespace KL.RuleBasedMatching
{
    public class MatchingRuleOutput : MatchingRuleItem
    {
        public MatchingRuleOutput() { }
        /// Matched pattern keywords in order
        /// E.g: {Thank}. {Hello} --> If match, PatternKeywords: [0] is the keyword that matches Thank, [1] is the keyword that matches Hello
        public List<string> Matches { get; set; } = new List<string>();

        public IEnumerable<string> GetRegexMatches()
        {
            var keywords = KeyWords.SplitPatterns().ToList();
            for (var i = 0; i < Matches.Count; i++)
            {
                if (keywords[i].IsPattern())
                {
                    yield return Matches[i];
                }
            }
        }
    }
}