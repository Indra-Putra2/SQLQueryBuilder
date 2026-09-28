using SQLQueryBuilder.Model.QueryBuilder;

namespace SQLQueryBuilder.Model.Context
{
    internal ref struct SQLValidatorContext
    {
        public readonly SQLTableManager TableManager { get; init; }
        public readonly SQLDTOManager DtoManager { get; init; }
        public ReadOnlySpan<char> Span;
    }
}
