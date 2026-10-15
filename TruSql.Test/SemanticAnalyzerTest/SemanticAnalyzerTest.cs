using HardwareInventoryLibrary.Ram;
using TruSql.Builder.SQLParser;
using TruSql.Core.SQLEnum.SemanticValidator;
using TruSql.Core.SQLExpression;
using TruSql.Helper;
using TruSql.Model.Context;
using TruSql.Model.QueryBuilder;
using TruSql.Model.SQL_Expression;
using System.Data;
using System.Reflection;

namespace TruSql.Test.SemanticAnalyzerTest
{
    public class SemanticAnalyzerTest
    {
        #region Pass
        #region Literal
        [Theory]
        [InlineData("'hello'", false, SQLExpressionType.String)]
        [InlineData("D'2020-05-01'", false, SQLExpressionType.Date)]
        [InlineData("t'20:30:12'", false, SQLExpressionType.Time)]
        [InlineData("Dt'2020-05-01 20:30:12'", false, SQLExpressionType.DateTime)]
        [InlineData("123", false, SQLExpressionType.Integer)]
        [InlineData("123.45", false, SQLExpressionType.Decimal)]
        [InlineData("1.5E10", false, SQLExpressionType.Double)]
        [InlineData("$100.00", false, SQLExpressionType.Decimal)]
        [InlineData("N'Hello 世界'", false, SQLExpressionType.String)]
        [InlineData("0x12AF", false, SQLExpressionType.Binary)]
        [InlineData("NULL", true, SQLExpressionType.NullLiteral)]
        public void Validator_Should_Analyze_Literals_Correctly(
            string input,
            bool expectedNullable,
            SQLExpressionType expectedType)
        {
            var metadata = GetMetadata(input);

            ValidateMetadata(expectedNullable, expectedType, metadata);
        }
        #endregion
        #region Column
        [Theory]
        [InlineData("Id", "r", SQLExpressionType.Integer)]
        [InlineData("Name", "r", SQLExpressionType.String)]
        [InlineData("BrandId", "r", SQLExpressionType.Integer)]
        [InlineData("MemoryType", "r", SQLExpressionType.String)]
        [InlineData("MemorySpeed", "r", SQLExpressionType.Integer)]
        [InlineData("Module", "r", SQLExpressionType.String)]
        [InlineData("TotalCapacity", "r", SQLExpressionType.Double)]
        [InlineData("FirstWordLatency", "r", SQLExpressionType.Double)]
        [InlineData("CASLatency", "r", SQLExpressionType.Double)]
        [InlineData("Rating", "r", SQLExpressionType.Double)]
        [InlineData("ReviewCount", "r", SQLExpressionType.Integer)]
        [InlineData("Color", "r", SQLExpressionType.String)]
        [InlineData("Price", "r", SQLExpressionType.Decimal)]
        [InlineData("PricePerGB", "r", SQLExpressionType.Decimal)]

        // Case Test
        [InlineData("name", "r", SQLExpressionType.String)]
        [InlineData("NAME", "r", SQLExpressionType.String)]
        [InlineData("price", "r", SQLExpressionType.Decimal)]
        [InlineData("PRICE", "r", SQLExpressionType.Decimal)]

        public void Validator_Should_Analyze_Column_Correctly(
            string columnInput,
            string tableAlias,
            SQLExpressionType expectedType)
        {
            var metadata = GetMetadata($"{tableAlias}.{columnInput}");

            var property = typeof(RamBaseModel).GetProperty(
                columnInput,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);

            Assert.NotNull(property);

            var nullabilityContext = new NullabilityInfoContext();
            var nullability = nullabilityContext.Create(property);

            var expectedNullable = nullability.ReadState == NullabilityState.Nullable;

            Assert.NotNull(metadata);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.False(metadata.IsConstant);
            Assert.Equal(expectedType, metadata.Type);
        }

