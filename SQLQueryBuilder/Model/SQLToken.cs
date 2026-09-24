using SQLQueryBuilder.SQLEnum.Tokenizer;

namespace SQLQueryBuilder.Model
{
    public readonly record struct SQLToken(
        SQLTokenType Type,
        int StartPosition,
        int Length
    );
}
