namespace SQLQueryBuilder.Core.SQLEnum.SQLExpression
{
    public enum SQLUnaryOperator
    {
        // Arithmetic Unary
        Positive,    // +  (e.g., +5)
        Negative,    // -  (e.g., -col)

        // Bitwise Unary
        BitwiseNOT,  // ~  (e.g., ~mask)

        // Logical Unary
        NOT,         // NOT (e.g., NOT condition)

        // Subquery Unary
        EXISTS       // EXISTS (subquery)
    }
}
