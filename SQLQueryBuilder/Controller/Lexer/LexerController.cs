using SQLQueryBuilder.Constant;
using SQLQueryBuilder.Controller.Lexer.Helper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Controller.Lexer.State;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.Lexer
{
    internal class LexerController : ILexerStateSwitcher
    {
        private List<TokenModel> _tokens;
        private Dictionary<LexerStateEnum, ILexerState> _states;
        public LexerController()
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

        public List<TokenModel> Tokenize(string input)
        {
            _tokens.Clear();
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new InvalidOperationException("Input Cannot Be Null!");
            }

            var context = new LexerContextModel(input);
            while (!context.IsAtEnd)
            {
                char c = context.Peek();

                //Handle WhiteSpace
                if (char.IsWhiteSpace(c))
                {
                    context.SetCurrentState(LexerStateEnum.WhiteSpace);
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
            _tokens.Add(new TokenModel() { Type = TokenTypeEnum.EOF, Value = "EOF", Start = input.Length, Length = 1 });
            return _tokens;
        }
        public void SwitchState(LexerStateEnum lexerState, ref LexerContextModel context)
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
