using System.Linq.Expressions;

namespace SQLQueryBuilder.Model
{
    public class Query
    {
        public List<Type> TypeStack { get; } = new();
        public List<OldJoinInfo> Joins { get; } = new();

        public Query From<T>()
        {
            this.TypeStack.Add(typeof(T));
            return this;
        }
        public Query Join<TNext>(
            Expression<Func<object, TNext, bool>> on)
        {
            Joins.Add(new OldJoinInfo
            {
                RightTable = typeof(TNext),
                Expression = on
            });

            return this;
        }
    }
}
