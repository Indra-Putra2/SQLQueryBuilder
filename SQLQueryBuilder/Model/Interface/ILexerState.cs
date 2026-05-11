using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Model.Interface
{
    internal interface ILexerState
    {
        Token Tokenize(ref LexerContext context);
    }
}
