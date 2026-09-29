using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Interface;

namespace SQLQueryBuilder.Helper
{
    internal class ASTWalker : IASTTraverser
    {
        private IASTVisitor _walker;
        public ASTWalker(IASTVisitor walker)
        {
            _walker = walker;
        }

        public void Walk(SQLExpression expr)
        {
            switch (expr)
            {
                case BinaryExpression binary:
                    _walker.OnBinary(binary);
                    break;
                case LiteralExpression literal:
                    _walker.OnLiteral(literal);
                    break;
                case ColumnExpression column:
                    _walker.OnColumn(column);
                    break;
                case UnaryExpression unary:
                    _walker.OnUnary(unary);
                    break;
                case FunctionExpression function:
                    _walker.OnFucntion(function);
                    break;
                case CaseExpression @case:
                    _walker.OnCase(@case);
                    break;
                case WildcardExpression wildcard:
                    _walker.OnWildcard(wildcard);
                    break;
                case IsNullExpression isNull:
                    _walker.OnIsNull(isNull);
                    break;
                case LikeExpression like:
                    _walker.OnLike(like);
                    break;
                case InExpression @in:
                    _walker.OnIn(@in);
                    break;
                case BetweenExpression between:
                    _walker.OnBetween(between);
                    break;
                case ParenthesizedExpression parenthesized:
                    _walker.OnParenthesized(parenthesized);
                    break;
                case SelectExpression select:
                    _walker.OnSelect(select);
                    break;
                case OrderByExpression orderBy:
                    _walker.OnOrderby(orderBy);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unsupported AST node: {expr.GetType().Name}");
            }
        }
    }
}
