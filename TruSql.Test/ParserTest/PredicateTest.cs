using TruSql.Core.SQLEnum.SQLExpression;
using TruSql.Core.SQLExpression;
using TruSql.Helper;
using System.Data;

namespace TruSql.Test.ParserTest
{
    public class PredicateTest
    {
        #region Pass
        #region IS
        [Theory]
        [InlineData("is_active IS NULL", "is_active", false)]
        [InlineData("is_active IS NOT NULL", "is_active", true)]
        public void Parser_Should_Parse_IsNull_Predicate(string input, string expectedColumn, bool expectedIsNot)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input);

            // Assert
            var isNullExpr = Assert.IsType<IsNullExpression>(exp);
            Assert.Equal(expectedIsNot, isNullExpr.IsNot);

            var col = Assert.IsType<ColumnExpression>(isNullExpr.Operand);
            Assert.Equal(expectedColumn, col.Column.Value);
        }
        #endregion
        #region LIKE
        #region LIKE - Detailed Property Spot-Checks
        [Fact]
        public void Parser_Should_Populate_LIKE_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression("user_name LIKE 'Alice%'");

            // Assert
            var likeExpr = Assert.IsType<LikeExpression>(exp);
            Assert.False(likeExpr.IsNot);

            var operand = Assert.IsType<ColumnExpression>(likeExpr.Operand);
            Assert.Equal("user_name", operand.Column.Value);

            var pattern = Assert.IsType<LiteralExpression>(likeExpr.Pattern);
            Assert.Equal("Alice%", pattern.Value);
        }

        [Fact]
        public void Parser_Should_Populate_NOT_LIKE_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression("code NOT LIKE N'ERR_%'");

            // Assert
            var likeExpr = Assert.IsType<LikeExpression>(exp);
            Assert.True(likeExpr.IsNot);

            var operand = Assert.IsType<ColumnExpression>(likeExpr.Operand);
            Assert.Equal("code", operand.Column.Value);

            var pattern = Assert.IsType<LiteralExpression>(likeExpr.Pattern);
            Assert.Equal("ERR_%", pattern.Value);
        }
        [Fact]
        public void Parser_Should_Parse_Like_With_Weird_Syntax()
        {
            var exp = SQLBuilderHelper.BuildExpression("name LIKE (1 > 2)");
            var like = Assert.IsType<LikeExpression>(exp);

            var operand = Assert.IsType<ColumnExpression>(like.Operand);
            var pattern = Assert.IsType<ParenthesizedExpression>(like.Pattern);
            var binary = Assert.IsType<BinaryExpression>(pattern.Expression);
            var left = Assert.IsType<LiteralExpression>(binary.Left);
            var right = Assert.IsType<LiteralExpression>(binary.Right);

            Assert.Equal("name", operand.Column.Value);
            Assert.False(like.IsNot);
            Assert.Equal(1, left.Value);
            Assert.Equal(2, right.Value);
        }
        #endregion
        #region LIKE - Structural & Type Coverage
        [Theory]
        // Standard & NOT LIKE
        [InlineData("name LIKE 'John'", typeof(ColumnExpression), false, typeof(LiteralExpression))]
        [InlineData("name NOT LIKE '%Smith'", typeof(ColumnExpression), true, typeof(LiteralExpression))]
        [InlineData("code LIKE 'A_%_Z'", typeof(ColumnExpression), false, typeof(LiteralExpression))]

        // Identifiers & Unicode
        [InlineData("name LIKE pattern_col", typeof(ColumnExpression), false, typeof(ColumnExpression))]
        [InlineData("name LIKE N'%test%'", typeof(ColumnExpression), false, typeof(LiteralExpression))]

        // Complex Expressions & Functions
        [InlineData("name LIKE '%' + prefix", typeof(ColumnExpression), false, typeof(BinaryExpression))]
        [InlineData("LOWER(name) LIKE '%test%'", typeof(FunctionExpression), false, typeof(LiteralExpression))]

        // Semantically Odd / Valid Syntax
        [InlineData("name LIKE 123", typeof(ColumnExpression), false, typeof(LiteralExpression))]
        [InlineData("name LIKE NULL", typeof(ColumnExpression), false, typeof(LiteralExpression))]
        [InlineData("123 LIKE '%abc%'", typeof(LiteralExpression), false, typeof(LiteralExpression))]
        public void Parser_Should_Parse_LIKE_Predicate_Types(
            string input,
            Type expectedOperandType,
            bool expectedIsNot,
            Type expectedPatternType)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input);

            // Assert
            var likeExpr = Assert.IsType<LikeExpression>(exp);

            Assert.IsType(expectedOperandType, likeExpr.Operand);
            Assert.Equal(expectedIsNot, likeExpr.IsNot);
            Assert.IsType(expectedPatternType, likeExpr.Pattern);
        }
        #endregion
        #endregion
        #region IN
        #region IN - Detailed Property Spot-Checks
        [Fact]
        public void Parser_Should_Populate_IN_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression("user_id IN (1, 2, 3)");

            // Assert
            var inExpr = Assert.IsType<InExpression>(exp);
            Assert.False(inExpr.IsNot);

            var operand = Assert.IsType<ColumnExpression>(inExpr.Operand);
            Assert.Equal("user_id", operand.Column.Value);

            Assert.Equal(3, inExpr.Values.Count);

            var value1 = Assert.IsType<LiteralExpression>(inExpr.Values[0]);
            var value2 = Assert.IsType<LiteralExpression>(inExpr.Values[1]);
            var value3 = Assert.IsType<LiteralExpression>(inExpr.Values[2]);

            Assert.Equal(1, value1.Value);
            Assert.Equal(2, value2.Value);
            Assert.Equal(3, value3.Value);
        }

        [Fact]
        public void Parser_Should_Populate_NOT_IN_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression("status NOT IN ('Deleted', 'Banned')");

            // Assert
            var inExpr = Assert.IsType<InExpression>(exp);
            Assert.True(inExpr.IsNot);

            var operand = Assert.IsType<ColumnExpression>(inExpr.Operand);
            Assert.Equal("status", operand.Column.Value);

            Assert.Equal(2, inExpr.Values.Count);

            var value1 = Assert.IsType<LiteralExpression>(inExpr.Values[0]);
            var value2 = Assert.IsType<LiteralExpression>(inExpr.Values[1]);

            Assert.Equal("Deleted", value1.Value);
            Assert.Equal("Banned", value2.Value);
        }
        #endregion
        #region IN - Structural & Type Coverage
        [Theory]
        [InlineData("id IN (1,2,3)", typeof(ColumnExpression), false, 3)]
        [InlineData("id NOT IN (1,2,3)", typeof(ColumnExpression), true, 3)]

        // String literals
        [InlineData("name IN ('Bob','Alice')", typeof(ColumnExpression), false, 2)]

        // Column expressions
        [InlineData("id IN (min_id,max_id)", typeof(ColumnExpression), false, 2)]

        // Expressions inside list
        [InlineData("score IN (1 + 2, 5 * 3)", typeof(ColumnExpression), false, 2)]

        // Function values
        [InlineData("name IN (LOWER('BOB'), LOWER('ALICE'))", typeof(ColumnExpression), false, 2)]

        // Odd but syntactically valid
        [InlineData("123 IN (1,2,3)", typeof(LiteralExpression), false, 3)]
        [InlineData("name IN (NULL)", typeof(ColumnExpression), false, 1)]
        [InlineData("name IN ((1 + 2), (3 + 4))", typeof(ColumnExpression), false, 2)]

        public void Parser_Should_Parse_IN_Predicate_Types(
            string input,
            Type expectedOperandType,
            bool expectedIsNot,
            int expectedValueCount)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input);

            // Assert
            var inExpr = Assert.IsType<InExpression>(exp);

            Assert.IsType(expectedOperandType, inExpr.Operand);
            Assert.Equal(expectedIsNot, inExpr.IsNot);
            Assert.Equal(expectedValueCount, inExpr.Values.Count);
        }
        #endregion
        #endregion
        #region BETWEEN
        #region BETWEEN - Detailed Property Spot-Checks
        [Fact]
        public void Parser_Should_Populate_BETWEEN_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression("age BETWEEN 18 AND 30");

            // Assert
            var betweenExpr = Assert.IsType<BetweenExpression>(exp);

            Assert.False(betweenExpr.IsNot);

            var operand = Assert.IsType<ColumnExpression>(betweenExpr.Operand);
            Assert.Equal("age", operand.Column.Value);

            var lower = Assert.IsType<LiteralExpression>(betweenExpr.Lower);
            Assert.Equal(18, lower.Value);

            var upper = Assert.IsType<LiteralExpression>(betweenExpr.Upper);
            Assert.Equal(30, upper.Value);
        }

        [Fact]
        public void Parser_Should_Populate_NOT_BETWEEN_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression("price NOT BETWEEN 100 AND 500");

            // Assert
            var betweenExpr = Assert.IsType<BetweenExpression>(exp);

            Assert.True(betweenExpr.IsNot);

            var operand = Assert.IsType<ColumnExpression>(betweenExpr.Operand);
            Assert.Equal("price", operand.Column.Value);

            var lower = Assert.IsType<LiteralExpression>(betweenExpr.Lower);
            Assert.Equal(100, lower.Value);

            var upper = Assert.IsType<LiteralExpression>(betweenExpr.Upper);
            Assert.Equal(500, upper.Value);
        }
        [Fact]
        public void Parser_Should_Not_Confuse_BETWEEN_AND_With_Logical_AND()
        {
            var exp = SQLBuilderHelper.BuildExpression("age BETWEEN 1 AND 10 AND active = 1");

            var root = Assert.IsType<BinaryExpression>(exp);

            Assert.Equal(SQLBinaryOperator.AND, root.Operator);

            var between = Assert.IsType<BetweenExpression>(root.Left);
            Assert.Equal("age", ((ColumnExpression)between.Operand).Column.Value);
        }
        [Fact]
        public void Parser_Should_Parse_Multiple_BETWEEN_Predicates()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                "age BETWEEN 10 AND 20 AND score BETWEEN 100 AND 200"
            );

            var root = Assert.IsType<BinaryExpression>(exp);

            Assert.Equal(SQLBinaryOperator.AND, root.Operator);

            Assert.IsType<BetweenExpression>(root.Left);
            Assert.IsType<BetweenExpression>(root.Right);
        }
        [Fact]
        public void Parser_Should_Parse_Negative_BETWEEN_Bounds()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                "temperature BETWEEN -10 AND 40"
            );

            var between = Assert.IsType<BetweenExpression>(exp);

            Assert.IsType<UnaryExpression>(between.Lower);
            Assert.IsType<LiteralExpression>(between.Upper);
        }
        [Fact]
        public void Parser_Should_Parse_Between_With_Weird_Syntax()
        {
            var exp = SQLBuilderHelper.BuildExpression("age BETWEEN (1 + 2) AND (5 * 2)");
            var between = Assert.IsType<BetweenExpression>(exp);

            var operand = Assert.IsType<ColumnExpression>(between.Operand);
            var parenLower = Assert.IsType<ParenthesizedExpression>(between.Lower);
            var parenUpper = Assert.IsType<ParenthesizedExpression>(between.Upper);
            var binaryLower = Assert.IsType<BinaryExpression>(parenLower.Expression);
            var binaryUpper = Assert.IsType<BinaryExpression>(parenUpper.Expression);
            var leftLower = Assert.IsType<LiteralExpression>(binaryLower.Left);
            var rightLower = Assert.IsType<LiteralExpression>(binaryLower.Right);
            var leftUpper = Assert.IsType<LiteralExpression>(binaryUpper.Left);
            var rightUpper = Assert.IsType<LiteralExpression>(binaryUpper.Right);

            Assert.Equal("age", operand.Column.Value);
            Assert.False(between.IsNot);
            Assert.Equal(1, leftLower.Value);
            Assert.Equal(5, leftUpper.Value);
            Assert.Equal(2, rightLower.Value);
            Assert.Equal(2, rightUpper.Value);
        }
        [Theory]
        [InlineData("active = 1 AND age BETWEEN 1 AND 10")]
        [InlineData("name LIKE 'Bob' OR age BETWEEN 1 AND 10")]
        [InlineData("deleted IS NULL OR name NOT LIKE '%test%'")]
        public void Parser_Should_Parse_Predicate_With_Logical_Operators(string input)
        {
            var exp = SQLBuilderHelper.BuildExpression(input);

            Assert.IsType<BinaryExpression>(exp);
        }
        [Theory]
        [InlineData("NOT age BETWEEN 1 AND 10")]
        [InlineData("NOT name LIKE 'Bob'")]
        [InlineData("NOT is_active IS NULL")]
        public void Parser_Should_Handle_Unary_NOT_Separately_From_Predicate_NOT(string input)
        {
            var exp = SQLBuilderHelper.BuildExpression(input);

            Assert.IsType<UnaryExpression>(exp);
        }
        #endregion
        #region BETWEEN - Structural & Type Coverage
        [Theory]
        [InlineData("age BETWEEN 18 AND 30", typeof(ColumnExpression), false, typeof(LiteralExpression), typeof(LiteralExpression))]
        [InlineData("price NOT BETWEEN 100 AND 500", typeof(ColumnExpression), true, typeof(LiteralExpression), typeof(LiteralExpression))]

        // Expression bounds
        [InlineData("score BETWEEN min_score AND max_score", typeof(ColumnExpression), false, typeof(ColumnExpression), typeof(ColumnExpression))]

        // Arithmetic bounds
        [InlineData("value BETWEEN 10 + 5 AND 100 - 10", typeof(ColumnExpression), false, typeof(BinaryExpression), typeof(BinaryExpression))]

        // Function operand
        [InlineData("LEN(name) BETWEEN 1 AND 10", typeof(FunctionExpression), false, typeof(LiteralExpression), typeof(LiteralExpression))]

        // Weird but syntactically valid
        [InlineData("123 BETWEEN 1 AND 5", typeof(LiteralExpression), false, typeof(LiteralExpression), typeof(LiteralExpression))]
        [InlineData("age BETWEEN NULL AND NULL", typeof(ColumnExpression), false, typeof(LiteralExpression), typeof(LiteralExpression))]
        public void Parser_Should_Parse_BETWEEN_Predicate_Types(
            string input,
            Type expectedOperandType,
            bool expectedIsNot,
            Type expectedLowerType,
            Type expectedUpperType)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input);

            // Assert
            var betweenExpr = Assert.IsType<BetweenExpression>(exp);
            Assert.IsType(expectedOperandType, betweenExpr.Operand);
            Assert.Equal(expectedIsNot, betweenExpr.IsNot);

            Assert.IsType(expectedLowerType, betweenExpr.Lower);
            Assert.IsType(expectedUpperType, betweenExpr.Upper);
        }
        #endregion
        #endregion
        #endregion

        #region Fail
        #region IS
        [Theory]
        [InlineData("is_active IS NULL hello")]
        [InlineData("is_active IS")]
        [InlineData("is_active IS NOT")]
        [InlineData("is_active NOT NULL")]
        [InlineData("is_active IS False")]
        [InlineData("is_active IS True")]
        [InlineData("is_active IS UNKNOWN")]
        [InlineData("is_active IS NOT 'Correct'")]
        public void Parser_Should_Fail_Invalid_IsNull_Predicates(string input)
        {
            // Act & Assert
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input, true);
            });
        }
        #endregion
        #region LIKE
        [Theory]
        // 1. Missing pattern (incomplete expression)
        [InlineData("name LIKE")]
        [InlineData("name NOT LIKE")]

        // 2. Missing left operand
        [InlineData("LIKE 'John'")]
        [InlineData("NOT LIKE 'John'")]

        // 3. Invalid keyword order or syntax
        [InlineData("name IS LIKE '%John%'")]        // 'IS LIKE' is invalid SQL
        [InlineData("name LIKE NOT '%John%'")]       // 'LIKE NOT' instead of 'NOT LIKE'

        // 4. Trailing unparsed tokens
        [InlineData("name LIKE 'John' extra_token")]
        [InlineData("name NOT LIKE 'John' 123")]

        // 5. Incomplete sub-expressions on either side
        [InlineData("name + LIKE 'John'")]
        [InlineData("name LIKE 'John' +")]

        public void Parser_Should_Fail_Invalid_LIKE_Predicates(string input)
        {
            // Act & Assert
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input, true);
            });
        }
        #endregion
        #region BETWEEN
        [Theory]
        [InlineData("age BETWEEN")]
        [InlineData("age NOT BETWEEN")]
        [InlineData("age NOT NOT BETWEEN 1 AND 10")]

        // Missing lower bound
        [InlineData("age BETWEEN AND 10")]
        [InlineData("age BETWEEN AND")]

        // Missing AND keyword
        [InlineData("age BETWEEN 1 10")]
        [InlineData("age BETWEEN 1 OR 10")]

        // Missing upper bound
        [InlineData("age BETWEEN 1 AND")]
        [InlineData("age NOT BETWEEN 1 AND")]

        // Missing left operand
        [InlineData("BETWEEN 1 AND 10")]
        [InlineData("NOT BETWEEN 1 AND 10")]

        // Invalid keyword order
        [InlineData("age BETWEEN NOT 1 AND 10")]
        [InlineData("age BETWEEN 1 NOT AND 10")]
        [InlineData("age AND BETWEEN 1 AND 10")]

        // LIKE-style wrong placement
        [InlineData("age BETWEEN NOT 1")]
        [InlineData("age BETWEEN 1 LIKE 10")]

        // Trailing unparsed tokens
        [InlineData("age BETWEEN 1 AND 10 extra_token")]
        [InlineData("age NOT BETWEEN 1 AND 10 123")]

        // Invalid expressions around predicate
        [InlineData("age + BETWEEN 1 AND 10")]
        [InlineData("age BETWEEN 1 AND 10 +")]

        // Missing closing parenthesis
        [InlineData("age BETWEEN (1 + 2 AND 10")]
        [InlineData("age BETWEEN 1 AND (10 + 5")]

        public void Parser_Should_Fail_Invalid_BETWEEN_Predicates(string input)
        {
            // Act & Assert
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input, true);
            });
        }
        #endregion
        #region IN
        [Theory]
        [InlineData("id IN")]
        [InlineData("id NOT IN")]

        // Missing parentheses
        [InlineData("id IN 1,2,3")]
        [InlineData("id IN 1")]

        // Empty list
        [InlineData("id IN ()")]

        // Missing values
        [InlineData("id IN (,)")]
        [InlineData("id IN (1,)")]
        [InlineData("id IN (,1)")]

        // Broken NOT placement
        [InlineData("id IN NOT (1,2,3)")]
        [InlineData("id NOT NOT IN (1,2,3)")]

        // Missing closing parenthesis
        [InlineData("id IN (1,2,3")]

        // Invalid trailing tokens
        [InlineData("id IN (1,2,3) extra")]
        [InlineData("id IN (1,2,3) 123")]

        public void Parser_Should_Fail_Invalid_IN_Predicates(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input, true);
            });
        }
        #endregion
        #endregion
    }
}