using SQLQueryBuilder.Model.QueryBuilder;

namespace SQLQueryBuilder.Model.Context
{
    internal ref struct SemanticValidatorContext
    {
        public readonly SQLTableManager TableManager { get; init; }
        public readonly SQLDTOManager DtoManager { get; init; }
        public ReadOnlySpan<char> Span;
    }
}
