using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLQueryBuilder.Model.DataModel
{
    [Table("Products")]
    internal class RamDataModel
    {
        [Key]
        public int id { get; set; }
        public string Name { get; set; }
        public string BrandName { get; set; }
        public int BrandID { get; set; }
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

        public override string ToString()
        {
            {
                return $"""
                    [RamData ID:        {id}]
                    Name:               {Name}
                    Brand:              {BrandName ?? "Null"} (ID: {BrandID})
                    Type:               {MemoryType} @ {MemorySpeed}MHz
                    Module:             {Module}
                    Capacity:           {TotalCapacity}GB
                    Latency:            FWL: {FirstWordLatency}ns, CAS: {CASLatency}
                    Rating:             {Rating} ({ReviewCount} reviews)
                    Color:              {Color}
                    Price:              {Price:C}
                    Price/GB:           {PricePerGB:C}
                    --------------------------------------------------
                    """;
            }
        }
    }
}