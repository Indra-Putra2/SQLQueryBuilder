using TruSql.Core.SQLEnum.SQLExpression;
using TruSql.Core.SQLExpression;
using TruSql.Helper;
using System.Data;

namespace TruSql.Test.ParserTest
{
    public class BinaryTest
    {
        #region Pass
        #region Arithmetic & Comparison
        [Theory]
        [InlineData("-1 + 2", SQLBinaryOperator.Add)]
        [InlineData("1 + -2", SQLBinaryOperator.Add)]
        [InlineData("1 * -2", SQLBinaryOperator.Multiply)]
        [InlineData("1 - -2", SQLBinaryOperator.Subtract)]
        public void Parser_Should_Parse_Unary_Inside_Binary(
            string input,
            SQLBinaryOperator expectedOperator)
        {
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedOperator, binary.Operator);

            Assert.IsType<UnaryExpression>(binary.Left is UnaryExpression
                ? binary.Left
                : binary.Right);
        }

        [Theory]
        // Arithmetic
        [InlineData("1+2", 1, SQLBinaryOperator.Add, 2)]
        [InlineData("5-6", 5, SQLBinaryOperator.Subtract, 6)]
        [InlineData("4*3", 4, SQLBinaryOperator.Multiply, 3)]
        [InlineData("10/3", 10, SQLBinaryOperator.Divide, 3)]
        [InlineData("8%9", 8, SQLBinaryOperator.Modulo, 9)]
        // Comparison
        [InlineData("10 = 2", 10, SQLBinaryOperator.Equal, 2)]
        [InlineData("11 <> 2", 11, SQLBinaryOperator.NotEqual, 2)]
        [InlineData("13 != 1", 13, SQLBinaryOperator.NotEqual, 1)]
        [InlineData("14 > 5", 14, SQLBinaryOperator.GreaterThan, 5)]
        [InlineData("15 >= 7", 15, SQLBinaryOperator.GreaterThanOrEqual, 7)]
        [InlineData("76 < 45", 76, SQLBinaryOperator.LessThan, 45)]
        [InlineData("34 <= 54", 34, SQLBinaryOperator.LessThanOrEqual, 54)]
        public void Parser_Should_Parse_Binary_Expression_With_Numeric_Literals(
            string input,
            int expectedLeft,
            SQLBinaryOperator expectedOp,
            int expectedRight)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            // Assert
            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedOp, binary.Operator);

            var left = Assert.IsType<LiteralExpression>(binary.Left);
            var right = Assert.IsType<LiteralExpression>(binary.Right);

            Assert.Equal(expectedLeft, left.Value);
            Assert.Equal(expectedRight, right.Value);
        }
        #endregion
        #region Logical
        [Theory]
        // Logical
        [InlineData("a OR b", "a", SQLBinaryOperator.OR, "b")]
        [InlineData("c AND d", "c", SQLBinaryOperator.AND, "d")]
        public void Parser_Should_Parse_Binary_Expression_With_Column_Identifiers(
            string input,
            string expectedLeftColumn,
            SQLBinaryOperator expectedOp,
            string expectedRightColumn)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            // Assert
            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedOp, binary.Operator);

            var left = Assert.IsType<ColumnExpression>(binary.Left);
            var right = Assert.IsType<ColumnExpression>(binary.Right);

            Assert.Equal(expectedLeftColumn, left.Column.Value);
            Assert.Equal(expectedRightColumn, right.Column.Value);
        }
        #endregion
        #region Bitwise
        [Theory]
        [InlineData("1 | 2", 1, SQLBinaryOperator.BitwiseOr, 2)]
        [InlineData("4 ^ 5", 4, SQLBinaryOperator.BitwiseXor, 5)]
        [InlineData("6 & 7", 6, SQLBinaryOperator.BitwiseAnd, 7)]
        [InlineData("5 >> 1", 5, SQLBinaryOperator.BitwiseRightShift, 1)]
        [InlineData("5 << 1", 5, SQLBinaryOperator.BitwiseLeftShift, 1)]
        public void Parser_Should_Parse_Binary_Expression_With_Literal(
            string input,
            int expectedLeftColumn,
            SQLBinaryOperator expectedOp,
            int expectedRightColumn)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            // Assert
            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedOp, binary.Operator);

            var left = Assert.IsType<LiteralExpression>(binary.Left);
            var right = Assert.IsType<LiteralExpression>(binary.Right);

            Assert.Equal(expectedLeftColumn, left.Value);
            Assert.Equal(expectedRightColumn, right.Value);
        }
        #endregion
        #region Precedence
        [Theory]
        [InlineData("1 + 2 * 3", SQLBinaryOperator.Add, SQLBinaryOperator.Multiply)]
        [InlineData("1 OR 2 AND 3", SQLBinaryOperator.OR, SQLBinaryOperator.AND)]
        public void Parser_Should_Parse_Right_Heavy_Precedence(
            string input,
            SQLBinaryOperator expectedRootOp,
            SQLBinaryOperator expectedRightOp)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            // Assert Root: 1 op1 (2 op2 3)
            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedRootOp, binary.Operator);

            var left = Assert.IsType<LiteralExpression>(binary.Left);
            var right = Assert.IsType<BinaryExpression>(binary.Right);

            // Assert Right Child: (2 op2 3)
            Assert.Equal(expectedRightOp, right.Operator);
            var rightLeft = Assert.IsType<LiteralExpression>(right.Left);
            var rightRight = Assert.IsType<LiteralExpression>(right.Right);

            // Assert Values
            Assert.Equal(1, left.Value);
            Assert.Equal(2, rightLeft.Value);
            Assert.Equal(3, rightRight.Value);
        }

        [Theory]
        [InlineData("1 * 2 + 3", SQLBinaryOperator.Multiply, SQLBinaryOperator.Add)]
        [InlineData("1 AND 2 OR 3", SQLBinaryOperator.AND, SQLBinaryOperator.OR)]
        [InlineData("1 + 2 > 3", SQLBinaryOperator.Add, SQLBinaryOperator.GreaterThan)]
        [InlineData("1 + 2 & 3", SQLBinaryOperator.Add, SQLBinaryOperator.BitwiseAnd)]
        public void Parser_Should_Parse_Left_Heavy_Precedence(
            string input,
            SQLBinaryOperator expectedLeftOp,
            SQLBinaryOperator expectedRootOp)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            // Assert Root: (1 op1 2) op2 3
            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedRootOp, binary.Operator);

            var left = Assert.IsType<BinaryExpression>(binary.Left);
            var right = Assert.IsType<LiteralExpression>(binary.Right);

            // Assert Left Child: (1 op1 2)
            Assert.Equal(expectedLeftOp, left.Operator);
            var leftLeft = Assert.IsType<LiteralExpression>(left.Left);
            var leftRight = Assert.IsType<LiteralExpression>(left.Right);

            // Assert Values
            Assert.Equal(1, leftLeft.Value);
            Assert.Equal(2, leftRight.Value);
            Assert.Equal(3, right.Value);
        }

        [Theory]
        [InlineData("(1 + 2) * 3", SQLBinaryOperator.Add, SQLBinaryOperator.Multiply)]
        [InlineData("(1 OR 2) AND 3", SQLBinaryOperator.OR, SQLBinaryOperator.AND)]
        public void Parser_Should_Parse_Parentheses_Override_Precedence(
            string input,
            SQLBinaryOperator expectedLeftOp,
            SQLBinaryOperator expectedRootOp)
        {
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedRootOp, binary.Operator);

            var Parenleft = Assert.IsType<ParenthesizedExpression>(binary.Left);
            var left = Assert.IsType<BinaryExpression>(Parenleft.Expression);
            Assert.Equal(expectedLeftOp, left.Operator);
        }

        [Fact]
        public void Parser_Should_Parse_Complex_Precedence_Chain()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                "1 + 2 * 3 > 4 AND 5 OR 6");

            var or = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(SQLBinaryOperator.OR, or.Operator);

            var and = Assert.IsType<BinaryExpression>(or.Left);
            Assert.Equal(SQLBinaryOperator.AND, and.Operator);

            var compare = Assert.IsType<BinaryExpression>(and.Left);
            Assert.Equal(SQLBinaryOperator.GreaterThan, compare.Operator);
        }
        #endregion
        #region Associativity
        [Theory]
        [InlineData("1 - 2 - 3", SQLBinaryOperator.Subtract)]
        [InlineData("1 + 2 + 3", SQLBinaryOperator.Add)]
        [InlineData("1 / 2 / 3", SQLBinaryOperator.Divide)]
        [InlineData("1 * 2 * 3", SQLBinaryOperator.Multiply)]
        public void Parser_Should_Parse_Left_Associative_Binary_Operators(string input, SQLBinaryOperator expectedOperator)
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(input, true);

            // Assert outer binary expression: ((1 op 2) op 3)
            var binary = Assert.IsType<BinaryExpression>(exp);
            Assert.Equal(expectedOperator, binary.Operator);

            // Left child is a nested BinaryExpression (1 op 2)
            var left = Assert.IsType<BinaryExpression>(binary.Left);
            Assert.Equal(expectedOperator, left.Operator);

            var leftLeft = Assert.IsType<LiteralExpression>(left.Left);
            var leftRight = Assert.IsType<LiteralExpression>(left.Right);

            // Right child is literal (3)
            var right = Assert.IsType<LiteralExpression>(binary.Right);

            // Assert values
            Assert.Equal(1, leftLeft.Value);
            Assert.Equal(2, leftRight.Value);
            Assert.Equal(3, right.Value);
        }
        #endregion
        #endregion
        #region Fail
        [Theory]
        [InlineData("1 +", "Missing right operand")]
        [InlineData("* 5", "Unexpected wildcard/asterisk at the start of expression")]
        [InlineData("1 + * 2", "Missing operand between operator and binary asterisk")]
        [InlineData("1 + ()", "Empty parenthesized expression")]
        [InlineData("1 + (2 * 3", "Missing closing parenthesis")]
        [InlineData("1 + FROM", "Unexpected keyword inside expression")]
        [InlineData("1 + +", "Missing operand")]
        [InlineData("1 ** 2", "Invalid operator")]
        [InlineData("1 2", "Missing operator")]
        [InlineData("(1 + 2))", "Extra closing parenthesis")]
        public void ParseExpression_Should_Throw_SyntaxErrorException_For_Invalid_Syntax(string input, string reason)
        {
            // Act & Assert
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input, true);
            });
        }
        #endregion
    }
}
