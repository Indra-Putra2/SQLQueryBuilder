using Dapper;
using SQLQueryBuilder;
using SQLQueryBuilder.Lexer;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.DataModel;
using SQLQueryBuilder.Perser;
using SQLQueryBuilder.SQLBuilder;
using System.Data.SQLite;

string input3 = "((Name:Kingston OR Name:Crucial) AND ((Price > 40 AND Price<50.5) AND (PricePerGB>0 AND PricePerGB<=10)))";
string input4 = "((Price > 40 AND Price<50) AND (PricePerGB>0 AND PricePerGB<=10))";
string input2 = "Name:'kingston' and name : '64'";
string input = "(price < 30 and price > 0)";

var lexer = new Lexer();
List<Token> Tokens = new();
try
{
    Tokens = lexer.Tokenize(input);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return;
}

var parser = new Parser();
Expr? ast;

try
{
    ast = parser.Parse(Tokens, input);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return;
}

AstPrinter.Print(ast, title:"\nAST TREE PRINTER\n");

string sql = null;
DynamicParameters parameters = null;
try
{
    (sql, parameters) = QueryBuilder
        .From<RamData>("r")
        .Select("r.*, b.Name AS BrandName")
        .Join<Brand>("b", "b.id = r.BrandID")
        .MapField(("b.Name", "BrandName"))
        .Where(ast)
        .OrderBy("r.Price")
        .Build();
    Console.WriteLine(sql);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return;
}
Console.WriteLine(sql);
string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var appFolder = Path.Combine(folder, "InventorySystem");
Directory.CreateDirectory(appFolder);

var _dbPath = Path.Combine(appFolder, "inventory.db");

var _connectionString = new SQLiteConnectionStringBuilder
{
    DataSource = _dbPath,
    Version = 3,
    ForeignKeys = true
}.ToString();

List<RamData> results = new();
using (var conn = new SQLiteConnection(_connectionString))
{
    conn.Open();
    results = conn.Query<RamData>(sql, parameters).ToList();
}
var stringResult = results.Select(r => r.ToString()).Take(10).Count() == 0 ? "Nothing To Show!" : string.Join(",\n", results.Select(r => r.ToString()).Take(10));
Console.WriteLine(stringResult);