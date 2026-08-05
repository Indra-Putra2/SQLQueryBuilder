using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model
{
    public record SQLToken(
        SQLTokenType Type,
        string RawText,
        string Value,
        int StartPosition,
        int RawLength,
        int CleanLength
    );
}
