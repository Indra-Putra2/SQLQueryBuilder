using SQLQueryBuilder.Controller.Lexer;
using SQLQueryBuilder.Controller.Perser;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.ASTBuilder
{
    public static class ASTController
    {
        private static LexerController _lexer = new LexerController(); 
        private static ParserController _parser = new ParserController();

        public static ExpressionModel BuildAST(string input)
        {
            List<TokenModel> tokens = null;
            ExpressionModel ast = null;

            try
            {
                tokens = _lexer.Tokenize(input);
                ast = _parser.Parse(tokens, input);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return ast;
        }
    }
}
