using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Model.Interface
{
    public interface ILexerState
    {
        Token Tokenize(ref LexerContext context);
    }
}
