using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.Perser.Helper
{
    internal static class ParserErrorHelper
    {
        private const int DIVIDER_LENGTH = 50;
        public static Exception CreateError(
            string input,
            TokenModel token,
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
            {new string('=', DIVIDER_LENGTH)}
            {input}
            {pointer}
            {new string('=', DIVIDER_LENGTH)}
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
            {new string('=', DIVIDER_LENGTH)}
            {input}
            {pointer}
            {new string('=', DIVIDER_LENGTH)}
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
            {new string('=', DIVIDER_LENGTH)}
            {input}
            {pointer}
            {new string('=', DIVIDER_LENGTH)}
            """);
        }
    }
}
