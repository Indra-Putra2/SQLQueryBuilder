using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Lexer.State;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Constant;
using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer
{
    internal class Lexer : ILexerStateSwitcher
    {
        private List<Token> _tokens;
        private Dictionary<LexerStateEnum, ILexerState> _states;
        public Lexer()
        {
            _tokens = new();
            _states = new Dictionary<LexerStateEnum, ILexerState>()
            {
                { LexerStateEnum.Parenthesis, new ParenthesisState(this)},
                { LexerStateEnum.Symbol, new SymbolState(this)},
                { LexerStateEnum.Digit, new DigitState(this)},
                { LexerStateEnum.String, new StringState(this)},
            };
        }

        public List<Token> Tokenize(string input)
        {
            var context = new LexerContext(input);
            while (!context.IsAtEnd)
            {
                char c = context.Peek();

                //Handle WhiteSpace
                if (char.IsWhiteSpace(c))
                {
                    context.SetCurrentState(Model.Enum.LexerStateEnum.WhiteSpace);
                    context.Consume();
                    continue;
                }

                //Handle Parenthesis
                else if (c == '(' || c == ')')
                {
                    SwitchState(LexerStateEnum.Parenthesis, ref context);
                    continue;
                }

                //Handle Symbol
                else if (SqlGrammar.OperatorChars.Contains(c))
                {
                    SwitchState(LexerStateEnum.Symbol, ref context);
                    continue;
                }

                // Handle Digits
                else if (char.IsDigit(c))
                {
                    SwitchState(LexerStateEnum.Digit, ref context);
                    continue;
                }

                // Handle String and letters
                else if (c == '\'' || char.IsLetter(c))
                {
                    SwitchState(LexerStateEnum.String, ref context);
                    continue;
                }
                throw LexerExceptionHelper.CreateSinglePointerError(GetType(), context.Index, context.Peek(), ref context, "Unkown Character");
            }
            _tokens.Add(new Token() { Type = TokenType.EOF, Value = "EOF", Start = input.Length, Length = 1 });
            foreach (var token in _tokens)
            {
                Console.WriteLine(token);
            }
            return _tokens;
        }
        public void SwitchState(LexerStateEnum lexerState, ref LexerContext context)
        {
            if (!_states.TryGetValue(lexerState, out var state))
            {
                throw new Exception($"Missing state: {lexerState}");
            }

            context.SetCurrentState(lexerState);
            _tokens.Add(state.Tokenize(ref context));
        }
    }
}
