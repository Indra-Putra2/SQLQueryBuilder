using TruSql.Core.SQLEnum.QueryBuilder;

namespace TruSql.Model.QueryBuilder
{
    internal class JoinInfo
    {
        public required string TableAlias = string.Empty;
        public required string JoinCondition = string.Empty;
        public required Type TableType = null;
        public SQLJoinType JoinType = SQLJoinType.INNER;
    }
}
