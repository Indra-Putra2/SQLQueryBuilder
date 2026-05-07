using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Perser
{
    public static class ParserErrorHelper
    {
        public static Exception CreateError(
            string input,
            Token token,
            string message)
        {
            string pointer = $"{new string(' ', token.Start)}^";
            if (token.Length > 1)
            {
                string pointerTrail = new string('~', token.Length - 1);
                pointer = $"{pointer}{pointerTrail}";
            }

            return new Exception(
            $"""
            {message}
            {new string('=', 25)}
            {input}
            {pointer}
            {new string('=', 25)}
            """);
        }

        public static Exception CreateErrorTo(
            string input,
            int end,
            string message)
        {
            string pointer = $"{new string(' ', 0)}^";
            string pointerTrail = new string('~', end);
            pointer = $"{pointer}{pointerTrail}";

            return new Exception(
            $"""
            {message}
            {new string('=', 25)}
            {input}
            {pointer}
            {new string('=', 25)}
            """);
        }

        public static Exception CreateErrorFrom(
            string input,
            int from,
            string message)
        {
            string pointer = $"{new string(' ', from)}^";
            string pointerTrail = new string('~', input.Length);
            pointer = $"{pointer}{pointerTrail}";

            return new Exception(
            $"""
            {message}
            {new string('=', 25)}
            {input}
            {pointer}
            {new string('=', 25)}
            """);
        }
    }
}
