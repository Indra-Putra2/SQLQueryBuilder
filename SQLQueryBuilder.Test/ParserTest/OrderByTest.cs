using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using System.Data;

namespace SQLQueryBuilder.Test.ParserTest
{
    public class OrderByTest
    {
        #region Pass

        #region Detailed Property Spot-Checks

        [Fact]
        public void Parser_Should_Populate_OrderBy_Ascending_Correctly()
        {
            var clause = SQLBuilderHelper.BuildOrderBy("name ASC");

            Assert.Single(clause);

            var orderBy = Assert.IsType<OrderByExpression>(clause[0]);

            Assert.False(orderBy.Descending);

            var column = Assert.IsType<ColumnExpression>(orderBy.Expression);
            Assert.Equal("name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Populate_OrderBy_Descending_Correctly()
        {
            var clause = SQLBuilderHelper.BuildOrderBy("name DESC");

            Assert.Single(clause);

            var orderBy = Assert.IsType<OrderByExpression>(clause[0]);

            Assert.True(orderBy.Descending);

            var column = Assert.IsType<ColumnExpression>(orderBy.Expression);
            Assert.Equal("name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Default_OrderBy_To_Ascending()
        {
            var clause = SQLBuilderHelper.BuildOrderBy("name");

            Assert.Single(clause);

            var orderBy = Assert.IsType<OrderByExpression>(clause[0]);

            Assert.False(orderBy.Descending);

            var column = Assert.IsType<ColumnExpression>(orderBy.Expression);
            Assert.Equal("name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Populate_Qualified_OrderBy_Correctly()
        {
            var clause = SQLBuilderHelper.BuildOrderBy("u.Name DESC");

            Assert.Single(clause);

            var orderBy = Assert.IsType<OrderByExpression>(clause[0]);

            Assert.True(orderBy.Descending);

            var column = Assert.IsType<ColumnExpression>(orderBy.Expression);

            Assert.Equal("u", column.Table[0].Value);
            Assert.Equal("Name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Multiple_OrderBy_Items()
        {
            var clause = SQLBuilderHelper.BuildOrderBy(
                "name ASC, age DESC, createdAt");

            Assert.Equal(3, clause.Count);

            var first = Assert.IsType<OrderByExpression>(clause[0]);
            Assert.False(first.Descending);

            var firstColumn = Assert.IsType<ColumnExpression>(first.Expression);
            Assert.Equal("name", firstColumn.Column.Value);

            var second = Assert.IsType<OrderByExpression>(clause[1]);
            Assert.True(second.Descending);

            var secondColumn = Assert.IsType<ColumnExpression>(second.Expression);
            Assert.Equal("age", secondColumn.Column.Value);

            var third = Assert.IsType<OrderByExpression>(clause[2]);
            Assert.False(third.Descending);

            var thirdColumn = Assert.IsType<ColumnExpression>(third.Expression);
            Assert.Equal("createdAt", thirdColumn.Column.Value);
        }

        #endregion

        #region Structural & Type Coverage

        [Theory]

        // Column
        [InlineData("name", typeof(ColumnExpression), false)]
        [InlineData("name ASC", typeof(ColumnExpression), false)]
        [InlineData("name DESC", typeof(ColumnExpression), true)]

        // Qualified column
        [InlineData("u.Name", typeof(ColumnExpression), false)]
        [InlineData("u.Name DESC", typeof(ColumnExpression), true)]

        // Literal
        [InlineData("123", typeof(LiteralExpression), false)]
        [InlineData("'hello' DESC", typeof(LiteralExpression), true)]

        // Binary
        [InlineData("price * quantity", typeof(BinaryExpression), false)]
        [InlineData("price * quantity DESC", typeof(BinaryExpression), true)]

        // Unary
        [InlineData("-price", typeof(UnaryExpression), false)]
        [InlineData("-price DESC", typeof(UnaryExpression), true)]

        // Function
        [InlineData("COUNT(*)", typeof(FunctionExpression), false)]
        [InlineData("COUNT(*) DESC", typeof(FunctionExpression), true)]

        // CASE
        [InlineData(
            "CASE WHEN id = 1 THEN 'A' END",
            typeof(CaseExpression),
            false)]

        [InlineData(
            "CASE WHEN id = 1 THEN 'A' END DESC",
            typeof(CaseExpression),
            true)]

        // Bracket identifier
        [InlineData("[User Name]", typeof(ColumnExpression), false)]
        [InlineData("[dbo].[Users]", typeof(ColumnExpression), false)]

        // Double quoted identifier
        [InlineData("\"User Name\"", typeof(ColumnExpression), false)]
        [InlineData("\"dbo\".\"Users\"", typeof(ColumnExpression), false)]

        public void Parser_Should_Parse_OrderBy_Expression_Types(
            string input,
            Type expectedExpression,
            bool expectedDescending)
        {
            var clause = SQLBuilderHelper.BuildOrderBy(input);

            Assert.Single(clause);

            var orderBy = Assert.IsType<OrderByExpression>(clause[0]);

            Assert.IsType(expectedExpression, orderBy.Expression);
            Assert.Equal(expectedDescending, orderBy.Descending);
        }

        #endregion

        #endregion

        #region Fail

        [Theory]

        // Missing expression
        [InlineData("ASC")]
        [InlineData("DESC")]

        // Missing expression after comma
        [InlineData("name,")]
        [InlineData("name ASC,")]
        [InlineData("name, DESC")]

        // Leading comma
        [InlineData(", name")]

        // Double comma
        [InlineData("name,,age")]

        // Invalid direction
        [InlineData("name RANDOM")]
        [InlineData("name ASC DESC")]

        // Direction without comma
        [InlineData("name age")]

        public void Parser_Should_Fail_Invalid_OrderBy_List(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildOrderBy(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Empty_OrderBy()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SQLBuilderHelper.BuildOrderBy("");
            });
        }

        #endregion
    }
}