using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;

namespace SQLQueryBuilder.Model.Interface
{
    internal interface ILexerStateSwitcher
    {
        void SwitchState(LexerStateEnum lexerState, ref LexerContext context);
    }
}
