namespace SQLQueryBuilder.Model.ASTModel
{
    public abstract class ExpressionModel { }
    public class BinaryExpr : ExpressionModel
    {
        public required string Op; // AND, OR
        public required ExpressionModel Left;
        public required ExpressionModel Right;
        public override string ToString()
        {
            return $"({Left} {Op} {Right})";
        }
    }

    public class ComparisonExpr : ExpressionModel
    {
        public required string Op; // =, <, <=, >
        public required string Identifier;
        public required object Value;
        public override string ToString()
        {
            return $"{Identifier} {Op} {Value}";
        }
    }

    public class LiteralExpr : ExpressionModel
    {
        public required string Value;
        public override string ToString()
        {
            return $"{Value}";
        }
    }
}
