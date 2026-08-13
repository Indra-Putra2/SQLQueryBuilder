using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model
{
    internal record SQLFunction
    {
        public int ArgumentCount { get; init; }
        public SQLExpressionType? FixedReturnType { get; init; }
        public bool AcceptsWildcard { get; init; } = false;

        // How literal NULL is handled by the function.
        public bool IsNullAware { get; init; } = false;

        // How result nullability is determined.
        public SQLNullabilityRule NullabilityRule { get; init; }
            = SQLNullabilityRule.Propagate;
    }
}
