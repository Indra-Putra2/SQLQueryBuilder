using SQLQueryBuilder.Enum;

namespace SQLQueryBuilder.Model.ASTModel
{
    internal class TokenModel
    {
        public required TokenTypeEnum Type { get; set; }
        public required string Value { get; set; }

        public required int Start { get; set; }
        public required int Length { get; set; }

        public override string ToString()
            => $"{Type}: {Value}";
    }
}
