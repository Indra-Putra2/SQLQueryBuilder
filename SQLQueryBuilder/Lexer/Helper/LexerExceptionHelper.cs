using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Lexer.Helper
{
    internal static class LexerExceptionHelper
    {
        public static InvalidOperationException CreateSinglePointerError(Type type,int index,char c, ref LexerContext context, string errorMessage)
        {
            return new InvalidOperationException($"{type.ToString()}: {errorMessage} {c}\n{context.Input}\n{new string(' ', index)}^");
        }
    }
}
