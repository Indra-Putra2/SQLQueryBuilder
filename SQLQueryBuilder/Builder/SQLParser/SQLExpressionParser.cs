using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;
using System.Globalization;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLExpressionParser
    {
        private SQLParserHelper _helper;
        public SQLExpression Parse(SQLParserContext context, bool validateEnd = false)
        {
            _helper = new SQLParserHelper(context);

            var expression = ParseOR();

            if (validateEnd && !_helper.Expect(_helper.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token after end of expression.",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return expression;
        }
        public SQLExpression Parse(SQLParserHelper helper, bool validateEnd = false)
        {
            _helper = helper;

            var expression = ParseOR();

            if (validateEnd && !_helper.Expect(_helper.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token after end of expression.",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return expression;
        }
        private SQLExpression ParseOR()
        {
            var left = ParseAND();

            while (!_helper.IsAtEnd && _helper.IsKeyword(_helper.Current, "OR"))
            {
                var op = SQLBinaryOperator.OR;
                _helper.Consume(); // Consume OR
                var right = ParseAND();

                left = new BinaryExpression
                {
                    Span = CreateSpan(left.Span.Start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
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

                left = new BinaryExpression
                {
                    Span = CreateSpan(left.Span.Start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }
            return left;
        }
        private SQLExpression ParseNOT()
        {
            if (_helper.IsKeyword(_helper.Current, "NOT") && !IsPredicateNot())
            {
                var start = _helper.Consume(); // NOT

                if (_helper.Expect(_helper.Current, SQLTokenType.NullLiteral))
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        _helper.Current.StartPosition,
                        "'NOT NULL' is not a valid expression. Did you mean 'IS NOT NULL'?"
                    );

                    throw new SyntaxErrorException(err);
                }

                var operand = ParsePredicate();

                return new UnaryExpression
                {
                    Span = CreateSpan(start.StartPosition, operand.Span),
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
                        ErrorFormatter.Create(
                            _helper.Input,
                            _helper.Current.StartPosition,
                            "Expected NULL after IS or IS NOT."));
                }

                var lastToken = _helper.Consume(); // NULL

                return new IsNullExpression
                {
                    Span = CreateSpan(left.Span.Start, lastToken.StartPosition, lastToken.Length),
                    Operand = left,
                    IsNot = isNot
                };
            }
            else if (IsPredicateNot())
            {
                _helper.Consume();
                isNot = true;
            }

            if (_helper.IsKeyword(_helper.Current, "LIKE"))
            {
                _helper.Consume(); // consume LIKE
                var pattern = ParseBitwiseXor();

                return new LikeExpression
                {
                    Span = CreateSpan(left.Span.Start, pattern.Span),
                    Operand = left,
                    IsNot = isNot,
                    Pattern = pattern
                };
            }
            else if (_helper.IsKeyword(_helper.Current, "IN"))
            {
                var start = _helper.Consume(); // consume IN

                List<SQLExpression> values = new();

                if (!_helper.Expect(_helper.Current, SQLTokenType.LeftParen))
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        _helper.Current.StartPosition, $"Expected '(' after IN");
                    throw new SyntaxErrorException(err);
                }

                _helper.Consume(); // consume (

                if (_helper.Expect(_helper.Current, SQLTokenType.RightParen))
                {
                    var err = ErrorFormatter.Create(
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
                        throw new SyntaxErrorException(
                            ErrorFormatter.Create(
                                _helper.Input,
                                _helper.Current.StartPosition,
                                "Expected expression after comma",
                                errorTextLength: _helper.Current.Length));
                    }

                } while (!_helper.Expect(_helper.Current, SQLTokenType.RightParen));

                if (!_helper.Expect(_helper.Current, SQLTokenType.RightParen))
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        _helper.Current.StartPosition, "Expected ')' after IN list");
                    throw new SyntaxErrorException(err);
                }

                var end = _helper.Consume(); // consume )

                return new InExpression
                {
                    Span = CreateSpan(left.Span.Start, end.StartPosition, end.Length),
                    Operand = left,
                    IsNot = isNot,
                    Values = values
                };
            }
            else if (_helper.IsKeyword(_helper.Current, "BETWEEN"))
            {
                _helper.Consume(); // consume BETWEEN
                var lower = ParseBitwiseOr();
                if (!_helper.IsKeyword(_helper.Current, "AND"))
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        _helper.Current.StartPosition, $"Expected 'AND' after lower value",
                        errorTextLength: _helper.Current.Length);
                    throw new SyntaxErrorException(err);
                }
                _helper.Consume(); //Consume AND 
                var upper = ParseBitwiseOr();

                return new BetweenExpression
                {
                    Span = CreateSpan(left.Span.Start, upper.Span),
                    Operand = left,
                    IsNot = isNot,
                    Lower = lower,
                    Upper = upper
                };
            }

            while (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLDictionary.Comparison))
            {
                SQLBinaryOperator op;
                if (!SQLDictionary.ComparisonOperators.TryGetValue(_helper.Current.Type, out op))
                {
                    var token = _helper.Current;
                    var err = ErrorFormatter.Create(_helper.Input, _helper.Current.StartPosition, $"Operator {_helper.GetSlice(token)} ({token.Type}) is not a valid comparison operator", errorTextLength: token.Length);
                    throw new SyntaxErrorException(err);
                }

                _helper.Consume();
                var right = ParseBitwiseOr();

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
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

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
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

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
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

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
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

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
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

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
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
                    _ => throw new SyntaxErrorException(ErrorFormatter.Create(_helper.Input, tokenType.StartPosition, $"Unexpected token at position {tokenType.StartPosition}", errorTextLength: tokenType.Length))
                };

                var right = ParseUnary();
                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private SQLExpression ParseUnary()
        {
            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Minus))
            {
                SQLToken opToken = _helper.Consume(); // Capture the operator token first
                SQLExpression operand = ParseUnary(); // Recurse to get the rest of the expression

                return new UnaryExpression()
                {
                    // Span starts at the operator '-' and ends wherever the operand ends
                    Span = CreateSpan(opToken.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.Negative,
                    Operand = operand
                };
            }

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.BitwiseNot))
            {
                SQLToken opToken = _helper.Consume();
                SQLExpression operand = ParseUnary();

                return new UnaryExpression()
                {
                    Span = CreateSpan(opToken.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.BitwiseNOT,
                    Operand = operand
                };
            }

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Plus))
            {
                SQLToken opToken = _helper.Consume();
                SQLExpression operand = ParseUnary();

                return new UnaryExpression()
                {
                    Span = CreateSpan(opToken.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.Positive,
                    Operand = operand
                };
            }

            return ParsePrimary();
        }

        private SQLExpression ParsePrimary()
        {
            // 1. Wildcard
            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Asterisk))
            {
                var token = _helper.Consume();
                return new WildcardExpression { Span = CreateSpan(token) };
            }

            // 2. Literals (123, 'abc', NULL, etc.)
            else if (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLDictionary.Literals))
                return ParseLiteral();

            // 3. Identifiers or Function Calls (Users.Age, COUNT(id), etc.)
            else if (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLDictionary.Identifiers))
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

            var err = ErrorFormatter.Create(
                _helper.Input,
                _helper.Current.StartPosition,
                $"Expected expression at position {_helper.Current.StartPosition}",
                errorTextLength: _helper.Current.Length
            );
            throw new SyntaxErrorException(err);
        }

        private SQLExpression ParseCASE()
        {
            var start = _helper.Consume(); // Consume CASE
            var conditions = new List<CaseWhen>();
            SQLExpression baseExpression = null;

            if (!_helper.IsAtEnd && !_helper.IsKeyword(_helper.Current, "WHEN"))
            {
                baseExpression = ParseOR();
            }

            if (!_helper.IsAtEnd && !_helper.IsKeyword(_helper.Current, "WHEN"))
            {
                var err = ErrorFormatter.Create(_helper.Input, _helper.Current.StartPosition, $"Expected 'WHEN' after CASE", errorTextLength: _helper.Current.Length);
                throw new SyntaxErrorException(err);
            }

            while (!_helper.IsAtEnd && _helper.IsKeyword(_helper.Current, "WHEN"))
            {
                _helper.Consume(); // consume WHEN

                var condition = ParseOR();

                if (!_helper.IsAtEnd && !_helper.IsKeyword(_helper.Current, "THEN"))
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        _helper.Current.StartPosition,
                        "Expected 'THEN' after WHEN",
                        errorTextLength: _helper.Current.Length);

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
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    "Expected 'END' after CASE expression",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            var end = _helper.Consume(); // consume END

            return new CaseExpression
            {
                Span = CreateSpan(start.StartPosition, end.StartPosition, end.Length),
                BaseExpression = baseExpression,
                Conditions = conditions,
                ElseExpression = elseExpression
            };
        }

        private SQLExpression ParseParenthesized()
        {
            var startToken = _helper.Current;

            _helper.Consume(); // (

            var expr = ParseOR();

            if (_helper.IsAtEnd || !_helper.Expect(_helper.Current, SQLTokenType.RightParen))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected ')' at position {_helper.Current.StartPosition}",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            var endToken = _helper.Current;
            _helper.Consume(); // )

            return new ParenthesizedExpression
            {
                Span = CreateSpan(startToken.StartPosition, endToken.StartPosition, endToken.Length),
                Expression = expr
            };
        }

        private SQLExpression ParseIdentifierOrFunction()
        {
            var start = _helper.Consume();
            var name = _helper.GetSlice(start);
            SQLToken lastToken = start;

            // Delimiter check '[' / '"'
            var delimiter = SQLParserHelper.GetIdentifierDelimiter(start);

            // Function check '('
            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.LeftParen))
            {
                return ParseFunction(name, start);
            }

            // Check for qualified column db.column etc
            else if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Dot))
            {
                // get the first column
                var stringName = SQLParserHelper.UnescapeIdentifier(name);
                var parts = new List<SQLIdentifier>
                {
                    new SQLIdentifier
                    {
                        Value = stringName,
                        Delimiter = delimiter
                    }
                };


                // check for more qualified column
                while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Dot))
                {
                    _helper.Consume(); // consume '.'

                    // Check for wildcard like 'table.*'
                    if (_helper.Expect(_helper.Current, SQLTokenType.Asterisk))
                    {
                        lastToken = _helper.Consume(); // consume '*'
                        return new WildcardExpression
                        {
                            Span = CreateSpan(start.StartPosition, lastToken.StartPosition, lastToken.Length),
                            TableAlias = parts
                        };
                    }

                    if (!_helper.Expects(_helper.Current, SQLDictionary.Identifiers) && !_helper.Expect(_helper.Current, SQLTokenType.Keyword))
                        throw new SyntaxErrorException("Expected identifier after '.'");

                    lastToken = _helper.Consume();

                    // Delimiter check
                    delimiter = SQLParserHelper.GetIdentifierDelimiter(lastToken);
                    name = _helper.GetSlice(lastToken);
                    parts.Add(
                        new SQLIdentifier
                        {
                            Value = SQLParserHelper.UnescapeIdentifier(name),
                            Delimiter = delimiter
                        });
                }

                // Get the last item from qualified column example server.dbo.ram -> get ram
                var qualifiedColumn = parts[^1]; // Last item
                parts.RemoveAt(parts.Count - 1); // remove last item

                return new ColumnExpression
                {
                    Span = CreateSpan(start.StartPosition, lastToken.StartPosition, lastToken.Length),
                    Table = parts.Count > 0 ? parts : null,
                    Column = qualifiedColumn
                };
            }

            var column = new SQLIdentifier
            {
                Value = SQLParserHelper.UnescapeIdentifier(name),
                Delimiter = delimiter
            };

            // 3. Simple Unqualified Identifier: age, name, status
            return new ColumnExpression()
            {
                Span = CreateSpan(start),
                Column = column
            };

        }

        private SQLExpression ParseFunction(ReadOnlySpan<char> name, SQLToken startToken)
        {
            _helper.Consume(); // consume (

            var args = new List<SQLExpression>();
            var declareType = SQLFunctionType.Unknown;
            var nullable = false;
            var constant = false;

            while (!_helper.IsAtEnd && !_helper.Expect(_helper.Current, SQLTokenType.RightParen))
            {
                args.Add(ParseOR());

                if (!_helper.Expect(_helper.Current, SQLTokenType.Comma))
                    break;

                _helper.Consume();
            }

            if (!_helper.IsAtEnd && !_helper.Expect(_helper.Current, SQLTokenType.RightParen))
            {
                var err = ErrorFormatter.Create(_helper.Input, _helper.Current.StartPosition, $"Expected ')' at position {_helper.Current.StartPosition}", errorTextLength: _helper.Current.Length);
                throw new SyntaxErrorException(err);
            }

            SQLToken lastToken = _helper.Consume(); // consume ')'

            bool typeIsNull = _helper.Expect(_helper.Current, SQLTokenType.NullLiteral);
            if (_helper.IsKeyword(_helper.Current, "TYPE") || _helper.IsKeyword(_helper.Current, "CONST") || _helper.Expect(_helper.Current, SQLTokenType.SQLType) || typeIsNull)
            {
                // TYPE?
                if (_helper.IsKeyword(_helper.Current, "TYPE"))
                {
                    _helper.Consume();
                }

                // CONST?
                if (_helper.IsKeyword(_helper.Current, "CONST"))
                {
                    _helper.Consume();
                    constant = true;
                }

                // SQLType
                var typeToken = _helper.Current;
                typeIsNull = typeIsNull || _helper.Expect(_helper.Current, SQLTokenType.NullLiteral);
                if (!_helper.Expect(typeToken, SQLTokenType.SQLType) && !typeIsNull)
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        typeToken.StartPosition,
                        $"Expected SQL type, got '{_helper.GetSlice(typeToken)}'",
                        errorTextLength: typeToken.Length);

                    throw new SyntaxErrorException(err);
                }

                if (typeIsNull)
                {
                    nullable = true;
                }

                if (!SQLDictionary.TryGetSQLType(
                        _helper.GetSlice(typeToken),
                        out declareType))
                {
                    var err = ErrorFormatter.Create(
                        _helper.Input,
                        typeToken.StartPosition,
                        $"Unknown function type '{_helper.GetSlice(typeToken)}'",
                        errorTextLength: typeToken.Length);

                    throw new SyntaxErrorException(err);
                }

                _helper.Consume();
                lastToken = typeToken;

                // NULLABLE?
                var nullableToken = _helper.Current;

                if (_helper.Expect(nullableToken, SQLTokenType.SQLTypeNullable))
                {
                    if (typeIsNull)
                    {
                        var err = ErrorFormatter.Create(
                            _helper.Input,
                            nullableToken.StartPosition,
                            "NULL type cannot be nullable.",
                            errorTextLength: nullableToken.Length);

                        throw new SyntaxErrorException(err);
                    }
                    _helper.Consume();
                    nullable = true;
                    lastToken = nullableToken;
                }
            }

            int endPosition = lastToken.StartPosition + lastToken.Length;

            return new FunctionExpression()
            {
                Span = new SQLSourceSpan
                {
                    Start = startToken.StartPosition,
                    Length = endPosition - startToken.StartPosition
                },
                Name = SQLParserHelper.UnescapeIdentifier(name),
                Arguments = args,
                DeclaredType = declareType,
                IsNullable = nullable,
                IsConstant = constant
            };
        }

        private SQLExpression ParseLiteral()
        {
            var token = _helper.Consume();
            var val = _helper.GetSlice(token);
            return token.Type switch
            {
                SQLTokenType.StringLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = SQLParserHelper.UnescapeStringLiteral(val),
                    Type = SQLLiteralType.String
                },

                SQLTokenType.UnicodeString => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = SQLParserHelper.UnescapeStringLiteral(val),
                    Type = SQLLiteralType.UnicodeString
                },

                SQLTokenType.DateLiteral => ParseDateTime(token),
                SQLTokenType.TimeLiteral => ParseDateTime(token),
                SQLTokenType.DatetimeLiteral => ParseDateTime(token),
                SQLTokenType.IntegerLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = int.Parse(val, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Integer
                },

                SQLTokenType.DecimalLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = decimal.Parse(val, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Decimal
                },

                SQLTokenType.ScientificLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = double.Parse(val, NumberStyles.Float, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Scientific
                },

                SQLTokenType.MoneyLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = decimal.Parse(val[1..], NumberStyles.Currency, CultureInfo.InvariantCulture), //strip $
                    Type = SQLLiteralType.Money
                },

                SQLTokenType.HexLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = Convert.FromHexString(val[2..]), // strip 0x
                    Type = SQLLiteralType.BinaryLiteral
                },

                SQLTokenType.NullLiteral => new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = null,
                    Type = SQLLiteralType.Null
                },

                _ => throw new SyntaxErrorException(ErrorFormatter.Create(_helper.Input, token.StartPosition, $"Expected Literal but got '{val}'", errorTextLength: token.Length))
            };
        }

        LiteralExpression ParseDateTime(SQLToken token)
        {
            var val = _helper.GetSlice(token);
            string err;

            if (token.Type == SQLTokenType.DateLiteral)
            {
                val = val[2..^1]; // remove d'...'

                if (!DateOnly.TryParseExact(val, SQLDictionary.DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    err = ErrorFormatter.Create(
                        _helper.Input,
                        token.StartPosition,
                        $"Invalid datetime format '{_helper.GetSlice(token)}'. Use format 'yyyy-MM-dd'",
                        errorTextLength: token.Length);

                    throw new SyntaxErrorException(err);
                }

                return new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = date,
                    Type = SQLLiteralType.Date
                };
            }
            else if (token.Type == SQLTokenType.TimeLiteral)
            {
                val = val[2..^1]; // remove t'...'

                if (!TimeOnly.TryParseExact(val, SQLDictionary.TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                {
                    err = ErrorFormatter.Create(
                        _helper.Input,
                        token.StartPosition,
                        $"Invalid time format '{_helper.GetSlice(token)}'. Use format 'HH:mm:ss.FFFFFFF'",
                        errorTextLength: token.Length);

                    throw new SyntaxErrorException(err);
                }

                return new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = time,
                    Type = SQLLiteralType.Time
                };
            }
            else if (token.Type == SQLTokenType.DatetimeLiteral)
            {
                val = val[3..^1]; // remove dt'...'

                if (!DateTime.TryParseExact(val, SQLDictionary.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                {
                    err = ErrorFormatter.Create(
                        _helper.Input,
                        token.StartPosition,
                        $"Invalid dateTime format '{_helper.GetSlice(token)}'. Use format 'yyyy-MM-dd HH:mm:ss.FFFFFFF'",
                        errorTextLength: token.Length);

                    throw new SyntaxErrorException(err);
                }

                return new LiteralExpression
                {
                    Span = CreateSpan(token),
                    Value = dateTime,
                    Type = SQLLiteralType.Datetime
                };
            }

            err = ErrorFormatter.Create(
                _helper.Input,
                token.StartPosition,
                $"Unexpected token type '{token.Type}'. Expected a date, time, or datetime literal.",
                errorTextLength: token.Length);

            throw new SyntaxErrorException(err);
        }
        private bool IsPredicateNot()
        {
            if (!_helper.IsKeyword(_helper.Current, "NOT"))
                return false;

            return _helper.IsKeyword(_helper.Peek(), "IN")
                || _helper.IsKeyword(_helper.Peek(), "LIKE")
                || _helper.IsKeyword(_helper.Peek(), "BETWEEN");
        }

        private SQLSourceSpan CreateSpan(SQLToken token)
        {
            return new SQLSourceSpan { Start = token.StartPosition, Length = token.Length };
        }
        private SQLSourceSpan CreateSpan(int startPosition, SQLSourceSpan endSpan)
        {
            int endPosition = endSpan.Start + endSpan.Length;
            return new SQLSourceSpan
            {
                Start = startPosition,
                Length = endPosition - startPosition
            };
        }
        private SQLSourceSpan CreateSpan(int startPosition, int endStartPosition, int endLength)
        {
            int endPosition = endStartPosition + endLength;
            return new SQLSourceSpan
            {
                Start = startPosition,
                Length = endPosition - startPosition
            };
        }
    }
}
