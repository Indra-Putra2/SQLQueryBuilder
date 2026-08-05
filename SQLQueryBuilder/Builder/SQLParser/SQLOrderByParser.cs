using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLOrderByParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new();
        public List<OrderByExpression> Parse(string input, List<SQLToken> tokens)
        {
            _helper = new SQLParserHelper(input, tokens);
            var orderBy = new List<OrderByExpression>();

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Expression but got '{_helper.Current.Value}'",
                    errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }
            var expr = _expressionParser.Parse(_helper);
            var desc = false;

            if (_helper.IsKeyword(_helper.Current, "DESC"))
            {
                _helper.Consume();
                desc = true;
            }
            else if (_helper.IsKeyword(_helper.Current, "ASC"))
            {
                _helper.Consume();
            }

            orderBy.Add(new OrderByExpression { Expression = expr, Descending = desc });


            while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                _helper.Consume(); // Consume ','

                expr = _expressionParser.Parse(_helper);
                desc = false;

                if (_helper.IsKeyword(_helper.Current, "DESC"))
                {
                    _helper.Consume();
                    desc = true;
                }
                else if (_helper.IsKeyword(_helper.Current, "ASC"))
                {
                    _helper.Consume();
                }
            }

            if (!_helper.Expect(_helper.Current, SQLTokenType.EOF))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token '{_helper.Current.Value}' after end of expression.",
                    errorTextLength: _helper.Current.RawLength);

                throw new SyntaxErrorException(err);
            }

            return orderBy;
        }
    }
}
