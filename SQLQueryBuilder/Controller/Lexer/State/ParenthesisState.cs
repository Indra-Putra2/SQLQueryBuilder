using SQLQueryBuilder.Controller.Lexer.Helper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.Lexer.State
{
    internal class ParenthesisState : ILexerState
    {
        private ILexerStateSwitcher _lexer;

        public ParenthesisState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public TokenModel Tokenize(ref LexerContextModel context)
        {
            var c = context.Peek();
            int currentIndex = context.Index;
            if (c == '(')
            {
                context.Consume();
                return new TokenModel()
                {
                    Type = TokenTypeEnum.LParen,
                    Value = c.ToString(),
                    Start = currentIndex,
                    Length = 1
                };
            }
            else if (c == ')')
            {
                context.Consume();
                return new TokenModel()
                {
                    Type = TokenTypeEnum.RParen,
                    Value = c.ToString(),
                    Start = currentIndex,
                    Length = 1
                };
            }

            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), currentIndex, c, ref context, "Invalid Character");
        }
    }
}
