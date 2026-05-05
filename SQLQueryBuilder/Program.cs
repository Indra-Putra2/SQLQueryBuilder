using Dapper;
using SQLQueryBuilder.Lexer;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.DataModel;
using SQLQueryBuilder.Perser;
using SQLQueryBuilder.SQLBuilder;

string input = "((Name:Kingston OR Name:Crucial) AND ((Price > 0 AND Price<500 OR PricePerGB>0 AND PricePerGB<=10))";

var lexer = new Lexer();
List<Token> Tokens = new Lexer().Tokenize(input);
var parser = new Parser(Tokens);
var ast = parser.Parse();
(string sql, DynamicParameters parameters) = new Query()
    .From<RamData>("r")
    .Join<Brand>("b", "b.UserId = r.Id")
    .Select("r.*")
    .Where(ast)
    .OrderBy("r.Name")
    .Build();
Console.WriteLine(sql);