using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class AddAreaToWhereClauseVisitor : SqlVisitor
    {
        private readonly List<ISqlElement> areas;
        private readonly IMappingSource mappingSource;

        public AddAreaToWhereClauseVisitor(IMappingSource mappingSource, List<ISqlElement> areas)
        {
            this.areas = areas;
            this.mappingSource = mappingSource;
        }
        public override SelectClause VisitSelect(SelectClause clause)
        {
            var tableClause = clause.Source as TableDeclarationClause;
            if (tableClause == null)
                tableClause = clause.JoinClauses
                    .Where(x => x.JoinKind == JoinKind.Inner)
                    .Where(x => x.Source is TableDeclarationClause)
                    .Select(x => x.Source)
                    .Cast<TableDeclarationClause>()
                    .FirstOrDefault();
            if (tableClause == null)
                return base.VisitSelect(clause);

            var tableMapping = mappingSource.ResolveTableByDbNameOrNull(tableClause.Name);
            PropertyMapping property;
            if (!tableMapping.TryGetProperty(PropertyNames.area, out property))
                return base.VisitSelect(clause);
            var areaExpression = new InExpression
            {
                Column = new ColumnReferenceExpression
                {
                    Name = property.SingleLayout.DbColumnName,
                    Table = tableClause
                },
                Source = new ListExpression
                {
                    Elements = areas
                }
            };
            if (clause.WhereExpression != null)
                clause.WhereExpression = new AndExpression
                {
                    Left = clause.WhereExpression,
                    Right = areaExpression
                };
            else
                clause.WhereExpression = areaExpression;

            return base.VisitSelect(clause);
        }
    }
}