using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Interface
{
    internal interface ILexerState
    {
        TokenModel Tokenize(ref LexerContextModel context);
    }
}
