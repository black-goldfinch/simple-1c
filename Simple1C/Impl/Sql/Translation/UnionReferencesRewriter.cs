using System.Linq;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class UnionReferencesRewriter : SqlVisitor
    {
        private readonly IMappingSource mappingSource;

        public UnionReferencesRewriter(IMappingSource mappingSource)
        {
            this.mappingSource = mappingSource;
        }

        public override ISqlElement VisitBinary(BinaryExpression expression)
        {
            if (expression.Operator == SqlBinaryOperator.Eq)
            {
                var left = GetUnionReference(expression.Left);
                var right = GetUnionReference(expression.Right);
                if (left == null || right == null)
                    return expression;
                return new BinaryExpression(SqlBinaryOperator.And)
                {
                    Left = new EqualityExpression
                    {
                        Left = GetReferenceTypeExpression(left),
                        Right = GetReferenceTypeExpression(right)
                    },
                    Right = new EqualityExpression
                    {
                        Left = left.Column,
                        Right = right.Column
                    }
                };
            }
            expression.Left = Visit(expression.Left);
            expression.Right = Visit(expression.Right);
            return expression;
        }

        private ISqlElement GetReferenceTypeExpression(Reference left)
        {
            if (left.Property.SingleLayout != null)
            {
                var table = mappingSource.ResolveTableOrNull(left.Property.SingleLayout.NestedTableName);
                if (table?.Index == null)
                    return null;
                return new LiteralExpression
                {
                    SqlType = SqlType.ByteArray,
                    Value = table.Index.Value
                };
            }

            if (left.Property.UnionLayout != null)
            {
                return new ColumnReferenceExpression
                {
                    Name = left.Property.UnionLayout.TableIndexColumnName,
                    Table = left.Column.Table
                };
            }
            return null;
        }

        private Reference GetUnionReference(ISqlElement expr)
        {
            if (!(expr is ColumnReferenceExpression column))
                return null;
            if (!(column.Table is TableDeclarationClause table))
                return null;
            var tableMapping = mappingSource.ResolveTableByDbNameOrNull(table.Name);
            if (tableMapping == null)
                return null;
            var mapping = tableMapping.Properties.Where(x => x.UnionLayout != null)
                .SingleOrDefault(x => x.UnionLayout.ReferenceColumnName == column.Name);
            if (mapping == null)
            {
                mapping = tableMapping.Properties.Where(x => x.SingleLayout != null)
                    .SingleOrDefault(x => x.SingleLayout.DbColumnName == column.Name);
                if (string.IsNullOrEmpty(mapping?.SingleLayout.NestedTableName))
                    return null;
            }
            return new Reference
            {
                Property = mapping,
                Column = column,
            };
        }

        private class Reference
        {
            public PropertyMapping Property { get; set; }
            public ColumnReferenceExpression Column { get; set; }
        }
    }
}