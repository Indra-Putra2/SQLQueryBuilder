using SQLQueryBuilder.Enum;

namespace SQLQueryBuilder.Model
{
    internal record SelectToken(
        SelectTokenType Type,
        string Value,
        int StartPosition,
        int length
    );
}
