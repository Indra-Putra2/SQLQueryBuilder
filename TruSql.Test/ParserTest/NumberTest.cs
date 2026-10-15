using TruSql.Core.SQLEnum.SQLExpression;
using TruSql.Core.SQLExpression;
using TruSql.Helper;
using System.Data;

namespace TruSql.Test.ParserTest
{
    public class NumberTest
    {
        #region Pass
        [Fact]
        public void Parser_Should_Parse_Integer()
        {
            var exp = SQLBuilderHelper.BuildExpression("123");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Integer, literal.Type); // or Integer
            Assert.Equal(123, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Integer_With_Leading_Zeros()
        {
            var exp = SQLBuilderHelper.BuildExpression("000123");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Integer, literal.Type);
            Assert.Equal(123, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Decimal()
        {
            var exp = SQLBuilderHelper.BuildExpression("123.45");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Decimal, literal.Type);
            Assert.Equal(123.45m, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Money()
        {
            var exp = SQLBuilderHelper.BuildExpression("$123");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Money, literal.Type);
            Assert.Equal(123m, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Money_Decimal()
        {
            var exp = SQLBuilderHelper.BuildExpression("$123.45");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Money, literal.Type);
            Assert.Equal(123.45m, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Decimal_With_Leading_Zeros()
        {
            var exp = SQLBuilderHelper.BuildExpression("00.123");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Decimal, literal.Type);
            Assert.Equal(0.123m, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Decimal_Leading_Dot()
        {
            var exp = SQLBuilderHelper.BuildExpression(".45");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Decimal, literal.Type);
            Assert.Equal(.45m, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Zero()
        {
            var exp = SQLBuilderHelper.BuildExpression("0");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Integer, literal.Type);
            Assert.Equal(0, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Scientific_Notation_Positive()
        {
            var exp = SQLBuilderHelper.BuildExpression("1e5");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Scientific, literal.Type);
            Assert.Equal(1e5d, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Scientific_Notation_Uppercase_E()
        {
            var exp = SQLBuilderHelper.BuildExpression("1E5");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Scientific, literal.Type);
            Assert.Equal(1e5d, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Scientific_Notation_Decimal()
        {
            var exp = SQLBuilderHelper.BuildExpression(".23e+4");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Scientific, literal.Type);
            Assert.Equal(.23e+4d, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Scientific_Notation_Negative_Exponent()
        {
            var exp = SQLBuilderHelper.BuildExpression("1.23e-4");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Scientific, literal.Type);
            Assert.Equal(1.23e-4d, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Scientific_Notation_Positive_Exponent()
        {
            var exp = SQLBuilderHelper.BuildExpression("1.23e+4");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.Scientific, literal.Type);
            Assert.Equal(1.23e+4d, literal.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Binary_Literal()
        {
            var exp = SQLBuilderHelper.BuildExpression("0x1234");
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(SQLLiteralType.BinaryLiteral, literal.Type);

            byte[] expected = new byte[] { 0x12, 0x34 };
            Assert.Equal(expected, Assert.IsType<byte[]>(literal.Value));
        }
        #endregion

        #region Fail
        [Fact]
        public void Parser_Should_Fail_Number_With_Multiple_Dots()
        {
            var input = "12.34.56";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Money_With_Missing_Number()
        {
            var input = "$";

            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Scientific_Number_With_Missing_Exponent()
        {
            var input = "1e";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Scientific_Number_Sign_Positive_With_Missing_Exponent_Digit()
        {
            var input = "1e+";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Scientific_Number_Sign_Negative_With_Missing_Exponent_Digit()
        {
            var input = "1e-";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Scientific_Number_With_Too_Many_Exponent_Markers()
        {
            var input = "1e2e3";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Scientific_Number_With_Decimal_Exponent()
        {
            var input = "1e.3";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Scientific_Number_With_Whitespace()
        {
            var input = "1e 23";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_Not_A_Number()
        {
            var input = ".e6";

            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }
        #endregion
    }
}
