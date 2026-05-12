using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLQueryBuilder.Model.DataModel
{
    [Table("Brands")]
    internal class BrandModel
    {
        [Key]
        [Column("IDBrand")]
        public int id { get; set; }
        public string Name { get; set; }
    }
}
