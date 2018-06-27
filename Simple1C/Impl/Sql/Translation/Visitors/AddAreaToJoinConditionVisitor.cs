using System;
using System.Collections.Generic;
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
                    AddAreaColumn(union.SelectClause, PropertyNames.area);
            if (clause.Source is SubqueryTable subquery)
                foreach (var union in subquery.Query.Query.Unions)
                    AddAreaColumn(union.SelectClause, PropertyNames.area);

            clause.Condition = new AndExpression
            {
                Left = new EqualityExpression
                {
                    Left = currentContext.AreaColumn,
                    Right = new ColumnReferenceExpression
                    {
                        Name = PropertyNames.area,
                        Table = clause.Source
                    }
                },
                Right = clause.Condition
            };
            return base.VisitJoin(clause);
        }

        private void AddAreaColumn(SelectClause select, string columnAlias)
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

                foreach (var union in subquery.Query.Query.Unions)
                    AddAreaColumn(union.SelectClause, PropertyNames.area);
                return;
            }

            select.Fields.Add(new SelectFieldExpression
            {
                Alias = columnAlias,
                Expression = new ColumnReferenceExpression
                {
                    Name = PropertyNames.area,
                    Table = tableClause
                }
            });
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