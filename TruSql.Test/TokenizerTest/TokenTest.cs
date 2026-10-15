using TruSql.Core.SQLEnum.Tokenizer;
using TruSql.Core.Tokenizer;
using TruSql.Helper;
using TruSql.Model;
using TruSql.Model.Context;
using System.Data;

namespace TruSql.Test.TokenizerTest;

public class TokenTest
{
    private Random _random = new Random();

    [Theory]
    [InlineData("'Hello'", "'Hello'", SQLTokenType.StringLiteral)]
    [InlineData("123", "123", SQLTokenType.IntegerLiteral)]
    [InlineData("123.45", "123.45", SQLTokenType.DecimalLiteral)]
    [InlineData("1.5E10", "1.5E10", SQLTokenType.ScientificLiteral)]
    [InlineData("NULL", "NULL", SQLTokenType.NullLiteral)]
    public void Tokenizer_Should_Tokenize_Literals(
        string input,
        string expectedValue,
        SQLTokenType expectedType)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(expectedValue, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Theory]
    [InlineData("Hello", "Hello")]
    [InlineData("myColumn", "myColumn")]
    [InlineData("_private", "_private")]
    [InlineData("Column123", "Column123")]
    public void Tokenizer_Should_Tokenize_Identifier(
        string input,
        string expectedValue)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(SQLTokenType.Identifier, tokens[0].Type);
    }
    [Theory]
    [InlineData("d'2026-05-20'", "d'2026-05-20'", SQLTokenType.DateLiteral)]
    [InlineData("t'12:30:00'", "t'12:30:00'", SQLTokenType.TimeLiteral)]
    [InlineData("dt'2026-05-20 12:30:00'", "dt'2026-05-20 12:30:00'", SQLTokenType.DatetimeLiteral)]

    // Case-insensitivity
    [InlineData("D'2026-05-20'", "D'2026-05-20'", SQLTokenType.DateLiteral)]
    [InlineData("T'12:30:00'", "T'12:30:00'", SQLTokenType.TimeLiteral)]
    [InlineData("DT'2026-05-20 12:30:00'", "DT'2026-05-20 12:30:00'", SQLTokenType.DatetimeLiteral)]

    // Zero boundaries
    [InlineData("d'0001-01-01'", "d'0001-01-01'", SQLTokenType.DateLiteral)]
    [InlineData("d'2026-01-01'", "d'2026-01-01'", SQLTokenType.DateLiteral)]
    [InlineData("d'2026-12-31'", "d'2026-12-31'", SQLTokenType.DateLiteral)]

    [InlineData("t'00:00:00'", "t'00:00:00'", SQLTokenType.TimeLiteral)]
    [InlineData("t'23:59:59'", "t'23:59:59'", SQLTokenType.TimeLiteral)]

    // Fractional seconds
    [InlineData("t'12:30:00.1'", "t'12:30:00.1'", SQLTokenType.TimeLiteral)]
    [InlineData("t'12:30:00.1234567'", "t'12:30:00.1234567'", SQLTokenType.TimeLiteral)]

    [InlineData("dt'2026-05-20 12:30:00.1234567'",
                "dt'2026-05-20 12:30:00.1234567'",
                SQLTokenType.DatetimeLiteral)]
    public void Tokenizer_Should_Tokenize_Datetime(
    string input,
    string expectedValue,
    SQLTokenType exprectedToken
    )
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(exprectedToken, tokens[0].Type);
    }

    [Theory]
    [InlineData("@myVar", "@myVar")]
    [InlineData("@value", "@value")]
    [InlineData("@@version", "@@version")]
    public void Tokenizer_Should_Tokenize_Variable(
        string input,
        string expectedValue
        )
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(SQLTokenType.Variable, tokens[0].Type);
    }

    [Theory]
    [InlineData("'Hello'", "'Hello'", SQLTokenType.StringLiteral)]
    [InlineData("'Hello World'", "'Hello World'", SQLTokenType.StringLiteral)]
    [InlineData("N'Unicode'", "N'Unicode'", SQLTokenType.UnicodeString)]
    [InlineData("D'2020-05-01'", "D'2020-05-01'", SQLTokenType.DateLiteral)]
    [InlineData("[Column]", "[Column]", SQLTokenType.BracketIdentifier)]
    [InlineData("\"Column\"", "\"Column\"", SQLTokenType.DoubleQuoteIdentifier)]
    [InlineData("[hello]", "[hello]", SQLTokenType.BracketIdentifier)]
    [InlineData("[[hello]", "[[hello]", SQLTokenType.BracketIdentifier)]
    [InlineData("[hello]]world]", "[hello]]world]", SQLTokenType.BracketIdentifier)]
    [InlineData("\"hello\"", "\"hello\"", SQLTokenType.DoubleQuoteIdentifier)]
    [InlineData("\"hello\"\"world\"", "\"hello\"\"world\"", SQLTokenType.DoubleQuoteIdentifier)]
    [InlineData("\"\"", "\"\"", SQLTokenType.DoubleQuoteIdentifier)]
    public void Tokenizer_Should_Tokenize_Quoted_Values(
        string input,
        string expectedValue,
        SQLTokenType expectedType)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(expectedValue, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Theory]
    [InlineData(".", SQLTokenType.Dot)]
    [InlineData(",", SQLTokenType.Comma)]
    [InlineData("(", SQLTokenType.LeftParen)]
    [InlineData(")", SQLTokenType.RightParen)]
    [InlineData(";", SQLTokenType.Semicolon)]
    [InlineData("::", SQLTokenType.ScopeResolution)]
    public void Tokenizer_Should_Tokenize_Punctuation(
        string input,
        SQLTokenType expectedType
        )
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(input, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Theory]
    [InlineData("+", SQLTokenType.Plus)]
    [InlineData("-", SQLTokenType.Minus)]
    [InlineData("/", SQLTokenType.Divide)]
    [InlineData("%", SQLTokenType.Modulo)]
    [InlineData("*", SQLTokenType.Asterisk)]
    public void Tokenizer_Should_Tokenize_Math_Operators(
        string input,
        SQLTokenType expectedType)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(input, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Theory]
    [InlineData("&", SQLTokenType.BitwiseAnd)]
    [InlineData("|", SQLTokenType.BitwiseOr)]
    [InlineData("^", SQLTokenType.BitwiseXor)]
    [InlineData("~", SQLTokenType.BitwiseNot)]
    [InlineData("<<", SQLTokenType.BitwiseLeftShift)]
    [InlineData(">>", SQLTokenType.BitwiseRightShift)]
    public void Tokenizer_Should_Tokenize_Bitwise_Operators(
        string input,
        SQLTokenType expectedType)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(input, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Theory]
    [InlineData("=", SQLTokenType.Equal)]
    [InlineData("<>", SQLTokenType.NotEqual)]
    [InlineData("!=", SQLTokenType.NotEqual)]
    [InlineData("<", SQLTokenType.LessThan)]
    [InlineData("<=", SQLTokenType.LessThanOrEqual)]
    [InlineData(">", SQLTokenType.GreaterThan)]
    [InlineData(">=", SQLTokenType.GreaterThanOrEqual)]
    public void Tokenizer_Should_Tokenize_Comparison_Operators(
        string input,
        SQLTokenType expectedType)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(input, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Theory]
    [InlineData("+=", SQLTokenType.PlusEquals)]
    [InlineData("-=", SQLTokenType.MinusEquals)]
    [InlineData("*=", SQLTokenType.MultiplyEquals)]
    [InlineData("/=", SQLTokenType.DivideEquals)]
    [InlineData("%=", SQLTokenType.ModuloEquals)]
    public void Tokenizer_Should_Tokenize_Compound_Assignment(
        string input,
        SQLTokenType expectedType)
    {
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(input, context.GetSlice(tokens[0]).ToString());
        Assert.Equal(expectedType, tokens[0].Type);
    }

    [Fact]
    public void Tokenizer_Should_Tokenize_Simple_Expression()
    {
        var input = "Price >= 100";
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal(4, tokens.Count);

        Assert.Equal("Price", context.GetSlice(tokens[0]).ToString());
        Assert.Equal(SQLTokenType.Identifier, tokens[0].Type);

        Assert.Equal(">=", context.GetSlice(tokens[1]).ToString());
        Assert.Equal(SQLTokenType.GreaterThanOrEqual, tokens[1].Type);

        Assert.Equal("100", context.GetSlice(tokens[2]).ToString());
        Assert.Equal(SQLTokenType.IntegerLiteral, tokens[2].Type);
    }

    [Fact]
    public void Tokenizer_Should_Tokenize_Complex_Expression()
    {
        var input = "Price >= 100 AND Name = 'John'";
        var context = CreateContext(input);
        ValidateContext(0, input.AsSpan(), ref context);
        var tokens = GetTokens(ref context);

        Assert.Equal("Price", context.GetSlice(tokens[0]).ToString());
        Assert.Equal(SQLTokenType.Identifier, tokens[0].Type);

        Assert.Equal(">=", context.GetSlice(tokens[1]).ToString());
        Assert.Equal(SQLTokenType.GreaterThanOrEqual, tokens[1].Type);

        Assert.Equal("100", context.GetSlice(tokens[2]).ToString());
        Assert.Equal(SQLTokenType.IntegerLiteral, tokens[2].Type);

        Assert.Equal("AND", context.GetSlice(tokens[3]).ToString());
        Assert.Equal(SQLTokenType.Keyword, tokens[3].Type);

        Assert.Equal("Name", context.GetSlice(tokens[4]).ToString());
        Assert.Equal(SQLTokenType.Identifier, tokens[4].Type);

        Assert.Equal("=", context.GetSlice(tokens[5]).ToString());
        Assert.Equal(SQLTokenType.Equal, tokens[5].Type);

        Assert.Equal("'John'", context.GetSlice(tokens[6]).ToString());
        Assert.Equal(SQLTokenType.StringLiteral, tokens[6].Type);
    }

    [Fact]
    public void Tokenizer_Should_Tokenize_Keywords()
    {
        IReadOnlyList<SQLToken> tokens;
        foreach (var sqlKeyword in SQLDictionary.SQLKeywords)
        {
            //Random Case 10x
            for (int i = 0; i < 10; i++)
            {
                var randomCase = RandomizeCase(sqlKeyword);
                var contextRandomCase = CreateContext(randomCase);
                ValidateContext(0, randomCase.AsSpan(), ref contextRandomCase);
                tokens = GetTokens(ref contextRandomCase);

                Assert.Equal(2, tokens.Count);
                Assert.Equal(SQLTokenType.Keyword, tokens[0].Type);
            }

            //all Upper Case
            var upperCase = sqlKeyword.ToUpper();
            var contextUpper = CreateContext(upperCase);
            ValidateContext(0, upperCase.AsSpan(), ref contextUpper);
            tokens = GetTokens(ref contextUpper);

            Assert.Equal(2, tokens.Count);
            Assert.Equal(SQLTokenType.Keyword, tokens[0].Type);

            //all Lower Case
            var lowerCase = sqlKeyword.ToLower();
            var contextLower = CreateContext(lowerCase);
            ValidateContext(0, lowerCase.AsSpan(), ref contextLower);
            tokens = GetTokens(ref contextLower);

            Assert.Equal(2, tokens.Count);
            Assert.Equal(SQLTokenType.Keyword, tokens[0].Type);
        }
    }

    [Fact]
    public void Tokenizer_Should_Tokenize_Function_Type()
    {
        foreach (var _sqlDataType in SQLDictionary.SQLCustomDataTypes)
        {
            IReadOnlyList<SQLToken> tokens;
            //Random Case 10x
            for (int i = 0; i < 10; i++)
            {
                var randomCase = RandomizeCase(_sqlDataType);
                var contextRandomCase = CreateContext(randomCase);
                ValidateContext(0, randomCase.AsSpan(), ref contextRandomCase);
                tokens = GetTokens(ref contextRandomCase);

                Assert.Equal(2, tokens.Count);
                Assert.Equal(SQLTokenType.SQLType, tokens[0].Type);
            }

            //all Upper Case
            var upperCase = _sqlDataType.ToUpper();
            var contextUpper = CreateContext(upperCase);
            ValidateContext(0, upperCase.AsSpan(), ref contextUpper);
            tokens = GetTokens(ref contextUpper);

            Assert.Equal(2, tokens.Count);
            Assert.Equal(SQLTokenType.SQLType, tokens[0].Type);

            //all Upper Case
            var lowerCase = _sqlDataType.ToLower();
            var contextLower = CreateContext(lowerCase);
            ValidateContext(0, lowerCase.AsSpan(), ref contextLower);
            tokens = GetTokens(ref contextLower);

            Assert.Equal(2, tokens.Count);
            Assert.Equal(SQLTokenType.SQLType, tokens[0].Type);
        }
    }

    [Theory]
    [InlineData("", typeof(ArgumentNullException))]
    [InlineData("'hello", typeof(FormatException))]
    [InlineData("\"hello", typeof(FormatException))]
    [InlineData("[hello", typeof(FormatException))]
    [InlineData("N'hello", typeof(FormatException))]
    [InlineData("1.2.3", typeof(FormatException))]
    [InlineData("1.5E", typeof(FormatException))]
    [InlineData("\"", typeof(FormatException))]
    [InlineData("\"\"name\"", typeof(FormatException))]
    public void Tokenizer_Should_Fail_To_Tokenize(
        string input,
        Type expectedException)
    {
        var exception = Assert.Throws(
            expectedException,
            () => CreateContext(input));

        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData("d'2020/05/01'")]
    [InlineData("d'2020-13-01'")]
    [InlineData("d'2020-02-30'")]
    [InlineData("d'20-05-01'")]
    [InlineData("d'2020-5-01'")]
    [InlineData("d'2020-05-1'")]

    [InlineData("t'25:00:00'")]
    [InlineData("t'12:60:00'")]
    [InlineData("t'12:30:60'")]
    [InlineData("t'12:30'")]
    [InlineData("t'12:30:00.12345678'")]
    [InlineData("t'12-30-00'")]

    [InlineData("dt'2020/05/01 12:30:00'")]
    [InlineData("dt'2020-13-01 12:30:00'")]
    [InlineData("dt'2020-02-30 12:30:00'")]
    [InlineData("dt'2020-05-01 25:00:00'")]
    [InlineData("dt'2020-05-01 12:60:00'")]
    [InlineData("dt'2020-05-01 12:30:60'")]
    [InlineData("dt'2020-05-01 12:30:00.12345678'")]
    [InlineData("dt'2020-05-01'")]
    [InlineData("dt'2020-05-01 12:30'")]
    public void Tokenizer_Should_Reject_Invalid_DateTime_Literals(string inputSql)
    {
        Assert.Throws<FormatException>(
            () => CreateContext(inputSql));
    }

    private SQLParserContext CreateContext(string inputSql)
    {
        return SQLBuilderHelper.BuildTokens(inputSql);
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
    string RandomizeCase(string input)
    {
        return string.Concat(
            input.Select(c =>
                _random.Next(2) == 0
                    ? char.ToLowerInvariant(c)
                    : char.ToUpperInvariant(c)));
    }
}