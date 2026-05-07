using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;

namespace SQLQueryBuilder.Model.Interface
{
    public interface ILexerStateSwitcher
    {
        void SwitchState(LexerStateEnum lexerState, ref LexerContext context);
    }
}
