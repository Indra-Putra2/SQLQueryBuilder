using Dapper;
using SQLQueryBuilder.Controller.ASTBuilder;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.DataModel;
using SQLQueryBuilder.SQLBuilder;

namespace SQLQueryBuilder
{
    internal class Test
    {
        private string input = "(price < 30 and price > 0) and brandname = 'corsair'";
        public void TestMethod()
        {
            ExpressionModel ast;
            string sql = null;
            DynamicParameters parameters = null;

            try
            {
                ast = ASTController.BuildAST(input);
                (sql, parameters) = QueryBuilder
                    .From<RamDataModel>("r")
                    .Select("r.*, b.Name AS BrandName")
                    .Join<BrandModel>("b", "b.id = r.BrandID")
                    .MapField(("b.Name", "BrandName"))
                    .Where(ast)
                    .OrderBy("r.Price")
                    .Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            AstPrinter.Print(ast, title: "\nAST TREE PRINTER\n");
            //Console.WriteLine(sql);
            foreach (var param in parameters.ParameterNames)
            {
                var value = parameters.Get<object>(param);
                Console.WriteLine($"{param} = {value}");
            }
        }
    }
}