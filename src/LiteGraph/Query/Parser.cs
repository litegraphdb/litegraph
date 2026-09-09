namespace LiteGraph.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using LiteGraph.Query.Ast;

    /// <summary>
    /// Parser for the supported LiteGraph native graph query profile.
    /// </summary>
    public class Parser
    {
        #region Private-Members

        private readonly List<GraphQueryToken> _Tokens;
        private int _Position = 0;

        internal const string ListExpressionPrefix = "__litegraph_list_json__";

        #endregion

        #region Constructors-and-Factories

        private Parser(List<GraphQueryToken> tokens)
        {
            _Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        /// <summary>
        /// Parse query text.
        /// </summary>
        /// <param name="query">Query text.</param>
        /// <returns>Query AST.</returns>
        public static GraphQueryAst Parse(string query)
        {
            Parser parser = new Parser(Lexer.Tokenize(query));
            GraphQueryAst ast = parser.ParseQuery();
            parser.ConsumeOptional(GraphQueryTokenTypeEnum.Semicolon);
            parser.Expect(GraphQueryTokenTypeEnum.End, "End of query expected");
            return ast;
        }

        #endregion

        #region Private-Methods

        private GraphQueryAst ParseQuery()
        {
            if (IsKeyword("OPTIONAL"))
            {
                Consume();
                ExpectKeyword("MATCH");
                return ParseMatch(optional: true);
            }

            if (IsKeyword("MATCH"))
            {
                Consume();
                return ParseMatch(optional: false);
            }

            if (IsKeyword("CREATE")) return ParseCreate();
            if (IsKeyword("CALL")) return ParseCall();
            throw Error(Current, "Expected MATCH, OPTIONAL MATCH, CREATE, or CALL");
        }

        private GraphQueryAst ParseMatch(bool optional)
        {
            bool shortest = false;
            if (IsKeyword("SHORTEST"))
            {
                shortest = true;
                Consume();
            }

            if (Current.Type == GraphQueryTokenTypeEnum.Identifier && IsNativeCreateObject(Current.Text))
            {
                if (optional) throw Error(Current, "OPTIONAL MATCH is not supported for graph child object mutation queries.");
                if (shortest) throw Error(Current, "MATCH SHORTEST is only supported for path queries.");
                return ParseNativeMatch();
            }

            NodePattern first = ParseNodePattern(false);

            if (ConsumeOptional(GraphQueryTokenTypeEnum.Dash))
            {
                List<GraphQueryPathSegment> segments = new List<GraphQueryPathSegment>();
                NodePattern from = first;

                while (true)
                {
                    EdgePattern edge = ParseEdgePattern(false, true);
                    Expect(GraphQueryTokenTypeEnum.Arrow, "'->' expected");
                    NodePattern to = ParseNodePattern(false);

                    segments.Add(new GraphQueryPathSegment
                    {
                        FromVariable = from.Variable,
                        FromLabel = from.Label,
                        EdgeVariable = edge.Variable,
                        EdgeLabel = edge.Label,
                        ToVariable = to.Variable,
                        ToLabel = to.Label,
                        IsVariableLength = edge.IsVariableLength,
                        MinHops = edge.MinHops,
                        MaxHops = edge.MaxHops
                    });

                    from = to;
                    if (!ConsumeOptional(GraphQueryTokenTypeEnum.Dash)) break;
                }

                bool hasVariableLengthSegment = segments.Any(segment => segment.IsVariableLength);
                if (segments.Count > 1 || hasVariableLengthSegment || shortest)
                {
                    if (shortest && !hasVariableLengthSegment)
                        throw Error(Previous, "MATCH SHORTEST requires a bounded variable-length path segment.");

                    GraphQueryAst pathAst = new GraphQueryAst
                    {
                        Kind = GraphQueryKindEnum.MatchPath,
                        IsOptional = optional,
                        IsShortestPath = shortest,
                        NodeVariable = first.Variable,
                        NodeLabel = first.Label,
                        FromVariable = segments[0].FromVariable,
                        EdgeVariable = segments[0].EdgeVariable,
                        EdgeLabel = segments[0].EdgeLabel,
                        ToVariable = segments[segments.Count - 1].ToVariable,
                        PathSegments = segments
                    };

                    ParseOptionalWhere(pathAst);
                    ParseReturnOrderLimit(pathAst);
                    return pathAst;
                }

                GraphQueryAst ast = new GraphQueryAst
                {
                    Kind = GraphQueryKindEnum.MatchEdge,
                    IsOptional = optional,
                    FromVariable = segments[0].FromVariable,
                    EdgeVariable = segments[0].EdgeVariable,
                    EdgeLabel = segments[0].EdgeLabel,
                    ToVariable = segments[0].ToVariable,
                    PathSegments = segments
                };

                ParseOptionalWhere(ast);
                ParseOptionalMatchMutation(ast, true);
                ParseReturnOrderLimit(ast);
                return ast;
            }

            GraphQueryAst nodeAst = new GraphQueryAst
            {
                Kind = GraphQueryKindEnum.MatchNode,
                IsOptional = optional,
                NodeVariable = first.Variable,
                NodeLabel = first.Label
            };

            if (shortest) throw Error(Previous, "MATCH SHORTEST is only supported for path queries.");
            ParseOptionalWhere(nodeAst);
            ParseOptionalMatchMutation(nodeAst, false);
            ParseReturnOrderLimit(nodeAst);
            return nodeAst;
        }

        private GraphQueryAst ParseNativeMatch()
        {
            string objectType = Consume().Text;
            string variable = Expect(GraphQueryTokenTypeEnum.Identifier, objectType + " variable expected").Text;

            GraphQueryAst ast = new GraphQueryAst
            {
                ObjectVariable = variable
            };

            ParseOptionalWhere(ast);
            ParseRequiredNativeMatchMutation(ast, objectType);
            ParseReturnOrderLimit(ast);
            return ast;
        }

        private GraphQueryAst ParseCreate()
        {
            ExpectKeyword("CREATE");

            if (Current.Type == GraphQueryTokenTypeEnum.Identifier && IsNativeCreateObject(Current.Text))
                return ParseNativeCreate();

            NodePattern first = ParseNodePattern(true);
            if (ConsumeOptional(GraphQueryTokenTypeEnum.Dash))
            {
                EdgePattern edge = ParseEdgePattern(true, false);
                Expect(GraphQueryTokenTypeEnum.Arrow, "'->' expected");
                ParseNodePattern(false);

                GraphQueryAst ast = new GraphQueryAst
                {
                    Kind = GraphQueryKindEnum.CreateEdge,
                    FromVariable = first.Variable,
                    EdgeVariable = edge.Variable,
                    EdgeLabel = edge.Label,
                    ToVariable = null,
                    Properties = edge.Properties
                };

                ParseReturnOrderLimit(ast);
                return ast;
            }

            GraphQueryAst nodeAst = new GraphQueryAst
            {
                Kind = GraphQueryKindEnum.CreateNode,
                NodeVariable = first.Variable,
                NodeLabel = first.Label,
                Properties = first.Properties
            };

            ParseReturnOrderLimit(nodeAst);
            return nodeAst;
        }

        private GraphQueryAst ParseNativeCreate()
        {
            string objectType = Consume().Text;
            string variable = objectType.ToLowerInvariant();
            if (Current.Type == GraphQueryTokenTypeEnum.Identifier && !IsKeyword("RETURN"))
                variable = Consume().Text;

            Dictionary<string, string> props = ParsePropertyMap();

            GraphQueryKindEnum kind;
            if (objectType.Equals("LABEL", StringComparison.OrdinalIgnoreCase)) kind = GraphQueryKindEnum.CreateLabel;
            else if (objectType.Equals("TAG", StringComparison.OrdinalIgnoreCase)) kind = GraphQueryKindEnum.CreateTag;
            else if (objectType.Equals("VECTOR", StringComparison.OrdinalIgnoreCase)) kind = GraphQueryKindEnum.CreateVector;
            else throw Error(Previous, "Unsupported CREATE object type");

            GraphQueryAst ast = new GraphQueryAst
            {
                Kind = kind,
                ObjectVariable = variable,
                Properties = props
            };

            ParseReturnOrderLimit(ast);
            return ast;
        }

        private GraphQueryAst ParseCall()
        {
            ExpectKeyword("CALL");
            GraphQueryToken procedureStart = Current;
            string procedure = ParseQualifiedName();
            Expect(GraphQueryTokenTypeEnum.LeftParen, "'(' expected");
            string argument = null;
            if (Current.Type != GraphQueryTokenTypeEnum.RightParen)
                argument = ParseValueExpression();
            Expect(GraphQueryTokenTypeEnum.RightParen, "')' expected");

            GraphQueryAst ast;
            if (IsAlgorithmProcedure(procedure))
            {
                ast = new GraphQueryAst
                {
                    Kind = GraphQueryKindEnum.Algorithm,
                    ProcedureName = procedure,
                    ProcedureArgumentExpression = argument
                };
            }
            else
            {
                ast = new GraphQueryAst
                {
                    Kind = GraphQueryKindEnum.VectorSearch,
                    ProcedureName = procedure,
                    ProcedureArgumentExpression = argument,
                    VectorDomain = ProcedureToVectorDomain(procedure, procedureStart)
                };
            }

            if (IsKeyword("YIELD"))
            {
                Consume();
                ast.YieldVariables = ParseIdentifierList();
            }

            ParseReturnOrderLimit(ast);
            return ast;
        }

        private NodePattern ParseNodePattern(bool allowProperties)
        {
            Expect(GraphQueryTokenTypeEnum.LeftParen, "'(' expected");

            string variable = null;
            string label = null;
            Dictionary<string, string> props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Current.Type == GraphQueryTokenTypeEnum.Identifier)
                variable = Consume().Text;

            if (ConsumeOptional(GraphQueryTokenTypeEnum.Colon))
                label = Expect(GraphQueryTokenTypeEnum.Identifier, "Label expected").Text;

            if (allowProperties && Current.Type == GraphQueryTokenTypeEnum.LeftBrace)
                props = ParsePropertyMap();

            Expect(GraphQueryTokenTypeEnum.RightParen, "')' expected");
            return new NodePattern(variable, label, props);
        }

        private EdgePattern ParseEdgePattern(bool allowProperties, bool allowVariableLength)
        {
            Expect(GraphQueryTokenTypeEnum.LeftBracket, "'[' expected");

            string variable = null;
            string label = null;
            Dictionary<string, string> props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool variableLength = false;
            int minHops = 1;
            int maxHops = 1;

            if (Current.Type == GraphQueryTokenTypeEnum.Identifier)
                variable = Consume().Text;

            if (ConsumeOptional(GraphQueryTokenTypeEnum.Colon))
                label = Expect(GraphQueryTokenTypeEnum.Identifier, "Edge label expected").Text;

            if (ConsumeOptional(GraphQueryTokenTypeEnum.Star))
            {
                if (!allowVariableLength) throw Error(Previous, "Variable-length edge patterns are only supported in MATCH queries.");
                variableLength = true;
                ParseVariableLengthBounds(out minHops, out maxHops);
            }

            if (allowProperties && Current.Type == GraphQueryTokenTypeEnum.LeftBrace)
                props = ParsePropertyMap();

            Expect(GraphQueryTokenTypeEnum.RightBracket, "']' expected");
            return new EdgePattern(variable, label, props, variableLength, minHops, maxHops);
        }

        private void ParseVariableLengthBounds(out int minHops, out int maxHops)
        {
            minHops = 1;
            maxHops = 1;

            if (Current.Type == GraphQueryTokenTypeEnum.Number)
            {
                minHops = ParsePositiveHopCount(Consume(), "Variable-length path minimum hop count must be a positive integer.");
                maxHops = minHops;
            }

            if (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
            {
                Expect(GraphQueryTokenTypeEnum.Dot, "'.' expected for variable-length path range.");
                if (Current.Type != GraphQueryTokenTypeEnum.Number)
                    throw Error(Current, "Variable-length path range requires a bounded maximum hop count.");

                maxHops = ParsePositiveHopCount(Consume(), "Variable-length path maximum hop count must be a positive integer.");
            }
            else if (maxHops == 1 && minHops == 1)
            {
                throw Error(Previous, "Variable-length path ranges must be bounded, for example *1..3.");
            }

            if (maxHops < minHops) throw Error(Previous, "Variable-length path maximum hop count must be greater than or equal to the minimum.");
            if (maxHops > 32) throw Error(Previous, "Variable-length path maximum hop count cannot exceed 32.");
        }

        private int ParsePositiveHopCount(GraphQueryToken token, string message)
        {
            if (!Int32.TryParse(token.Text, out int value) || value < 1) throw Error(token, message);
            return value;
        }

        private Dictionary<string, string> ParsePropertyMap()
        {
            Dictionary<string, string> props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Expect(GraphQueryTokenTypeEnum.LeftBrace, "'{' expected");

            while (Current.Type != GraphQueryTokenTypeEnum.RightBrace)
            {
                string key = Expect(GraphQueryTokenTypeEnum.Identifier, "Property name expected").Text;
                Expect(GraphQueryTokenTypeEnum.Colon, "':' expected");
                props[key] = ParseValueExpression();

                if (!ConsumeOptional(GraphQueryTokenTypeEnum.Comma)) break;
            }

            Expect(GraphQueryTokenTypeEnum.RightBrace, "'}' expected");
            return props;
        }

        private void ParseOptionalWhere(GraphQueryAst ast)
        {
            if (!IsKeyword("WHERE")) return;
            Consume();

            ast.WhereExpression = ParseWhereOrExpression();

            List<GraphQueryPredicate> predicates = new List<GraphQueryPredicate>();
            CollectPredicates(ast.WhereExpression, predicates);
            ast.WherePredicates.AddRange(predicates);

            GraphQueryPredicate first = predicates.FirstOrDefault();
            if (first == null) return;
            ast.WhereVariable = first.Variable;
            ast.WhereField = first.Field;
            ast.WhereOperator = first.Operator;
            ast.WhereValueExpression = first.ValueExpression;
        }

        private GraphQueryPredicateExpression ParseWhereOrExpression()
        {
            GraphQueryPredicateExpression left = ParseWhereAndExpression();

            while (IsKeyword("OR"))
            {
                Consume();
                left = new GraphQueryPredicateExpression
                {
                    Kind = GraphQueryPredicateExpressionKindEnum.Or,
                    Left = left,
                    Right = ParseWhereAndExpression()
                };
            }

            return left;
        }

        private GraphQueryPredicateExpression ParseWhereAndExpression()
        {
            GraphQueryPredicateExpression left = ParseWhereUnaryExpression();

            while (IsKeyword("AND"))
            {
                Consume();
                left = new GraphQueryPredicateExpression
                {
                    Kind = GraphQueryPredicateExpressionKindEnum.And,
                    Left = left,
                    Right = ParseWhereUnaryExpression()
                };
            }

            return left;
        }

        private GraphQueryPredicateExpression ParseWhereUnaryExpression()
        {
            if (IsKeyword("NOT"))
            {
                Consume();
                return new GraphQueryPredicateExpression
                {
                    Kind = GraphQueryPredicateExpressionKindEnum.Not,
                    Left = ParseWhereUnaryExpression()
                };
            }

            if (ConsumeOptional(GraphQueryTokenTypeEnum.LeftParen))
            {
                GraphQueryPredicateExpression expression = ParseWhereOrExpression();
                Expect(GraphQueryTokenTypeEnum.RightParen, "')' expected");
                return expression;
            }

            return new GraphQueryPredicateExpression
            {
                Kind = GraphQueryPredicateExpressionKindEnum.Predicate,
                Predicate = ParseWherePredicate()
            };
        }

        private static void CollectPredicates(GraphQueryPredicateExpression expression, List<GraphQueryPredicate> predicates)
        {
            if (expression == null) return;
            if (expression.Kind == GraphQueryPredicateExpressionKindEnum.Predicate)
            {
                if (expression.Predicate != null) predicates.Add(expression.Predicate);
                return;
            }

            CollectPredicates(expression.Left, predicates);
            CollectPredicates(expression.Right, predicates);
        }

        private GraphQueryPredicate ParseWherePredicate()
        {
            string variable = Expect(GraphQueryTokenTypeEnum.Identifier, "WHERE variable expected").Text;
            Expect(GraphQueryTokenTypeEnum.Dot, "'.' expected");
            List<string> fields = new List<string>
            {
                Expect(GraphQueryTokenTypeEnum.Identifier, "WHERE field expected").Text
            };

            while (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
            {
                fields.Add(Expect(GraphQueryTokenTypeEnum.Identifier, "WHERE field segment expected").Text);
            }

            return new GraphQueryPredicate
            {
                Variable = variable,
                Field = String.Join(".", fields),
                Operator = ParseWhereOperator(),
                ValueExpression = ParseValueExpression()
            };
        }

        private string ParseWhereOperator()
        {
            switch (Current.Type)
            {
                case GraphQueryTokenTypeEnum.Equals:
                case GraphQueryTokenTypeEnum.GreaterThan:
                case GraphQueryTokenTypeEnum.GreaterThanOrEquals:
                case GraphQueryTokenTypeEnum.LessThan:
                case GraphQueryTokenTypeEnum.LessThanOrEquals:
                    return Consume().Text;
                default:
                    if (IsKeyword("IN")) return Consume().Text.ToUpperInvariant();
                    if (IsKeyword("CONTAINS")) return Consume().Text.ToUpperInvariant();
                    if (IsKeyword("STARTS"))
                    {
                        Consume();
                        ExpectKeyword("WITH");
                        return "STARTS WITH";
                    }
                    if (IsKeyword("ENDS"))
                    {
                        Consume();
                        ExpectKeyword("WITH");
                        return "ENDS WITH";
                    }
                    throw Error(Current, "WHERE operator expected");
            }
        }

        private void ParseOptionalMatchMutation(GraphQueryAst ast, bool edgePattern)
        {
            if (IsKeyword("SET"))
            {
                if (ast.IsOptional) throw Error(Current, "OPTIONAL MATCH does not support SET mutations.");
                Consume();
                ParseSetClause(ast);

                if (edgePattern)
                {
                    if (!String.Equals(ast.SetVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase))
                        throw Error(Previous, "SET can only target the matched edge variable in edge mutation queries.");
                    ast.Kind = GraphQueryKindEnum.UpdateEdge;
                }
                else
                {
                    if (!String.Equals(ast.SetVariable, ast.NodeVariable, StringComparison.OrdinalIgnoreCase))
                        throw Error(Previous, "SET can only target the matched node variable in node mutation queries.");
                    ast.Kind = GraphQueryKindEnum.UpdateNode;
                }

                return;
            }

            if (IsKeyword("DELETE"))
            {
                if (ast.IsOptional) throw Error(Current, "OPTIONAL MATCH does not support DELETE mutations.");
                Consume();
                ast.DeleteVariable = Expect(GraphQueryTokenTypeEnum.Identifier, "DELETE variable expected").Text;

                if (edgePattern)
                {
                    if (!String.Equals(ast.DeleteVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase))
                        throw Error(Previous, "DELETE can only target the matched edge variable in edge mutation queries.");
                    ast.Kind = GraphQueryKindEnum.DeleteEdge;
                }
                else
                {
                    if (!String.Equals(ast.DeleteVariable, ast.NodeVariable, StringComparison.OrdinalIgnoreCase))
                        throw Error(Previous, "DELETE can only target the matched node variable in node mutation queries.");
                    ast.Kind = GraphQueryKindEnum.DeleteNode;
                }
            }
        }

        private void ParseRequiredNativeMatchMutation(GraphQueryAst ast, string objectType)
        {
            if (IsKeyword("SET"))
            {
                Consume();
                ParseSetClause(ast);
                if (!String.Equals(ast.SetVariable, ast.ObjectVariable, StringComparison.OrdinalIgnoreCase))
                    throw Error(Previous, "SET can only target the matched " + objectType + " variable.");

                if (objectType.Equals("LABEL", StringComparison.OrdinalIgnoreCase)) ast.Kind = GraphQueryKindEnum.UpdateLabel;
                else if (objectType.Equals("TAG", StringComparison.OrdinalIgnoreCase)) ast.Kind = GraphQueryKindEnum.UpdateTag;
                else if (objectType.Equals("VECTOR", StringComparison.OrdinalIgnoreCase)) ast.Kind = GraphQueryKindEnum.UpdateVector;
                else throw Error(Previous, "Unsupported MATCH object type.");
                return;
            }

            if (IsKeyword("DELETE"))
            {
                Consume();
                ast.DeleteVariable = Expect(GraphQueryTokenTypeEnum.Identifier, "DELETE variable expected").Text;
                if (!String.Equals(ast.DeleteVariable, ast.ObjectVariable, StringComparison.OrdinalIgnoreCase))
                    throw Error(Previous, "DELETE can only target the matched " + objectType + " variable.");

                if (objectType.Equals("LABEL", StringComparison.OrdinalIgnoreCase)) ast.Kind = GraphQueryKindEnum.DeleteLabel;
                else if (objectType.Equals("TAG", StringComparison.OrdinalIgnoreCase)) ast.Kind = GraphQueryKindEnum.DeleteTag;
                else if (objectType.Equals("VECTOR", StringComparison.OrdinalIgnoreCase)) ast.Kind = GraphQueryKindEnum.DeleteVector;
                else throw Error(Previous, "Unsupported MATCH object type.");
                return;
            }

            throw Error(Current, "MATCH " + objectType + " requires SET or DELETE.");
        }

        private void ParseSetClause(GraphQueryAst ast)
        {
            while (true)
            {
                string variable = Expect(GraphQueryTokenTypeEnum.Identifier, "SET variable expected").Text;
                Expect(GraphQueryTokenTypeEnum.Dot, "'.' expected");
                string property = Expect(GraphQueryTokenTypeEnum.Identifier, "SET property expected").Text;
                Expect(GraphQueryTokenTypeEnum.Equals, "'=' expected");
                string value = ParseValueExpression();

                if (String.IsNullOrEmpty(ast.SetVariable))
                    ast.SetVariable = variable;
                else if (!String.Equals(ast.SetVariable, variable, StringComparison.OrdinalIgnoreCase))
                    throw Error(Previous, "SET assignments must target a single variable.");

                ast.SetProperties[property] = value;

                if (!ConsumeOptional(GraphQueryTokenTypeEnum.Comma)) break;
            }

            if (ast.SetProperties.Count < 1) throw Error(Current, "SET requires at least one assignment.");
        }

        private void ParseReturn(GraphQueryAst ast)
        {
            ExpectKeyword("RETURN");
            ast.ReturnItems = ParseReturnItemList();
            if (ast.ReturnItems.Count < 1) throw Error(Current, "RETURN requires at least one item");

            bool hasVariable = ast.ReturnItems.Any(item => item.Kind == GraphQueryReturnItemKindEnum.Variable);
            bool hasAggregate = ast.ReturnItems.Any(item => item.Kind == GraphQueryReturnItemKindEnum.Aggregate);
            if (hasVariable && hasAggregate)
                throw Error(Current, "RETURN cannot mix aggregate expressions and graph variables in this release.");

            ast.ReturnVariables = ast.ReturnItems
                .Where(item => item.Kind == GraphQueryReturnItemKindEnum.Variable)
                .Select(item => item.Variable)
                .ToList();
        }

        private void ParseReturnOrderLimit(GraphQueryAst ast)
        {
            ParseReturn(ast);
            ParseOptionalOrder(ast);
            ParseOptionalLimit(ast);
        }

        private void ParseOptionalOrder(GraphQueryAst ast)
        {
            if (!IsKeyword("ORDER")) return;
            Consume();
            ExpectKeyword("BY");

            string first = Expect(GraphQueryTokenTypeEnum.Identifier, "ORDER BY variable expected").Text;
            if (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
            {
                ast.OrderVariable = first;
                List<string> fields = new List<string>
                {
                    Expect(GraphQueryTokenTypeEnum.Identifier, "ORDER BY field expected").Text
                };

                while (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
                {
                    fields.Add(Expect(GraphQueryTokenTypeEnum.Identifier, "ORDER BY field segment expected").Text);
                }

                ast.OrderField = String.Join(".", fields);
            }
            else
            {
                ast.OrderField = first;
            }

            if (IsKeyword("ASC"))
            {
                Consume();
            }
            else if (IsKeyword("DESC"))
            {
                Consume();
                ast.OrderDescending = true;
            }
        }

        private void ParseOptionalLimit(GraphQueryAst ast)
        {
            if (!IsKeyword("LIMIT")) return;
            Consume();
            string value = Expect(GraphQueryTokenTypeEnum.Number, "LIMIT value expected").Text;
            if (!Int32.TryParse(value, out int limit) || limit < 1)
                throw Error(Previous, "LIMIT must be a positive integer");
            ast.Limit = limit;
        }

        private List<string> ParseIdentifierList()
        {
            List<string> identifiers = new List<string>();
            while (true)
            {
                identifiers.Add(Expect(GraphQueryTokenTypeEnum.Identifier, "Identifier expected").Text);
                if (!ConsumeOptional(GraphQueryTokenTypeEnum.Comma)) break;
            }

            return identifiers;
        }

        private List<GraphQueryReturnItem> ParseReturnItemList()
        {
            List<GraphQueryReturnItem> items = new List<GraphQueryReturnItem>();
            while (true)
            {
                items.Add(ParseReturnItem());
                if (!ConsumeOptional(GraphQueryTokenTypeEnum.Comma)) break;
            }

            ValidateReturnAliases(items);
            return items;
        }

        private GraphQueryReturnItem ParseReturnItem()
        {
            GraphQueryToken first = Expect(GraphQueryTokenTypeEnum.Identifier, "RETURN item expected");
            if (Current.Type == GraphQueryTokenTypeEnum.LeftParen)
                return ParseAggregateReturnItem(first);

            return new GraphQueryReturnItem
            {
                Kind = GraphQueryReturnItemKindEnum.Variable,
                Variable = first.Text,
                Alias = first.Text
            };
        }

        private GraphQueryReturnItem ParseAggregateReturnItem(GraphQueryToken functionToken)
        {
            GraphQueryAggregateFunctionEnum function = ParseAggregateFunction(functionToken);
            Consume();

            bool wildcard = false;
            string variable = null;
            string field = null;

            if (ConsumeOptional(GraphQueryTokenTypeEnum.Star))
            {
                wildcard = true;
            }
            else
            {
                variable = Expect(GraphQueryTokenTypeEnum.Identifier, "Aggregate variable expected").Text;
                if (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
                {
                    List<string> fields = new List<string>
                    {
                        Expect(GraphQueryTokenTypeEnum.Identifier, "Aggregate field expected").Text
                    };

                    while (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
                    {
                        fields.Add(Expect(GraphQueryTokenTypeEnum.Identifier, "Aggregate field segment expected").Text);
                    }

                    field = String.Join(".", fields);
                }
            }

            Expect(GraphQueryTokenTypeEnum.RightParen, "')' expected");

            if (function != GraphQueryAggregateFunctionEnum.Count && wildcard)
                throw Error(functionToken, "Only COUNT supports '*'.");
            if (function != GraphQueryAggregateFunctionEnum.Count && String.IsNullOrEmpty(field))
                throw Error(functionToken, functionToken.Text.ToUpperInvariant() + " requires a variable field path.");

            string alias = null;
            if (IsKeyword("AS"))
            {
                Consume();
                alias = Expect(GraphQueryTokenTypeEnum.Identifier, "Aggregate alias expected").Text;
            }

            return new GraphQueryReturnItem
            {
                Kind = GraphQueryReturnItemKindEnum.Aggregate,
                AggregateFunction = function,
                AggregateWildcard = wildcard,
                Variable = variable,
                Field = field,
                Alias = alias ?? DefaultAggregateAlias(function)
            };
        }

        private GraphQueryAggregateFunctionEnum ParseAggregateFunction(GraphQueryToken token)
        {
            if (token.Text.Equals("COUNT", StringComparison.OrdinalIgnoreCase)) return GraphQueryAggregateFunctionEnum.Count;
            if (token.Text.Equals("SUM", StringComparison.OrdinalIgnoreCase)) return GraphQueryAggregateFunctionEnum.Sum;
            if (token.Text.Equals("AVG", StringComparison.OrdinalIgnoreCase)) return GraphQueryAggregateFunctionEnum.Avg;
            if (token.Text.Equals("MIN", StringComparison.OrdinalIgnoreCase)) return GraphQueryAggregateFunctionEnum.Min;
            if (token.Text.Equals("MAX", StringComparison.OrdinalIgnoreCase)) return GraphQueryAggregateFunctionEnum.Max;
            throw Error(token, "Unsupported aggregate function '" + token.Text + "'");
        }

        private static string DefaultAggregateAlias(GraphQueryAggregateFunctionEnum function)
        {
            return function.ToString().ToLowerInvariant();
        }

        private void ValidateReturnAliases(List<GraphQueryReturnItem> items)
        {
            HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (GraphQueryReturnItem item in items)
            {
                if (String.IsNullOrEmpty(item.Alias)) continue;
                if (!aliases.Add(item.Alias))
                    throw Error(Current, "RETURN aliases must be unique.");
            }
        }

        private string ParseQualifiedName()
        {
            List<string> parts = new List<string>
            {
                Expect(GraphQueryTokenTypeEnum.Identifier, "Procedure name expected").Text
            };

            while (ConsumeOptional(GraphQueryTokenTypeEnum.Dot))
            {
                parts.Add(Expect(GraphQueryTokenTypeEnum.Identifier, "Procedure name segment expected").Text);
            }

            return String.Join(".", parts);
        }

        private string ParseValueExpression()
        {
            GraphQueryToken token = Current;
            switch (token.Type)
            {
                case GraphQueryTokenTypeEnum.LeftBracket:
                    return ParseListValueExpression();
                case GraphQueryTokenTypeEnum.Parameter:
                case GraphQueryTokenTypeEnum.String:
                case GraphQueryTokenTypeEnum.Number:
                    Consume();
                    return token.Text;
                case GraphQueryTokenTypeEnum.Identifier:
                    if (token.Text.Equals("true", StringComparison.OrdinalIgnoreCase)
                        || token.Text.Equals("false", StringComparison.OrdinalIgnoreCase)
                        || token.Text.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        Consume();
                        return token.Text;
                    }
                    break;
            }

            throw Error(token, "Value expression expected");
        }

        private string ParseListValueExpression()
        {
            List<string> values = new List<string>();
            Expect(GraphQueryTokenTypeEnum.LeftBracket, "'[' expected");

            if (!ConsumeOptional(GraphQueryTokenTypeEnum.RightBracket))
            {
                while (true)
                {
                    values.Add(ParseValueExpression());
                    if (!ConsumeOptional(GraphQueryTokenTypeEnum.Comma)) break;
                }

                Expect(GraphQueryTokenTypeEnum.RightBracket, "']' expected");
            }

            return ListExpressionPrefix + JsonSerializer.Serialize(values);
        }

        private static bool IsNativeCreateObject(string text)
        {
            return text.Equals("LABEL", StringComparison.OrdinalIgnoreCase)
                || text.Equals("TAG", StringComparison.OrdinalIgnoreCase)
                || text.Equals("VECTOR", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAlgorithmProcedure(string procedure)
        {
            return procedure != null
                && procedure.StartsWith("litegraph.algo.", StringComparison.OrdinalIgnoreCase);
        }

        private static VectorSearchDomainEnum ProcedureToVectorDomain(string procedure, GraphQueryToken procedureStart)
        {
            if (procedure.Equals("litegraph.vector.searchNodes", StringComparison.OrdinalIgnoreCase))
                return VectorSearchDomainEnum.Node;
            if (procedure.Equals("litegraph.vector.searchEdges", StringComparison.OrdinalIgnoreCase))
                return VectorSearchDomainEnum.Edge;
            if (procedure.Equals("litegraph.vector.searchGraph", StringComparison.OrdinalIgnoreCase))
                return VectorSearchDomainEnum.Graph;

            throw new GraphQueryParseException("Unsupported CALL procedure '" + procedure + "'", procedureStart.Line, procedureStart.Column);
        }

        private void ExpectKeyword(string keyword)
        {
            if (!IsKeyword(keyword)) throw Error(Current, "Expected " + keyword);
            Consume();
        }

        private bool IsKeyword(string keyword)
        {
            return Current.Type == GraphQueryTokenTypeEnum.Identifier
                && Current.Text.Equals(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private GraphQueryToken Expect(GraphQueryTokenTypeEnum type, string message)
        {
            if (Current.Type != type) throw Error(Current, message);
            return Consume();
        }

        private bool ConsumeOptional(GraphQueryTokenTypeEnum type)
        {
            if (Current.Type != type) return false;
            Consume();
            return true;
        }

        private GraphQueryToken Consume()
        {
            GraphQueryToken token = Current;
            if (_Position < _Tokens.Count - 1) _Position++;
            return token;
        }

        private GraphQueryParseException Error(GraphQueryToken token, string message)
        {
            return new GraphQueryParseException(message, token.Line, token.Column);
        }

        private GraphQueryToken Current => _Tokens[_Position];

        private GraphQueryToken Previous => _Tokens[Math.Max(0, _Position - 1)];

        #endregion

        #region Private-Classes

        private sealed class NodePattern
        {
            internal string Variable { get; }
            internal string Label { get; }
            internal Dictionary<string, string> Properties { get; }

            internal NodePattern(string variable, string label, Dictionary<string, string> properties)
            {
                Variable = variable;
                Label = label;
                Properties = properties;
            }
        }

        private sealed class EdgePattern
        {
            internal string Variable { get; }
            internal string Label { get; }
            internal Dictionary<string, string> Properties { get; }
            internal bool IsVariableLength { get; }
            internal int MinHops { get; }
            internal int MaxHops { get; }

            internal EdgePattern(string variable, string label, Dictionary<string, string> properties, bool isVariableLength, int minHops, int maxHops)
            {
                Variable = variable;
                Label = label;
                Properties = properties;
                IsVariableLength = isVariableLength;
                MinHops = minHops;
                MaxHops = maxHops;
            }
        }

        #endregion
    }
}
