using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using System.Data;

namespace SQLQueryBuilder.Test.ParserTest
{
    public class FunctionTest
    {
        [Theory]
        [InlineData("Count(*)", "Count", 1, typeof(WildcardExpression), SQLFunctionType.Unknown)]
        [InlineData("UNKOWN(*)", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Unknown)]
        [InlineData("Count(*) TYPE STRING", "Count", 1, typeof(WildcardExpression), SQLFunctionType.String)]
        [InlineData("UNKOWN(*) TYPE INT", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Integer)]
        [InlineData("UNKOWN(*) TYPE GUID", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Guid)]
        [InlineData("UNKOWN(*) TYPE DATE", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Date)]
        [InlineData("UNKOWN(*) TYPE TIME", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Time)]
        [InlineData("UNKOWN(*) TYPE DATETIME", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.DateTime)]
        [InlineData("UNKOWN(*) TYPE BINARY", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Binary)]
        [InlineData("UNKOWN(*) TYPE BOOL", "UNKOWN", 1, typeof(WildcardExpression), SQLFunctionType.Boolean)]
        public void Parser_Should_Parse_Function_Correctly(
            string input,
            string expectedFunctionName,
            int expectedargumentCount,
            Type expectedArgumentType,
            SQLFunctionType? expectedReturnType)
        {
            var expr = SQLBuilderHelper.BuildExpression(input);

            Assert.NotNull(expr);
            var func = Assert.IsType<FunctionExpression>(expr);
            Assert.Equal(expectedFunctionName, func.Name);
            Assert.Equal(expectedargumentCount, func.Arguments.Count);
            Assert.IsType(expectedArgumentType, func.Arguments[0]);
            Assert.Equal(expectedReturnType, func.DeclaredType);
        }

        [Theory]
        //syntax check
        [InlineData("func(*)", "func", SQLFunctionType.Unknown, false, false)]
        [InlineData("func(*) null", "func", SQLFunctionType.Null, true, false)]
        [InlineData("func(*) int", "func", SQLFunctionType.Integer, false, false)]
        [InlineData("func(*) TYPE int", "func", SQLFunctionType.Integer, false, false)]
        [InlineData("func(*) const int", "func", SQLFunctionType.Integer, false, true)]
        [InlineData("func(*) TYPE const int", "func", SQLFunctionType.Integer, false, true)]
        [InlineData("func(*) int?", "func", SQLFunctionType.Integer, true, false)]
        [InlineData("func(*) TYPE int?", "func", SQLFunctionType.Integer, true, false)]
        [InlineData("func(*) const int?", "func", SQLFunctionType.Integer, true, true)]
        [InlineData("func(*) TYPE const int?", "func", SQLFunctionType.Integer, true, true)]
        //function type check
        [InlineData("func(*) TYPE const string?", "func", SQLFunctionType.String, true, true)]
        [InlineData("func(*) TYPE const bool?", "func", SQLFunctionType.Boolean, true, true)]
        [InlineData("func(*) TYPE const double?", "func", SQLFunctionType.Double, true, true)]
        [InlineData("func(*) TYPE const decimal?", "func", SQLFunctionType.Decimal, true, true)]
        [InlineData("func(*) TYPE const date?", "func", SQLFunctionType.Date, true, true)]
        [InlineData("func(*) TYPE const time?", "func", SQLFunctionType.Time, true, true)]
        [InlineData("func(*) TYPE const datetime?", "func", SQLFunctionType.DateTime, true, true)]
        [InlineData("func(*) TYPE const guid?", "func", SQLFunctionType.Guid, true, true)]
        [InlineData("func(*) TYPE const binary?", "func", SQLFunctionType.Binary, true, true)]
        [InlineData("func(*) TYPE const null", "func", SQLFunctionType.Null, true, true)]
        public void Parser_Should_Parse_Function_Syntax_Correctly(
            string input,
            string expectedFunctionName,
            SQLFunctionType expectedFunctionType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var expr = SQLBuilderHelper.BuildExpression(input);

            Assert.NotNull(expr);
            var func = Assert.IsType<FunctionExpression>(expr);
            Assert.Equal(expectedFunctionName, func.Name);
            Assert.Equal(expectedFunctionType, func.DeclaredType);
            Assert.Equal(expectedNullable, func.IsNullable);
            Assert.Equal(expectedConstant, func.IsConstant);
        }

        [Theory]
        [InlineData("COUNT(*")]
        [InlineData("COUNT*)")]
        [InlineData("UNKOWN(*) TYPE")]
        [InlineData("UNKOWN(*) TYPE UNKOWN")]
        [InlineData("func(*) TYPE null?")]
        [InlineData("func(*) null?")]

        // incorrect TYPE / CONST / SQLType ordering
        [InlineData("func(*) const TYPE int")]
        [InlineData("func(*) const type int")]
        [InlineData("func(*) int const")]
        [InlineData("func(*) int TYPE")]
        [InlineData("func(*) int TYPE const")]
        [InlineData("func(*) int? const")]
        [InlineData("func(*) int? TYPE")]
        [InlineData("func(*) int? TYPE const")]
        [InlineData("func(*) TYPE int const")]
        [InlineData("func(*) TYPE int TYPE")]
        [InlineData("func(*) TYPE int const?")]
        public void Parser_Should_fail_Invalid_Function(
            string input)
        {
            Assert.Throws<SyntaxErrorException>(() => SQLBuilderHelper.BuildExpression(input));
        }
    }
}