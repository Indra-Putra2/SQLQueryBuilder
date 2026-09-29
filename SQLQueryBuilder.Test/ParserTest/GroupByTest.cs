using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using System.Data;

namespace SQLQueryBuilder.Test.ParserTest
{
    public class GroupByTest
    {
        #region Pass

        #region Detailed Property Spot-Checks

        [Fact]
        public void Parser_Should_Parse_Single_GroupBy_Column()
        {
            var clause = SQLBuilderHelper.BuildGroupBy("name");

            Assert.Single(clause);

            var column = Assert.IsType<ColumnExpression>(clause[0]);
            Assert.Equal("name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Multiple_GroupBy_Items()
        {
            var clause = SQLBuilderHelper.BuildGroupBy("name, age, country");

            Assert.Equal(3, clause.Count);

            var first = Assert.IsType<ColumnExpression>(clause[0]);
            Assert.Equal("name", first.Column.Value);

            var second = Assert.IsType<ColumnExpression>(clause[1]);
            Assert.Equal("age", second.Column.Value);

            var third = Assert.IsType<ColumnExpression>(clause[2]);
            Assert.Equal("country", third.Column.Value);
        }

        [Fact]
        public void Parser_Should_Populate_Qualified_GroupBy_Column_Correctly()
        {
            var clause = SQLBuilderHelper.BuildGroupBy("u.Name");

            Assert.Single(clause);

            var column = Assert.IsType<ColumnExpression>(clause[0]);

            Assert.Equal("u", column.Table[0].Value);
            Assert.Equal("Name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Bracket_Qualified_GroupBy_Column()
        {
            var clause = SQLBuilderHelper.BuildGroupBy("[dbo].[Users]");

            Assert.Single(clause);

            var column = Assert.IsType<ColumnExpression>(clause[0]);

            Assert.Equal("dbo", column.Table[0].Value);
            Assert.Equal("Users", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Quoted_Qualified_GroupBy_Column()
        {
            var clause = SQLBuilderHelper.BuildGroupBy("\"dbo\".\"Users\".\"Name\"");

            Assert.Single(clause);

            var column = Assert.IsType<ColumnExpression>(clause[0]);

            Assert.Equal("dbo", column.Table[0].Value);
            Assert.Equal("Users", column.Table[1].Value);
            Assert.Equal("Name", column.Column.Value);
        }

        #endregion

        #region Structural & Type Coverage

        [Theory]

        // Column
        [InlineData("name", typeof(ColumnExpression))]
        [InlineData("u.Name", typeof(ColumnExpression))]

        // Literal
        [InlineData("123", typeof(LiteralExpression))]
        [InlineData("'hello'", typeof(LiteralExpression))]

        // Binary
        [InlineData("price * quantity", typeof(BinaryExpression))]
        [InlineData("price + tax", typeof(BinaryExpression))]

        // Unary
        [InlineData("-price", typeof(UnaryExpression))]

        // Function
        [InlineData("COUNT(*)", typeof(FunctionExpression))]
        [InlineData("YEAR(createdAt)", typeof(FunctionExpression))]

        // CASE
        [InlineData(
            "CASE WHEN id = 1 THEN 'A' END",
            typeof(CaseExpression))]

        // Bracket identifier
        [InlineData("[User Name]", typeof(ColumnExpression))]
        [InlineData("[dbo].[Users]", typeof(ColumnExpression))]

        // Double quoted identifier
        [InlineData("\"User Name\"", typeof(ColumnExpression))]
        [InlineData("\"dbo\".\"Users\"", typeof(ColumnExpression))]

        public void Parser_Should_Parse_GroupBy_Expression_Types(
            string input,
            Type expectedExpression)
        {
            var clause = SQLBuilderHelper.BuildGroupBy(input);

            Assert.Single(clause);

            Assert.IsType(
                expectedExpression,
                clause[0]);
        }

        #endregion

        #endregion

        #region Fail

        [Theory]

        // Leading comma
        [InlineData(", name")]

        // Trailing comma
        [InlineData("name,")]

        // Double comma
        [InlineData("name,,age")]

        // Missing expression after comma
        [InlineData("name, , age")]

        public void Parser_Should_Fail_Invalid_GroupBy_List(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildGroupBy(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Empty_GroupBy()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SQLBuilderHelper.BuildGroupBy("");
            });
        }

        #endregion
    }
}