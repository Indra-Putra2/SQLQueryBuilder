using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLQueryBuilder.Model
{
    [Table("Products")]
    public class RamData
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("BrandID")]
        public virtual Brand Brand { get; set; }
        [ForeignKey("TestID")]
        public virtual Test Test { get; set; }
        public string TestName { get; set; }
        public string BrandName { get; set; }
        [Column("Column_Name_Test")]
        public string Name { get; set; }
        public int BrandID { get; set; }
        public int TestID { get; set; }
        public string MemoryType { get; set; }
        public int MemorySpeed { get; set; }
        public string Module { get; set; }
        public double TotalCapacity { get; set; }
        public double FirstWordLatency { get; set; }
        public double CASLatency { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public decimal PricePerGB { get; set; }
    }
}
