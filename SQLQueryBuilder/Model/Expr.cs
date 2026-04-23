namespace SQLQueryBuilder.Model
{
    public abstract class Expr { }
    public class BinaryExpr : Expr
    {
        public string Op; // AND, OR
        public Expr Left;
        public Expr Right;

        public override string ToString()
        {
            return $"({Left} {Op} {Right})";
        }
    }

    public class ComparisonExpr : Expr
    {
        public string Op; // =, <, <=, >
        public string Identifier;
        public object Value;

        public override string ToString()
        {
            return $"{Identifier} {Op} {Value}";
        }
    }

    public class LiteralExpr : Expr
    {
        public string Value;

        public override string ToString()
        {
            return $"{Value}";
        }
    }
}
