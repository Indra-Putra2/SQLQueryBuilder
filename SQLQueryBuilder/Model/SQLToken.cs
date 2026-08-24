using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model
{
    public readonly record struct SQLToken(
        SQLTokenType Type,
        int StartPosition,
        int Length
    );
}
