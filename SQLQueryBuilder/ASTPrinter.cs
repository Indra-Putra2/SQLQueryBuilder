using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder
{
    internal static class AstPrinter
    {
        public static void Print(Expr expr, string indent = "", string title = "", bool isLast = true)
        {
            Console.WriteLine(title);
            PrintInternal(expr, indent, isLast);
        }
        private static void PrintInternal(Expr expr, string indent = "", bool isLast = true)
        {
            Console.Write(indent);
            Console.Write(isLast ? "└── " : "├── ");

            if (expr is BinaryExpr bin)
            {
                Console.WriteLine(bin.Op);

                PrintInternal(bin.Left, indent + (isLast ? "    " : "│   "), false);
                PrintInternal(bin.Right, indent + (isLast ? "    " : "│   "), true);
            }
            else if (expr is ComparisonExpr comp)
            {
                Console.WriteLine($"{comp.Identifier} {comp.Op} {comp.Value}");
            }
        }
    }
}
