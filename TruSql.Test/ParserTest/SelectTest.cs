using TruSql.Core.SQLExpression;
using TruSql.Helper;
using System.Data;

namespace TruSql.Test.ParserTest
{
    public class SelectTest
    {
        #region Pass

        #region Detailed Property Spot-Checks

        [Fact]
        public void Parser_Should_Populate_Select_Alias_Correctly()
        {
            var clause = SQLBuilderHelper.BuildSelect("name AS userName");

            Assert.Single(clause);

            var select = Assert.IsType<SelectExpression>(clause[0]);

            Assert.Equal("userName", select.Alias.Value);

            var column = Assert.IsType<ColumnExpression>(select.Expression);
            Assert.Equal("name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Populate_Select_Without_Alias_Correctly()
        {
            var clause = SQLBuilderHelper.BuildSelect("price");

            Assert.Single(clause);

            var select = Assert.IsType<SelectExpression>(clause[0]);

            Assert.Null(select.Alias);

            var column = Assert.IsType<ColumnExpression>(select.Expression);
            Assert.Equal("price", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Multiple_Select_Items()
        {
            var clause = SQLBuilderHelper.BuildSelect(
                "id, name AS username, COUNT(*) AS total");

            Assert.Equal(3, clause.Count);

            var first = Assert.IsType<SelectExpression>(clause[0]);
            Assert.IsType<ColumnExpression>(first.Expression);
            Assert.Null(first.Alias);

            var second = Assert.IsType<SelectExpression>(clause[1]);
            Assert.IsType<ColumnExpression>(second.Expression);
            Assert.Equal("username", second.Alias.Value);

            var third = Assert.IsType<SelectExpression>(clause[2]);
            Assert.IsType<FunctionExpression>(third.Expression);
            Assert.Equal("total", third.Alias.Value);
        }

        [Fact]
        public void Parser_Should_Populate_Qualified_Column_Correctly()
        {
            var clause = SQLBuilderHelper.BuildSelect("u.Name");

            Assert.Single(clause);

            var select = Assert.IsType<SelectExpression>(clause[0]);
            Assert.Null(select.Alias);

            var column = Assert.IsType<ColumnExpression>(select.Expression);

            Assert.Equal("u", column.Table[0].Value);
            Assert.Equal("Name", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Populate_Bracket_Qualified_Column_Correctly()
        {
            var clause = SQLBuilderHelper.BuildSelect("[dbo].[Users]");

            Assert.Single(clause);

            var select = Assert.IsType<SelectExpression>(clause[0]);
            Assert.Null(select.Alias);

            var column = Assert.IsType<ColumnExpression>(select.Expression);

            Assert.Equal("dbo", column.Table[0].Value);
            Assert.Equal("Users", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Populate_Quoted_Qualified_Column_Correctly()
        {
            var clause = SQLBuilderHelper.BuildSelect("\"dbo\".\"Ram\".\"Name\"");

            Assert.Single(clause);

            var select = Assert.IsType<SelectExpression>(clause[0]);
            Assert.Null(select.Alias);

            var column = Assert.IsType<ColumnExpression>(select.Expression);

            Assert.Equal("dbo", column.Table[0].Value);
            Assert.Equal("Ram", column.Table[1].Value);
            Assert.Equal("Name", column.Column.Value);
        }
        #endregion

        #region Structural & Type Coverage

        [Theory]

        // Column
        [InlineData("name", typeof(ColumnExpression), false)]

        // Literal
        [InlineData("123", typeof(LiteralExpression), false)]

        // Binary
        [InlineData("price * quantity", typeof(BinaryExpression), false)]

        // Unary
        [InlineData("-price", typeof(UnaryExpression), false)]

        // Function
        [InlineData("COUNT(*)", typeof(FunctionExpression), false)]

        // CASE
        [InlineData("CASE WHEN id = 1 THEN 'A' END", typeof(CaseExpression), false)]

        // Wildcard
        [InlineData("*", typeof(WildcardExpression), false)]
        [InlineData("r.*", typeof(WildcardExpression), false)]

        // Alias
        [InlineData("COUNT(*) AS Total", typeof(FunctionExpression), true)]

        // Regular identifier
        [InlineData("name", typeof(ColumnExpression), false)]

        // Bracket identifier
        [InlineData("[User Name]", typeof(ColumnExpression), false)]
        [InlineData("[dbo].[Users]", typeof(ColumnExpression), false)]

        // Double-quoted identifier
        [InlineData("\"User Name\"", typeof(ColumnExpression), false)]
        [InlineData("\"dbo\".\"Users\"", typeof(ColumnExpression), false)]

        // Qualified identifier
        [InlineData("dbo.Users", typeof(ColumnExpression), false)]
        [InlineData("u.Name", typeof(ColumnExpression), false)]

        // Escaped delimiters inside identifiers
        [InlineData("[my]]table].[my]]column]", typeof(ColumnExpression), false)]
        [InlineData("\"my\"\"table\".\"my\"\"column\"", typeof(ColumnExpression), false)]

        [InlineData("dbo.[Users]", typeof(ColumnExpression), false)]
        [InlineData("[dbo].Users", typeof(ColumnExpression), false)]
        [InlineData("u.[Name]", typeof(ColumnExpression), false)]
        [InlineData("[u].Name", typeof(ColumnExpression), false)]

        [InlineData("\"dbo\".Users", typeof(ColumnExpression), false)]
        [InlineData("dbo.\"Users\"", typeof(ColumnExpression), false)]

        public void Parser_Should_Parse_Select_Expression_Types(
            string input,
            Type expectedExpression,
            bool hasAlias)
        {
            var clause = SQLBuilderHelper.BuildSelect(input);

            Assert.Single(clause);

            var select = Assert.IsType<SelectExpression>(clause[0]);

            Assert.IsType(expectedExpression, select.Expression);

            if (hasAlias)
                Assert.NotNull(select.Alias);
            else
                Assert.Null(select.Alias);
        }

        #endregion

        #endregion

        #region Fail

        [Theory]
        // Missing alias
        [InlineData("name AS")]

        // Missing expression
        [InlineData("AS test")]

        // Leading comma
        [InlineData(", name")]

        // Trailing comma
        [InlineData("name,")]

        // Double comma
        [InlineData("name,,age")]

        // Missing expression after comma
        [InlineData("name, AS test")]

        // Invalid alias
        [InlineData("name AS +")]

        public void Parser_Should_Fail_Invalid_Select_List(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildSelect(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Empty_Select()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SQLBuilderHelper.BuildSelect("");
            });
        }
        #endregion
    }
}