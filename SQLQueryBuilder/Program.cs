using SQLQueryBuilder.SQLBuilder.SELECT_Parser;

var input = "*";
var tokens = new SQLSelectTokenizer().Parse(input);
var parsed = new SQLSelectParser2().Parse(input, tokens);

foreach (var item in tokens)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine($"Token Type: {item.Type}");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Value: {item.Value}");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(
        $"Start Position: {item.StartPosition}\n" +
        $"Length: {item.length}\n");
    Console.ResetColor();
}
foreach (var item in parsed)
{
    Console.WriteLine($"Original Query: {item.OriginalQuery}");
    Console.WriteLine($"Table Alias: {item.TableAlias}");
    Console.WriteLine($"Column: {item.ColumnName}");
    Console.WriteLine($"Alias: {item.Alias}");
    Console.WriteLine($"Is Expression: {item.IsExpression}\n");
}