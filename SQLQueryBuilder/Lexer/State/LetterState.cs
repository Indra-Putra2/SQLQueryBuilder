using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer.State
{
    internal class LetterState : ILexerState
    {
        private ILexerStateSwitcher _lexer;
        public LetterState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public Token Tokenize(ref LexerContext context)
        {
            char c = context.Peek();
            if (char.IsLetter(c))
            {
                int start = context.Index;
                string word = string.Empty;
                while (!context.IsAtEnd && char.IsLetter(context.Peek()))
                    word += context.Consume();

                if (word.Equals("and", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Token() 
                    { 
                        Type = TokenType.And, 
                        Value = word,
                        Start = start,
                        Length = word.Length,
                    };
                }
                else if (word.Equals("or", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Token() 
                    { 
                        Type = TokenType.Or, 
                        Value = word,
                        Start = start,
                        Length = word.Length,
                    };
                }
                else
                {
                    return new Token() 
                    { 
                        Type = TokenType.String, 
                        Value = word,
                        Start = start,
                        Length = word.Length,
                    };
                }
            }
            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), context.Index, ref context);
        }
    }
}
