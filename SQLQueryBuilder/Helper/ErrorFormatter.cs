using System.Text;

namespace SQLQueryBuilder.Helper
{
    internal static class ErrorFormatter
    {
        public static string Create(string text, int errorIndexPos, string errorMessage, int errorTextLength = 1, int contextLength = 25)
        {
            errorTextLength = errorTextLength <= 0 ? 1 : errorTextLength;
            int endIdx = errorIndexPos + errorTextLength - 1;
            var context = GetContext(text, errorIndexPos, endIdx, contextLength);

            bool left = context.Start > 0;
            bool right = context.End < text.Length;

            var str = new StringBuilder();
            string trailingStr = "...";
            string leftDots = left ? trailingStr : "";
            string rightDots = right ? trailingStr : "";

            string enclosing = new string('=', context.Length + leftDots.Length + rightDots.Length);
            int pointerOffset = context.Pointer + leftDots.Length;
            string pointer = $"{new string(' ', pointerOffset)}{new string('^', errorTextLength)}";

            return str
                .Append('\n')
                .Append($"Error At Position: {errorIndexPos}\n")
                .Append(enclosing).Append('\n')
                .Append(leftDots).Append(context.Snippet).Append(rightDots).Append('\n')
                .Append(pointer).Append('\n')
                .Append(enclosing).Append('\n')
                .Append(errorMessage).ToString();
        }
        private static (string Snippet, int Pointer, int Length, int Start, int End) GetContext(string text, int startIdx, int endIdx, int contextLength)
        {
            int start = Math.Max(0, startIdx - contextLength);
            int end = Math.Min(text.Length, endIdx + contextLength + 1);
            int length = end - start;
            return (text[start..end], startIdx - start, length, start, end);
        }
    }
}
