using SQLQueryBuilder.Controller.Lexer.Helper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.Lexer.State
{
    internal class DigitState : ILexerState
    {
        private ILexerStateSwitcher _lexer;
        private int _commaCounter = 0;
        public DigitState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public TokenModel Tokenize(ref LexerContextModel context)
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

                return new TokenModel() 
                { 
                    Type = TokenTypeEnum.Number, 
                    Value = digit,
                    Start = start,
                    Length = digit.Length
                };
            }
            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), start, c, ref context, "Invalid Character");
        }
    }
}
