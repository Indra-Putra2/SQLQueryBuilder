using SQLQueryBuilder.Enum;

namespace SQLQueryBuilder.Model
{
    public class OrderByModel
    {
        public required string Column { get; set; }
        public OrderTypeEnum OrderType { get; set; }
    }
}
