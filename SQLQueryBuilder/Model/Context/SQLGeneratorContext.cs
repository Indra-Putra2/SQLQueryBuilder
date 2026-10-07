using Dapper;
using System.Text;

namespace SQLQueryBuilder.Model.Context
{
    internal record SQLGeneratorContext
    {
        private SQLGeneratorBase? _base;

        public int TemporaryIndex { get; private set; } = 1;
        public int Indent { get; private set; } = 0; 
        public bool Parameterize { get; private set; } = false;
        public bool BaseValidated => _base != null;
        public StringBuilder TemporaryString;
        public DynamicParameters TemporaryParameters;

        public SQLGeneratorContext(
            StringBuilder stringBuilder,
            DynamicParameters parameters,
            int parameterIndex = 1,
            int indent = 0,
            SQLGeneratorBase? generatorBase = null)
        {
            TemporaryIndex = parameterIndex;
            Indent = indent;
            TemporaryString = stringBuilder;
            TemporaryParameters = parameters;
            _base = generatorBase;
        }

        public int GetNextIndex() => TemporaryIndex++;
        public void IncreaseIndent() => Indent++;
        public void DecreaseIndent() => Indent = Math.Max(0, Indent - 1);
        public void SetParameterize(bool val) => Parameterize = val;
        public void SetBase()
        {
            if (_base != null)
                throw new InvalidOperationException(
                    "Base has already been set.");

            _base = new SQLGeneratorBase(
                TemporaryString.ToString(),
                CloneParameters(TemporaryParameters),
                TemporaryIndex);
        }

        public void RefreshToBase()
        {
            if (_base == null)
                throw new InvalidOperationException(
                    "Base has not been set.");

            TemporaryIndex = _base.ParameterIndex;

            TemporaryString.Clear().Append(_base.SQL);
            TemporaryParameters = CloneParameters(_base.Parameters);
        }

        private static DynamicParameters CloneParameters(DynamicParameters source)
        {
            var result = new DynamicParameters();
            result.AddDynamicParams(source);
            return result;
        }
    }
}
