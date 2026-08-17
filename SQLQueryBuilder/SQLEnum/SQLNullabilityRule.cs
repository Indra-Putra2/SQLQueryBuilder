namespace SQLQueryBuilder.SQLEnum
{
    internal enum SQLNullabilityRule
    {
        Propagate,   // Any nullable argument can make the result nullable
        AllNullable, // Result is nullable only when all arguments are nullable
        Never,       // Result is never nullable
        Unknown      // For Unknown Function
    }
}
