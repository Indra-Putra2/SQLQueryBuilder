using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using System.Data;

namespace SQLQueryBuilder.Test.ParserTest
{
    public class UnaryTest
    {
        #region Pass
        #region Numbers
        [Fact]
        public void Parser_Should_Parse_Negative_Unary_Integer()
        {
            var exp = SQLBuilderHelper.BuildExpression("-123");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary.Operator);

            var operand = Assert.IsType<LiteralExpression>(unary.Operand);
            Assert.Equal(SQLLiteralType.Integer, operand.Type);
            Assert.Equal(123, operand.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Unary_Integer()
        {
            var exp = SQLBuilderHelper.BuildExpression("+ 123");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary.Operator);

            var operand = Assert.IsType<LiteralExpression>(unary.Operand);
            Assert.Equal(SQLLiteralType.Integer, operand.Type);
            Assert.Equal(123, operand.Value);
        }

        #endregion
        #region String
        [Fact]
        public void Parser_Should_Parse_Negative_Unary_String()
        {
            var exp = SQLBuilderHelper.BuildExpression("-'why?'");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary.Operator);

            var operand = Assert.IsType<LiteralExpression>(unary.Operand);
            Assert.Equal(SQLLiteralType.String, operand.Type);
            Assert.Equal("why?", operand.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Unary_String()
        {
            var exp = SQLBuilderHelper.BuildExpression("+'pass'");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary.Operator);

            var operand = Assert.IsType<LiteralExpression>(unary.Operand);
            Assert.Equal(SQLLiteralType.String, operand.Type);
            Assert.Equal("pass", operand.Value);
        }
        #endregion
        #region Column
        [Fact]
        public void Parser_Should_Parse_Negative_Unary_Column()
        {
            var exp = SQLBuilderHelper.BuildExpression("-hello");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary.Operator);

            var operand = Assert.IsType<ColumnExpression>(unary.Operand);
            Assert.Equal("hello", operand.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Unary_Column()
        {
            var exp = SQLBuilderHelper.BuildExpression("+world");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary.Operator);

            var operand = Assert.IsType<ColumnExpression>(unary.Operand);
            Assert.Equal("world", operand.Column.Value);
        }
        #endregion
        #region Function
        [Fact]
        public void Parser_Should_Parse_Negative_Unary_Function()
        {
            var exp = SQLBuilderHelper.BuildExpression("-HELLO(WORLD)");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary.Operator);

            var func = Assert.IsType<FunctionExpression>(unary.Operand);
            Assert.Equal("HELLO", func.Name);

            var arg = Assert.IsType<ColumnExpression>(func.Arguments[0]);
            Assert.Equal("WORLD", arg.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Unary_Function()
        {
            var exp = SQLBuilderHelper.BuildExpression("+WORLD(HELLO)");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary.Operator);

            var func = Assert.IsType<FunctionExpression>(unary.Operand);
            Assert.Equal("WORLD", func.Name);

            var arg = Assert.IsType<ColumnExpression>(func.Arguments[0]);
            Assert.Equal("HELLO", arg.Column.Value);
        }
        #endregion
        #region Parenthesized
        [Fact]
        public void Parser_Should_Parse_Negative_Unary_Parenthesized()
        {
            var exp = SQLBuilderHelper.BuildExpression("-(hello + 'world')");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary.Operator);

            var paren = Assert.IsType<ParenthesizedExpression>(unary.Operand);
            var binary = Assert.IsType<BinaryExpression>(paren.Expression);

            var left = Assert.IsType<ColumnExpression>(binary.Left);
            Assert.Equal(null!, left.Table);
            Assert.Equal("hello", left.Column.Value);

            Assert.Equal(SQLBinaryOperator.Add, binary.Operator);

