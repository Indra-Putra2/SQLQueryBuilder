using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer.State
{
    internal class StringState : ILexerState
    {
        private ILexerStateSwitcher _lexer;
        public StringState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public Token Tokenize(ref LexerContext context)
        {
            char c = context.Peek();
            int start = context.Index;
            string String = string.Empty;

            if (c == '\'')
            {
                context.Consume(); // consume initial quote
                while (!context.IsAtEnd && context.Peek() != '\'')
                    String += context.Consume();

                if (context.IsAtEnd)
                {
                    throw LexerExceptionHelper.CreateSinglePointerError(GetType(), start, c, ref context, "Unterminated string literal");
                }

                context.Consume(); // closing quote

                return new Token()
                {
                    Type = TokenType.String,
                    Value = String,
                    Start = start,
                    Length = String.Length,
                };
            }
            else if (char.IsLetter(c))
            {
                while (!context.IsAtEnd && char.IsLetter(context.Peek()))
                    String += context.Consume();

                if (String.Equals("and", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Token()
                    {
                        Type = TokenType.AND,
                        Value = String,
                        Start = start,
                        Length = String.Length,
                    };
                }
                else if (String.Equals("or", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Token()
                    {
                        Type = TokenType.OR,
                        Value = String,
                        Start = start,
                        Length = String.Length,
                    };
                }
                else
                {
                    return new Token()
                    {
                        Type = TokenType.String,
                        Value = String,
                        Start = start,
                        Length = String.Length,
                    };
                }
            }
            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), start, c, ref context, "Invalid Character");
        }
    }
}
