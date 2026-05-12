using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Interface
{
    internal interface ILexerStateSwitcher
    {
        void SwitchState(LexerStateEnum lexerState, ref LexerContextModel context);
    }
}
