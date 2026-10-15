using TruSql.Core.SQLExpression;
using TruSql.Helper;
using System.Data;

namespace TruSql.Test.ParserTest
{
    public class JoinConditionTest
    {
        #region Pass
        [Theory]
        // Comparison
        [InlineData("Orders.CustomerID = Customers.CustomerID", typeof(BinaryExpression))]
        [InlineData("Orders.CustomerID <> Customers.CustomerID", typeof(BinaryExpression))]
        [InlineData("Orders.CustomerID > Customers.CustomerID", typeof(BinaryExpression))]
        [InlineData("Orders.CustomerID >= Customers.CustomerID", typeof(BinaryExpression))]
        [InlineData("Orders.CustomerID < Customers.CustomerID", typeof(BinaryExpression))]
        [InlineData("Orders.CustomerID <= Customers.CustomerID", typeof(BinaryExpression))]

        // Logical
        [InlineData("A = B AND C = D", typeof(BinaryExpression))]
        [InlineData("A = B OR C = D", typeof(BinaryExpression))]
        [InlineData("NOT (A = B)", typeof(UnaryExpression))]

        // Predicate expressions
        [InlineData("Name LIKE 'John%'", typeof(LikeExpression))]
        [InlineData("Name NOT LIKE 'John%'", typeof(LikeExpression))]
        [InlineData("Age BETWEEN 18 AND 30", typeof(BetweenExpression))]
        [InlineData("Age NOT BETWEEN 18 AND 30", typeof(BetweenExpression))]
        [InlineData("Id IN (1,2,3)", typeof(InExpression))]
        [InlineData("Id NOT IN (1,2,3)", typeof(InExpression))]
        [InlineData("DeletedAt IS NULL", typeof(IsNullExpression))]
        [InlineData("DeletedAt IS NOT NULL", typeof(IsNullExpression))]
        public void Parser_Should_Parse_Valid_JOIN_Condition(
            string input,
            Type expectedType)
        {
            var expr = SQLBuilderHelper.BuildPredicate(input);

            Assert.IsType(expectedType, expr);
        }
        #endregion

        #region Fail
        [Theory]
        // Arithmetic
        [InlineData("1 + 2")]
        [InlineData("Price * Quantity")]
        [InlineData("10 / 5")]
        [InlineData("A & B")]
        [InlineData("A | B")]
        [InlineData("A ^ B")]

        // Single expressions
        [InlineData("CustomerID")]
        [InlineData("123")]
        [InlineData("'John'")]
        [InlineData("NULL")]

        // Function returning non-boolean
        [InlineData("LOWER(Name)")]
        [InlineData("COUNT(*)")]
        public void Parser_Should_Fail_Invalid_JOIN_Condition(string input)
        {
            Assert.Throws<SyntaxErrorException>(() =>
            {
                SQLBuilderHelper.BuildPredicate(input);
            });
        }
        #endregion
    }
}