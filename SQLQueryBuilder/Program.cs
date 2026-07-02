using SQLQueryBuilder.SQLBuilder.SELECT_Parser;
var input = "r.id,\r\nr.Name AS CustomerName,\r\nb.[Brand Name],\r\n`Category`.`Display Name`,\r\n\"Order\".\"Total\",\r\nCOUNT(*),\r\nSUM(r.Price * r.Quantity) AS TotalPrice,\r\nAVG(CASE WHEN r.Score >= 90 THEN 1 ELSE 0 END) PassedRate,\r\nCASE\r\n    WHEN r.Status = 'A' THEN 'Active'\r\n    WHEN r.Status = 'I' THEN 'Inactive'\r\n    ELSE 'Unknown'\r\nEND StatusText,\r\nCOALESCE(r.Nickname, 'No Name') AS DisplayName,\r\nISNULL(r.Description, ''),\r\nCAST(r.Price AS DECIMAL(18,2)) AS Price,\r\nCONCAT(r.FirstName, ' ', r.LastName) FullName,\r\nr.Price * r.Quantity * (1 - r.Discount / 100.0) NetPrice,\r\n(r.Value1 + r.Value2) / NULLIF(r.Divisor, 0),\r\nCASE\r\n    WHEN r.Remark = 'It''s OK' THEN 'Accepted'\r\n    ELSE 'Rejected'\r\nEND,\r\nNULL,\r\n12345,\r\n123.456,\r\n-999,\r\n+r.Bonus,\r\nr.CreatedDate,\r\nr.ModifiedDate LastModified,\r\nr.id <> b.Id,\r\nr.Score >= 80,\r\nr.Score <= 100,\r\nr.Flag != 0,\r\nr.Amount + 5,\r\nr.Amount - 3,\r\nr.Amount / 2,\r\nr.Amount % 10;";
var parsed = new SQLSelectTokenizer().Parse(input);
Console.WriteLine($"Input:{input}\n");
foreach(var item in parsed)
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