using Dapper;
using HardwareInventoryLibrary.Brand;
using HardwareInventoryLibrary.Ram;
using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLQueryBuilder.Test.QueryBuilderTest
{
    public class QueryTest
    {
        public static IEnumerable<object[]> DecimalCases =>
        [
            [
                "\"r\".\"PricePerGB\"",
                "\"r\".\"PricePerGB\" BETWEEN 0.01 AND 9999.99",
                "SELECT \"r\".\"PricePerGB\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"PricePerGB\" BETWEEN @p1 AND @p2",
                0.01m,
                9999.99m
            ],
            [
                "\"r\".\"Price\"",
                "\"r\".\"Price\" = 0 OR \"r\".\"Price\" = -10.5",
                "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" = @p1 OR \"r\".\"Price\" = @p2",
                0,
                -10.5m
            ],

        ];
        #region Parameter Test
        [Fact]
        public void Query_Builder_Must_Keep_Parameter_Index_Across_Clauses()
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select("\"r\".\"Price\" + 5 AS \"Price\"")
                .Join<BrandReadModel>(
                    "b",
                    "\"r\".\"Id\" = \"b\".\"Id\" AND \"b\".\"Name\" = 'Corsair'")
                .Where("\"r\".\"Price\" > 100 AND \"r\".\"Name\" = 'Vengeance'")
                .GroupBy("\"r\".\"Name\"")
                .Having("AVG(\"r\".\"Price\") TYPE DECIMAL? > 50")
                .Build();

            Assert.Equal(
                CleanString(
                    "SELECT \"r\".\"Price\" + 5 AS \"Price\" " +
                    "FROM \"Rams\" AS \"r\" " +
                    "INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\" AND \"b\".\"Name\" = @p1 " +
                    "WHERE \"r\".\"Price\" > @p2 AND \"r\".\"Name\" = @p3 " +
                    "GROUP BY \"r\".\"Name\" " +
                    "HAVING AVG(\"r\".\"Price\") > @p4"),
                CleanString(query.SQL));

            Assert.Equal(4, query.Parameters.ParameterNames.Count());

            Assert.Equal("Corsair", query.Parameters.Get<string>("@p1"));
            Assert.Equal(100, query.Parameters.Get<int>("@p2"));
            Assert.Equal("Vengeance", query.Parameters.Get<string>("@p3"));
            Assert.Equal(50, query.Parameters.Get<int>("@p4"));
        }
        [Theory]
        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" = 'corsair'",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" = @p1",
        new object[] { "corsair" })]

        [InlineData(
        "\"r\".\"Price\"",
        "\"r\".\"Price\" >= 100 AND \"r\".\"Price\" <= 500",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" >= @p1 AND \"r\".\"Price\" <= @p2",
        new object[] { 100, 500 })]

        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" IS NOT NULL",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" IS NOT NULL",
        new object[] { })] // Empty array for no-param queries

        [InlineData(
        "\"r\".\"Price\"",
        "\"r\".\"Price\" > 100",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" > @p1",
        new object[] { 100 })]

        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" LIKE 'cors%'",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" LIKE @p1",
        new object[] { "cors%" })]

        [InlineData(
        "\"r\".\"Price\"",
        "\"r\".\"Price\" BETWEEN 100 AND 500",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" BETWEEN @p1 AND @p2",
        new object[] { 100, 500 })]

        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" IN ('corsair', 'kingston', 'gskill')",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" IN (@p1, @p2, @p3)",
        new object[] { "corsair", "kingston", "gskill" })]

        [InlineData(
        "\"r\".\"Price\"",
        "\"r\".\"Price\" NOT IN (10, 20, 30)",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" NOT IN (@p1, @p2, @p3)",
        new object[] { 10, 20, 30 })]

        [InlineData(
        "\"r\".\"Price\"",
        "NOT \"r\".\"Price\" > 100",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE NOT \"r\".\"Price\" > @p1",
        new object[] { 100 })]

        [InlineData(
        "\"r\".\"Price\"",
        "(\"r\".\"Price\" > 100 OR \"r\".\"Price\" < 20) AND \"r\".\"Price\" != 50",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE (\"r\".\"Price\" > @p1 OR \"r\".\"Price\" < @p2) AND \"r\".\"Price\" != @p3",
        new object[] { 100, 20, 50 })]

        [InlineData(
        "\"r\".\"Price\"",
        "\"r\".\"Price\" + 10 > 100",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" + @p1 > @p2",
        new object[] { 10, 100 })]

        [InlineData(
        "\"r\".\"Price\"",
        "CASE WHEN \"r\".\"Price\" > 100 THEN 1 ELSE 0 END = 1",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE CASE WHEN \"r\".\"Price\" > @p1 THEN @p2 ELSE @p3 END = @p4",
        new object[] { 100, 1, 0, 1 })]
        public void Test_QueryParameterization(string select, string where, string expectedSql, object[] expectedParams)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(select)
                .Where(where)
                .Build();

            Assert.Equal(CleanString(expectedSql), CleanString(query.SQL));

            Assert.Equal(expectedParams.Length, query.Parameters.ParameterNames.Count());

            for (int i = 0; i < expectedParams.Length; i++)
            {
                string paramName = $"@p{i + 1}";
                Assert.Equal(expectedParams[i], query.Parameters.Get<object>(paramName));
            }
        }

        [Theory]
        // 1. Escaped Quotes & Special Characters in String Literals
        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" = 'Corsair ''Vengeance'' Pro'",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" = @p1",
        new object[] { "Corsair 'Vengeance' Pro" })] // Asserts unescaping logic

        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" LIKE '%16GB''s%'",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" LIKE @p1",
        new object[] { "%16GB's%" })]

        // 3. Duplicate Literal Values (Ensures parameters aren't deduplicated/merged improperly)
        [InlineData(
        "\"r\".\"Price\"",
        "\"r\".\"Price\" = 100 OR \"r\".\"Price\" > 100",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" = @p1 OR \"r\".\"Price\" > @p2",
        new object[] { 100, 100 })]

        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" = 'Corsair' OR \"r\".\"BrandName\" = 'Corsair'",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" = @p1 OR \"r\".\"BrandName\" = @p2",
        new object[] { "Corsair", "Corsair" })]

        // 4. Empty Strings & Single Characters
        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" = ''",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" = @p1",
        new object[] { "" })]

        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" = ' '",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" = @p1",
        new object[] { " " })]

        // 5. Large IN List (Sequential Indexing Check)
        [InlineData(
        "\"r\".\"Id\"",
        "\"r\".\"Id\" IN (1, 2, 3, 4, 5)",
        "SELECT \"r\".\"Id\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Id\" IN (@p1, @p2, @p3, @p4, @p5)",
        new object[] { 1, 2, 3, 4, 5 })]

        // 6. Compound Predicates with Parentheses & NOT
        [InlineData(
        "\"r\".\"Price\"",
        "NOT (\"r\".\"Price\" < 50 AND (\"r\".\"Name\" = 'Generic' OR \"r\".\"Name\" = 'Unknown'))",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE NOT (\"r\".\"Price\" < @p1 AND (\"r\".\"Name\" = @p2 OR \"r\".\"Name\" = @p3))",
        new object[] { 50, "Generic", "Unknown" })]

        // 7. CASE Statement with Multiple WHEN Arms
        [InlineData(
        "\"r\".\"Price\"",
        "CASE WHEN \"r\".\"Price\" > 200 THEN 'Expensive' WHEN \"r\".\"Price\" > 100 THEN 'Moderate' ELSE 'Budget' END = 'Budget'",
        "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE CASE WHEN \"r\".\"Price\" > @p1 THEN @p2 WHEN \"r\".\"Price\" > @p3 THEN @p4 ELSE @p5 END = @p6",
        new object[] { 200, "Expensive", 100, "Moderate", "Budget", "Budget" })]

        // 8. Null Expressions (Literals like NULL should NOT generate parameters)
        [InlineData(
        "\"r\".\"Name\"",
        "\"r\".\"Name\" IS NULL OR \"r\".\"Name\" IS NOT NULL",
        "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" IS NULL OR \"r\".\"Name\" IS NOT NULL",
        new object[] { })]
        public void Test_QueryParameterization_EdgeCases(string select, string where, string expectedSql, object[] expectedParams)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(select)
                .Where(where)
                .Build();

            // 1. Assert Clean SQL Output
            Assert.Equal(CleanString(expectedSql), CleanString(query.SQL));

            // 2. Assert Parameter Count Matches
            Assert.Equal(expectedParams.Length, query.Parameters.ParameterNames.Count());

            // 3. Assert Parameter Indexing & Values Match
            for (int i = 0; i < expectedParams.Length; i++)
            {
                string paramName = $"@p{i + 1}";
                Assert.Equal(expectedParams[i], query.Parameters.Get<object>(paramName));
            }
        }

        [Theory]
        [MemberData(nameof(DecimalCases))]
        public void Query_Builder_Must_Parameterize_Decimals(
            string input,
            string where,
            string expected,
            object first,
            object second)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(input)
                .Where(where)
                .Build();

            Assert.Equal(CleanString(expected), CleanString(query.SQL));

            Assert.Equal(first, query.Parameters.Get<object>("@p1"));
            Assert.Equal(second, query.Parameters.Get<object>("@p2"));
        }
        #endregion

        #region Regular Select + Join
        [Theory]
        // Arithmetic & Modulo Operations (require AS alias to target DTO property)
        [InlineData("\"r\".\"Price\" + 5 AS \"Price\"", "SELECT \"r\".\"Price\" + 5 AS \"Price\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("\"r\".\"Price\" * 10 AS \"Price\"", "SELECT \"r\".\"Price\" * 10 AS \"Price\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("\"r\".\"Price\" / 2.5 AS \"Price\"", "SELECT \"r\".\"Price\" / 2.5 AS \"Price\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("\"r\".\"PricePerGB\" % 2 AS \"PricePerGB\"", "SELECT \"r\".\"PricePerGB\" % 2 AS \"PricePerGB\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        // String Functions (using BrandName slot on RamReadModel)
        [InlineData("UPPER(\"b\".\"Name\") TYPE STRING AS \"BrandName\"", "SELECT UPPER(\"b\".\"Name\") AS \"BrandName\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("LOWER(\"b\".\"Name\") TYPE STRING AS \"BrandName\"", "SELECT LOWER(\"b\".\"Name\") AS \"BrandName\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("LENGTH(\"b\".\"Name\") TYPE INT AS \"ReviewCount\"", "SELECT LENGTH(\"b\".\"Name\") AS \"ReviewCount\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        // Aggregates & Math Functions
        [InlineData("COUNT(\"r\".\"Id\") TYPE INT AS \"ReviewCount\"", "SELECT COUNT(\"r\".\"Id\") AS \"ReviewCount\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("AVG(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"", "SELECT AVG(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        [InlineData("ROUND(\"r\".\"Price\", 2) TYPE DECIMAL AS \"Price\"", "SELECT ROUND(\"r\".\"Price\", 2) AS \"Price\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        // Complex Expressions & Functions
        [InlineData("CASE WHEN \"r\".\"Price\" > 10 THEN RANDOM() TYPE INT + 5 ELSE CASE WHEN \"b\".\"Name\" LIKE 'A%' THEN 1 ELSE 0 END END AS \"ReviewCount\"",
            "SELECT CASE WHEN \"r\".\"Price\" > 10 THEN RANDOM() + 5 ELSE CASE WHEN \"b\".\"Name\" LIKE 'A%' THEN 1 ELSE 0 END END AS \"ReviewCount\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        [InlineData("COALESCE(\"r\".\"Price\", 0) TYPE DECIMAL AS \"Price\"", "SELECT COALESCE(\"r\".\"Price\", 0) AS \"Price\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        // Wildcard pass-through
        [InlineData("\"r\".*", "SELECT \"r\".* FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        // Dates
        [InlineData(
        "d'2020-05-01' AS [DATE]",
        "SELECT CAST('2020-05-01' AS DATE) AS [DATE] FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        [InlineData(
        "t'12:30:15' AS \"TIME\"",
        "SELECT CAST('12:30:15' AS TIME) AS \"TIME\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        [InlineData(
        "t'12:30:15.1234567' AS \"TIME\"",
        "SELECT CAST('12:30:15.1234567' AS TIME) AS \"TIME\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        [InlineData(
        "dt'2020-05-01 12:30:15' AS \"DATETIME\"",
        "SELECT CAST('2020-05-01 12:30:15' AS DATETIME) AS \"DATETIME\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]

        [InlineData(
        "dt'2020-05-01 12:30:15.1234567' AS \"DATETIME\"",
        "SELECT CAST('2020-05-01 12:30:15.1234567' AS DATETIME) AS \"DATETIME\" FROM \"Rams\" AS \"r\" INNER JOIN \"Brands\" AS \"b\" ON \"r\".\"Id\" = \"b\".\"Id\"")]
        public void Query_Builder_Must_Create_SQL_Select_And_Join_Correctly(string input, string expected)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(input)
                .Join<BrandReadModel>("b", "\"r\".\"Id\" = \"b\".\"Id\"")
                .Build();

            Assert.Equal(CleanString(expected), CleanString(query.SQL));
        }
        #endregion

        #region Select + Filter
        [Theory]
        [InlineData(
            "\"r\".\"Name\"",
            "\"r\".\"Name\" = 'corsair'",
            "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" = @p1")]

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" > 100",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" > @p1")]

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" >= 100 AND \"r\".\"Price\" <= 500",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" >= @p1 AND \"r\".\"Price\" <= @p2")]

        [InlineData(
            "\"r\".\"Name\"",
            "\"r\".\"Name\" LIKE 'cors%'",
            "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" LIKE @p1")]

        [InlineData(
            "\"r\".\"Name\"",
            "\"r\".\"Name\" IS NOT NULL",
            "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" IS NOT NULL")] // Null checks contain no literals/parameters

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" BETWEEN 100 AND 500",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" BETWEEN @p1 AND @p2")]

        [InlineData(
            "\"r\".\"Name\"",
            "\"r\".\"Name\" IN ('corsair', 'kingston', 'gskill')",
            "SELECT \"r\".\"Name\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Name\" IN (@p1, @p2, @p3)")]

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" NOT IN (10, 20, 30)",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" NOT IN (@p1, @p2, @p3)")]

        [InlineData(
            "\"r\".\"Price\"",
            "NOT \"r\".\"Price\" > 100",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE NOT \"r\".\"Price\" > @p1")]

        [InlineData(
            "\"r\".\"Price\"",
            "(\"r\".\"Price\" > 100 OR \"r\".\"Price\" < 20) AND \"r\".\"Price\" != 50",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE (\"r\".\"Price\" > @p1 OR \"r\".\"Price\" < @p2) AND \"r\".\"Price\" != @p3")]

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" + 10 > 100",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" + @p1 > @p2")]

        [InlineData(
            "\"r\".\"Price\"",
            "CASE WHEN \"r\".\"Price\" > 100 THEN 1 ELSE 0 END = 1",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" WHERE CASE WHEN \"r\".\"Price\" > @p1 THEN @p2 ELSE @p3 END = @p4")]
        public void Query_Builder_Must_Create_SQL_Select_And_Where_Correctly(string select, string where, string expectedSql)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(select)
                .Where(where)
                .Build();

            Assert.Equal(CleanString(expectedSql), CleanString(query.SQL));
        }

        [Theory]
        [InlineData(
            "AVG(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"",
            "\"r\".\"Price\"",
            "AVG(\"r\".\"Price\") TYPE DECIMAL < 100",
            "SELECT AVG(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" GROUP BY \"r\".\"Price\" HAVING AVG(\"r\".\"Price\") < @p1")]

        [InlineData(
            "\"r\".\"Name\", COUNT(*) TYPE INT AS \"ReviewCount\"",
            "\"r\".\"Name\"",
            "COUNT(*) TYPE INT > 5",
            "SELECT \"r\".\"Name\", COUNT(*) AS \"ReviewCount\" FROM \"Rams\" AS \"r\" GROUP BY \"r\".\"Name\" HAVING COUNT(*) > @p1")]

        [InlineData(
            "\"r\".\"Name\", AVG(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"",
            "\"r\".\"Name\", \"r\".\"Price\"",
            "AVG(\"r\".\"Price\") TYPE DECIMAL >= 50",
            "SELECT \"r\".\"Name\", AVG(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" GROUP BY \"r\".\"Name\", \"r\".\"Price\" HAVING AVG(\"r\".\"Price\") >= @p1")]

        [InlineData(
            "\"r\".\"Name\", MIN(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"",
            "\"r\".\"Name\"",
            "MIN(\"r\".\"Price\") TYPE DECIMAL > 10 AND MAX(\"r\".\"Price\") TYPE DECIMAL < 500",
            "SELECT \"r\".\"Name\", MIN(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" GROUP BY \"r\".\"Name\" HAVING MIN(\"r\".\"Price\") > @p1 AND MAX(\"r\".\"Price\") < @p2")]

        [InlineData(
            "\"r\".\"Name\", COUNT(*) TYPE INT AS \"ReviewCount\"",
            "\"r\".\"Name\"",
            "COUNT(*) TYPE INT = 1",
            "SELECT \"r\".\"Name\", COUNT(*) AS \"ReviewCount\" FROM \"Rams\" AS \"r\" GROUP BY \"r\".\"Name\" HAVING COUNT(*) = @p1")]

        [InlineData(
            "\"r\".\"Name\", SUM(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"",
            "\"r\".\"Name\"",
            "SUM(\"r\".\"Price\") TYPE DECIMAL BETWEEN 100 AND 500",
            "SELECT \"r\".\"Name\", SUM(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" GROUP BY \"r\".\"Name\" HAVING SUM(\"r\".\"Price\") BETWEEN @p1 AND @p2")]
        public void Query_Builder_Must_Create_SQL_Select_And_GroupBy_And_Having_Correctly(string input, string groupBy, string having, string expected)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(input)
                .GroupBy(groupBy)
                .Having(having)
                .Build();

            Assert.Equal(CleanString(expected), CleanString(query.SQL));
        }

        [Theory]
        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" DESC",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" ORDER BY \"r\".\"Price\" DESC")]

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" ASC",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" ORDER BY \"r\".\"Price\" ASC")]

        [InlineData(
            "\"r\".\"Price\", \"r\".\"Id\"",
            "\"r\".\"Price\" DESC",
            "SELECT \"r\".\"Price\", \"r\".\"Id\" FROM \"Rams\" AS \"r\" ORDER BY \"r\".\"Price\" DESC")]

        [InlineData(
            "\"r\".\"Price\"",
            "\"r\".\"Price\" + 5 DESC",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" ORDER BY \"r\".\"Price\" + @p1 DESC")]

        [InlineData(
            "d'2020-05-01' AS [Date]",
            "\"r\".\"Price\" + 5 DESC",
            "SELECT CAST('2020-05-01' AS DATE) AS [Date] FROM \"Rams\" AS \"r\" ORDER BY \"r\".\"Price\" + @p1 DESC")]

        [InlineData(
            "\"r\".\"Price\"",
            "CASE WHEN \"r\".\"Price\" > 10 THEN \"r\".\"Price\" ELSE 0 END DESC",
            "SELECT \"r\".\"Price\" FROM \"Rams\" AS \"r\" ORDER BY CASE WHEN \"r\".\"Price\" > @p1 THEN \"r\".\"Price\" ELSE @p2 END DESC")]
        public void Query_Builder_Must_Create_SQL_Select_And_OrderBy_Correctly(string input, string orderBy, string expected)
        {
            var query = CreateQuery()
                .From<RamReadModel>("r")
                .Select(input)
                .OrderBy(orderBy)
                .Build();

            Assert.Equal(CleanString(expected), CleanString(query.SQL));
        }

        [Theory]
        // 1. Average price grouped by name
        [InlineData(
            "\"r\".\"Name\", AVG(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"",
            "\"r\".\"TotalCapacity\" > 8",
            "\"r\".\"Name\"",
            "AVG(\"r\".\"Price\") TYPE DECIMAL < 150",
            "AVG(\"r\".\"Price\") TYPE DECIMAL DESC",
            "SELECT \"r\".\"Name\", AVG(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"TotalCapacity\" > @p1 GROUP BY \"r\".\"Name\" HAVING AVG(\"r\".\"Price\") < @p2 ORDER BY AVG(\"r\".\"Price\") DESC")]

        // 2. Grouping by MemoryType with COUNT
        [InlineData(
            "\"r\".\"MemoryType\", COUNT(\"r\".\"Id\") TYPE INT AS \"ReviewCount\"",
            "\"r\".\"Price\" IS NOT NULL",
            "\"r\".\"MemoryType\"",
            "COUNT(\"r\".\"Id\") TYPE INT >= 5",
            "\"r\".\"MemoryType\" ASC",
            "SELECT \"r\".\"MemoryType\", COUNT(\"r\".\"Id\") AS \"ReviewCount\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Price\" IS NOT NULL GROUP BY \"r\".\"MemoryType\" HAVING COUNT(\"r\".\"Id\") >= @p1 ORDER BY \"r\".\"MemoryType\" ASC")]

        // 3. Grouping by Name with MAX speed
        [InlineData(
            "\"r\".\"Name\", MAX(\"r\".\"MemorySpeed\") TYPE INT AS \"MemorySpeed\"",
            "\"r\".\"Rating\" >= 4.0",
            "\"r\".\"Name\"",
            "MAX(\"r\".\"MemorySpeed\") TYPE INT > 3200",
            "MAX(\"r\".\"MemorySpeed\") TYPE INT DESC",
            "SELECT \"r\".\"Name\", MAX(\"r\".\"MemorySpeed\") AS \"MemorySpeed\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"Rating\" >= @p1 GROUP BY \"r\".\"Name\" HAVING MAX(\"r\".\"MemorySpeed\") > @p2 ORDER BY MAX(\"r\".\"MemorySpeed\") DESC")]

        // 4. Filtering by Color with MIN price
        [InlineData(
            "\"r\".\"Color\", MIN(\"r\".\"Price\") TYPE DECIMAL AS \"Price\"",
            "\"r\".\"FirstWordLatency\" <= 10.0 AND \"r\".\"Color\" = 'Black'",
            "\"r\".\"Color\"",
            "MIN(\"r\".\"Price\") TYPE DECIMAL > 50",
            "MIN(\"r\".\"Price\") TYPE DECIMAL ASC",
            "SELECT \"r\".\"Color\", MIN(\"r\".\"Price\") AS \"Price\" FROM \"Rams\" AS \"r\" WHERE \"r\".\"FirstWordLatency\" <= @p1 AND \"r\".\"Color\" = @p2 GROUP BY \"r\".\"Color\" HAVING MIN(\"r\".\"Price\") > @p3 ORDER BY MIN(\"r\".\"Price\") ASC")]

        // 5. Parenthesized WHERE predicate testing with AVG
        [InlineData(
            "\"r\".\"MemoryType\", AVG(\"r\".\"PricePerGB\") TYPE DECIMAL AS \"PricePerGB\"",
            "NOT (\"r\".\"MemorySpeed\" = 2400) AND \"r\".\"ReviewCount\" > 10",
            "\"r\".\"MemoryType\"",
            "AVG(\"r\".\"PricePerGB\") TYPE DECIMAL <= 5.0",
            "AVG(\"r\".\"PricePerGB\") TYPE DECIMAL ASC",
            "SELECT \"r\".\"MemoryType\", AVG(\"r\".\"PricePerGB\") AS \"PricePerGB\" FROM \"Rams\" AS \"r\" WHERE NOT (\"r\".\"MemorySpeed\" = @p1) AND \"r\".\"ReviewCount\" > @p2 GROUP BY \"r\".\"MemoryType\" HAVING AVG(\"r\".\"PricePerGB\") <= @p3 ORDER BY AVG(\"r\".\"PricePerGB\") ASC")]
        public void Query_Builder_Must_Create_SQL_Full_Query_With_All_Clauses_Correctly(
            string select,
            string where,
            string groupBy,
            string having,
            string orderBy,
            string expected)
        {
            var query = QueryBuilder.Create<RamReadModel>()
                .From<RamReadModel>("r")
                .Select(select)
                .Where(where)
                .GroupBy(groupBy)
                .Having(having)
                .OrderBy(orderBy)
                .Build();

            Assert.Equal(CleanString(expected), CleanString(query.SQL));
        }
        #endregion

        #region Duplicate Check
        [Fact]
        public void QueryBuilder_Must_Create_New_Copy_On_Demand()
        {
            // Cache building
            var cache = QueryBuilder
                .Create<RamReadModel>()
                .From<RamBaseModel>("r")
                .Select("r.*")
                .ValidateBaseQuery();

            var copy1 = cache.Copy().Where("r.price > 10");
            var copy2 = cache.Copy().OrderBy("r.name");

            var result1 = copy1.Build();
            var result2 = copy2.Build();
            var original = cache.Build();

            Assert.Contains("WHERE", result1.SQL);
            Assert.DoesNotContain("ORDER BY", result1.SQL);

            Assert.Contains("ORDER BY", result2.SQL);
            Assert.DoesNotContain("WHERE", result2.SQL);

            Assert.DoesNotContain("WHERE", original.SQL);
            Assert.DoesNotContain("ORDER BY", original.SQL);
        }
        #endregion

        #region Regular test
        [Theory]
        [InlineData("\"r\".\"ReviewCount\"", "SELECT \"r\".\"ReviewCount\" FROM \"Rams\" AS \"r\"", "\"Rams\"", "\"r\"")]
        [InlineData("\"r\".\"Review\"\"Count\"", "SELECT \"r\".\"Review\"\"Count\" FROM \"Rams\" AS \"r\"", "\"Rams\"", "\"r\"")]
        [InlineData("\"r\".\"\"", "SELECT \"r\".\"\" FROM \"Rams\" AS \"r\"", "\"Rams\"", "\"r\"")]
        [InlineData("\"r\".\"\"\"\"", "SELECT \"r\".\"\"\"\" FROM \"Rams\" AS \"r\"", "\"Rams\"", "\"r\"")]
        [InlineData("\"r\".\"Review\"\"\"\"Count\"", "SELECT \"r\".\"Review\"\"\"\"Count\" FROM \"Rams\" AS \"r\"", "\"Rams\"", "\"r\"")]

        [InlineData("[r].[ReviewCount]", "SELECT [r].[ReviewCount] FROM [Rams] AS [r]", "[Rams]", "[r]")]
        [InlineData("[r].[Review]]Count]", "SELECT [r].[Review]]Count] FROM [Rams] AS [r]", "[Rams]", "[r]")]
        [InlineData("[r].[]", "SELECT [r].[] FROM [Rams] AS [r]", "[Rams]", "[r]")]
        [InlineData("[r].[[]]]", "SELECT [r].[[]]] FROM [Rams] AS [r]", "[Rams]", "[r]")]
        [InlineData("[r].[Review]]]]Count]", "SELECT [r].[Review]]]]Count] FROM [Rams] AS [r]", "[Rams]", "[r]")]
        public void Query_Builder_Should_Build_Valid_Query(
            string columns,
            string expectedQuery,
            string table,
            string tableAlias)
        {
            var fromIden = SQLBuilderHelper.BuildFrom(table);
            var aliasIden = SQLBuilderHelper.BuildAlias(tableAlias);
            var fromSource = new TableSource
            {
                TableName = new List<SQLIdentifier>
                {
                    new SQLIdentifier
                    {
                        Value = fromIden[0].Value,
                        Delimiter = fromIden[0].Delimiter
                    }
                },
                Alias =
                new SQLIdentifier
                {
                    Value = aliasIden.Value,
                    Delimiter = aliasIden.Delimiter
                },
                Type = typeof(RamBaseModel)
            };

            var select = SQLBuilderHelper.BuildSelect(columns);
            var sqlQuery = new SQLQuery
            {
                From = fromSource,
                Select = select
            };
            var context = new SQLGeneratorContext(new StringBuilder(), new DynamicParameters());
            SQLGenerator.BuildSelect(select, ref context);
            SQLGenerator.BuildFrom(fromSource, ref context);
            var result = SQLGenerator.Build(ref context);
            Assert.Equal(CleanString(expectedQuery), CleanString(result.SQL));
        }
        #endregion

        #region Fail
        [Fact]
        public void query_builder_should_fail_on_invalid_predicate_join()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                CreateQuery()
                    .From<RamBaseModel>("r")
                    .Select("*")
                    .Join<BrandBaseModel>("b", "r.name")
                    .Build()
            );
        }
        [Fact]
        public void query_builder_should_fail_on_invalid_predicate_where()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                CreateQuery()
                    .From<RamBaseModel>("r")
                    .Select("*")
                    .Where("r.name")
                    .Build()
            );
        }
        [Fact]
        public void query_builder_should_fail_on_invalid_predicate_having()
        {
            Assert.Throws<SyntaxErrorException>(() =>
                CreateQuery()
                    .From<RamBaseModel>("r")
                    .Select("*")
                    .Having("r.name")
                    .Build()
            );
        }
        #endregion
        private IQueryFrom CreateQuery() => QueryBuilder.Create<RamReadModel>();
        //private IQueryFrom CreateQuery(SQLValidationMode mode) => QueryBuilder<>.Create(SQLValidationMode.Strict);
        private string CleanString(string input) => Regex.Replace(input, @"\s+", " ");
    }
}