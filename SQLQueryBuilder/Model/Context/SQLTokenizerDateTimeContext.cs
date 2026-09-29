namespace SQLQueryBuilder.Model.Context
{
    internal ref struct SQLTokenizerDateTimeContext
    {
        public int Start;
        public int ExpectedDigit;
        public int Year = -1;
        public int Month = -1;
        public int Day = -1;
        public int Hour = -1;
        public int Minute = -1;
        public decimal Second = -1;
        public string Name;
        public string Format;
        public SQLTokenizerDateTimeContext(int start, int expectedDigit, string name, string format)
        {
            Start = start;
            ExpectedDigit = expectedDigit;
            Name = name;
            Format = format;
        }
    }
}
