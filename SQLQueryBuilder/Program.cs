using Dapper;
using SQLQueryBuilder.Lexer;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Perser;
using SQLQueryBuilder.SQLBuilder;

string input = "((Name:Kingston OR Name:Crucial) AND ((Price > 0 AND Price<500 OR PricePerGB>0 AND PricePerGB<=10))";

var lexer = new Lexer();
List<Token> Tokens = new Lexer().Tokenize(input);
var parser = new Parser(Tokens);
var ast = parser.Parse();
var query = new Query<RamData>();
query.Select(r => new { r.Name, r.MemoryType }, "r")
    .Join<Test>((r, t) => r.BrandID == t.id, t => new { t.id, t.Name }, SQLQueryBuilder.Model.Enum.JoinType.LEFT)
    .Join<Test>((r, t) => r.BrandID == t.id, t => new { t.id, t.TestNew }, SQLQueryBuilder.Model.Enum.JoinType.LEFT);
(string sql, DynamicParameters param) result = query.Build();
Console.WriteLine(result.sql);
var builder = new SQLBuilder();
//builder.BuildColumnSet<RamData>();
//builder.BuildColumnSet<Brand>();
//try
//{
//    (string sql, DynamicParameters param) = builder
//        .Select<RamData>(r=> new
//        {
//            RamName = r.Name,
//            RamType = r.MemoryType,
//        },alias:"r")
//        .Join<Brand>(b => new RamData
//        {
//            BrandName = b.Name,
//            BrandID = b.id
//        }, alias:"b")
//        .Join<Test>(t => new Brand
//        {
//            Name = t.TestNew,
//            id = t.id,
//        }, alias:"t")
//        .Where(ast)
//        .Build();
//    Console.WriteLine($"{sql}");
//}
//catch (Exception ex)
//{
//    Console.WriteLine(ex.Message);
//}