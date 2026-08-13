using System;
using System.Collections.Generic;
using System.Text;

namespace SQLQueryBuilder.SQLEnum
{
    internal enum SQLNullabilityRule
    {
        Propagate,   // Any nullable argument can make the result nullable
        AllNullable, // Result is nullable only when all arguments are nullable
        Never        // Result is never nullable
    }
}
