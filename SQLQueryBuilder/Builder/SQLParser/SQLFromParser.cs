using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.SQLEnum;
using System.Data;

internal class SQLFromParser
{
    private SQLParserHelper _helper;

    public List<SQLIdentifier> Parse(SQLParserContext context)
    {
        _helper = new SQLParserHelper(context);

        var parts = new List<SQLIdentifier>();

        // First identifier
        if (!_helper.Expects(_helper.Current, SQLDictionary.Identifiers))
        {
            var err = ErrorFormatter.Create(
                _helper.Input,
                _helper.Current.StartPosition,
                $"Expected Table Name",
                errorTextLength: _helper.Current.Length);
            throw new SyntaxErrorException(err);
        }
        var token = _helper.Consume();
        var name = _helper.GetSlice(token);
        var stringName = SQLParserHelper.UnescapeIdentifier(name);
        var delimiter = SQLParserHelper.GetIdentifierDelimiter(token);
        parts.Add(new SQLIdentifier { Value = stringName, Delimiter = delimiter});


        // dbo.Table style
        while (_helper.Expect(_helper.Current, SQLTokenType.Dot))
        {
            _helper.Consume(); // consume .

            if (!_helper.Expects(_helper.Current, SQLDictionary.Identifiers))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Identifier after '.'",
                    errorTextLength: _helper.Current.Length);
                throw new SyntaxErrorException(err);
            }

            token = _helper.Consume();
            name = _helper.GetSlice(token);
            stringName = SQLParserHelper.UnescapeIdentifier(name);
            delimiter = SQLParserHelper.GetIdentifierDelimiter(token);
            parts.Add(new SQLIdentifier { Value = stringName, Delimiter = delimiter });

            if (parts.Count > 4)
            {
                throw new SyntaxErrorException(
                    $"Invalid table name '{_helper.Input}'. Maximum format is server.database.schema.table.");
            }
        }

        if (!_helper.Expect(_helper.Current, SQLTokenType.EOF))
        {
            var err = ErrorFormatter.Create(
                _helper.Input,
                _helper.Current.StartPosition,
                $"Unexpected token '{_helper.GetSlice(_helper.Consume())}' after table name. Aliases must be provided separately.",
                errorTextLength: _helper.Current.Length);
            throw new SyntaxErrorException(err);
        }

        return parts;
    }
}