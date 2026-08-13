using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;
using System.Globalization;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLExpressionParser
    {
        private SQLParserHelper _helper;

        public SQLExpression Parse(SQLParserHelper helper)
        {
            _helper = helper;
            if (_helper.Tokens.Count == 1 && _helper.Tokens[0].Type == SQLTokenType.EOF) return null;

            return ParseOR();
        }
        private SQLExpression ParseOR()
        {
            var left = ParseAND();

            while (!_helper.IsAtEnd && _helper.IsKeyword(_helper.Current, "OR"))
            {
                var op = SQLBinaryOperator.OR;
                _helper.Consume(); // Consume OR
                var right = ParseAND();

                left = new BinaryExpression() { Left = left, Operator = op, Right = right };
            }
            return left;
        }

        private SQLExpression ParseAND()
        {
            var left = ParseNOT();

            while (!_helper.IsAtEnd && _helper.IsKeyword(_helper.Current, "AND"))
            {
                var op = SQLBinaryOperator.AND;
                _helper.Consume(); // Consume AND
                var right = ParseNOT();

                left = new BinaryExpression() { Left = left, Operator = op, Right = right };
            }
            return left;
        }
        private SQLExpression ParseNOT()
        {
            if (_helper.IsKeyword(_helper.Current, "NOT") && !IsPredicateNot())
            {
                _helper.Consume(); // NOT

                if (_helper.Expect(_helper.Current, SQLTokenType.NullLiteral))
                {
                    var err = _helper.CreateErrorMessage(
                        _helper.Input,
                        _helper.Current.StartPosition,
                        "'NOT NULL' is not a valid expression. Did you mean 'IS NOT NULL'?"
                    );

                    throw new SyntaxErrorException(err);
                }

                var operand = ParsePredicate();

                return new UnaryExpression
                {
                    Operator = SQLUnaryOperator.NOT,
                    Operand = operand
                };
            }

            return ParsePredicate();
        }

        private SQLExpression ParsePredicate()
        {
            var left = ParseBitwiseOr();
            var isNot = false;

            // Check for IS [NOT] NULL
            if (_helper.IsKeyword(_helper.Current, "IS"))
            {
                _helper.Consume(); // IS

                isNot = false;

                if (_helper.IsKeyword(_helper.Current, "NOT"))
                {
                    isNot = true;
                    _helper.Consume();
                }

                if (!_helper.Expect(_helper.Current, SQLTokenType.NullLiteral))
                {
                    throw new SyntaxErrorException(
                        _helper.CreateErrorMessage(
                            _helper.Input,
                            _helper.Current.StartPosition,
                            "Expected NULL after IS or IS NOT."));
                }

                _helper.Consume(); // NULL

                return new IsNullExpression
                {
                    Operand = left,
                    IsNot = isNot
                };
            }
            else if(IsPredicateNot())
            {
                _helper.Consume();
                isNot = true;                
            }

            if (_helper.IsKeyword(_helper.Current, "LIKE"))
            {
                _helper.Consume(); // consume LIKE

                return new LikeExpression
                {
                    Operand = left,
                    IsNot = isNot,
                    Pattern = ParseBitwiseXor()
                };
            }
            else if (_helper.IsKeyword(_helper.Current, "IN"))
            {
                _helper.Consume(); // consume IN

                List<SQLExpression> values = new();

                if(!_helper.Expect(_helper.Current, SQLTokenType.LeftParen))
                {
                    var err = _helper.CreateErrorMessage(
                        _helper.Input, 
                        _helper.Current.StartPosition, $"Expected '(' after IN");
                    throw new SyntaxErrorException(err);
                }

                _helper.Consume(); // consume (

                if (_helper.Expect(_helper.Current, SQLTokenType.RightParen))
                {
                    var err = _helper.CreateErrorMessage(
                        _helper.Input,
                        _helper.Current.StartPosition,
                        "IN requires at least one value");

                    throw new SyntaxErrorException(err);
                }

                do
                {
                    values.Add(ParseBitwiseOr());

                    if (!_helper.Expect(_helper.Current, SQLTokenType.Comma))
                        break;

                    _helper.Consume();

                    if (_helper.Expect(_helper.Current, SQLTokenType.RightParen))
                    {
                        throw new SyntaxErrorException("Expected expression after comma");
                    }

                } while (!_helper.Expect(_helper.Current, SQLTokenType.RightParen));

                if (!_helper.Expect(_helper.Current, SQLTokenType.RightParen))
                {
                    var err = _helper.CreateErrorMessage(
                        _helper.Input, 
                        _helper.Current.StartPosition, "Expected ')' after IN list");
                    throw new SyntaxErrorException(err);
                }

                _helper.Consume(); // consume )

                return new InExpression
                {
                    Operand = left,
                    IsNot = isNot,
                    Values = values
                };
            }
            else if (_helper.IsKeyword(_helper.Current, "BETWEEN"))
            {
                _helper.Consume(); // consume BETWEEN
                var lower = ParseBitwiseOr();
                if(!_helper.IsKeyword(_helper.Current, "AND"))
                {
                    var err = _helper.CreateErrorMessage(
                        _helper.Input,
                        _helper.Current.StartPosition, $"Expected 'AND' after lower value",
                        errorTextLength: _helper.Current.RawLength);
                    throw new SyntaxErrorException(err);
                }
                _helper.Consume(); //Consume AND 
                var upper = ParseBitwiseOr();

                return new BetweenExpression
                {
                    Operand = left,
                    IsNot = isNot,
                    Lower = lower,
                    Upper = upper
                };
            }

            while (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLParserHelper.Comparison))
            {
                SQLBinaryOperator op;
                if (!SQLParserHelper.ComparisonOperators.TryGetValue(_helper.Current.Type, out op))
                {
                    var err = _helper.CreateErrorMessage(_helper.Input, _helper.Current.StartPosition, $"Operator {_helper.Current.Value} ({_helper.Current.Type}) is not a valid comparison operator", errorTextLength: _helper.Current.RawLength);
                    throw new SyntaxErrorException(err);
                }

                _helper.Consume();
                var right = ParseBitwiseOr();

                left = new BinaryExpression() { Left = left, Operator = op, Right = right };
            }

            return left;
        }
        private SQLExpression ParseBitwiseOr()
        {
            var left = ParseBitwiseXor();

            while (!_helper.IsAtEnd &&
                (_helper.Expect(_helper.Current, SQLTokenType.BitwiseOr)))
            {
                var op = SQLBinaryOperator.BitwiseOr;
                _helper.Consume(); // Consume BitwiseOr
                var right = ParseBitwiseXor();

                left = new BinaryExpression()
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private SQLExpression ParseBitwiseXor()
        {
            var left = ParseBitwiseAnd();

            while (!_helper.IsAtEnd &&
                (_helper.Expect(_helper.Current, SQLTokenType.BitwiseXor)))
            {
                var op = SQLBinaryOperator.BitwiseXor;
                _helper.Consume(); // Consume BitwiseXor
                var right = ParseBitwiseAnd();

                left = new BinaryExpression()
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private SQLExpression ParseBitwiseAnd()
        {
            var left = ParseBitwiseShift();

            while (!_helper.IsAtEnd &&
                (_helper.Expect(_helper.Current, SQLTokenType.BitwiseAnd)))
            {
                var op = SQLBinaryOperator.BitwiseAnd;
                _helper.Consume(); // Consume BitwiseAnd
                var right = ParseBitwiseShift();

                left = new BinaryExpression() 
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private SQLExpression ParseBitwiseShift()
        {
            var left = ParseAddition();

            while (!_helper.IsAtEnd &&
                (_helper.Expect(_helper.Current, SQLTokenType.BitwiseLeftShift) ||
                 _helper.Expect(_helper.Current, SQLTokenType.BitwiseRightShift)))
            {
                var token = _helper.Consume();

                var op = token.Type switch
                {
                    SQLTokenType.BitwiseLeftShift => SQLBinaryOperator.BitwiseLeftShift,
                    SQLTokenType.BitwiseRightShift => SQLBinaryOperator.BitwiseRightShift,
                    _ => throw new InvalidOperationException()
                };

                var right = ParseAddition();

                left = new BinaryExpression()
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }

        private SQLExpression ParseAddition()
        {
            var left = ParseMultiplication();

            while (!_helper.IsAtEnd &&
                (_helper.Expect(_helper.Current, SQLTokenType.Plus) ||
                _helper.Expect(_helper.Current, SQLTokenType.Minus)))
            {
                var op = _helper.Consume().Type == SQLTokenType.Plus ? SQLBinaryOperator.Add : SQLBinaryOperator.Subtract; // Consume  +/-
                var right = ParseMultiplication();

                left = new BinaryExpression()
                {
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }

        private SQLExpression ParseMultiplication()
        {
            var left = ParseUnary();

            while (!_helper.IsAtEnd && (
                _helper.Expect(_helper.Current, SQLTokenType.Asterisk) ||
                _helper.Expect(_helper.Current, SQLTokenType.Divide) ||
                _helper.Expect(_helper.Current, SQLTokenType.Modulo)))
            {
                var tokenType = _helper.Consume();
                var op = tokenType.Type switch
                {
                    SQLTokenType.Asterisk => SQLBinaryOperator.Multiply,
                    SQLTokenType.Divide => SQLBinaryOperator.Divide,
                    SQLTokenType.Modulo => SQLBinaryOperator.Modulo,
                    _ => throw new SyntaxErrorException(_helper.CreateErrorMessage(_helper.Input, tokenType.StartPosition, $"Unexpected token at position {tokenType.StartPosition}", errorTextLength: tokenType.RawLength))
                };

                var right = ParseUnary();
                left = new BinaryExpression { Left = left, Operator = op, Right = right };
            }

            return left;
        }
        private SQLExpression ParseUnary()
        {
            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Minus))
            {
                _helper.Consume();

                return new UnaryExpression()
                {
                    Operator = SQLUnaryOperator.Negative,
                    Operand = ParseUnary()
                };
            }

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.BitwiseNot))
            {
                _helper.Consume();

                return new UnaryExpression()
                {
                    Operator = SQLUnaryOperator.BitwiseNOT,
                    Operand = ParseUnary()
                };
            }

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Plus))
            {
                _helper.Consume();

                return new UnaryExpression
                {
                    Operator = SQLUnaryOperator.Positive,
                    Operand = ParseUnary()
                };
            }
            return ParsePrimary();
        }

        private SQLExpression ParsePrimary()
        {
            // 1. Wildcard
            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Asterisk))
            {
                _helper.Consume();
                return new WildcardExpression();
            }

            // 2. Literals (123, 'abc', NULL, etc.)
            else if (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLParserHelper.Literals))
                return ParseLiteral();

            // 3. Identifiers or Function Calls (Users.Age, COUNT(id), etc.)
            else if (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLParserHelper.Identifiers))
                return ParseIdentifierOrFunction();

            // 4. // Parenthesized expression: (a + b)
            else if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.LeftParen))
                return ParseParenthesized();

            // 5. CASE ... WHEN ... THEN ... END
            else if (!_helper.IsAtEnd && _helper.IsKeyword(_helper.Current, "CASE"))
                return ParseCASE();

            // 6. Variable @new, @@new
            else if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Variable))
                throw new NotSupportedException("Variables are not supported yet.");
                //return new ParameterExpression { Name = _helper.Consume().Value };

            var err = _helper.CreateErrorMessage(
                _helper.Input,
                _helper.Current.StartPosition,
                $"Expected expression at position {_helper.Current.StartPosition}",
                errorTextLength: _helper.Current.RawLength
            );
            throw new SyntaxErrorException(err);
        }

        private SQLExpression ParseCASE()
        {
            _helper.Consume(); // Consume CASE
            var conditions = new List<CaseWhen>();
            SQLExpression baseExpression = null;

            if (!_helper.IsAtEnd && !_helper.IsKeyword(_helper.Current, "WHEN"))
            {
                baseExpression = ParseOR();
            }

            if (!_helper.IsAtEnd && !_helper.IsKeyword(_helper.Current, "WHEN"))
            {
                var err = _helper.CreateErrorMessage(_helper.Input, _helper.Current.StartPosition, $"Expected 'WHEN' after CASE", errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }

            while (!_helper.IsAtEnd && _helper.IsKeyword(_helper.Current, "WHEN"))
            {
                _helper.Consume(); // consume WHEN

                var condition = ParseOR();

                if (!_helper.IsAtEnd && !_helper.IsKeyword(_helper.Current, "THEN"))
                {
                    var err = _helper.CreateErrorMessage(
                        _helper.Input,
                        _helper.Current.StartPosition,
                        "Expected 'THEN' after WHEN",
                        errorTextLength: _helper.Current.RawLength);

                    throw new SyntaxErrorException(err);
                }

                _helper.Consume(); // consume THEN

                var result = ParseOR();

                conditions.Add(new CaseWhen
                {
                    Condition = condition,
                    Result = result
                });
            }

            SQLExpression? elseExpression = null;

            if (_helper.IsKeyword(_helper.Current, "ELSE"))
            {
                _helper.Consume(); // consume ELSE

                elseExpression = ParseOR();
            }

            if (!_helper.IsKeyword(_helper.Current, "END"))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    "Expected 'END' after CASE expression",
                    errorTextLength: _helper.Current.RawLength);

                throw new SyntaxErrorException(err);
            }

            _helper.Consume(); // consume END

            return new CaseExpression
            {
                BaseExpression = baseExpression,
                Conditions = conditions,
                ElseExpression = elseExpression
            };
        }

        private SQLExpression ParseParenthesized()
        {
            _helper.Consume(); // consume (

            var expr = ParseOR(); // parse everything inside

            if (!_helper.IsAtEnd && !_helper.Expect(_helper.Current, SQLTokenType.RightParen))
            {
                var err = _helper.CreateErrorMessage(_helper.Input, _helper.Current.StartPosition, $"Expected ')' at position {_helper.Current.StartPosition}", errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }

            _helper.Consume(); // consume )

            return expr;
        }

        private SQLExpression ParseIdentifierOrFunction()
        {
            var name = _helper.Consume().Value;

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.LeftParen))
            {
                return ParseFunction(name);
            }
            else if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Dot))
            {
                var parts = new List<string> { name };

                while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Dot))
                {
                    _helper.Consume(); // consume '.'

                    // Check for wildcard like 'table.*'
                    if (_helper.Expect(_helper.Current, SQLTokenType.Asterisk))
                    {
                        _helper.Consume(); // consume '*'
                        return new WildcardExpression { TableAlias = parts };
                    }

                    if (!_helper.Expects(_helper.Current, SQLParserHelper.Identifiers) && !_helper.Expect(_helper.Current, SQLTokenType.Keyword))
                        throw new SyntaxErrorException("Expected identifier after '.'");

                    parts.Add(_helper.Consume().Value);
                }

                var column = parts[^1]; // Last item

                parts.RemoveAt(parts.Count - 1);

                return new ColumnExpression
                {
                    Table = parts.Count > 0
                        ? parts
                        : null,

                    Column = column
                };
            }

            // 3. Simple Unqualified Identifier: age, name, status
            return new ColumnExpression()
            {
                Column = name
            };

        }

        private SQLExpression ParseFunction(string name)
        {
            _helper.Consume(); // consume (

            var args = new List<SQLExpression>();
            var funcType = SQLFunctionType.Unknown;

            while (!_helper.IsAtEnd && !_helper.Expect(_helper.Current, SQLTokenType.RightParen))
            {
                args.Add(ParseOR());

                if (!_helper.Expect(_helper.Current, SQLTokenType.Comma))
                    break;

                _helper.Consume();
            }

            if (!_helper.IsAtEnd && !_helper.Expect(_helper.Current, SQLTokenType.RightParen))
            {
                var err = _helper.CreateErrorMessage(_helper.Input, _helper.Current.StartPosition, $"Expected ')' at position {_helper.Current.StartPosition}", errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }

            _helper.Consume(); // consume )

            if (_helper.IsKeyword(_helper.Current, "TYPE"))
            {
                _helper.Consume(); // Consume keyword Type
                if (!SQLParserHelper.SQLTypes.TryGetValue(_helper.Consume().Value, out funcType))
                {
                    var err = _helper.CreateErrorMessage(_helper.Input, _helper.Current.StartPosition, $"Expected function types after keyword 'TYPE' {_helper.Current.StartPosition}", errorTextLength: _helper.Current.RawLength);
                    throw new SyntaxErrorException(err);
                }
            }

            return new FunctionExpression()
            {
                Name = name,
                Arguments = args,
                FunctionType = funcType
            };
        }

        private SQLExpression ParseLiteral()
        {
            var token = _helper.Consume();

            return token.Type switch
            {
                SQLTokenType.StringLiteral => new LiteralExpression
                {
                    Value = token.Value,
                    Type = SQLLiteralType.String
                },

                SQLTokenType.UnicodeString => new LiteralExpression
                {
                    Value = token.Value,
                    Type = SQLLiteralType.UnicodeString
                },

                SQLTokenType.DatetimeLiteral => ParseDateTime(token),

                SQLTokenType.IntegerLiteral => new LiteralExpression
                {
                    Value = int.Parse(token.Value),
                    Type = SQLLiteralType.Integer
                },

                SQLTokenType.DecimalLiteral => new LiteralExpression
                {
                    Value = decimal.Parse(token.Value),
                    Type = SQLLiteralType.Decimal
                },

                SQLTokenType.ScientificLiteral => new LiteralExpression
                {
                    Value = double.Parse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Scientific
                },

                // Lexer already stripped '$'
                SQLTokenType.MoneyLiteral => new LiteralExpression
                {
                    Value = decimal.Parse(token.Value, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Money
                },

                // Lexer already stripped '0x'
                SQLTokenType.HexLiteral => new LiteralExpression
                {
                    Value = Convert.FromHexString(token.Value),
                    Type = SQLLiteralType.BinaryLiteral
                },

                SQLTokenType.NullLiteral => new LiteralExpression
                {
                    Value = null,
                    Type = SQLLiteralType.Null
                },

                _ => throw new SyntaxErrorException(_helper.CreateErrorMessage(_helper.Input, token.StartPosition, $"Expected Literal but got '{token.Value}'", errorTextLength: token.RawLength))
            };
        }

        LiteralExpression ParseDateTime(SQLToken token)
        {
            if (!DateTime.TryParse(
                    token.Value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var value))
            {
                throw new SyntaxErrorException(
                    _helper.CreateErrorMessage(
                        _helper.Input,
                        token.StartPosition,
                        $"Invalid DateTime literal '{token.Value}'",
                        errorTextLength: token.RawLength));
            }

            return new LiteralExpression
            {
                Value = token.Value,
                Type = SQLLiteralType.Datetime
            };
        }

        private bool IsPredicateNot()
        {
            if (!_helper.IsKeyword(_helper.Current, "NOT"))
                return false;

            return _helper.IsKeyword(_helper.Peek(), "IN")
                || _helper.IsKeyword(_helper.Peek(), "LIKE")
                || _helper.IsKeyword(_helper.Peek(), "BETWEEN");
        }
    }
}
