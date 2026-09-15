using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.SQL_Expression;

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
                default:
                    throw new NotSupportedException(
                        $"Unsupported AST node: {expr.GetType().Name}");
            }
        }
    }
}
