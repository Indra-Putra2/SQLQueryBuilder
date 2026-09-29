namespace SQLQueryBuilder.Core.SQLEnum.SQLExpression
{
    public enum SQLBinaryOperator
    {
        // Logical
        AND,
        OR,

        // Comparison
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,

        // Arithmetic
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,

        // Bitwise
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseLeftShift,
        BitwiseRightShift,
    }
}
