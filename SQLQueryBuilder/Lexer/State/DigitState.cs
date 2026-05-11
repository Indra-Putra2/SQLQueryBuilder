using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer.State
{
    internal class DigitState : ILexerState
    {
        private ILexerStateSwitcher _lexer;
        private int _commaCounter = 0;
        public DigitState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public Token Tokenize(ref LexerContext context)
        {
            _commaCounter = 0;
            char c = context.Peek();
            int start = context.Index;
            string digit = string.Empty;

            if (char.IsDigit(c))
            {
                while (!context.IsAtEnd && (char.IsDigit(context.Peek()) || context.Peek() == '.'))
                {
                    if(context.Peek() == '.') _commaCounter++;
                    if(_commaCounter > 1) throw LexerExceptionHelper.CreateSinglePointerError(GetType(), context.Index, context.Peek(), ref context, "Invalid Numbering");
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
            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), start, c, ref context, "Invalid Character");
        }
    }
}
