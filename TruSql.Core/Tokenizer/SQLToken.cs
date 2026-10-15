using TruSql.Core.SQLEnum.Tokenizer;

namespace TruSql.Core.Tokenizer
{
    public readonly record struct SQLToken(
        SQLTokenType Type,
        int StartPosition,
        int Length
    );
}
