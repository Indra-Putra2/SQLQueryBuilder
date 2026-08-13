using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    public abstract class SQLExpression {}

    public sealed class ColumnExpression : SQLExpression
    {
        public List<string>? Table { get; init; }
        public string Column { get; init; } = string.Empty;
    }
    public sealed class LiteralExpression : SQLExpression
    {
        public SQLLiteralType Type { get; init; }
        public object? Value { get; init; }
    }
    public sealed class BinaryExpression : SQLExpression
    {
        public SQLExpression Left { get; init; }
        public SQLBinaryOperator Operator { get; init; }
        public SQLExpression Right { get; init; }
    }
    public sealed class UnaryExpression : SQLExpression
    {
        public SQLUnaryOperator Operator { get; init; }
        public SQLExpression Operand { get; init; }
    }
    public sealed class FunctionExpression : SQLExpression
    {
        public string Name { get; init; } = string.Empty;
        public List<SQLExpression> Arguments { get; init; } = new();
        public SQLFunctionType FunctionType { get; init; } = SQLFunctionType.Unknown;
    }
    public sealed class CaseExpression : SQLExpression
    {
        public SQLExpression? BaseExpression { get; init; }
        public List<CaseWhen> Conditions { get; init; } = new();
        public SQLExpression? ElseExpression { get; init; }
    }
    public sealed class CaseWhen
    {
        public SQLExpression Condition { get; init; }
        public SQLExpression Result { get; init; }
    }
    public sealed class WildcardExpression : SQLExpression
    {
        public List<string>? TableAlias { get; init; }
    }
    public sealed class IsNullExpression : SQLExpression
    {
        public SQLExpression Operand { get; init; }
        public bool IsNot { get; init; }
    }
    public sealed class LikeExpression : SQLExpression
    {
        public SQLExpression Operand { get; init; }
        public SQLExpression Pattern { get; init; }
        public bool IsNot { get; init; }
    }
    public sealed class InExpression : SQLExpression
    {
        public SQLExpression Operand { get; init; }
        public List<SQLExpression> Values { get; init; } = [];
        public bool IsNot { get; init; }
    }
    public sealed class BetweenExpression : SQLExpression
    {
        public SQLExpression Operand { get; init; }
        public SQLExpression Lower { get; init; }
        public SQLExpression Upper { get; init; }
        public bool IsNot { get; init; }
    }
    public sealed class ParameterExpression : SQLExpression
    {
        public string Name { get; init; } = string.Empty;
    }
    public sealed class SelectExpression : SQLExpression
    {
        public SQLExpression Expression { get; init; }
        public string? Alias { get; init; }
    }
    public sealed class OrderByExpression : SQLExpression
    {
        public SQLExpression Expression { get; init; }
        public bool Descending { get; init; }
    }
}