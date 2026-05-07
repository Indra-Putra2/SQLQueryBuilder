using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer.State
{
    internal class SymbolState : ILexerState
    {
        private Dictionary<string, string> _validOperator = new()
        {
            {"<","<"},
            {">",">"},
            {">=",">="},
            {"<=","<="},
            {"=","="},
            {"!=","!="},
            {":",":"},
        };
        private ILexerStateSwitcher _lexer;
        public SymbolState(ILexerStateSwitcher lexer)
        {
            this._lexer = lexer;
        }

        public Token Tokenize(ref LexerContext context)
        {
            int start = context.Index;
            char first = context.Consume();
            char next = context.Peek();

            // Check for 2-character operators first
            string potentialTwoChar = $"{first}{next}";
            if (_validOperator.ContainsKey(potentialTwoChar))
            {
                context.Consume(); // Consume the second character
                return new Token 
                { 
                    Type = TokenType.Operator, 
                    Value = potentialTwoChar,
                    Start = start,
                    Length = potentialTwoChar.Length
                };
            }

            // Fallback to 1-character operator
            string potentialOneChar = first.ToString();
            if (_validOperator.ContainsKey(potentialOneChar))
            {
                return new Token 
                { 
                    Type = TokenType.Operator, 
                    Value = potentialOneChar,
                    Start = start,
                    Length = potentialOneChar.Length
                };
            }

            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), context.Index, ref context);
        }
    }
}
