using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLQueryBuilder.Model.DataModel
{
    [Table("test")]
    public class TestModel
    {
        [Key]
        [Column("IDTest")]
        public int id { get; set; }
        [Column("TestName")]
        public string Name { get; set; }
        public string TestNew { get; set; }
    }
}
