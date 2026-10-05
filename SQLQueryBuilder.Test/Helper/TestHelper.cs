using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLQueryBuilder.Test.Helper
{
    internal class TestHelper
    {
        public static (string str, SQLTokenType type) GetRandomIdentifier(int length, Random random)
        {
            const string chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            SQLTokenType type = SQLTokenType.Unknown;

            var sb = new StringBuilder(length + 2);

            for (var i = 0; i < length; i++)
                sb.Append(chars[random.Next(chars.Length)]);

            var text = sb.ToString();
            if (SQLDictionary.SQLKeywords.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                return (text, SQLTokenType.Keyword);
            }

            switch (random.Next(3))
            {
                case 0:
                    text = $"\"{text}\"";
                    type = SQLTokenType.DoubleQuoteIdentifier;
                    break;
                case 1:
                    text = $"[{text}]";
                    type = SQLTokenType.BracketIdentifier;
                    break;
                default:
                    type = SQLTokenType.Identifier;
                    break;
            }

            return (text, type);
        }
        public static (string str, SQLTokenType type) GetRandomString(int length, Random random)
        {
            const string chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890~!@#$%^&*()_+`-=[]\\;\",./<>?:{}|";

            SQLTokenType type = SQLTokenType.Unknown;

            var sb = new StringBuilder(length + 2);

            for (var i = 0; i < length; i++)
                sb.Append(chars[random.Next(chars.Length)]);

            var text = sb.ToString();

            switch (random.Next(3))
            {
                case 0:
                    text = $"N'{text}'";
                    type = SQLTokenType.UnicodeString;
                    break;
                default:
                    text = $"'{text}'";
                    type = SQLTokenType.StringLiteral;
                    break;
            }

            return (text, type);
        }
        public static (string str, SQLTokenType type) GetRandomLiteral(int length, Random random)
        {
            switch (random.Next(0, 2))
            {
                case 0:
                    return GetRandomIdentifier(length, random);
                default:
                    return GetRandomString(length, random);
            }
        }

        public static SQLGeneratorContext GetGeneratorContext() => new SQLGeneratorContext(new StringBuilder(), new Dapper.DynamicParameters());
        public static string CleanString(string input) => Regex.Replace(input, @"\s+", " ").Trim();
    }
}
