using HardwareInventoryLibrary.Brand;
using HardwareInventoryLibrary.Ram;
using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.SQLEnum.QueryBuilder;
using SQLQueryBuilder.Test.Helper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Reflection.Emit;

namespace SQLQueryBuilder.Test.SQLGeneratorTest
{
    public class GeneratorTest
    {
        [Fact]
        public void SQLGenerator_BuildSelect_Must_emit_select_expression()
        {
            var random = new Random(69);

            for (var i = 0; i < 10_000; i++)
            {
                var input = TestHelper.GetRandomLiteral(10, random);
                var selectExpr = SQLBuilderHelper.BuildSelect(input.str);
                var context = TestHelper.GetGeneratorContext();

                SQLGenerator.BuildSelect(selectExpr, ref context);

                var result = SQLGenerator.Build(ref context);

                var expectedInput = GetExpected(input);

                var expected = TestHelper.CleanString($"SELECT {expectedInput}");
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void SQLGenerator_BuildFrom_Must_emit_From_expression()
        {
            var random = new Random(69);
            var type = typeof(RamBaseModel);
            for (var i = 0; i < 10_000; i++)
            {
                var alias = TestHelper.GetRandomIdentifier(10, random);
                var tableName = TestHelper.GetRandomIdentifier(5, random);
                var from = BuildTableSource(tableName.str, alias.str, type);
                var context = TestHelper.GetGeneratorContext();

                SQLGenerator.BuildFrom(from, ref context);

                var result = SQLGenerator.Build(ref context);

                var expected = TestHelper.CleanString($"FROM {GetExpected(tableName)} AS {GetExpected(alias)}");
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void SQLGenerator_BuildJoin_Must_emit_Join_expression()
        {
            var random = new Random(69);
            var joinTypes = Enum.GetValues<SQLJoinType>();
            var type = typeof(BrandBaseModel);
            for (var i = 0; i < 10_000; i++)
            {
                var tableAliasLeft = TestHelper.GetRandomIdentifier(5, random);
                var tableAliasRight = TestHelper.GetRandomIdentifier(5, random);
                var expectedAliasLeft = GetExpected(tableAliasLeft);
                var expectedAliasRight = GetExpected(tableAliasRight);

                var columnLeft = TestHelper.GetRandomIdentifier(10, random);
                var columnRight = TestHelper.GetRandomIdentifier(10, random);
                var expectedColumnLeft = GetExpected(columnLeft);
                var expectedColumnRight = GetExpected(columnRight);

                var condition = $"{tableAliasLeft.str}.{columnLeft.str} = {tableAliasRight.str}.{columnRight.str}";

                var joinType = joinTypes[random.Next(joinTypes.Length)];
                var tableName = TestHelper.GetRandomIdentifier(5, random);

                var tableSource = BuildTableSource(tableName.str, tableAliasLeft.str, type);

                var joinInfo = new JoinInfo
                {
                    JoinCondition = condition,
                    JoinType = joinType,
                    TableAlias = tableAliasLeft.str,
                    TableType = type
                };

                var joinSource = SQLBuilderHelper.BuildJoinSource(tableSource, joinInfo);

                var context = TestHelper.GetGeneratorContext();
                SQLGenerator.BuildJoins(new List<JoinSource>() { joinSource }, ref context);
                var result = SQLGenerator.Build(ref context);
                var expected = $"{joinType} JOIN {GetExpected(tableName)} AS {expectedAliasLeft} ON {expectedAliasLeft}.{expectedColumnLeft} = {expectedAliasRight}.{expectedColumnRight}";
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void SQLGenerator_BuildHaving_Must_emit_Having_expression()
        {
            var random = new Random(69);

            for (var i = 0; i < 10_000; i++)
            {
                var col = TestHelper.GetRandomIdentifier(5, random);
                var str = TestHelper.GetRandomString(10, random);

                var havingExpr = SQLBuilderHelper.BuildExpression($"{col.str} like {str.str}");
                var context = TestHelper.GetGeneratorContext();

                SQLGenerator.BuildHaving(havingExpr, ref context);

                var result = SQLGenerator.Build(ref context);

                var expectedCol = GetExpected(col);

                var expected = TestHelper.CleanString($"HAVING {expectedCol} LIKE @p1");
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
                Assert.Equal(SQLParserHelper.UnescapeStringLiteral(str.str), result.Parameters.Get<string>("@p1"));
            }
        }

        [Fact]
        public void SQLGenerator_BuildWhere_Must_emit_Where_expression()
        {
            var random = new Random(69);

            for (var i = 0; i < 10_000; i++)
            {
                var col = TestHelper.GetRandomIdentifier(5, random);
                var str = TestHelper.GetRandomString(10, random);

                var whereExpr = SQLBuilderHelper.BuildExpression($"{col.str} like {str.str}");
                var context = TestHelper.GetGeneratorContext();

                SQLGenerator.BuildWhere(whereExpr, ref context);

                var result = SQLGenerator.Build(ref context);

                var expectedInput = GetExpected(col);

                var expected = TestHelper.CleanString($"WHERE {expectedInput} LIKE @p1");
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
                Assert.Equal(SQLParserHelper.UnescapeStringLiteral(str.str), result.Parameters.Get<string>("@p1"));
            }
        }

        [Fact]
        public void SQLGenerator_BuildGroupBy_Must_emit_GroupBy_expression()
        {
            var random = new Random(69);

            for (var i = 0; i < 10_000; i++)
            {
                var input = TestHelper.GetRandomIdentifier(10, random);
                var groupByExpr = SQLBuilderHelper.BuildGroupBy(input.str);
                var context = TestHelper.GetGeneratorContext();

                SQLGenerator.BuildGroupBy(groupByExpr, ref context);

                var result = SQLGenerator.Build(ref context);

                var expectedInput = GetExpected(input);

                var expected = TestHelper.CleanString($"GROUP BY {expectedInput}");
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void SQLGenerator_BuildOrderBy_Must_emit_OrderBy_expression()
        {
            var random = new Random(69);
            for (var i = 0; i < 10_000; i++)
            {
                var order = random.Next(0, 2) == 1 ? "ASC" : "DESC";
                var input = TestHelper.GetRandomIdentifier(10, random);
                var orderByExpr = SQLBuilderHelper.BuildOrderBy($"{input.str} {order}");
                var context = TestHelper.GetGeneratorContext();

                SQLGenerator.BuildOrderBy(orderByExpr, ref context);

                var result = SQLGenerator.Build(ref context);

                var expectedInput = GetExpected(input);

                var expected = TestHelper.CleanString($"ORDER BY {expectedInput} {order}");
                var actual = TestHelper.CleanString(result.SQL);

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void SQLGenerator_BuildLimit_Must_emit_Limit_expression()
        {
            for(var i = 0; i < 10_000; i++)
            {
                var context = TestHelper.GetGeneratorContext();
                SQLGenerator.BuildLimit(i, ref context);
            }
        }

        private string GetExpected((string str, SQLTokenType type) input)
        {
            return
                input.type == SQLTokenType.Identifier
                    ? $"\"{input.str}\""
                    : input.str;
        }

        private static TableSource BuildTableSource(string tableName,string alias, Type type)
        {
            var name = SQLBuilderHelper.BuildFrom(tableName);

            var source = new TableSource
            {
                Type = type,
                TableName = name,
                Alias = SQLBuilderHelper.BuildAlias(alias)
            };

            return source;
        }
    }
}
