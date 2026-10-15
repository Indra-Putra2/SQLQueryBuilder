using TruSql.Core.SQLEnum.SQLExpression;
using TruSql.Core.SQLExpression;
using TruSql.Helper;

namespace TruSql.Test.ParserTest
{
    public class StringTest
    {
        #region Pass
        [Theory]
        // Standard strings
        [InlineData("'abc123'", SQLLiteralType.String, "abc123")]
        [InlineData("'!@#$%^&*()_+'", SQLLiteralType.String, "!@#$%^&*()_+")]
        [InlineData("'hello world'", SQLLiteralType.String, "hello world")]
        [InlineData("''", SQLLiteralType.String, "")]

        // Escaped quotes
        [InlineData("'It''s working'", SQLLiteralType.String, "It's working")]
        [InlineData("'It''s ''great'''", SQLLiteralType.String, "It's 'great'")]

        // National / Unicode string
        [InlineData("N'hello'", SQLLiteralType.UnicodeString, "hello")]
        [InlineData("n'hello'", SQLLiteralType.UnicodeString, "hello")]

        // Whitespace and special syntax lookalikes
        [InlineData("'It\nworking'", SQLLiteralType.String, "It\nworking")]
        [InlineData("'hello -- world'", SQLLiteralType.String, "hello -- world")]
        [InlineData("'/* not a comment */'", SQLLiteralType.String, "/* not a comment */")]
        public void Parser_Should_Parse_String_Literals(string inputSql, SQLLiteralType expectedType, string expectedValue)
        {
            var exp = SQLBuilderHelper.BuildExpression(inputSql);
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(expectedType, literal.Type);
            Assert.Equal(expectedValue, literal.Value);
        }

        [Theory]
        [InlineData("dt'2020-05-01 20:30:15'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15)]
        [InlineData("dT'2020-05-01 20:30:15'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15)]
        [InlineData("Dt'2020-05-01 20:30:15'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15)]
        [InlineData("DT'2020-05-01 20:30:15'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15)]

        // Fraction Seconds
        [InlineData("dt'2020-05-01 20:30:15.1234567'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15.1234567)]
        [InlineData("dT'2020-05-01 20:30:15.1234567'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15.1234567)]
        [InlineData("Dt'2020-05-01 20:30:15.1234567'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15.1234567)]
        [InlineData("DT'2020-05-01 20:30:15.1234567'", SQLLiteralType.Datetime, 2020, 5, 1, 20, 30, 15.1234567)]
        public void Parser_Should_Parse_dateTime_Literals(
            string inputSql,
            SQLLiteralType expectedType,
            int year,
            int month,
            int day,
            int hours,
            int minutes,
            double seconds)
        {
            var exp = SQLBuilderHelper.BuildExpression(inputSql);
            var literal = Assert.IsType<LiteralExpression>(exp);

            var expected = new DateTime(year, month, day, hours, minutes, 0).AddSeconds(seconds);
            Assert.Equal(expected, literal.Value);
        }

        [Theory]
        [InlineData("d'2020-05-01'", SQLLiteralType.Date, 2020, 5, 1)]
        [InlineData("D'2020-05-01'", SQLLiteralType.Date, 2020, 5, 1)]
        [InlineData("d'1999-12-31'", SQLLiteralType.Date, 1999, 12, 31)]
        [InlineData("D'2026-08-27'", SQLLiteralType.Date, 2026, 8, 27)]
        public void Parser_Should_Parse_Date_Literals(
            string inputSql,
            SQLLiteralType expectedType,
            int year,
            int month,
            int day)
        {
            var exp = SQLBuilderHelper.BuildExpression(inputSql);
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(expectedType, literal.Type);

            var expected = new DateOnly(year, month, day);
            Assert.Equal(expected, literal.Value);
        }

        [Theory]
        // Standard Time
        [InlineData("t'20:30:15'", SQLLiteralType.Time, 20, 30, 15)]
        [InlineData("T'20:30:15'", SQLLiteralType.Time, 20, 30, 15)]

        // Fractional Seconds
        [InlineData("t'20:30:15.1234567'", SQLLiteralType.Time, 20, 30, 15.1234567)]
        [InlineData("T'20:30:15.1234567'", SQLLiteralType.Time, 20, 30, 15.1234567)]
        [InlineData("t'08:00:00.5'", SQLLiteralType.Time, 8, 0, 0.5)]
        public void Parser_Should_Parse_Time_Literals(
            string inputSql,
            SQLLiteralType expectedType,
            int hours,
            int minutes,
            double seconds)
        {
            var exp = SQLBuilderHelper.BuildExpression(inputSql);
            var literal = Assert.IsType<LiteralExpression>(exp);

            Assert.Equal(expectedType, literal.Type);

            var expected = new TimeOnly(hours, minutes, 0).Add(TimeSpan.FromSeconds(seconds));
            Assert.Equal(expected, literal.Value);
        }
        #endregion

        #region Fail
        [Fact]
        public void Parser_Should_Fail_String_Missing_End_Quote()
        {
            var input = "'hello";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        [Fact]
        public void Parser_Should_Fail_String_Missing_Opening_Quote()
        {
            var input = "hello'";

            Assert.Throws<FormatException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }
        #endregion
    }
}