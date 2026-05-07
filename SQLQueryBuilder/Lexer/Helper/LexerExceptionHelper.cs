using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Lexer.Helper
{
    public static class LexerExceptionHelper
    {
        public static InvalidOperationException CreateSinglePointerError(Type type,int index, ref LexerContext context)
        {
            return new InvalidOperationException($"{type.ToString()}: Invalid Character {context.GetChar(index)}\n{context.Input}\n{new string(' ', index)}^");
        }
    }
}
