using SQLQueryBuilder.Lexer.Helper;
using SQLQueryBuilder.Lexer.State;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Constant;
using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.Interface;

namespace SQLQueryBuilder.Lexer
{
    public class Lexer : ILexerStateSwitcher
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
                { LexerStateEnum.Letter, new LetterState(this)},
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

                // Handle Letters
                else if (char.IsLetter(c))
                {
                    SwitchState(LexerStateEnum.Letter, ref context);
                    continue;
                }
                throw LexerExceptionHelper.CreateSinglePointerError(GetType(), context.Index, ref context);
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
            context.SetCurrentState(lexerState);
            if (_states.TryGetValue(lexerState, out var state))
            {
                _tokens.Add(state.Tokenize(ref context));
                return;
            }

            var frame = new System.Diagnostics.StackTrace().GetFrame(1); // 1 is the immediate caller
            var method = frame.GetMethod();
            var callerInfo = $"{method.DeclaringType.Name}.{method.Name}";

            throw new InvalidOperationException($"{callerInfo} State {lexerState} Doesnt Exist!");
        }
    }
}
