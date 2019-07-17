using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace KL.RuleBasedMatching.Tests
{
    public class TestRuleBasedMatching
    {
        private ITestOutputHelper OutputHelper { get; }

        public TestRuleBasedMatching(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }
        [Fact]
        public void Match()
        {
            var ruleBasedIndex = new RuleBasedIndex(5);

            foreach (var line in File.ReadLines("RuleBasedMatchingData.tsv"))
            {
                var splits = line.Split('\t');
                if (splits.Length < 3)
                {
                    OutputHelper.WriteLine($"[ERROR] Invalid line: {line}");
                    continue;
                }

                if (!Enum.TryParse(splits[1], true, out MatchingRuleType ruleType))
                {
                    OutputHelper.WriteLine($"[ERROR] Invalid ruletype: {line}");
                    continue;
                }

                ruleBasedIndex.Add(MatchingRuleItem.Create(splits[0].Split(';').Where(x=> !string.IsNullOrEmpty(x)), splits.ToList().GetRange(2, splits.Length-2), ruleType));
            }

            var stopWatch = Stopwatch.StartNew();
            foreach (var perfect in ruleBasedIndex.Perfect)
            {
                var ret = ruleBasedIndex.Retrieve(perfect.Key).FirstOrDefault();
                Assert.NotNull(ret);
                OutputHelper.WriteLine($"Variation={perfect.Key}. Ret={JsonConvert.SerializeObject(ret)}");
                Assert.Equal(MatchingRuleType.Perfect, ret.Type);
            }

            foreach (var ruleItems in ruleBasedIndex.RuleItems)
            {
                foreach (var ruleItem in ruleItems.Value)
                {
                    var variations = new List<string>()
                    {
                        string.Join("", ruleItem.KeyWords),
                        "fdfd" + string.Join("---", ruleItem.KeyWords) + "fdfdfd",
                        string.Join(";fdfd", ruleItem.KeyWords.OrderBy(a=> Guid.NewGuid()))
                    };

                    foreach (var variation in variations)
                    {
                        var ret = ruleBasedIndex.Retrieve(variation).FirstOrDefault();
                        Assert.NotNull(ret);
                        OutputHelper.WriteLine($"Variation={variation}. Ret={JsonConvert.SerializeObject(ret)}");
                        Assert.Equal(MatchingRuleType.Contain, ret.Type);
                    }
                }
            }
            OutputHelper.WriteLine($"Elapsed={stopWatch.Elapsed}");
            OutputHelper.WriteLine($"Elapsed={stopWatch.ElapsedMilliseconds}");

        }
    }
}
