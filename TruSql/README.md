# TruSql

TruSql is a SQL query builder for dynamically constructing and validating queries at runtime.

## Features

- Dynamic SQL query construction
- Runtime expression parsing and semantic validation
- Parameterized SQL generation
- Support for filtering, column selection, joins, and supported SQL expressions
- Optional database integration

## Installation

```bash
dotnet add package TruSql
```

## Quick Start

See the example below for a basic query-building workflow.

```csharp
QueryBuilder
	.Create<RamReadModel>()
	.From<RamBaseModel>("r")
	.Select("r.name, r.price")
	.Join<BrandBaseModel>("b", "b.id = r.Brandid")
	.Where("r.name = 'corsair'")
	.GroupBy("r.name")
	.Having("r.price < 100")
	.OrderBy("r.price")
	.Limit(15)
	.Build();
```


## How It Works

TruSql processes expressions through several stages:

1. **Tokenization** — converts input into tokens.
2. **Parsing** — constructs an abstract syntax tree (AST).
3. **Semantic Analysis** — validates expressions and resolves column references.
4. **SQL Generation** — generates SQL and associated parameters.

## Supported SQL Features

Document the supported operators, expressions, functions, joins, and literal types here.

Operator supported by this library is as follow:
| Operators      | Type       | Status      |
|----------------|------------|-------------|
| +,-,*,/,%      | Arithmetic | Supported   |
| =              | Assignment | In progress |
| ++,--,+,-,NOT  | Unary      | Supported   |
| =,!=,<,>,<=,>= | Comparison | Supported   |
| AND,OR,NOT     | Logical    | Supported   |
| <<,>>,~,&,\|,^ | Bitwise    | Supported   |

## Limitations

- Subquery
- Aggregate functions are not validated for clause-specific SQL restrictions. The query builder treats functions as generic expressions and leaves database-specific validation to the target database.
- Column aliases defined in SELECT cannot be referenced inside WHERE or GROUP BY.
- Row/tuple expressions are not currently supported. Parenthesized expressions must contain a single expression; constructs such as (a, b) are not yet implemented.
- Dates must strictly follow ISO format with prefixes: d'YYYY-MM-DD' or t'HH:mm:ss' or dt'YYYY-MM-DD HH:mm:ss'.
- Functions are parsed by their structure rather than their database-specific semantics. Return types require an explicit TYPE hint.
- Aggregate return types require explicit hints (e.g., TYPE DECIMAL, CONST DECIMAL?).
- String values must use single quotes ('text'). Double quotes ("text") are reserved for identifiers.
- Qualified column names require table aliases (e.g., ram.Price instead of bare Price).

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.