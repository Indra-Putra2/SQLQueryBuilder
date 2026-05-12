using SQLQueryBuilder.Controller.Lexer.Helper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.Lexer.State
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

        public TokenModel Tokenize(ref LexerContextModel context)
        {
            int start = context.Index;
            char first = context.Consume();
            char next = context.Peek();

            // Check for 2-character operators first
            string potentialTwoChar = $"{first}{next}";
            if (_validOperator.ContainsKey(potentialTwoChar))
            {
                context.Consume(); // Consume the second character
                return new TokenModel 
                { 
                    Type = TokenTypeEnum.Operator, 
                    Value = potentialTwoChar,
                    Start = start,
                    Length = potentialTwoChar.Length
                };
            }

            // Fallback to 1-character operator
            string potentialOneChar = first.ToString();
            if (_validOperator.ContainsKey(potentialOneChar))
            {
                return new TokenModel 
                { 
                    Type = TokenTypeEnum.Operator, 
                    Value = potentialOneChar,
                    Start = start,
                    Length = potentialOneChar.Length
                };
            }

            throw LexerExceptionHelper.CreateSinglePointerError(GetType(), start, first, ref context, "Invalid Character");
        }
    }
}
