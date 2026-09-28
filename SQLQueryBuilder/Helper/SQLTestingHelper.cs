using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Helper
{
    public static class SQLTestingHelper
    {
        public static SQLExpressionMetadata DebugAnalyzeExpression<TargetDTO>(SQLExpression expression, IEnumerable<(string tableAlias, Type type)> sources, string input)
        {
            var dtoManager = new SQLDTOManager();
            dtoManager.RegisterDTO(typeof(TargetDTO));

            var tableManager = new SQLTableManager();

            foreach (var source in sources)
            {
                tableManager.RegisterTable(source.tableAlias, source.type);
            }

            var context = new SQLValidatorContext
            {
                DtoManager = dtoManager,
                TableManager = tableManager,
                Span = input.AsSpan()
            };
            return SQLSemanticValidator.AnalyzeExpression(expression, ref context);
        }
    }
}
