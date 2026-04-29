using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder
{
    public static class AstPrinter
    {
        public static void Print(Expr expr, string indent = "", bool isLast = true)
        {
            Console.Write(indent);
            Console.Write(isLast ? "└── " : "├── ");

            if (expr is BinaryExpr bin)
            {
                Console.WriteLine(bin.Op);

                Print(bin.Left, indent + (isLast ? "    " : "│   "), false);
                Print(bin.Right, indent + (isLast ? "    " : "│   "), true);
            }
            else if (expr is ComparisonExpr comp)
            {
                Console.WriteLine($"{comp.Identifier} {comp.Op} {comp.Value}");
            }
        }
    }
}