            var right = Assert.IsType<LiteralExpression>(binary.Right);
            Assert.Equal(SQLLiteralType.String, right.Type);
            Assert.Equal("world", right.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Unary_Parenthesized()
        {
            var exp = SQLBuilderHelper.BuildExpression("+(COUNT(*) - 5)");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary.Operator);

            var paren = Assert.IsType<ParenthesizedExpression>(unary.Operand);
            var binary = Assert.IsType<BinaryExpression>(paren.Expression);

            var left = Assert.IsType<FunctionExpression>(binary.Left);
            Assert.Equal("COUNT", left.Name);

            var leftArg = Assert.IsType<WildcardExpression>(left.Arguments[0]);
            Assert.Equal(null!, leftArg.TableAlias);

            Assert.Equal(SQLBinaryOperator.Subtract, binary.Operator);

            var right = Assert.IsType<LiteralExpression>(binary.Right);
            Assert.Equal(SQLLiteralType.Integer, right.Type);
            Assert.Equal(5, right.Value);
        }
        #endregion
        #region NestedRegion
        [Fact]
        public void Parser_Should_Parse_Negative_Negative_Unary_Nested()
        {
            var exp = SQLBuilderHelper.BuildExpression("- - hello");
            var unary1 = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary1.Operator);

            var unary2 = Assert.IsType<UnaryExpression>(unary1.Operand);
            Assert.Equal(SQLUnaryOperator.Negative, unary2.Operator);

            var column = Assert.IsType<ColumnExpression>(unary2.Operand);
            Assert.Equal(null!, column.Table);
            Assert.Equal("hello", column.Column.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Positive_Unary_Nested()
        {
            var exp = SQLBuilderHelper.BuildExpression("++10");
            var unary1 = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary1.Operator);

            var unary2 = Assert.IsType<UnaryExpression>(unary1.Operand);
            Assert.Equal(SQLUnaryOperator.Positive, unary2.Operator);

            var num = Assert.IsType<LiteralExpression>(unary2.Operand);
            Assert.Equal(SQLLiteralType.Integer, num.Type);
            Assert.Equal(10, num.Value);
        }

        [Fact]
        public void Parser_Should_Parse_Positive_Negative_Unary_Nested()
        {
            var exp = SQLBuilderHelper.BuildExpression("+-COUNT(*)");
            var unary1 = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Positive, unary1.Operator);

            var unary2 = Assert.IsType<UnaryExpression>(unary1.Operand);
            Assert.Equal(SQLUnaryOperator.Negative, unary2.Operator);

            var func = Assert.IsType<FunctionExpression>(unary2.Operand);
            Assert.Equal("COUNT", func.Name);

            var arg = Assert.IsType<WildcardExpression>(func.Arguments[0]);
            Assert.Equal(null!, arg.TableAlias);
        }

        [Fact]
        public void Parser_Should_Parse_Negative_Positive_Unary_Nested()
        {
            var exp = SQLBuilderHelper.BuildExpression("-+'life'");
            var unary1 = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary1.Operator);

            var unary2 = Assert.IsType<UnaryExpression>(unary1.Operand);
            Assert.Equal(SQLUnaryOperator.Positive, unary2.Operator);

            var str = Assert.IsType<LiteralExpression>(unary2.Operand);
            Assert.Equal(SQLLiteralType.String, str.Type);
            Assert.Equal("life", str.Value);
        }
        #endregion
        #region WhiteSpace
        [Fact]
        public void Parser_Should_Parse_Unary_With_Whitespace()
        {
            var exp = SQLBuilderHelper.BuildExpression("-    123");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.Negative, unary.Operator);
        }
        #endregion
        #region NOT
        [Fact]
        public void Parser_Should_Parse_NOT_Unary()
        {
            var exp = SQLBuilderHelper.BuildExpression("NOT is_active");

            var unary = Assert.IsType<UnaryExpression>(exp);
            Assert.Equal(SQLUnaryOperator.NOT, unary.Operator); // Adjust enum name as needed

            var col = Assert.IsType<ColumnExpression>(unary.Operand);
            Assert.Equal("is_active", col.Column.Value);
        }
        #endregion
        #endregion

        #region Fail

        [Fact]
        public void Parser_Should_Fail_Standalone_NOT_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("NOT");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Standalone_NOT_NULL()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("NOT NULL");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Unary_Negative_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("-");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Unary_Positive_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("+");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Unary_Negative_Negative_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("- -");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Unary_Positive_Positive_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("++");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Unary_Negative_Positive_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("-+");
            });
        }

        [Fact]
        public void Parser_Should_Fail_Unary_Positive_Negative_Without_Operand()
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression("+-");
            });
        }
        #endregion
    }
}
