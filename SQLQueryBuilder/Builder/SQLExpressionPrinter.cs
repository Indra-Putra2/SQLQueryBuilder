using SQLQueryBuilder.Model.SQL_Expression;
using System.Text;

namespace SQLQueryBuilder.Utils
{
    public static class SQLExpressionPrinter
    {
        public static string Print(SQLExpression expression)
        {
            var sb = new StringBuilder();
            PrintNode(expression, sb, indent: "", isLast: true);
            return sb.ToString();
        }

        private static void PrintNode(SQLExpression? node, StringBuilder sb, string indent, bool isLast)
        {
            if (node == null)
            {
                sb.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}<null>");
                return;
            }

            string marker = isLast ? "└── " : "├── ";
            string childIndent = indent + (isLast ? "    " : "│   ");

            switch (node)
            {
                case ColumnExpression col:
                    string aliasText = col.Table.Count == 0 ? "" : $" (AS {col.Table})";
                    sb.AppendLine($"{indent}{marker}ColumnExpression: {col.Column}{aliasText}");
                    break;

                case LiteralExpression lit:
                    string valText = lit.Value == null ? "NULL" : $"\"{lit.Value}\"";
                    sb.AppendLine($"{indent}{marker}LiteralExpression: {valText} [{lit.Type}]");
                    break;

                case BinaryExpression bin:
                    sb.AppendLine($"{indent}{marker}BinaryExpression ({bin.Operator})");
                    PrintNode(bin.Left, sb, childIndent, isLast: false);
                    PrintNode(bin.Right, sb, childIndent, isLast: true);
                    break;

                case UnaryExpression un:
                    sb.AppendLine($"{indent}{marker}UnaryExpression ({un.Operator})");
                    PrintNode(un.Operand, sb, childIndent, isLast: true);
                    break;

                case FunctionExpression fn:
                    sb.AppendLine($"{indent}{marker}FunctionExpression: {fn.Name}()");
                    for (int i = 0; i < fn.Arguments.Count; i++)
                    {
                        bool lastArg = i == fn.Arguments.Count - 1;
                        PrintNode(fn.Arguments[i], sb, childIndent, lastArg);
                    }
                    break;

                case CaseExpression caseExpr:
                    sb.AppendLine($"{indent}{marker}CaseExpression");
                    int total = caseExpr.Conditions.Count + (caseExpr.ElseExpression != null ? 1 : 0);
                    int count = 0;

                    foreach (var when in caseExpr.Conditions)
                    {
                        count++;
                        bool lastWhen = count == total;
                        sb.AppendLine($"{childIndent}{(lastWhen ? "└── " : "├── ")}WHEN");

                        string whenChildIndent = childIndent + (lastWhen ? "    " : "│   ");
                        PrintNode(when.Condition, sb, whenChildIndent, isLast: false);

                        sb.AppendLine($"{whenChildIndent}└── THEN");
                        string thenChildIndent = whenChildIndent + "    ";
                        PrintNode(when.Result, sb, thenChildIndent, isLast: true);
                    }

                    if (caseExpr.ElseExpression != null)
                    {
                        sb.AppendLine($"{childIndent}└── ELSE");
                        PrintNode(caseExpr.ElseExpression, sb, childIndent + "    ", isLast: true);
                    }
                    break;

                case WildcardExpression wild:
                    string prefix = string.IsNullOrEmpty(wild.TableAlias) ? "" : $"{wild.TableAlias}.";
                    sb.AppendLine($"{indent}{marker}WildcardExpression: {prefix}*");
                    break;

                default:
                    sb.AppendLine($"{indent}{marker}Unknown node: {node.GetType().Name}");
                    break;
            }
        }
    }
}