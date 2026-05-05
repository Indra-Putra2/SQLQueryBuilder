using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.SQLBuilderModel;

public class JoinInfo
{
    public required TableRef Table { get; init; }
    private string _on = null!;
    public required string On 
    {  
        get => _on; 
        init => _on = value.Trim() ?? throw new ArgumentNullException("ON Condition Cannot Be Null!");
    }
    public JoinTypeEnum JoinType { get; init; }
}