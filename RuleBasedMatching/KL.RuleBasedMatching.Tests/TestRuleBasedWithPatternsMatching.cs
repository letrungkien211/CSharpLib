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
    public class TestRuleBasedWithPatternsMatching
    {
        private ITestOutputHelper OutputHelper { get; }

        public TestRuleBasedWithPatternsMatching(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        [Theory]
        [InlineData("Test1afdkfjdkfTemp", "NewParttern")]
        public void MatchPattern(string str, string expected)
        {
            var ruleBasedIndex = CreateRuleBasedIndexWithPatterns();
            var match = ruleBasedIndex.Retrieve(str);

            Assert.NotEmpty(match);
            OutputHelper.WriteLine($"{str}, {JsonConvert.SerializeObject(match)}");
            Assert.Equal(((IEnumerable<string>)(match[0].Value)).FirstOrDefault(), expected);
        }

        [Theory]
        [InlineData("Test1aTepTest2b", "Test1a;Test2b")]
        [InlineData("Test1aTempTest2aTest1b", "Test1a;Test2a;Test1b")]

        public void MatchRegex(string str, string expected)
        {
            var ruleBasedIndex = CreateRuleBasedIndexWithPatterns();
            var matches = ruleBasedIndex.Retrieve(str);

            Assert.NotEmpty(matches);
            var match = matches.First();
            OutputHelper.WriteLine(JsonConvert.SerializeObject(match));
            Assert.Equal(expected, string.Join(";", match.GetRegexMatches()));
        }

        [Theory]
        [InlineData("{Hello}xxxxx", "{Hello};xxxxx")]
        [InlineData("aaa{Hello}xxxxx", "aaa;{Hello};xxxxx")]
        [InlineData("aaa{Hello}xxxxx{bcd}fff", "aaa;{Hello};xxxxx;{bcd};fff")]
        [InlineData("a{Hello}xxxxx{bcd}f", "a;{Hello};xxxxx;{bcd};f")]
        public void SplitPatterns(string str, string expected)
        {
            Assert.Equal(expected, string.Join(";", str.SplitPatterns().ToList()));
        }

        [Fact]
        public void MatchWithPatterns()
        {
            var ruleBasedIndex = CreateRuleBasedIndexWithPatterns();

            var stopWatch = Stopwatch.StartNew();
            foreach (var perfect in ruleBasedIndex.Perfect)
            {
                var ret = ruleBasedIndex.Retrieve(perfect.Key).FirstOrDefault();
                Assert.NotNull(ret);
                OutputHelper.WriteLine($"Variation={perfect.Key}. Ret={JsonConvert.SerializeObject(ret)}");
                Assert.Equal(MatchingRuleType.Perfect, ret.Type);
            }

            var random = new Random();
            foreach (var ruleItems in ruleBasedIndex.RuleItems)
            {
                foreach (var ruleItem in ruleItems.Value)
                {
                    if (ruleItem.Type != MatchingRuleType.Contain)
                        continue;
                    var randomKeyWords = ruleItem.KeyWords.SplitPatterns().Select(x =>
                    {
                        if (x.IsPattern())
                        {
                            ruleBasedIndex.PatternToPhrases.TryGetValue(x, out var phrases);
                            return phrases[random.Next(phrases.Count)];
                        }
                        else
                        {
                            return x;
                        }
                    });
                    var variations = new List<string>()
                    {
                        string.Join("", randomKeyWords),
                        "fdfd" + string.Join("---", randomKeyWords) + "fdfdfd",
                        string.Join(";fdfd", randomKeyWords.OrderBy(a=> Guid.NewGuid()))
                    };

                    foreach (var variation in variations)
                    {
                        var ret = ruleBasedIndex.Retrieve(variation).FirstOrDefault();
                        OutputHelper.WriteLine($"Variation={variation}");
                        Assert.NotNull(ret);
                        OutputHelper.WriteLine($"Variation={variation}. Ret={JsonConvert.SerializeObject(ret)}");
                        Assert.True(ret.Type == MatchingRuleType.Pattern || ret.Type == MatchingRuleType.Contain);
                    }
                }
            }
            OutputHelper.WriteLine($"Elapsed={stopWatch.ElapsedMilliseconds}");
        }

        [Theory]
        [InlineData("Test1aHello", "{Test1};Hello", "Test1a;Hello")]
        [InlineData("Test1bHelloTest2b", "{Test1};Hello;{Test2}", "Test1b;Hello;Test2b")]
        [InlineData("Test1afdfdHello", "{Test1};Hello", "")]
        [InlineData("Test1aHellofdfdfd", "{Test1};Hello", "")]
        public void MatchPatternType(string str, string keywordsStr, string expected)
        {
            var ruleBasedIndex = CreateRuleBasedIndexWithPatterns();
            var ret = ruleBasedIndex.PatternMatches(str, keywordsStr.Split(';').ToList());

            Assert.Equal(expected, string.Join(";", ret));
        }

        private RuleBasedWithPatternsIndex CreateRuleBasedIndexWithPatterns()
        {
            var ruleBasedIndex = RuleBasedIndexFactory.Create(5,
                File.ReadLines("RuleBasedMatchingWithPatternsData-Patterns.tsv").Select(x => x.Split('\t'))
                .Where(y => y.Length >= 3).ToDictionary(z => z[0], z => z.Skip(2).ToList())) as RuleBasedWithPatternsIndex;

            foreach (var line in File.ReadLines("RuleBasedMatchingWithPatternsData.tsv"))
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

                foreach (var temp in splits[0].Split('|'))
                {
                    ruleBasedIndex.Add(MatchingRuleItem.Create(temp.Split(';').Where(x => !string.IsNullOrEmpty(x)), splits.Skip(2), ruleType));
                }
            }
            return ruleBasedIndex;
        }
    }
}
