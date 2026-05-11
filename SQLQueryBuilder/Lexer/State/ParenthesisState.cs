using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer.State
{
    internal class ParenthesisState : ILexerState
    {
        private ILexerStateSwitcher _lexer;

        public ParenthesisState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public Token Tokenize(ref LexerContext context)
        {
            var c = context.Peek();
            int currentIndex = context.Index;
            if (c == '(')
            {
                context.Consume();
                return new Token()
                {
                    Type = TokenType.LParen,
                    Value = c.ToString(),
                    Start = currentIndex,
                    Length = 1
                };
            }
            else if (c == ')')
            {
                context.Consume();
                return new Token()
                {
                    Type = TokenType.RParen,
                    Value = c.ToString(),
                    Start = currentIndex,
                    Length = 1
                };
            }

            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), currentIndex, c, ref context, "Invalid Character");
        }
    }
}
