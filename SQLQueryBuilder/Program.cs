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
var builder = new SQLBuilder();
builder.BuildColumnSet<RamData>();
builder.BuildColumnSet<Brand>();
try
{
    (string sql, DynamicParameters param) = builder
        .Select<RamData>(r=> new
        {
            RamName = r.Name,
            RamType = r.MemoryType,
        },alias:"p")
        .Join<Brand>(b => new RamData
        {
            BrandName = b.Name,
            BrandID = b.id
        }, alias:"b")
        .Where(ast)
        .Build();
    Console.WriteLine($"{sql}");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}