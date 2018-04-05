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
            if (expression.Operator == SqlBinaryOperator.Eq || expression.Operator == SqlBinaryOperator.Neq)
            {
                var left = GetReference(expression.Left);
                var right = GetReference(expression.Right);
                if (left == null || right == null)
                    return expression;
                var result =  new BinaryExpression(SqlBinaryOperator.And)
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
                if (expression.Operator == SqlBinaryOperator.Neq)
                    return new UnaryExpression()
                    {
                        Operator = UnaryOperator.Not,
                        Argument = result
                    };
                return result;
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

        private Reference GetReference(ISqlElement expr)
        {
            var column = expr as ColumnReferenceExpression;
            if (column == null)
                return null;
            var table = column.Table as TableDeclarationClause;
            if (table == null)
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
                if (mapping == null || string.IsNullOrEmpty(mapping.SingleLayout.NestedTableName))
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