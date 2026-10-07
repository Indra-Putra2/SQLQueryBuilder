using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using System.Data;
using System.Globalization;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLExpressionParser
    {
        public static SQLExpression Parse(ref SQLParserContext context, bool validateEnd = false)
        {
            var expression = ParseOR(ref context);

            if (validateEnd && !SQLParserHelper.Expect(context.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"Unexpected token after end of expression.",
                    errorTextLength: context.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return expression;
        }
        private static SQLExpression ParseOR(ref SQLParserContext context)
        {
            var left = ParseAND(ref context);

            while (!context.IsAtEnd && SQLParserHelper.IsCurrentKeyword(context, "OR"))
            {
                var op = SQLBinaryOperator.OR;
                context.Consume(); // Consume OR
                var right = ParseAND(ref context);

                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(left.Span.Start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }
            return left;
        }
        private static SQLExpression ParseAND(ref SQLParserContext context)
        {
            var left = ParseNOT(ref context);

            while (!context.IsAtEnd && SQLParserHelper.IsCurrentKeyword(context, "AND"))
            {
                var op = SQLBinaryOperator.AND;
                context.Consume(); // Consume AND
                var right = ParseNOT(ref context);

                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(left.Span.Start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }
            return left;
        }
        private static SQLExpression ParseNOT(ref SQLParserContext context)
        {
            if (SQLParserHelper.IsCurrentKeyword(context, "NOT") && !IsPredicateNot(ref context))
            {
                var start = context.Consume(); // NOT

                if (SQLParserHelper.Expect(context.Current, SQLTokenType.NullLiteral))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition,
                        "'NOT NULL' is not a valid expression. Did you mean 'IS NOT NULL'?"
                    );

                    throw new SyntaxErrorException(err);
                }

                var operand = ParsePredicate(ref context);

                return new UnaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.NOT,
                    Operand = operand
                };
            }

            return ParsePredicate(ref context);
        }
        private static SQLExpression ParsePredicate(ref SQLParserContext context)
        {
            var left = ParseBitwiseOr(ref context);
            var isNot = false;

            // Check for IS [NOT] NULL
            if (SQLParserHelper.IsCurrentKeyword(context, "IS"))
            {
                context.Consume(); // IS

                isNot = false;

                if (SQLParserHelper.IsCurrentKeyword(context, "NOT"))
                {
                    isNot = true;
                    context.Consume();
                }

                if (!SQLParserHelper.Expect(context.Current, SQLTokenType.NullLiteral))
                {
                    throw new SyntaxErrorException(
                        ErrorFormatter.Create(
                            context.Span.ToString(),
                            context.Current.StartPosition,
                            "Expected NULL after IS or IS NOT."));
                }

                var lastToken = context.Consume(); // NULL

                return new IsNullExpression
                {
                    Span = SQLParserHelper.CreateSpan(left.Span.Start, lastToken.StartPosition, lastToken.Length),
                    Operand = left,
                    IsNot = isNot
                };
            }
            else if (IsPredicateNot(ref context))
            {
                context.Consume();
                isNot = true;
            }

            if (SQLParserHelper.IsCurrentKeyword(context, "LIKE"))
            {
                context.Consume(); // consume LIKE
                var pattern = ParseBitwiseXor(ref context);

                return new LikeExpression
                {
                    Span = SQLParserHelper.CreateSpan(left.Span.Start, pattern.Span),
                    Operand = left,
                    IsNot = isNot,
                    Pattern = pattern
                };
            }
            else if (SQLParserHelper.IsCurrentKeyword(context, "IN"))
            {
                var start = context.Consume(); // consume IN

                List<SQLExpression> values = new();

                if (!SQLParserHelper.Expect(context.Current, SQLTokenType.LeftParen))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition, $"Expected '(' after IN");
                    throw new SyntaxErrorException(err);
                }

                context.Consume(); // consume (

                if (SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition,
                        "IN requires at least one value");

                    throw new SyntaxErrorException(err);
                }

                do
                {
                    values.Add(ParseBitwiseOr(ref context));

                    if (!SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
                        break;

                    context.Consume();

                    if (SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen))
                    {
                        throw new SyntaxErrorException(
                            ErrorFormatter.Create(
                                context.Span.ToString(),
                                context.Current.StartPosition,
                                "Expected expression after comma",
                                errorTextLength: context.Current.Length));
                    }

                } while (!SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen));

                if (!SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition, "Expected ')' after IN list");
                    throw new SyntaxErrorException(err);
                }

                var end = context.Consume(); // consume )

                return new InExpression
                {
                    Span = SQLParserHelper.CreateSpan(left.Span.Start, end.StartPosition, end.Length),
                    Operand = left,
                    IsNot = isNot,
                    Values = values
                };
            }
            else if (SQLParserHelper.IsCurrentKeyword(context, "BETWEEN"))
            {
                context.Consume(); // consume BETWEEN
                var lower = ParseBitwiseOr(ref context);
                if (!SQLParserHelper.IsCurrentKeyword(context, "AND"))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition, $"Expected 'AND' after lower value",
                        errorTextLength: context.Current.Length);
                    throw new SyntaxErrorException(err);
                }
                context.Consume(); //Consume AND 
                var upper = ParseBitwiseOr(ref context);

                return new BetweenExpression
                {
                    Span = SQLParserHelper.CreateSpan(left.Span.Start, upper.Span),
                    Operand = left,
                    IsNot = isNot,
                    Lower = lower,
                    Upper = upper
                };
            }

            while (!context.IsAtEnd && SQLParserHelper.Expects(context.Current, SQLDictionary.Comparison))
            {
                SQLBinaryOperator op;
                if (!SQLDictionary.ComparisonOperators.TryGetValue(context.Current.Type, out op))
                {
                    var token = context.Current;
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition,
                        $"Operator {context.GetSlice(token)} ({token.Type}) is not a valid comparison operator",
                        errorTextLength: token.Length);
                    throw new SyntaxErrorException(err);
                }

                context.Consume();
                var right = ParseBitwiseOr(ref context);

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseBitwiseOr(ref SQLParserContext context)
        {
            var left = ParseBitwiseXor(ref context);

            while (!context.IsAtEnd &&
                (SQLParserHelper.Expect(context.Current, SQLTokenType.BitwiseOr)))
            {
                var op = SQLBinaryOperator.BitwiseOr;
                context.Consume(); // Consume BitwiseOr
                var right = ParseBitwiseXor(ref context);

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseBitwiseXor(ref SQLParserContext context)
        {
            var left = ParseBitwiseAnd(ref context);

            while (!context.IsAtEnd &&
                (SQLParserHelper.Expect(context.Current, SQLTokenType.BitwiseXor)))
            {
                var op = SQLBinaryOperator.BitwiseXor;
                context.Consume(); // Consume BitwiseXor
                var right = ParseBitwiseAnd(ref context);

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseBitwiseAnd(ref SQLParserContext context)
        {
            var left = ParseBitwiseShift(ref context);

            while (!context.IsAtEnd &&
                (SQLParserHelper.Expect(context.Current, SQLTokenType.BitwiseAnd)))
            {
                var op = SQLBinaryOperator.BitwiseAnd;
                context.Consume(); // Consume BitwiseAnd
                var right = ParseBitwiseShift(ref context);

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseBitwiseShift(ref SQLParserContext context)
        {
            var left = ParseAddition(ref context);

            while (!context.IsAtEnd &&
                (SQLParserHelper.Expect(context.Current, SQLTokenType.BitwiseLeftShift) ||
                 SQLParserHelper.Expect(context.Current, SQLTokenType.BitwiseRightShift)))
            {
                var token = context.Consume();

                var op = token.Type switch
                {
                    SQLTokenType.BitwiseLeftShift => SQLBinaryOperator.BitwiseLeftShift,
                    SQLTokenType.BitwiseRightShift => SQLBinaryOperator.BitwiseRightShift,
                    _ => throw new SyntaxErrorException(
                        ErrorFormatter.Create(
                            context.Span.ToString(),
                            context.Current.StartPosition,
                            $"Invalid Bitwise Operator {context.GetSlice(token)}"))
                };

                var right = ParseAddition(ref context);

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseAddition(ref SQLParserContext context)
        {
            var left = ParseMultiplication(ref context);

            while (!context.IsAtEnd &&
                (SQLParserHelper.Expect(context.Current, SQLTokenType.Plus) ||
                SQLParserHelper.Expect(context.Current, SQLTokenType.Minus)))
            {
                var op = context.Consume().Type == SQLTokenType.Plus ? SQLBinaryOperator.Add : SQLBinaryOperator.Subtract; // Consume  +/-
                var right = ParseMultiplication(ref context);

                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseMultiplication(ref SQLParserContext context)
        {
            var left = ParseUnary(ref context);

            while (!context.IsAtEnd && (
                SQLParserHelper.Expect(context.Current, SQLTokenType.Asterisk) ||
                SQLParserHelper.Expect(context.Current, SQLTokenType.Divide) ||
                SQLParserHelper.Expect(context.Current, SQLTokenType.Modulo)))
            {
                var tokenType = context.Consume();
                var op = tokenType.Type switch
                {
                    SQLTokenType.Asterisk => SQLBinaryOperator.Multiply,
                    SQLTokenType.Divide => SQLBinaryOperator.Divide,
                    SQLTokenType.Modulo => SQLBinaryOperator.Modulo,
                    _ => throw new SyntaxErrorException(ErrorFormatter.Create(context.Span.ToString(), tokenType.StartPosition, $"Unexpected token at position {tokenType.StartPosition}", errorTextLength: tokenType.Length))
                };

                var right = ParseUnary(ref context);
                var start = left.Span.Start;
                left = new BinaryExpression
                {
                    Span = SQLParserHelper.CreateSpan(start, right.Span),
                    Left = left,
                    Operator = op,
                    Right = right
                };
            }

            return left;
        }
        private static SQLExpression ParseUnary(ref SQLParserContext context)
        {
            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Minus))
            {
                SQLToken opToken = context.Consume(); // Capture the operator token first
                SQLExpression operand = ParseUnary(ref context); // Recurse to get the rest of the expression

                return new UnaryExpression()
                {
                    // Span starts at the operator '-' and ends wherever the operand ends
                    Span = SQLParserHelper.CreateSpan(opToken.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.Negative,
                    Operand = operand
                };
            }

            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.BitwiseNot))
            {
                SQLToken opToken = context.Consume();
                SQLExpression operand = ParseUnary(ref context);

                return new UnaryExpression()
                {
                    Span = SQLParserHelper.CreateSpan(opToken.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.BitwiseNOT,
                    Operand = operand
                };
            }

            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Plus))
            {
                SQLToken opToken = context.Consume();
                SQLExpression operand = ParseUnary(ref context);

                return new UnaryExpression()
                {
                    Span = SQLParserHelper.CreateSpan(opToken.StartPosition, operand.Span),
                    Operator = SQLUnaryOperator.Positive,
                    Operand = operand
                };
            }

            return ParsePrimary(ref context);
        }
        private static SQLExpression ParsePrimary(ref SQLParserContext context)
        {
            // 1. Wildcard
            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Asterisk))
            {
                var token = context.Consume();
                return new WildcardExpression { Span = SQLParserHelper.CreateSpan(token) };
            }

            // 2. Literals (123, 'abc', NULL, etc.)
            else if (!context.IsAtEnd && SQLParserHelper.Expects(context.Current, SQLDictionary.Literals))
                return ParseLiteral(ref context);

            // 3. Identifiers or Function Calls (Users.Age, COUNT(id), etc.)
            else if (!context.IsAtEnd && SQLParserHelper.Expects(context.Current, SQLDictionary.Identifiers))
                return ParseIdentifierOrFunction(ref context);

            // 4. // Parenthesized expression: (a + b)
            else if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.LeftParen))
                return ParseParenthesized(ref context);

            // 5. CASE ... WHEN ... THEN ... END
            else if (!context.IsAtEnd && SQLParserHelper.IsCurrentKeyword(context, "CASE"))
                return ParseCASE(ref context);

            // 6. Variable @new, @@new
            else if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Variable))
                throw new NotSupportedException("Variables are not supported yet.");
            //return new ParameterExpression { Name = context.Consume().Value };

            var err = ErrorFormatter.Create(
                context.Span.ToString(),
                context.Current.StartPosition,
                $"Expected expression at position {context.Current.StartPosition}",
                errorTextLength: context.Current.Length
            );
            throw new SyntaxErrorException(err);
        }
        private static SQLExpression ParseCASE(ref SQLParserContext context)
        {
            var start = context.Consume(); // Consume CASE
            var conditions = new List<CaseWhen>();
            SQLExpression baseExpression = null;

            if (!context.IsAtEnd && !SQLParserHelper.IsCurrentKeyword(context, "WHEN"))
            {
                baseExpression = ParseOR(ref context);
            }

            if (!context.IsAtEnd && !SQLParserHelper.IsCurrentKeyword(context, "WHEN"))
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), context.Current.StartPosition, $"Expected 'WHEN' after CASE", errorTextLength: context.Current.Length);
                throw new SyntaxErrorException(err);
            }

            while (!context.IsAtEnd && SQLParserHelper.IsCurrentKeyword(context, "WHEN"))
            {
                context.Consume(); // consume WHEN

                var condition = ParseOR(ref context);

                if (!context.IsAtEnd && !SQLParserHelper.IsCurrentKeyword(context, "THEN"))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Current.StartPosition,
                        "Expected 'THEN' after WHEN",
                        errorTextLength: context.Current.Length);

                    throw new SyntaxErrorException(err);
                }

                context.Consume(); // consume THEN

                var result = ParseOR(ref context);

                conditions.Add(new CaseWhen
                {
                    Condition = condition,
                    Result = result
                });
            }

            SQLExpression? elseExpression = null;

            if (SQLParserHelper.IsCurrentKeyword(context, "ELSE"))
            {
                context.Consume(); // consume ELSE

                elseExpression = ParseOR(ref context);
            }

            if (!SQLParserHelper.IsCurrentKeyword(context, "END"))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    "Expected 'END' after CASE expression",
                    errorTextLength: context.Current.Length);

                throw new SyntaxErrorException(err);
            }

            var end = context.Consume(); // consume END

            return new CaseExpression
            {
                Span = SQLParserHelper.CreateSpan(start.StartPosition, end.StartPosition, end.Length),
                BaseExpression = baseExpression,
                Conditions = conditions,
                ElseExpression = elseExpression
            };
        }
        private static SQLExpression ParseParenthesized(ref SQLParserContext context)
        {
            var startToken = context.Current;

            context.Consume(); // (

            var expr = ParseOR(ref context);

            if (context.IsAtEnd || !SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"Expected ')' at position {context.Current.StartPosition}",
                    errorTextLength: context.Current.Length);

                throw new SyntaxErrorException(err);
            }

            var endToken = context.Current;
            context.Consume(); // )

            return new ParenthesizedExpression
            {
                Span = SQLParserHelper.CreateSpan(startToken.StartPosition, endToken.StartPosition, endToken.Length),
                Expression = expr
            };
        }
        private static SQLExpression ParseIdentifierOrFunction(ref SQLParserContext context)
        {
            var start = context.Consume();
            var name = context.GetSlice(start);
            SQLToken lastToken = start;

            // Delimiter check '[' / '"'
            var delimiter = SQLParserHelper.GetIdentifierDelimiter(start);

            // Function check '('
            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.LeftParen))
            {
                return ParseFunction(name, start, ref context);
            }

            // Check for qualified column db.column etc
            else if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Dot))
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
                while (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Dot))
                {
                    context.Consume(); // consume '.'

                    // Check for wildcard like 'table.*'
                    if (SQLParserHelper.Expect(context.Current, SQLTokenType.Asterisk))
                    {
                        lastToken = context.Consume(); // consume '*'
                        return new WildcardExpression
                        {
                            Span = SQLParserHelper.CreateSpan(start.StartPosition, lastToken.StartPosition, lastToken.Length),
                            TableAlias = parts
                        };
                    }

                    if (!SQLParserHelper.Expects(context.Current, SQLDictionary.Identifiers) && !SQLParserHelper.Expect(context.Current, SQLTokenType.Keyword))
                        throw new SyntaxErrorException("Expected identifier after '.'");

                    lastToken = context.Consume();

                    // Delimiter check
                    delimiter = SQLParserHelper.GetIdentifierDelimiter(lastToken);
                    name = context.GetSlice(lastToken);
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
                    Span = SQLParserHelper.CreateSpan(start.StartPosition, lastToken.StartPosition, lastToken.Length),
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
                Span = SQLParserHelper.CreateSpan(start),
                Column = column
            };
        }
        private static SQLExpression ParseFunction(ReadOnlySpan<char> name, SQLToken startToken, ref SQLParserContext context)
        {
            context.Consume(); // consume (

            var args = new List<SQLExpression>();
            var declareType = SQLFunctionType.Unknown;
            var nullable = false;
            var constant = false;

            while (!context.IsAtEnd && !SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen))
            {
                args.Add(ParseOR(ref context));

                if (!SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
                    break;

                context.Consume();
            }

            if (!context.IsAtEnd && !SQLParserHelper.Expect(context.Current, SQLTokenType.RightParen))
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), context.Current.StartPosition, $"Expected ')' at position {context.Current.StartPosition}", errorTextLength: context.Current.Length);
                throw new SyntaxErrorException(err);
            }

            SQLToken lastToken = context.Consume(); // consume ')'

            bool typeIsNull = SQLParserHelper.Expect(context.Current, SQLTokenType.NullLiteral);
            if (SQLParserHelper.IsCurrentKeyword(context, "TYPE") ||
                SQLParserHelper.IsCurrentKeyword(context, "CONST") ||
                SQLParserHelper.Expect(context.Current, SQLTokenType.SQLType) ||
                typeIsNull)
            {
                // TYPE?
                if (SQLParserHelper.IsCurrentKeyword(context, "TYPE"))
                {
                    context.Consume();
                }

                // CONST?
                if (SQLParserHelper.IsCurrentKeyword(context, "CONST"))
                {
                    context.Consume();
                    constant = true;
                }

                // SQLType
                var typeToken = context.Current;
                typeIsNull = typeIsNull || SQLParserHelper.Expect(context.Current, SQLTokenType.NullLiteral);
                if (!SQLParserHelper.Expect(typeToken, SQLTokenType.SQLType) && !typeIsNull)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        typeToken.StartPosition,
                        $"Expected SQL type, got '{context.GetSlice(typeToken)}'",
                        errorTextLength: typeToken.Length);

                    throw new SyntaxErrorException(err);
                }

                if (typeIsNull)
                {
                    nullable = true;
                }

                if (!SQLDictionary.TryGetSQLType(
                        context.GetSlice(typeToken),
                        out declareType))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        typeToken.StartPosition,
                        $"Unknown function type '{context.GetSlice(typeToken)}'",
                        errorTextLength: typeToken.Length);

                    throw new SyntaxErrorException(err);
                }

                context.Consume();
                lastToken = typeToken;

                // NULLABLE?
                var nullableToken = context.Current;

                if (SQLParserHelper.Expect(nullableToken, SQLTokenType.SQLTypeNullable))
                {
                    if (typeIsNull)
                    {
                        var err = ErrorFormatter.Create(
                            context.Span.ToString(),
                            nullableToken.StartPosition,
                            "NULL type cannot be nullable.",
                            errorTextLength: nullableToken.Length);

                        throw new SyntaxErrorException(err);
                    }
                    context.Consume();
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
        private static SQLExpression ParseLiteral(ref SQLParserContext context)
        {
            var token = context.Consume();
            var val = context.GetSlice(token);
            return token.Type switch
            {
                SQLTokenType.StringLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = SQLParserHelper.UnescapeStringLiteral(val),
                    Type = SQLLiteralType.String
                },

                SQLTokenType.UnicodeString => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = SQLParserHelper.UnescapeStringLiteral(val),
                    Type = SQLLiteralType.UnicodeString
                },

                SQLTokenType.DateLiteral => ParseDateTime(token, ref context),
                SQLTokenType.TimeLiteral => ParseDateTime(token, ref context),
                SQLTokenType.DatetimeLiteral => ParseDateTime(token, ref context),
                SQLTokenType.IntegerLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = int.Parse(val, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Integer
                },

                SQLTokenType.DecimalLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = decimal.Parse(val, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Decimal
                },

                SQLTokenType.ScientificLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = double.Parse(val, NumberStyles.Float, CultureInfo.InvariantCulture),
                    Type = SQLLiteralType.Scientific
                },

                SQLTokenType.MoneyLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = decimal.Parse(val[1..], NumberStyles.Currency, CultureInfo.InvariantCulture), //strip $
                    Type = SQLLiteralType.Money
                },

                SQLTokenType.HexLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = Convert.FromHexString(val[2..]), // strip 0x
                    Type = SQLLiteralType.BinaryLiteral
                },

                SQLTokenType.NullLiteral => new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = null,
                    Type = SQLLiteralType.Null
                },

                _ => throw new SyntaxErrorException(ErrorFormatter.Create(context.Span.ToString(), token.StartPosition, $"Expected Literal but got '{val}'", errorTextLength: token.Length))
            };
        }
        private static LiteralExpression ParseDateTime(SQLToken token, ref SQLParserContext context)
        {
            var val = context.GetSlice(token);
            string err;

            if (token.Type == SQLTokenType.DateLiteral)
            {
                val = val[2..^1]; // remove d'...'

                if (!DateOnly.TryParseExact(val, SQLDictionary.DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        token.StartPosition,
                        $"Invalid datetime format '{context.GetSlice(token)}'. Use format 'yyyy-MM-dd'",
                        errorTextLength: token.Length);

                    throw new SyntaxErrorException(err);
                }

                return new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
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
                        context.Span.ToString(),
                        token.StartPosition,
                        $"Invalid time format '{context.GetSlice(token)}'. Use format 'HH:mm:ss.FFFFFFF'",
                        errorTextLength: token.Length);

                    throw new SyntaxErrorException(err);
                }

                return new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
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
                        context.Span.ToString(),
                        token.StartPosition,
                        $"Invalid dateTime format '{context.GetSlice(token)}'. Use format 'yyyy-MM-dd HH:mm:ss.FFFFFFF'",
                        errorTextLength: token.Length);

                    throw new SyntaxErrorException(err);
                }

                return new LiteralExpression
                {
                    Span = SQLParserHelper.CreateSpan(token),
                    Value = dateTime,
                    Type = SQLLiteralType.Datetime
                };
            }

            err = ErrorFormatter.Create(
                context.Span.ToString(),
                token.StartPosition,
                $"Unexpected token type '{token.Type}'. Expected a date, time, or datetime literal.",
                errorTextLength: token.Length);

            throw new SyntaxErrorException(err);
        }
        private static bool IsPredicateNot(ref SQLParserContext context)
        {
            if (!SQLParserHelper.IsCurrentKeyword(context, "NOT"))
                return false;

            var next = context.Peek();

            return SQLParserHelper.IsKeyword(next, "IN", context)
                || SQLParserHelper.IsKeyword(next, "LIKE", context)
                || SQLParserHelper.IsKeyword(next, "BETWEEN", context);
        }
    }
}
