using Dapper;
using SQLQueryBuilder.Attributes;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLQueryBuilder.SQLBuilder
{
    public class SQLBuilder
    {
        private static Type[] scalarTypes = new Type[]
        {
                typeof(string),
                typeof(int),
                typeof(decimal),
                typeof(double),
                typeof(float),
                typeof(bool),
                typeof(DateTime)
        };
        private Dictionary<Type, Dictionary<string, string>> _columnMaps = new();
        private Dictionary<Type, string> _primaryKeys = new();
        private Dictionary<Type, Dictionary<Type, string>> _foreignKeys = new();
        private int _index = 0;
        private OldSQLInfo? _sqlInfo;

        public void BuildColumnSet<T>()
        {
            var type = typeof(T);
            var props = type.GetProperties();
            var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

            if (keyProp == null)
                throw new InvalidOperationException($"Missing '[Key]' Attribute for Type {type.Name}");

            _primaryKeys[type] = keyProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? keyProp.Name;

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<ForeignKeyAttribute>();
                if (attr != null)
                {
                    string fkColumnName = attr.Name;
                    var relatedType = prop.PropertyType;

                    if (!_foreignKeys.ContainsKey(type))
                        _foreignKeys[type] = new Dictionary<Type, string>();

                    _foreignKeys[type][relatedType] = fkColumnName;
                }
            }
            var map = type.GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<IgnoreAttribute>() == null &&
                    p.GetCustomAttribute<NotMappedAttribute>() == null &&
                    scalarTypes.Contains(p.PropertyType)
                ).ToDictionary(
                p => p.Name,
                p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name,
                StringComparer.OrdinalIgnoreCase
                );

            _columnMaps[type] = map;
        }
        public SQLBuilder Select<T>(string? alias = null)
        {
            Reset();
            var type = EnsureColumnSet<T>();
            var columns = _columnMaps[type].Values.ToList();
            _sqlInfo = new OldSQLInfo()
            {
                Type = type,
                TableName = GetTableName(type),
                Alias = alias,
                Columns = columns
            };
            return this;
        }
        public SQLBuilder Select<T>(Expression<Func<T, object>> expression, string? alias = null)
        {
            Reset();
            var type = EnsureColumnSet<T>();
            List<(string source, string alias)> columns = GetSelectedColumns(expression);
            var finalColumns = MapAlias<T>(columns);

            _sqlInfo = new OldSQLInfo()
            {
                Type = type,
                TableName = GetTableName(type),
                Alias = alias,
                Columns = finalColumns
            };
            return this;
        }
        public SQLBuilder Where(Expr expr)
        {
            _index = 0;
            _sqlInfo.DynamicParameters = new DynamicParameters();
            _sqlInfo.Where = $"\nWHERE {BuildWhere(expr)}";
            return this;
        }
        public SQLBuilder Join<TJoin>(Expression<Func<TJoin, object>> expression, string? alias = null,JoinType jType = JoinType.INNER)
        {
            List<(string source, string alias)> columns = GetSelectedColumns(expression);
            var finalColumns = MapAlias<TJoin>(columns);

            var joinType = typeof(TJoin);
            var join = new OldJoinInfo()
            {
                Type = joinType,
                TableName = GetTableName(joinType),
                Alias = alias,
                JoinType = jType,
            };
            var primaryTable = _sqlInfo.Alias ?? _sqlInfo.TableName; 
            var primaryTableFK = _foreignKeys[_sqlInfo.Type][joinType];
            var joinTablePK = _primaryKeys[joinType];

            var finalJoinTable = alias ?? join.TableName;
            var on = alias == null ?
                $"ON {primaryTable}.{primaryTableFK} = {finalJoinTable}.{joinTablePK}":
                $"ON {primaryTable}.{primaryTableFK} = {finalJoinTable}.{joinTablePK}";

            join.OnCondition = on;

            var col = finalColumns.Count() > 0 ? finalColumns.ToHashSet() : _columnMaps[joinType].Values.ToHashSet();

            join.Columns.UnionWith(col);

            if(_sqlInfo.Joins == null) 
            { 
                _sqlInfo.Joins = new List<OldJoinInfo>();
            }

            var existing = _sqlInfo.Joins.FirstOrDefault(j =>
                j.Type == join.Type &&
                j.Alias == join.Alias
            );

            if (existing != null)
            {
                existing.Columns.UnionWith(join.Columns);
            }
            else
            {
                _sqlInfo.Joins.Add(join);
            }

            return this;
        }
        private string BuildWhere(Expr expr)
        {
            if (expr is BinaryExpr bin)
            {
                var left = BuildWhere(bin.Left);
                var right = BuildWhere(bin.Right);

                return $"({left} {NormalizeOperator(bin.Op)} {right})";
            }

            if (expr is ComparisonExpr comp)
            {
                var map = _columnMaps[_sqlInfo.Type];

                if (comp.Identifier == null)
                    throw new InvalidDataException("Identifier cannot be null");

                if (!map.ContainsKey(comp.Identifier))
                    throw new InvalidDataException($"Column {comp.Identifier} doesn't exist!");

                map.TryGetValue(comp.Identifier, out var columnName);

                _index++;
                _sqlInfo.DynamicParameters.Add($"p{_index}", FormatValue(comp.Op, comp.Value));
                Console.WriteLine($"@p{_index} val: {FormatValue(comp.Op, comp.Value)}");
                var primaryTable = _sqlInfo.Alias ?? _sqlInfo.TableName;
                return $"{primaryTable}.{columnName} {NormalizeOperator(comp.Op)} @p{_index}";
            }

            throw new Exception("Unknown expression");
        }
        public (string sql, DynamicParameters param) Build()
        {
            var select = "";
            var joinclause = "";
            var primaryTableName = _sqlInfo.TableName;
            var NameOrAlias = _sqlInfo.Alias ?? primaryTableName;

            var primaryCols = _sqlInfo.Columns.Select(c => $"{NameOrAlias}.{c}");

            if (_sqlInfo == null)
                throw new InvalidOperationException("Select() must be called before Build()");

            IEnumerable<string> allCols = primaryCols;

            if (_sqlInfo.Joins != null)
            {
                var joinCols = _sqlInfo.Joins.SelectMany(j => 
                { 
                    var name = j.Alias ?? j.TableName; 
                    return j.Columns.Select(c => $"{name}.{c}"); 
                }); 
                allCols = allCols.Concat(joinCols);

                joinclause = string.Join("",
                    _sqlInfo.Joins.Select(j =>
                    {
                        var table = j.TableName;
                        var aliasPart = string.IsNullOrEmpty(j.Alias) ? "" : $" AS {j.Alias}";
                        return $"\n{j.JoinType.ToSql()} {table}{aliasPart} {j.OnCondition}";
                    })
                );
            }

            select = $"SELECT\n{string.Join(",\n", allCols)}";
            var PrimaryAlias = _sqlInfo.Alias != null ? $"{primaryTableName} AS {_sqlInfo.Alias}" : primaryTableName;
            var finalSQL = $"{select}\nFROM {PrimaryAlias}{joinclause}{_sqlInfo.Where}";

            return (finalSQL, _sqlInfo.DynamicParameters);
        }
        public string BuildSql()
        {
            var (sql, _) = Build();
            return sql;
        }
        private string NormalizeOperator(string op)
        {
            return op switch
            {
                ":" => "LIKE",
                _ => op
            };
        }
        private object FormatValue(string op, object value)
        {
            if (op == ":")
            {
                return $"%{value}%";
            }

            return value;
        }
        private void Reset()
        {
            _sqlInfo = null;
            _index = 0;
        }
        private Type EnsureColumnSet<T>()
        {
            var type = typeof(T);

            if (!_columnMaps.ContainsKey(type))
            {
                BuildColumnSet<T>();
            }
            return type;
        }
        private List<string> MapAlias<T>(List<(string source, string alias)> columns)
        {
            var type = EnsureColumnSet<T>();
            var map = _columnMaps[type];
            var results = columns.Select(c =>
            {
                if (!map.TryGetValue(c.source, out var mapped))
                    throw new InvalidDataException($"Column {c.source} doesn't exist!");

                return string.IsNullOrEmpty(c.alias)
                        ? $"{mapped}"
                        : $"{mapped} AS {c.alias}";
            }).ToList();
            return results;
        }
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;
        private List<(string Source, string Alias)> GetSelectedColumns<T>(Expression<Func<T, object>> expr)
        {
            var result = new List<(string Source, string Alias)>();

            void Visit(Expression expression)
            {
                switch (expression)
                {
                    case MemberExpression member:
                        result.Add((Source: member.Member.Name, Alias: string.Empty));
                        break;

                    case NewExpression @new:
                        for (int i = 0; i < @new.Arguments.Count; i++)
                        {
                            var arg = @new.Arguments[i];
                            var alias = @new.Members?[i].Name ?? string.Empty;

                            if (arg is MemberExpression memberArg)
                            {
                                var finalAlias = alias == memberArg.Member.Name ? string.Empty : alias;
                                result.Add((Source: memberArg.Member.Name, Alias: finalAlias));
                            }
                        }
                        break;

                    case UnaryExpression unary:
                        Visit(unary.Operand);
                        break;

                    case MemberInitExpression init:
                        foreach (var binding in init.Bindings)
                        {
                            if (binding is MemberAssignment assign)
                            {
                                var alias = assign.Member.Name; // BrandName

                                if (assign.Expression is MemberExpression memberArg)
                                {
                                    var source = memberArg.Member.Name;

                                    var finalAlias = alias == source ? string.Empty : alias;

                                    result.Add((Source: source, Alias: finalAlias));
                                }
                                else
                                {
                                    throw new NotSupportedException($"Assignment '{assign}' not supported");
                                }
                            }
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Expression '{expression}' not supported");
                }
            }

            Visit(expr.Body);
            return result;
        }
    }
}
