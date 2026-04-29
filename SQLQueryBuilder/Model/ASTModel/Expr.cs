namespace SQLQueryBuilder.Model.ASTModel
{
    public abstract class Expr { }
    public class BinaryExpr : Expr
    {
        public required string Op; // AND, OR
        public required Expr Left;
        public required Expr Right;
        public override string ToString()
        {
            return $"({Left} {Op} {Right})";
        }
    }

    public class ComparisonExpr : Expr
    {
        public required string Op; // =, <, <=, >
        public required string Identifier;
        public required object Value;
        public override string ToString()
        {
            return $"{Identifier} {Op} {Value}";
        }
    }

    public class LiteralExpr : Expr
    {
        public required string Value;
        public override string ToString()
        {
            return $"{Value}";
        }
    }
}
