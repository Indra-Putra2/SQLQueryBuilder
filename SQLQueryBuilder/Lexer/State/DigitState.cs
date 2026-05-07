using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer.State
{
    internal class DigitState : ILexerState
    {
        private ILexerStateSwitcher _lexer;
        public DigitState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public Token Tokenize(ref LexerContext context)
        {
            char c = context.Peek();
            if (char.IsDigit(c))
            {
                int start = context.Index;
                string digit = string.Empty;
                while (!context.IsAtEnd && (char.IsDigit(context.Peek()) || context.Peek() == '.'))
                {
                    digit += context.Consume();
                }

                return new Token() 
                { 
                    Type = TokenType.Number, 
                    Value = digit,
                    Start = start,
                    Length = digit.Length
                };
            }
            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), context.Index, ref context);
        }
    }
}
