using HardwareInventoryLibrary.Ram;
using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using System.Diagnostics;
using System.Text;

namespace SQLQueryBuilder.Test.TreadSafetyTest
{
    public class ThreadSafetyTest
    {
        [Fact]
        public async Task BuildTokens_ShouldBeThreadSafe()
        {
            const int taskCount = 100;
            const int iterationsPerTask = 1_000;

            using var barrier = new Barrier(taskCount);

            var tasks = Enumerable.Range(0, taskCount)
            .Select(taskIdx => Task.Run(() =>
            {
                var random = new Random(69 + taskIdx);

                barrier.SignalAndWait();

                for (var i = 0; i < iterationsPerTask; i++)
                {
                    var input = GetRandomLiteral(
                        random.Next(1, 21),
                        random);

                    Debug.WriteLine(input);

                    var context = CreateContext(input.str);
                    ValidateContext(0, input.str.AsSpan(), ref context);
                    var tokens = GetTokens(ref context);

                    Assert.Equal(2, tokens.Count);
                    Assert.Equal(input.type, tokens[0].Type);
                }
            }))
            .ToArray();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task BuildExpression_ShouldBeThreadSafe()
        {
            const int taskCount = 10;
            const int iterationsPerTask = 1_000;

            using var barrier = new Barrier(taskCount);

            var tasks = Enumerable.Range(0, taskCount)
            .Select(taskIdx => Task.Run(() =>
            {
                var random = new Random(69 + taskIdx);

                barrier.SignalAndWait();

                for (var i = 0; i < iterationsPerTask; i++)
                {
                    var input = $"{i}";

                    Debug.WriteLine(input);

                    var ast = CreateExpression(input);
                    var literal = Assert.IsType<LiteralExpression>(ast);
                    Assert.Equal(int.Parse(input), literal.Value);
                }
            }))
            .ToArray();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task QueryBuilder_ShouldBeThreadSafe()
        {
            var cache = QueryBuilder
                .Create<RamReadModel>()
                .From<RamBaseModel>("r")
                .Select("r.*")
                .ValidateBaseQuery();

            const int taskCount = 10;
            const int iterationsPerTask = 1_000;

            using var barrier = new Barrier(taskCount);

            var tasks = Enumerable.Range(0, taskCount)
                .Select(taskIdx => Task.Run(() =>
                {
                    barrier.SignalAndWait();

                    for (var i = 0; i < iterationsPerTask; i++)
                    {
                        var input = $"r.price < {i}";

                        var result = cache
                            .Copy()
                            .Where(input)
                            .Build();

                        Assert.Contains($"WHERE \"r\".\"price\" < @p1", result.SQL);
                        Assert.Equal(i, result.Parameters.Get<int>("p1"));
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);
        }


        private SQLParserContext CreateContext(string inputSql)
        {
            return SQLBuilderHelper.BuildTokens(inputSql);
        }
        private SQLExpression CreateExpression(string inputSql)
        {
            return SQLBuilderHelper.BuildExpression(inputSql);
        }
        private void ValidateIndex(int expected, ref SQLParserContext context)
        {
            Assert.Equal(expected, context.Index);
        }
        private void ValidateSpan(ReadOnlySpan<char> expected, ref SQLParserContext context)
        {
            Assert.Equal(expected, context.Span);
        }
        private void ValidateTokens(ref SQLParserContext context)
        {
            Assert.NotNull(context.Tokens);
        }
        private void ValidateContext(int expectedIndex, ReadOnlySpan<char> expectedSpan, ref SQLParserContext context)
        {
            ValidateIndex(expectedIndex, ref context);
            ValidateSpan(expectedSpan, ref context);
            ValidateTokens(ref context);
        }
        private IReadOnlyList<SQLToken> GetTokens(ref SQLParserContext context) => context.Tokens;

        private (string str, SQLTokenType type) GetRandomIdentifier(int length, Random random)
        {
            const string chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            SQLTokenType type = SQLTokenType.Unknown;

            var sb = new StringBuilder(length + 2);

            for (var i = 0; i < length; i++)
                sb.Append(chars[random.Next(chars.Length)]);

            var text = sb.ToString();
            if (SQLDictionary.SQLKeywords.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                return (text, SQLTokenType.Keyword);
            }

            switch (random.Next(3))
            {
                case 0:
                    text = $"\"{text}\"";
                    type = SQLTokenType.DoubleQuoteIdentifier;
                    break;
                case 1:
                    text = $"[{text}]";
                    type = SQLTokenType.BracketIdentifier;
                    break;
                default:
                    type = SQLTokenType.Identifier;
                    break;
            }

            return (text, type);
        }
        private (string str, SQLTokenType type) GetRandomString(int length, Random random)
        {
            const string chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890~!@#$%^&*()_+`-=[]\\;\",./<>?:{}|";

            SQLTokenType type = SQLTokenType.Unknown;

            var sb = new StringBuilder(length + 2);

            for (var i = 0; i < length; i++)
                sb.Append(chars[random.Next(chars.Length)]);

            var text = sb.ToString();

            switch (random.Next(3))
            {
                case 0:
                    text = $"N'{text}'";
                    type = SQLTokenType.UnicodeString;
                    break;
                default:
                    text = $"'{text}'";
                    type = SQLTokenType.StringLiteral;
                    break;
            }

            return (text, type);
        }
        private (string str, SQLTokenType type) GetRandomLiteral(int length, Random random)
        {
            switch (random.Next(0, 2))
            {
                case 0:
                    return GetRandomIdentifier(length, random);
                default:
                    return GetRandomString(length, random);
            }
        }
    }
}
