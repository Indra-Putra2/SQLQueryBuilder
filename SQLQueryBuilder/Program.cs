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
var query = new Query<RamData>();
query.Select(r =>new {r.Name, r.MemoryType})
    .Join<Brand>((r, b) => r.BrandID == b.id, b => new { b.Name }, r => new { r.BrandName }, SQLQueryBuilder.Model.Enum.JoinType.INNER)
    .Join<Brand>((r, b) => b.id == r.BrandID, b => new { b.id }, r => new { r.TestID }, SQLQueryBuilder.Model.Enum.JoinType.INNER)
    .Where(ast);
(string sql, DynamicParameters param) result = query.Build();
Console.WriteLine(result.sql);