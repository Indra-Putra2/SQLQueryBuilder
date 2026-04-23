namespace SQLQueryBuilder.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JoinAttribute : Attribute
    {
        public string Table { get; }
        public string On { get; } // e.g., "Products.BrandID = Brands.id"
        public JoinAttribute(string table, string on)
        {
            Table = table;
            On = on;
        }
    }
}
