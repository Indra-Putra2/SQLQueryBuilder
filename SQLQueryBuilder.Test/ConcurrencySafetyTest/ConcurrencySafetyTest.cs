using HardwareInventoryLibrary.Ram;
using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Test.Helper;
using System.Diagnostics;

namespace SQLQueryBuilder.Test.ConcurrencySafetyTest
{
    public class ConcurrencySafetyTest
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
                    var input = TestHelper.GetRandomLiteral(
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

            const int taskCount = 100;
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
                            .CopyBase()
                            .Where(input)
                            .Build();

                        Assert.Contains($"WHERE \"r\".\"price\" < @p1", result.ParameterizedSQL);
                        TestHelper.IsEqual(i, "@p1", result);
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
    }
}
