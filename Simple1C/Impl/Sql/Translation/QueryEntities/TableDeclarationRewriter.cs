using System;
using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.Visitors;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class TableDeclarationRewriter : SqlVisitor
    {
        private readonly NameGenerator nameGenerator;
        private readonly QueryEntityRegistry queryEntityRegistry;
        private readonly QueryEntityAccessor queryEntityAccessor;
        private readonly List<ISqlElement> areas;
        private const byte configurationItemReferenceType = 8;

        private readonly Dictionary<IColumnSource, IColumnSource> rewrittenTables
            = new Dictionary<IColumnSource, IColumnSource>();

        public TableDeclarationRewriter(QueryEntityRegistry queryEntityRegistry,
            QueryEntityAccessor queryEntityAccessor,
            NameGenerator nameGenerator, List<ISqlElement> areas)
        {
            this.queryEntityRegistry = queryEntityRegistry;
            this.queryEntityAccessor = queryEntityAccessor;
            this.areas = areas;
            this.nameGenerator = nameGenerator;
        }

        public void RewriteTables(ISqlElement element)
        {
            Visit(element);
            new ColumnReferenceVisitor(column =>
            {
                IColumnSource generatedTable;
                if (rewrittenTables.TryGetValue(column.Table, out generatedTable))
                    column.Table = generatedTable;
                return column;
            }).Visit(element);
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause original)
        {
            var rewritten = RewriteTableIfNeeded(original);
            if (rewritten != original)
                rewrittenTables.Add(original, rewritten);
            return rewritten;
        }

        private IColumnSource RewriteTableIfNeeded(TableDeclarationClause declaration)
        {
            var queryRoot = queryEntityRegistry.Get(declaration);
            var subqueryRequired = queryRoot.subqueryRequired || areas != null;
            if (!subqueryRequired)
            {
                declaration.Name = queryRoot.entity.mapping.DbTableName;
                return declaration;
            }
            if (Strip(queryRoot.entity) == StripResult.HasNoReferences)
                throw new InvalidOperationException("assertion failure");
            var selectClause = new SelectClause
            {
                Source = queryEntityAccessor.GetTableDeclaration(queryRoot.entity)
            };
            if (areas != null)
                selectClause.WhereExpression = new InExpression
                {
                    Column = new ColumnReferenceExpression
                    {
                        Name = queryRoot.entity.GetAreaColumnName(),
                        Table = (TableDeclarationClause)selectClause.Source
                    },
                    Source = new ListExpression {Elements = areas}
                };
            AddJoinClauses(queryRoot.entity, selectClause);
            AddColumns(queryRoot, selectClause);
            return new SubqueryTable
            {
                Alias = declaration.Alias ?? nameGenerator.GenerateSubqueryName(),
                Query = new SubqueryClause
                {
                    Query = new SqlQuery
                    {
                        Unions = {new UnionClause {SelectClause = selectClause}}
                    }
                }
            };
        }

        private void AddJoinClauses(QueryEntity entity, SelectClause target)
        {
            foreach (var p in entity.properties)
                foreach (var nestedEntity in p.nestedEntities)
                {
                    if (nestedEntity == entity)
                        continue;
                    var eqConditions = new List<ISqlElement>();
                    if (!nestedEntity.mapping.IsEnum())
                        eqConditions.Add(new EqualityExpression
                        {
                            Left = new ColumnReferenceExpression
                            {
                                Name = nestedEntity.GetAreaColumnName(),
                                Table = queryEntityAccessor.GetTableDeclaration(nestedEntity)
                            },
                            Right = new ColumnReferenceExpression
                            {
                                Name = p.referer.GetAreaColumnName(),
                                Table = queryEntityAccessor.GetTableDeclaration(p.referer)
                            }
                        });
                    if (p.mapping.UnionLayout != null)
                        eqConditions.Add(nestedEntity.unionCondition = GetUnionCondition(p, nestedEntity));
                    var referenceColumnName = p.mapping.SingleLayout == null
                        ? p.mapping.UnionLayout.ReferenceColumnName
                        : p.mapping.SingleLayout.DbColumnName;
                    if (string.IsNullOrEmpty(referenceColumnName))
                    {
                        const string messageFormat = "ref column is not defined for [{0}.{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            p.referer.mapping.QueryTableName, p.mapping.PropertyName));
                    }
                    eqConditions.Add(new EqualityExpression
                    {
                        Left = new ColumnReferenceExpression
                        {
                            Name = nestedEntity.GetIdColumnName(),
                            Table = queryEntityAccessor.GetTableDeclaration(nestedEntity)
                        },
                        Right = new ColumnReferenceExpression
                        {
                            Name = referenceColumnName,
                            Table = queryEntityAccessor.GetTableDeclaration(p.referer)
                        }
                    });
                    var joinClause = new JoinClause
                    {
                        Source = queryEntityAccessor.GetTableDeclaration(nestedEntity),
                        JoinKind = JoinKind.Left,
                        Condition = eqConditions.Combine()
                    };
                    target.JoinClauses.Add(joinClause);
                    AddJoinClauses(nestedEntity, target);
                }
        }

        private void AddColumns(QueryRoot root, SelectClause target)
        {
            foreach (var f in root.fields.Values)
            {
                var expression = GetFieldExpression(f, target);
                if (f.invert)
                    expression = new QueryFunctionExpression
                    {
                        Function = KnownQueryFunction.SqlNot,
                        Arguments = new List<ISqlElement> { expression }
                    };
                target.Fields.Add(new SelectFieldExpression
                {
                    Expression = expression,
                    Alias = f.alias
                });
            }
        }

        private ISqlElement GetFieldExpression(QueryField field, SelectClause selectClause)
        {
            if (field.properties.Length < 1)
                throw new InvalidOperationException("assertion failure");
            if (field.properties.Length == 1)
                return GetPropertyReference(field.properties[0], selectClause);
            var result = new CaseExpression();
            var eqConditions = new List<ISqlElement>();
            foreach (var property in field.properties)
            {
                eqConditions.Clear();
                var entity = property.referer;
                while (entity != null)
                {
                    if (entity.unionCondition != null)
                        eqConditions.Add(entity.unionCondition);
                    entity = entity.referer == null ? null : entity.referer.referer;
                }
                result.Elements.Add(new CaseElement
                {
                    Value = GetPropertyReference(property, selectClause),
                    Condition = eqConditions.Combine()
                });
            }
            return result;
        }

        private ColumnReferenceExpression GetPropertyReference(QueryEntityProperty property, SelectClause selectClause)
        {
            if (property.referer.mapping.IsEnum())
            {
                var enumMappingsJoinClause = queryEntityAccessor.CreateEnumMappingsJoinClause(property.referer);
                selectClause.JoinClauses.Add(enumMappingsJoinClause);
                return new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    Table = (TableDeclarationClause)enumMappingsJoinClause.Source
                };
            }
            return new ColumnReferenceExpression
            {
                Name = property.GetDbColumnName(),
                Table = queryEntityAccessor.GetTableDeclaration(property.referer)
            };
        }

        private static StripResult Strip(QueryEntity queryEntity)
        {
            var result = StripResult.HasNoReferences;
            for (var i = queryEntity.properties.Count - 1; i >= 0; i--)
            {
                var p = queryEntity.properties[i];
                var propertyReferenced = p.referenced;
                for (var j = p.nestedEntities.Count - 1; j >= 0; j--)
                {
                    var nestedEntity = p.nestedEntities[j];
                    if (nestedEntity == queryEntity)
                        continue;
                    if (Strip(nestedEntity) == StripResult.HasNoReferences)
                        p.nestedEntities.RemoveAt(j);
                    else
                        propertyReferenced = true;
                }
                if (propertyReferenced)
                    result = StripResult.HasReferences;
                else
                    queryEntity.properties.RemoveAt(i);
            }
            return result;
        }

        private ISqlElement GetUnionCondition(QueryEntityProperty property, QueryEntity nestedEntity)
        {
            var typeColumnName = property.mapping.UnionLayout.TypeColumnName;
            if (string.IsNullOrEmpty(typeColumnName))
            {
                const string messageFormat = "type column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
            }
            if (!nestedEntity.mapping.Index.HasValue)
            {
                var message = string.Format("Invalid table name {0}. Table name must contain index.",
                    nestedEntity.mapping.DbTableName);
                throw new InvalidOperationException(message);
            }
            var tableIndexColumnName = property.mapping.UnionLayout.TableIndexColumnName;
            if (string.IsNullOrEmpty(tableIndexColumnName))
            {
                const string messageFormat = "tableIndex column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
            }
            return new AndExpression
            {
                Left = new EqualityExpression
                {
                    Left = new ColumnReferenceExpression
                    {
                        Name = typeColumnName,
                        Table = queryEntityAccessor.GetTableDeclaration(property.referer)
                    },
                    Right = new LiteralExpression
                    {
                        Value = configurationItemReferenceType,
                        SqlType = SqlType.ByteArray
                    }
                },
                Right = new EqualityExpression
                {
                    Left = new ColumnReferenceExpression
                    {
                        Name = tableIndexColumnName,
                        Table = queryEntityAccessor.GetTableDeclaration(property.referer)
                    },
                    Right = new LiteralExpression
                    {
                        Value = nestedEntity.mapping.Index,
                        SqlType = SqlType.ByteArray
                    }
                }
            };
        }

        private enum StripResult
        {
            HasReferences,
            HasNoReferences
        }

        private class ColumnReferenceVisitor : SqlVisitor
        {
            private readonly Func<ColumnReferenceExpression, ColumnReferenceExpression> visitor;

            public ColumnReferenceVisitor(Func<ColumnReferenceExpression, ColumnReferenceExpression> visitor)
            {
                this.visitor = visitor;
            }

            public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
            {
                return visitor((ColumnReferenceExpression) base.VisitColumnReference(expression));
            }
        }
    }
}