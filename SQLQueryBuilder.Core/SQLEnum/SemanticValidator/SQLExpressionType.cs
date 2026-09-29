namespace SQLQueryBuilder.Core.SQLEnum.SemanticValidator
{
    public enum SQLExpressionType
    {
        Unknown,
        NullLiteral,
        Boolean,
        Integer,
        Decimal,
        Double,
        String,
        Date,
        Time,
        DateTime,
        Guid,
        Binary
    }
}
