using TruSql.Helper;
using System.Data;

namespace TruSql.Test.ParserTest
{
    public class FromTest
    {
        #region Pass

        #region Detailed Property Spot-Checks

        [Fact]
        public void Parser_Should_Parse_Single_Table()
        {
            var clause = SQLBuilderHelper.BuildFrom("Users");

            Assert.Single(clause);
            Assert.Equal("Users", clause[0].Value);
        }

        [Fact]
        public void Parser_Should_Parse_Qualified_Table()
        {
            var clause = SQLBuilderHelper.BuildFrom("dbo.Users");

            Assert.Equal(2, clause.Count);

            Assert.Equal("dbo", clause[0].Value);
            Assert.Equal("Users", clause[1].Value);
        }

        [Fact]
        public void Parser_Should_Parse_MultiLevel_Qualified_Table()
        {
            var clause = SQLBuilderHelper.BuildFrom("server.dbo.Users");

            Assert.Equal(3, clause.Count);

            Assert.Equal("server", clause[0].Value);
            Assert.Equal("dbo", clause[1].Value);
            Assert.Equal("Users", clause[2].Value);
        }

        [Fact]
        public void Parser_Should_Parse_Bracket_Qualified_Table()
        {
            var clause = SQLBuilderHelper.BuildFrom("[dbo].[Users]");

            Assert.Equal(2, clause.Count);

            Assert.Equal("dbo", clause[0].Value);
            Assert.Equal("Users", clause[1].Value);
        }

        [Fact]
        public void Parser_Should_Parse_DoubleQuoted_Qualified_Table()
        {
            var clause = SQLBuilderHelper.BuildFrom("\"dbo\".\"Users\"");

            Assert.Equal(2, clause.Count);

            Assert.Equal("dbo", clause[0].Value);
            Assert.Equal("Users", clause[1].Value);
        }

        #endregion

        #region Structural Coverage

        [Theory]

        // Single identifier
        [InlineData("Users", "Users")]

        // Qualified identifiers
        [InlineData("dbo.Users", "dbo", "Users")]
        [InlineData("server.dbo.Users", "server", "dbo", "Users")]
        [InlineData("server.dbo.schema.Users", "server", "dbo", "schema", "Users")]

        // Bracket identifiers
        [InlineData("[Users]", "Users")]
        [InlineData("[dbo].[Users]", "dbo", "Users")]
        [InlineData("[server].[dbo].[Users]", "server", "dbo", "Users")]

        // Escaped bracket identifiers
        [InlineData("[User]]s]", "User]s")]
        [InlineData("[My]]Table]", "My]Table")]
        [InlineData("[dbo].[My]]Table]", "dbo", "My]Table")]
        [InlineData("[server].[dbo].[My]]Table]", "server", "dbo", "My]Table")]

        // Double-quoted identifiers
        [InlineData("\"Users\"", "Users")]
        [InlineData("\"dbo\".\"Users\"", "dbo", "Users")]
        [InlineData("\"server\".\"dbo\".\"Users\"", "server", "dbo", "Users")]

        // Escaped double-quoted identifiers
        [InlineData("\"User\"\"s\"", "User\"s")]
        [InlineData("\"My\"\"Table\"", "My\"Table")]
        [InlineData("\"dbo\".\"My\"\"Table\"", "dbo", "My\"Table")]
        [InlineData("\"server\".\"dbo\".\"My\"\"Table\"", "server", "dbo", "My\"Table")]

        // Mixed delimiter styles
        [InlineData("[dbo].\"Users\"", "dbo", "Users")]
        [InlineData("\"dbo\".[Users]", "dbo", "Users")]
        [InlineData("[server].\"dbo\".[Users]", "server", "dbo", "Users")]
        [InlineData("\"server\".[dbo].\"Users\"", "server", "dbo", "Users")]

        // Escaped + mixed
        [InlineData("[My]]Server].\"dbo\".[User]]s]", "My]Server", "dbo", "User]s")]
        [InlineData("\"My\"\"Server\".[dbo].\"User\"\"s\"", "My\"Server", "dbo", "User\"s")]

        public void Parser_Should_Parse_From_Table_Identifier(
            string input,
            params string[] expectedParts)
        {
            var clause = SQLBuilderHelper.BuildFrom(input);

            // Verify element count matches expected parts count
            Assert.Equal(expectedParts.Length, clause.Count);

            // Verify each parsed segment matches the unquoted/unescaped expected value in order
            for (int i = 0; i < expectedParts.Length; i++)
            {
                Assert.Equal(expectedParts[i], clause[i].Value);
            }
        }

        #endregion

        #endregion

        #region Fail

        [Theory]

        // Invalid / incomplete identifier
        [InlineData(".")]
        [InlineData("dbo.")]
        [InlineData(".Users")]

        // Multiple tables are not currently supported
        [InlineData("Users, Orders")]

        // Subqueries are not supported
        [InlineData("(SELECT * FROM Users)")]

        // Invalid qualification
        [InlineData("dbo..Users")]

        // Invalid path
        [InlineData("server1.system.dbo.ram.Users")]

        public void Parser_Should_Fail_Invalid_From(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildFrom(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Empty_From()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                SQLBuilderHelper.BuildFrom("");
            });
        }

        #endregion
    }
}