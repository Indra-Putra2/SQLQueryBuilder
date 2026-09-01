namespace SQLQueryBuilder.Test
{
    internal class TestClass
    {
        public void Test(string input)
        {
            //var tokens = new SQLTokenizer().Tokenize(input);
            //var parsed = new SQLSelectParser().Parse(input, tokens);
            //var sql = new QueryBuilder().From<RamReadModel>("r").Select(input).Build();

            //foreach (var item in tokens)
            //{
            //    Console.ForegroundColor = ConsoleColor.Blue;
            //    Console.WriteLine($"Token Type: {item.Type}");
            //    Console.ForegroundColor = ConsoleColor.Green;
            //    Console.WriteLine($"Value: {item.Value}");
            //    Console.ForegroundColor = ConsoleColor.Yellow;
            //    Console.WriteLine(
            //        $"Start Position: {item.StartPosition}\n" +
            //        $"Length: {item.RawLength}\n");
            //    Console.ResetColor();
            //}
            //foreach (var item in parsed)
            //{
            //    Console.WriteLine($"Original Query: {item.OriginalQuery}");
            //    Console.WriteLine($"Table Alias: {item.TableAlias}");
            //    Console.WriteLine($"Column: {item.ColumnName}");
            //    Console.WriteLine($"Alias: {item.Alias}");
            //    Console.WriteLine($"Is Expression: {item.IsExpression}");
            //    Console.WriteLine($"Is Wildcard: {item.IsWildcard}\n");
            //}
            //Console.WriteLine(sql.sql);
        }
    }
}
