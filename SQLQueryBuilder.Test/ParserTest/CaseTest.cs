using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using System.Data;

namespace SQLQueryBuilder.Test.ParserTest
{
    public class CaseTest
    {
        #region PASS
        #region CASE - Detailed Property Spot Checks

        [Fact]
        public void Parser_Should_Populate_CASE_Properties_Correctly()
        {
            // Act
            var exp = SQLBuilderHelper.BuildExpression(
                "CASE WHEN age >= 18 THEN 'Adult' ELSE 'Minor' END"
            );

            // Assert
            var caseExpr = Assert.IsType<CaseExpression>(exp);

            Assert.Single(caseExpr.Conditions);

            var condition = caseExpr.Conditions[0];

            var comparison = Assert.IsType<BinaryExpression>(condition.Condition);
            Assert.Equal(SQLBinaryOperator.GreaterThanOrEqual, comparison.Operator);

            var result = Assert.IsType<LiteralExpression>(condition.Result);
            Assert.Equal("Adult", result.Value);

            var elseExpr = Assert.IsType<LiteralExpression>(caseExpr.ElseExpression);
            Assert.Equal("Minor", elseExpr.Value);
        }

        #endregion
        #region CASE - Multiple WHEN

        [Fact]
        public void Parser_Should_Parse_Multiple_CASE_WHEN_Conditions()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                """
                CASE
                    WHEN score >= 90 THEN 'A'
                    WHEN score >= 80 THEN 'B'
                    WHEN score >= 70 THEN 'C'
                    ELSE 'F'
                END
                """
            );

            var caseExpr = Assert.IsType<CaseExpression>(exp);

            Assert.Equal(3, caseExpr.Conditions.Count);

            Assert.Equal("A",
                ((LiteralExpression)caseExpr.Conditions[0].Result).Value);

            Assert.Equal("B",
                ((LiteralExpression)caseExpr.Conditions[1].Result).Value);

            Assert.Equal("C",
                ((LiteralExpression)caseExpr.Conditions[2].Result).Value);
        }

        #endregion

        [Fact]
        public void Parser_Should_Parse_CASE_Without_ELSE()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                "CASE WHEN active = 1 THEN 'Yes' END"
            );

            var caseExpr = Assert.IsType<CaseExpression>(exp);

            Assert.Single(caseExpr.Conditions);
            Assert.Null(caseExpr.ElseExpression);
        }

        [Fact]
        public void Parser_Should_Parse_CASE_With_Complex_Expressions()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                "CASE WHEN price > 100 THEN price * 0.9 ELSE price END"
            );

            var caseExpr = Assert.IsType<CaseExpression>(exp);

            var when = caseExpr.Conditions[0];

            Assert.IsType<BinaryExpression>(when.Condition);
            Assert.IsType<BinaryExpression>(when.Result);
        }

        [Fact]
        public void Parser_Should_Parse_CASE_With_Function()
        {
            var exp = SQLBuilderHelper.BuildExpression(
                "CASE WHEN LEN(name) > 0 THEN 'Valid' ELSE 'Empty' END"
            );

            var caseExpr = Assert.IsType<CaseExpression>(exp);

            Assert.IsType<BinaryExpression>(
                caseExpr.Conditions[0].Condition);
        }
        #endregion

        #region FAIL
        #region CASE - Invalid Syntax

        [Theory]
        [InlineData("CASE END")]
        [InlineData("CASE WHEN END")]
        [InlineData("CASE WHEN age > 10 END")]
        [InlineData("CASE WHEN age > 10 THEN END")]
        [InlineData("CASE WHEN age > 10 THEN 'A'")]
        [InlineData("CASE WHEN THEN 'A' END")]
        [InlineData("CASE WHEN age > 10 ELSE 'A' END")]
        [InlineData("CASE WHEN age > 10 THEN 'A' ELSE END")]

        public void Parser_Should_Fail_Invalid_CASE(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildExpression(input);
            });
        }

        #endregion
        #endregion
    }
}
