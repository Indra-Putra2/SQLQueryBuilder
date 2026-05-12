using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Model.SQLBuilderModel;

internal class JoinInfoModel
{
    public required TableRefModel Table { get; init; }
    private string _on = null!;
    public required string On
    {
        get => _on;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Table On cannot be null or whitespace.", nameof(On));
            _on = value.Trim();
        }
    }
    public JoinTypeEnum JoinType { get; init; }
}