using System.Collections.Generic;

namespace KL.RuleBasedMatching
{
    /// <summary>
    /// Rule based index for matching
    /// </summary>
    public interface IRuleBasedIndex
    {
        /// <summary>
        /// Add a matching rule
        /// </summary>
        /// <param name="matchingRuleItem"></param>
        void Add(MatchingRuleItem matchingRuleItem);

        /// <summary>
        /// Retrieve a list of matching rule for a given string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        List<MatchingRuleOutput> Retrieve(string str);
    }
}