        [Theory]
        [InlineData("r.Name")]
        [InlineData("R.Name")]
        [InlineData("r.NAME")]
        [InlineData("R.NAME")]
        public void Validator_Analyze_Column_Should_Accept_Case_Insensitive_Alias(
            string input)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
        }
        #endregion
        #region Wildcard
        [Theory]
        [InlineData("r.*")]
        [InlineData("R.*")]
        [InlineData("*")]
        public void Validator_Analyze_Wildcard_Should_Return_Wildcard(string input)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(SQLExpressionType.Unknown, metadata.Type);
            Assert.True(metadata.IsWildcard);
            Assert.False(metadata.IsConstant);
            Assert.False(metadata.IsNullable);
        }
        #endregion
        #region Function

        [Theory]
        [InlineData("COUNT(*) TYPE INT", SQLExpressionType.Integer, false, false)]
        [InlineData("COUNT(r.Name) TYPE INT", SQLExpressionType.Integer, false, false)]
        [InlineData("SUM(r.Price) TYPE DECIMAL", SQLExpressionType.Decimal, false, false)]
        [InlineData("AVG(r.Price) TYPE DECIMAL", SQLExpressionType.Decimal, false, false)]
        [InlineData("LOWER(r.Name) TYPE STRING", SQLExpressionType.String, false, false)]
        [InlineData("UPPER(r.Name) TYPE STRING", SQLExpressionType.String, false, false)]
        [InlineData("ABS(r.TotalCapacity) TYPE DOUBLE", SQLExpressionType.Double, false, false)]
        [InlineData("ROUND(r.Price, 2) TYPE INT", SQLExpressionType.Integer, false, false)]
        [InlineData("COUNT(NULL) TYPE INT", SQLExpressionType.Integer, false, false)]
        [InlineData("MIN(r.Price) TYPE INT", SQLExpressionType.Integer, false, false)]
        [InlineData("MAX(r.Price) TYPE INT", SQLExpressionType.Integer, false, false)]
        [InlineData("UNKOWN_FUNCTION('hello') TYPE STRING", SQLExpressionType.String, false, false)]
        [InlineData("COALESCE(NULL, 'hello') TYPE STRING", SQLExpressionType.String, false, false)]
        public void Validator_Should_Validate_Function_Properly(
            string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
            Assert.False(metadata.IsWildcard);
        }

        #endregion
        #region CASE
        [Theory]
        // --- 1. Searched CASE (No Base Expression) ---
        // All string branches, columns in conditions -> String, Not Constant, Not Nullable (has ELSE)
        [InlineData("CASE WHEN r.price >= 21 THEN 'Adult' WHEN r.name = 'Pending' THEN 'Wait' ELSE 'Unknown' END", SQLExpressionType.String, false, false)]
        // Pure constants everywhere! -> String, Constant, Not Nullable
        [InlineData("CASE WHEN 5 > 3 THEN 'Adult' ELSE 'Minor' END", SQLExpressionType.String, false, true)]

        // Missing ELSE clause -> Automatically forces IsNullable = true
        [InlineData("CASE WHEN (5 = 5) THEN 'Adult' END", SQLExpressionType.String, true, true)]

        // Nullable column in THEN branch propagates up
        [InlineData("CASE WHEN (5 = 5) THEN r.name ELSE 'Default' END", SQLExpressionType.String, true, false)]

        // Raw NULL literal in THEN branch blends into the String type container
        [InlineData("CASE WHEN (5 = 5) THEN NULL ELSE 'Fallback' END", SQLExpressionType.String, true, true)]


        // --- 2. Simple CASE (Has Base Expression) ---
        // Base expression matches WHEN types (Numbers), THEN matches ELSE (Strings)
        [InlineData("CASE r.price WHEN 1 THEN 'Active' WHEN 2 THEN 'Inactive' ELSE 'Unknown' END", SQLExpressionType.String, false, false)]

        // Pure constant Simple CASE -> Number, Constant, Not Nullable
        [InlineData("CASE 42 WHEN 40 THEN 100 WHEN 42 THEN 200 ELSE 0 END", SQLExpressionType.Integer, false, true)]

        // Simple CASE with a nullable THEN column
        [InlineData("CASE 1 WHEN 1 THEN FUNCBOOL() TYPE const bool? ELSE (5 = 5) END", SQLExpressionType.Boolean, true, true)]
        public void Validator_Must_Validate_CASE_Correctly(
            string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
        }
        #endregion
        #region IN
        [Theory]
        [InlineData("r.name IN ('a', 'b', 'c')", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.name IN ('a', NULL, 'c')", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.price IN (1, 2, 3)", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.price IN (1, NULL, 3)", SQLExpressionType.Boolean, true, false)]
        [InlineData("1 IN (1, 2, 3)", SQLExpressionType.Boolean, false, true)]
        [InlineData("'b' IN ('a', 'b', 'c')", SQLExpressionType.Boolean, false, true)]
        public void Validator_Must_Validate_IN_Correctly(
            string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
        }
        #endregion
        #region LIKE
        [Theory]
        [InlineData("r.name LIKE 'a%'", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.name LIKE '%hello%'", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.name LIKE NULL", SQLExpressionType.Boolean, true, false)]
        [InlineData("'hello' LIKE 'he%'", SQLExpressionType.Boolean, false, true)]
        [InlineData("'hello' LIKE '%llo'", SQLExpressionType.Boolean, false, true)]
        [InlineData("'hello' LIKE NULL", SQLExpressionType.Boolean, true, true)]
        [InlineData("r.name LIKE '%Admin%'", SQLExpressionType.Boolean, true, false)]
        [InlineData("'Hello' LIKE 'H%'", SQLExpressionType.Boolean, false, true)] // Pure Constant!
        public void Validator_Must_Validate_LIKE_Correctly(
            string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
        }
        #endregion
        #region Unary
        [Theory]
        // 1. Standard Numeric & Boolean Literals / Columns (Strict & Lenient)
        [InlineData("+5", SQLExpressionType.Integer)]
        [InlineData("-5", SQLExpressionType.Integer)]
        [InlineData("+r.price", SQLExpressionType.Decimal)]
        [InlineData("-r.price", SQLExpressionType.Decimal)]
        [InlineData("NOT FUNCBOOL() TYPE bool", SQLExpressionType.Boolean)]
        [InlineData("NOT MY_FUNCTION(*) TYPE BOOL", SQLExpressionType.Boolean)]
        // 2. Early Null Propagation (Should pass through as NullLiteral)
        [InlineData("+NULL", SQLExpressionType.NullLiteral)]
        [InlineData("-NULL", SQLExpressionType.NullLiteral)]
        public void Validator_Must_Validate_Unary_Correctly(string input, SQLExpressionType expectedType)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
        }
        #endregion
        #region Binary
        [Theory]
        // --- Arithmetic ---
        [InlineData("5 + 6", SQLExpressionType.Integer, false, true)]
        [InlineData("r.price - 10", SQLExpressionType.Decimal, true, false)] // Column is nullable, result becomes nullable
        [InlineData("5 + NULL", SQLExpressionType.Integer, true, true)] // Number container, but nullable and constant

        // --- Comparison ---
        [InlineData("5 = 6", SQLExpressionType.Boolean, false, true)]
        [InlineData("r.price > 10", SQLExpressionType.Boolean, true, false)]
        [InlineData("'abc' != 'xyz'", SQLExpressionType.Boolean, false, true)]
        [InlineData("5 = NULL", SQLExpressionType.Boolean, true, true)]

        // --- Logical ---
        [InlineData("FUNCBOOL() TYPE bool AND FAKE() TYPE BOOL", SQLExpressionType.Boolean, false, false)]
        [InlineData("5 = 5 OR (5 = 6)", SQLExpressionType.Boolean, false, true)]
        [InlineData("NULL AND 5 = 6", SQLExpressionType.Boolean, true, true)]

        // --- Bitwise ---
        [InlineData("5 & 3", SQLExpressionType.Integer, false, true)]
        [InlineData("r.ReviewCount | 1", SQLExpressionType.Integer, true, false)]
        [InlineData("5 & NULL", SQLExpressionType.Integer, true, true)]
        public void Validator_Must_Validate_Binary_Correctly(string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
        }
        #endregion
        #region IsNull
        [Theory]
        // --- IS NULL / IS NOT NULL ---
        [InlineData("r.price IS NULL", SQLExpressionType.Boolean, false, false)]
        [InlineData("r.name IS NOT NULL", SQLExpressionType.Boolean, false, false)]
        [InlineData("NULL IS NULL", SQLExpressionType.Boolean, false, true)] // Constant result!
        [InlineData("(5 + 6) IS NOT NULL", SQLExpressionType.Boolean, false, true)]

        public void Validator_Must_Validate_IsNull_Correctly(
            string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
        }
        #endregion
        #region Between
        [Theory]
        [InlineData("r.ReviewCount BETWEEN 0 AND 5", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.ReviewCount BETWEEN 0 AND 5.5", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.ReviewCount BETWEEN NULL AND 5", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.ReviewCount BETWEEN 0 AND NULL", SQLExpressionType.Boolean, true, false)]

        [InlineData("r.name BETWEEN 'a' AND 'z'", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.name BETWEEN NULL AND 'z'", SQLExpressionType.Boolean, true, false)]
        [InlineData("r.name BETWEEN 'a' AND NULL", SQLExpressionType.Boolean, true, false)]

        [InlineData("1 BETWEEN 0 AND 5", SQLExpressionType.Boolean, false, true)]
        [InlineData("'hello' BETWEEN 'a' AND 'z'", SQLExpressionType.Boolean, false, true)]

        public void Validator_Must_Validate_Between_Correctly(
            string input,
            SQLExpressionType expectedType,
            bool expectedNullable,
            bool expectedConstant)
        {
            var metadata = GetMetadata(input);

            Assert.NotNull(metadata);
            Assert.Equal(expectedType, metadata.Type);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.Equal(expectedConstant, metadata.IsConstant);
        }
        #endregion
        #endregion

        #region Fail
        #region Column
        [Theory]
        // Multi-part table reference is unsupported
        [InlineData("dbo.Rams.Name", typeof(SyntaxErrorException))]

        // Column without table alias
        [InlineData("Name", typeof(SyntaxErrorException))]

        // Unknown table alias
        [InlineData("new.hello", typeof(SyntaxErrorException))]

        // Unknown column
        [InlineData("r.hello", typeof(SyntaxErrorException))]

        // Multiple table qualifiers
        [InlineData("dbo.r.hello", typeof(SyntaxErrorException))]
        public void Validator_Analyze_Column_Should_Reject_Invalid_Column(string selected, Type expectedException)
        {
            Assert.Throws(expectedException, () => GetMetadata(selected));
        }
        #endregion
        #region Wildcard
        [Fact]
        public void Validator_Analyze_Wildcard_Should_Reject_Unknown_Alias()
        {
            var input = "doesNotExist.*";
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(SQLBuilderHelper.BuildSelect(input)[0], input));
        }
        [Theory]
        [InlineData("dbo.r.*")]
        [InlineData("a.b.*")]
        public void Validator_Analyze_Wildcard_Should_Reject_Multiple_Qualifiers(
            string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion
        #region Function
        [Theory]
        [InlineData("COUNT()")]
        [InlineData("COUNT(Name, Price)")]
        [InlineData("ABS()")]
        [InlineData("ABS(Price, 2)")]
        [InlineData("ROUND(Price)")]
        public void Validator_Should_Reject_Invalid_Function_Argument_Count(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }

        [Theory]
        [InlineData("SUM(*)")]
        [InlineData("AVG(*)")]
        [InlineData("ABS(*)")]
        [InlineData("LOWER(*)")]
        [InlineData("ROUND(*, 2)")]
        public void Validator_Should_Reject_Wildcard_When_Function_Does_Not_Accept_It(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }

        [Theory]
        [InlineData("UNKOWN_FUNCTION('hello')")]
        [InlineData("DOES_NOT_EXIST(123)")]
        public void Validator_Should_Reject_Unknown_Function_Without_Type(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion
        #region CASE
        [Theory]
        // --- Searched CASE Violations ---
        // Mismatched return types (Branch 1 returns String, Branch 2 returns Number)
        [InlineData("CASE WHEN (5 = 5) THEN 'Success' ELSE 999 END")]

        // Condition is not a boolean (r.price is a Number, not a Boolean condition)
        [InlineData("CASE WHEN r.price THEN 'Invalid' ELSE 'Valid' END")]

        // --- Simple CASE Violations ---
        // Base expression (Number) does not match WHEN expression type (String)
        [InlineData("CASE r.price WHEN 'not a number' THEN 'Error' ELSE 'OK' END")]

        // Base expression (String) does not match WHEN expression type (Number)
        [InlineData("CASE r.name WHEN 42 THEN 'Error' ELSE 'OK' END")]

        // Return types cannot be unified (Boolean vs String)
        [InlineData("CASE r.price WHEN 1 THEN FUNCBOOL() TYPE bool ELSE 'String Text' END")]

        // Simple CASE where the WHEN clause is a naked comparison condition instead of a value matching the base type
        [InlineData("CASE r.price WHEN (5 = 5) THEN 'Wrong Syntax' ELSE 'OK' END")]
        public void Validator_Must_Throw_On_Invalid_CASE_Strict(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion

        #region Unary
        [Theory]
        [InlineData("NOT 5")]            // Can't logical NOT a number
        [InlineData("NOT 'hello'")]      // Can't logical NOT a string
        [InlineData("-'hello'")]         // Can't negate a string
        [InlineData("-FUNCBOOL() TYPE bool")]     // Can't negate a boolean column
        public void Validator_Must_Throw_On_Invalid_Unary_Types(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion
        #region Binary

        [Theory]
        // Incompatible structural types
        [InlineData("5 + 'hello'")]       // Mixing explicit string and number
        [InlineData("r.price = 'abc'")]    // Mismatched comparison
        [InlineData("5 AND FAKE() TYPE BOOL")]         // Logical applied to a non-boolean number
        [InlineData("r.name & 3")]     // Bitwise applied to a string

        // Strict mode blocking Unknown variables
        [InlineData("tt.AnUnknownVar + 5")]
        [InlineData("tt.AnUnknownVar = 'abc'")]
        [InlineData("tt.AnUnknownVar AND true")]
        public void Validator_Must_Throw_On_Invalid_Binary_Strict(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }

        #endregion
        #region ISNull
        [Theory]
        [InlineData("r.name IS")]
        [InlineData("r.name IS NOT")]
        [InlineData("r.name IS NULL NULL")]
        [InlineData("r.name IS NOT NULL NULL")]
        [InlineData("r.name IS NOT NULL IS NULL")]
        public void Validator_Must_Throw_On_Invalid_IsNull_Strict(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion
        #region BETWEEN
        [Theory]
        [InlineData("r.price BETWEEN 'a' AND 'z'")]
        [InlineData("r.name BETWEEN 0 AND 5")]
        [InlineData("r.price BETWEEN 0 AND '5'")]
        [InlineData("r.name BETWEEN 'a' AND 5")]
        [InlineData("UNKNOWN_FUNCTION('hello') BETWEEN 'a' AND 'z'")]

        public void Validator_Must_Fail_Invalid_Between_Correctly(
            string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion
        #region IN
        [Theory]
        [InlineData("r.name IN (1, 2, 3)")]
        [InlineData("r.price IN ('a', 'b', 'c')")]
        [InlineData("r.price IN (1, '2', 3)")]
        [InlineData("r.name IN ('a', 2, 'c')")]
        [InlineData("UNKNOWN_FUNCTION(*) IN (1, 2, 3)")]
        public void Validator_Must_Throw_On_Incompatible_In_Types(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion
        #region LIKE
        [Theory]
        [InlineData("r.price LIKE '1%'")]
        [InlineData("r.name LIKE 123")]
        [InlineData("123 LIKE '1%'")]
        [InlineData("FUNCBOOL() TYPE bool LIKE 'true%'")]
        [InlineData("UNKNOWN_FUNCTION(*) LIKE 42")]
        [InlineData("r.price LIKE '%123%'")]
        [InlineData("'Text' LIKE 42")]
        public void Validator_Must_Throw_On_Invalid_LIKE(string input)
        {
            Assert.ThrowsAny<SyntaxErrorException>(() => GetMetadata(input));
        }
        #endregion

        #region FROM
        [Fact]
        public void _Validator_Validate_From_Source_Should_Reject_Duplicate_Alias()
        {
            var sources = new List<(string tableAlias, Type type)>()
            {
                ("r", typeof(RamBaseModel)),
                ("r", typeof(RamBaseModel))
            };

            Assert.Throws<SyntaxErrorException>(() => GetMetadata("'duplicate Alias'", sources));
        }
        #endregion

        #endregion

        #region Helper
        private SQLExpressionMetadata GetMetadata(string input)
        {
            var expr = SQLBuilderHelper.BuildExpression(input);
            var sources = new List<(string tableAlias, Type type)>() { ("r", typeof(RamBaseModel)) };
            return DebugAnalyzeExpression<RamReadModel>(expr, sources, input);
        }

        private SQLExpressionMetadata GetMetadata(SQLExpression expression, string input)
        {
            var sources = new List<(string tableAlias, Type type)>() { ("r", typeof(RamBaseModel)) };
            return DebugAnalyzeExpression<RamReadModel>(expression, sources, input);
        }

        private SQLExpressionMetadata GetMetadata(string input, List<(string tableAlias, Type type)> sources)
        {
            var expr = SQLBuilderHelper.BuildExpression(input);
            return DebugAnalyzeExpression<RamReadModel>(expr, sources, input);
        }

        private void ValidateMetadata(bool expectedNullable, SQLExpressionType expectedType, SQLExpressionMetadata metadata)
        {
            Assert.NotNull(metadata);
            Assert.Equal(expectedNullable, metadata.IsNullable);
            Assert.True(metadata.IsConstant);
            Assert.Equal(expectedType, metadata.Type);
        }

        private SQLExpressionMetadata DebugAnalyzeExpression<TargetDTO>(SQLExpression expression, IEnumerable<(string tableAlias, Type type)> sources, string input)
        {
            var dtoManager = new SQLDTOManager();
            dtoManager.RegisterDTO(typeof(TargetDTO));

            var tableManager = new SQLTableManager();

            foreach (var source in sources)
            {
                tableManager.RegisterTable(source.tableAlias, source.type);
            }

            var context = new SQLValidatorContext
            {
                DtoManager = dtoManager,
                TableManager = tableManager,
                Span = input.AsSpan()
            };
            return SQLSemanticValidator.AnalyzeExpression(expression, ref context);
        }
        #endregion
    }
}
