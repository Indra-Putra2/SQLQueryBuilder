using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLQueryBuilder.Model
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        public int id { get; set; }
        public string Name { get; set; }
    }
}
