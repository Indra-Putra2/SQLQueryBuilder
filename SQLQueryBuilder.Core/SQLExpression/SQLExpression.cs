using SQLQueryBuilder.Core.SQLEnum.SQLExpression;

namespace SQLQueryBuilder.Core.SQLExpression
{
    public abstract class SQLExpression
    {
        public required SQLSourceSpan Span { get; init; }
    }
    public sealed class ColumnExpression : SQLExpression
    {
        public List<SQLIdentifier>? Table { get; init; }
        public required SQLIdentifier Column { get; init; }
    }
    public sealed class LiteralExpression : SQLExpression
    {
        public required SQLLiteralType Type { get; init; }
        public required object? Value { get; init; }
    }
    public sealed class BinaryExpression : SQLExpression
    {
        public required SQLExpression Left { get; init; }
        public required SQLBinaryOperator Operator { get; init; }
        public required SQLExpression Right { get; init; }
    }
    public sealed class UnaryExpression : SQLExpression
    {
        public required SQLUnaryOperator Operator { get; init; }
        public required SQLExpression Operand { get; init; }
    }
    public sealed class FunctionExpression : SQLExpression
    {
        public required string Name { get; init; } = string.Empty;
        public required List<SQLExpression> Arguments { get; init; } = new();
        public SQLFunctionType DeclaredType { get; init; } = SQLFunctionType.Unknown;
        public bool IsNullable { get; init; } = false;
        public bool IsConstant { get; init; } = false;
    }
    public sealed class CaseExpression : SQLExpression
    {
        public SQLExpression? BaseExpression { get; init; }
        public List<CaseWhen> Conditions { get; init; } = new();
        public SQLExpression? ElseExpression { get; init; }
    }
    public sealed class CaseWhen
    {
        public required SQLExpression Condition { get; init; }
        public required SQLExpression Result { get; init; }
    }
    public sealed class WildcardExpression : SQLExpression
    {
        public List<SQLIdentifier>? TableAlias { get; init; }
    }
    public sealed class IsNullExpression : SQLExpression
    {
        public required SQLExpression Operand { get; init; }
        public required bool IsNot { get; init; }
    }
    public sealed class LikeExpression : SQLExpression
    {
        public required SQLExpression Operand { get; init; }
        public required SQLExpression Pattern { get; init; }
        public required bool IsNot { get; init; }
    }
    public sealed class InExpression : SQLExpression
    {
        public required SQLExpression Operand { get; init; }
        public required List<SQLExpression> Values { get; init; } = [];
        public required bool IsNot { get; init; }
    }
    public sealed class BetweenExpression : SQLExpression
    {
        public required SQLExpression Operand { get; init; }
        public required SQLExpression Lower { get; init; }
        public required SQLExpression Upper { get; init; }
        public required bool IsNot { get; init; }
    }
    public sealed class ParameterExpression : SQLExpression
    {
        public required string Name { get; init; } = string.Empty;
    }
    public sealed class ParenthesizedExpression : SQLExpression
    {
        public required SQLExpression Expression { get; init; }
    }
    public sealed class SelectExpression : SQLExpression
    {
        public required SQLExpression Expression { get; init; }
        public SQLIdentifier? Alias { get; init; }
    }
    public sealed class OrderByExpression : SQLExpression
    {
        public required SQLExpression Expression { get; init; }
        public required bool Descending { get; init; }
    }
}
