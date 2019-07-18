using System;
using System.Collections.Generic;
using System.Text;

namespace KL.RuleBasedMatching
{
    /// <summary>
    /// Rule based index factory
    /// </summary>
    public class RuleBasedIndexFactory
    {
        /// <summary>
        /// Create a rule based index
        /// </summary>
        /// <param name="maxIndexLength">max index length. Eg: 5</param>
        /// <returns></returns>
        public static IRuleBasedIndex Create(int maxIndexLength)
        {
            return new RuleBasedIndex(maxIndexLength);
        }

        /// <summary>
        /// Create a rule based index with patterns
        /// </summary>
        /// <param name="maxIndexLength">max index length. Eg: 5</param>
        /// <param name="patternToPhrases">pattern to phrases mapping.</param>
        /// <returns></returns>
        public static IRuleBasedIndex Create(int maxIndexLength, IDictionary<string, List<string>> patternToPhrases)
        {
            return new RuleBasedWithPatternsIndex(maxIndexLength, patternToPhrases);
        }
    }
}
