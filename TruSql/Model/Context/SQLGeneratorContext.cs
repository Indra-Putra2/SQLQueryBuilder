using System.Text;
using TruSql.Core.QueryBuilder;

namespace TruSql.Model.Context
{
    internal record SQLGeneratorContext
    {
        private SQLGeneratorBase? _base;

        public int TemporaryIndex { get; private set; } = 1;
        public int Indent { get; private set; } = 0;
        public bool Parameterize { get; private set; } = false;
        public bool BaseValidated => _base != null;
        public StringBuilder TempParametizedSQL;
        public StringBuilder TempRawSQL;
        public Dictionary<string, QueryParameter> TempParameters;

        public SQLGeneratorContext(
            StringBuilder parametized,
            StringBuilder raw,
            int parameterIndex = 1,
            int indent = 0,
            SQLGeneratorBase? generatorBase = null)
        {
            TemporaryIndex = parameterIndex;
            Indent = indent;
            TempParametizedSQL = parametized;
            TempRawSQL = raw;
            TempParameters = new();
            _base = generatorBase;
        }

        public int GetNextIndex() => (_base?.Parameters.Count ?? 0) + TempParameters.Count + 1;
        public void IncreaseIndent() => Indent++;
        public void DecreaseIndent() => Indent = Math.Max(0, Indent - 1);
        public void SetParameterize(bool val) => Parameterize = val;
        public SQLGeneratorBase? GetBase => _base;
        public void SetBase()
        {
            if (_base != null)
                throw new InvalidOperationException(
                    "Base has already been set.");

            _base = new SQLGeneratorBase(
                TempParametizedSQL.ToString(),
                TempRawSQL.ToString(),
                TempParameters);
        }

        public void RefreshToBase()
        {
            if (_base == null)
                throw new InvalidOperationException(
                    "Base has not been set.");

            TemporaryIndex = _base.Parameters.Count;

            TempParametizedSQL.Clear().Append(_base.ParametizedSQL);
            TempRawSQL.Clear().Append(_base.RawSQL);
            TempParameters.Clear();
        }
    }
}
