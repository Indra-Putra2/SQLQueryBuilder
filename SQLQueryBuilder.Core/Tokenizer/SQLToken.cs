using SQLQueryBuilder.Core.SQLEnum.Tokenizer;

namespace SQLQueryBuilder.Core.Tokenizer
{
    public readonly record struct SQLToken(
        SQLTokenType Type,
        int StartPosition,
        int Length
    );
}
