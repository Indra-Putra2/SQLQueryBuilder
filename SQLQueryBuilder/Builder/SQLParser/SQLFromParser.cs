using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.SQLEnum;
using System.Data;

internal class SQLFromParser
{
    private SQLParserHelper _helper;

    public List<string> Parse(string input, List<SQLToken> tokens)
    {
        _helper = new SQLParserHelper(input, tokens);

        var parts = new List<string>();

        // First identifier
        if (!_helper.Expects(_helper.Current, SQLParserHelper.Identifiers))
        {
            var err = _helper.CreateErrorMessage(
                _helper.Input,
                _helper.Current.StartPosition,
                $"Expected Table Name",
                errorTextLength: _helper.Current.RawLength);
            throw new SyntaxErrorException(err);
        }

        parts.Add(_helper.Consume().Value);


        // dbo.Table style
        while (_helper.Expect(_helper.Current, SQLTokenType.Dot))
        {
            _helper.Consume(); // consume .

            if (!_helper.Expects(_helper.Current, SQLParserHelper.Identifiers))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Identifier after '.'",
                    errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }

            parts.Add(_helper.Consume().Value);

            if (parts.Count > 4)
            {
                throw new SyntaxErrorException(
                    $"Invalid table name '{_helper.Input}'. Maximum format is server.database.schema.table.");
            }
        }

        if (!_helper.Expect(_helper.Current, SQLTokenType.EOF))
        {
            var err = _helper.CreateErrorMessage(
                _helper.Input,
                _helper.Current.StartPosition,
                $"Unexpected token '{_helper.Current.Value}' after table name. Aliases must be provided separately.",
                errorTextLength: _helper.Current.RawLength);
            throw new SyntaxErrorException(err);
        }

        return parts;
    }
}