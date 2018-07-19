using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class AddAreaToJoinConditionVisitor : SqlVisitor
    {
        private readonly Stack<Context> contexts = new Stack<Context>();

        public override JoinClause VisitJoin(JoinClause clause)
        {
            var currentContext = contexts.Peek();
            if (currentContext == null || currentContext.AreaColumn == null)
                return base.VisitJoin(clause);
            if (currentContext.AreaColumn.Table is SubqueryTable subq)
                foreach (var union in subq.Query.Query.Unions)
                    AddAreaColumn(union.SelectClause);
            var columnAlias = PropertyNames.area;
            if (clause.Source is SubqueryTable subquery)
                columnAlias = subquery.Query.Query.Unions.Select(x => AddAreaColumn(x.SelectClause)).First();

            clause.Condition = new AndExpression
            {
                Left = new EqualityExpression
                {
                    Left = currentContext.AreaColumn,
                    Right = new ColumnReferenceExpression
                    {
                        Name = columnAlias,
                        Table = clause.Source
                    }
                },
                Right = clause.Condition
            };
            return base.VisitJoin(clause);
        }

        private string AddAreaColumn(SelectClause select)
        {
            var tableClause = select.Source as TableDeclarationClause;
            if (tableClause == null)
            {
                var subquery = select.Source as SubqueryTable;
                if (subquery == null)
                {
                    const string message = "assertion failure: unknown source found for alias [{0}] : [{1}]";
                    throw new InvalidOperationException(string.Format(
                        message, select.Source.Alias, select.Source.GetType().Name));
                }

                return subquery.Query.Query.Unions.Select(x => AddAreaColumn(x.SelectClause)).First();
            }

            var column = select.Fields
                .Where(x => x.Expression is ColumnReferenceExpression col && col.Name == PropertyNames.area)
                .Select(x => new
                {
                    Column = x.Expression as ColumnReferenceExpression,
                    Alias = x.Alias
                })
                .SingleOrDefault();
            string columnAlias = PropertyNames.area;
            if (column == null)
                select.Fields.Add(new SelectFieldExpression
                {
                    Alias = columnAlias,
                    Expression = new ColumnReferenceExpression
                    {
                        Name = PropertyNames.area,
                        Table = tableClause
                    }
                });
            else
                columnAlias = column.Alias ?? column.Column.Name;
            return columnAlias;
        }

        public override SubqueryTable VisitSubqueryTable(SubqueryTable clause)
        {
            var context = contexts.Peek();
            if (context.AreaColumn == null)
                context.AreaColumn = new ColumnReferenceExpression
                {
                    Name = PropertyNames.area,
                    Table = clause
                };

            return base.VisitSubqueryTable(clause);
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            var context = contexts.Peek();
            if (context.AreaColumn == null)
                context.AreaColumn = new ColumnReferenceExpression
                {
                    Name = PropertyNames.area,
                    Table = clause
                };
            return base.VisitTableDeclaration(clause);
        }

        public override SqlQuery VisitSqlQuery(SqlQuery sqlQuery)
        {
            contexts.Push(new Context());
            var result = base.VisitSqlQuery(sqlQuery);
            contexts.Pop();
            return result;
        }

        private class Context
        {
            public ColumnReferenceExpression AreaColumn { get; set; }
        }
    }
}