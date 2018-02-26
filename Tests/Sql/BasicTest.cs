﻿using System;
using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class BasicTest : TranslationTestBase
    {
        [Test]
        public void Simple()
        {
            const string sourceSql = @"select ИНН as CounterpartyInn
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select
    c1 as CounterpartyInn
from t1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void SimpleWithAreas()
        {
            const string sourceSql = @"select ИНН as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    ОбластьДанныхОсновныеДанные Single c2";
            const string expectedResult = @"select
    contractors.c1 as CounterpartyInn
from (select
    __nested_table0.c1
from t1 as __nested_table0
where __nested_table0.c2 in (10, 200)) as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult, 10, 200);
        }

        [Test]
        public void CanUseNowParameter()
        {
            const string sourceSql = @"select *
    from Справочник.ДоговорыКонтрагентов as contracts
    where contracts.ДатаНачала <= &NoW and contracts.ДатаКонца >= &Now";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ДатаНачала Single c1
    ДатаКонца Single c2";
            currentDate = new DateTime(2016, 8, 9);
            const string expectedResult = @"select
    *
from t1 as contracts
where contracts.c1 <= cast('2016-08-09' as date) and contracts.c2 >= cast('2016-08-09' as date)";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void CanUseCustomParameter()
        {
            const string sourceSql = @"select *
    from Справочник.ДоговорыКонтрагентов as contracts
    where contracts.ДатаНачала <= &Param1";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ДатаНачала Single c1";
            const string expectedResult = @"select
    *
from t1 as contracts
where contracts.c1 <= @Param1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ReplaceTableWithoutAliasWithSubquery_GenerateAliasForSubquery()
        {
            const string sourceSql = @"select ИНН as CounterpartyInn
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    ОбластьДанныхОсновныеДанные Single c2";
            const string expectedResult = @"select
    __subquery0.c1 as CounterpartyInn
from (select
    __nested_table0.c1
from t1 as __nested_table0
where __nested_table0.c2 in (10, 200)) as __subquery0";
            CheckTranslate(mappings, sourceSql, expectedResult, 10, 200);
        }

        [Test]
        public void SimpleWithoutAlias()
        {
            const string sourceSql = @"select ИНН as CounterpartyInn
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select
    c1 as CounterpartyInn
from t1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void StripPresentationFunctionFromSimpleProperties()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(contractors.ИНН) as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select
    contractors.c1 as CounterpartyInn
from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionAll()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as Type
from Справочник.Контрагенты as contractors
where contractors.ИНН = ""test-inn1""

union all

select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as Type
from Справочник.Контрагенты as contractors
where contractors.ИНН = ""test-inn2""

union

select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as Type
from Справочник.Контрагенты as contractors
where contractors.ИНН = ""test-inn3""";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    ЮридическоеФизическоеЛицо Single c2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2 Main
    Ссылка Single c3
    Порядок Single c4 ";
            const string expectedResult = @"select
    contractors.__nested_field0 as Type
from (select
    __nested_table2.enumValueName as __nested_field0,
    __nested_table0.c1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.c3 = __nested_table0.c2
left join simple1c.enumMappings as __nested_table2 on __nested_table2.enumName = 'юридическоефизическоелицо' and __nested_table2.orderIndex = __nested_table1.c4) as contractors
where contractors.c1 = 'test-inn1'

union all

select
    contractors.__nested_field1 as Type
from (select
    __nested_table5.enumValueName as __nested_field1,
    __nested_table3.c1
from t1 as __nested_table3
left join t2 as __nested_table4 on __nested_table4.c3 = __nested_table3.c2
left join simple1c.enumMappings as __nested_table5 on __nested_table5.enumName = 'юридическоефизическоелицо' and __nested_table5.orderIndex = __nested_table4.c4) as contractors
where contractors.c1 = 'test-inn2'

union

select
    contractors.__nested_field2 as Type
from (select
    __nested_table8.enumValueName as __nested_field2,
    __nested_table6.c1
from t1 as __nested_table6
left join t2 as __nested_table7 on __nested_table7.c3 = __nested_table6.c2
left join simple1c.enumMappings as __nested_table8 on __nested_table8.enumName = 'юридическоефизическоелицо' and __nested_table8.orderIndex = __nested_table7.c4) as contractors
where contractors.c1 = 'test-inn3'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void CaseStatement()
        {
            const string sourceSql = @"
select СуммаДокумента,
	case when СуммаДокумента > 1000 then 3
	when СуммаДокумента > 300 then 2
	else 0
	end
 from Документ.ПоступлениеНаРасчетныйСчет";
            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет t1 Main
    СуммаДокумента Single sum";
            const string expected = @"
select
    sum as СуммаДокумента,
    case
    when sum > 1000 then 3
    when sum > 300 then 2
    else 0
end
from t1";
            CheckTranslate(mappings, sourceSql, expected);
        }

        [Test]
        public void CanUseRussianSyntax()
        {
            const string sourceSql = @"выбрать contractors.ИНН, КОЛИЧЕСТВО(contracts.Владелец) как ContractCount
    из Справочник.Контрагенты как contractors
    полное соединение Справочник.ДоговорыКонтрагентов contracts
        по contracts.Владелец = contractors.Ссылка
    ГДЕ contractors.наименование <> ""test-name"" и contractors.ИНН <> ""test-inn""
    сгруппировать по contractors.Ссылка";
            const string mappings = @"Справочник.Контрагенты contractors0 Main
    Ссылка Single id
    ОбластьДанныхОсновныеДанные Single area
    ИНН Single inn
    Наименование Single name
Справочник.ДоговорыКонтрагентов contracts1 Main
    ОбластьДанныхОсновныеДанные Single area
    Владелец Single owner Справочник.Контрагенты";
            const string expectedResult = @"select
    contractors.inn as ИНН,
    count(contracts.owner) as ContractCount
from contractors0 as contractors
full outer join contracts1 as contracts on contractors.area = contracts.area and contracts.owner = contractors.id
where contractors.name <> 'test-name' and contractors.inn <> 'test-inn'
group by contractors.id";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void MatchEntityAliasCaseInsensitive()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
    from Справочник.Контрагенты as Contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select
    Contractors.c1 as CounterpartyInn
from t1 as Contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void DoNotIncludeBraceInPropertyName()
        {
            const string sourceSql = @"select (contractors.ИНН) as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select
    contractors.c1 as CounterpartyInn
from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void DoNotIncludeEqualSignInPropertyName()
        {
            const string sourceSql = @"select contractors.Наименование as CounterpartyInn
    from Справочник.Контрагенты as contractors
    where contractors.ИНН=""test-inn""";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    Наименование Single c2";
            const string expectedResult = @"select
    contractors.c2 as CounterpartyInn
from t1 as contractors
where contractors.c1 = 'test-inn'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void RestoreParenthesesFromOriginalExpression()
        {
            const string sourceSql = @"select Наименование from Справочник.Контрагенты where (true or false) and false";
            const string mappings = @"Справочник.Контрагенты contractors1 Main
    Наименование Single name";
            const string expectedResult = @"select
    name as Наименование
from contractors1
where (true or false) and false";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void MapPresentationToDescriptionForReferences()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(contracts.ВалютаВзаиморасчетов) as Currency
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ВалютаВзаиморасчетов Single c1 Справочник.Валюты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Валюты t2 Main
    Ссылка Single с2
    Наименование Single c3
    ОбластьДанныхОсновныеДанные Single d2";
            const string expectedResult = @"select
    contracts.__nested_field0 as Currency
from (select
    __nested_table1.c3 as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table1.с2 = __nested_table0.c1) as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void MapTypeNameToIdentifierForReferences()
        {
            const string sourceSql = @"select ИдентификаторТипа(contracts.ВалютаВзаиморасчетов) as Currency
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ВалютаВзаиморасчетов Single c1 Справочник.Валюты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Валюты t2 Main
    ОбластьДанныхОсновныеДанные Single d2";
            const string expectedResult = @"select
    E'\\x00000002' as Currency
from t1 as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void MapTypeNameToIdentifierForUnionReferences()
        {
            const string sourceSql = @"select ИдентификаторТипа(payments.Контрагент) as Contractor from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    payments.f1_tableIndex as Contractor
from t1 as payments";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void CantGetIdentifierTypeForSubquery()
        {
            const string sourceSql = @"select ИдентификаторТипа(contracts.ВалютаВзаиморасчетов) as Currency
    from ( select c.ВалютаВзаиморасчетов from Справочник.ДоговорыКонтрагентов as c) as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ВалютаВзаиморасчетов Single c1 Справочник.Валюты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Валюты t2 Main
    ОбластьДанныхОсновныеДанные Single d1";
            var exception = Assert.Throws<InvalidOperationException>(() => CheckTranslate(mappings, sourceSql, ""));
            Assert.That(exception.Message, Is.EqualTo("[TypeIdentifier] function not supported for subquery column" +
                                                      " reference, [contracts.c1]"));
        }

        [Test]
        public void CantGetIdentifierTypeForSimpleColumn()
        {
            const string sourceSql = @"select ИдентификаторТипа(contracts.Наименование) as Currency
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    Наименование Single e1
    ОбластьДанныхОсновныеДанные Single d1";
            var exception = Assert.Throws<InvalidOperationException>(() => CheckTranslate(mappings, sourceSql, ""));
            Assert.That(exception.Message, Is.EqualTo("[TypeIdentifier] function not supported for non-reference" +
                                                      " columns, [contracts.e1]"));
        }

        [Test]
        public void CantGetIdentifierTypeForNonColumn()
        {
            const string sourceSql = @"select ИдентификаторТипа(""привет"") as Currency
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    Наименование Single e1
    ОбластьДанныхОсновныеДанные Single d1";
            var exception = Assert.Throws<InvalidOperationException>(() => CheckTranslate(mappings, sourceSql, ""));
            Assert.That(exception.Message, Is.EqualTo("[TypeIdentifier] function expected to have column reference as " +
                                                      "an argument"));
        }

        [Test]
        public void EqualityForUnionReferenceColumns()
        {
            const string sourceSql = @"select p.Контрагент as Contractor
    from Документ.СписаниеСРасчетногоСчета as p
    JOIN Документ.ПоступлениеНаРасчетныйСчет as i ON p.Контрагент = i.Контрагент";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences  f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Документ.ПоступлениеНаРасчетныйСчет t3 Main
    Контрагент UnionReferences  f3_tableIndex f3_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    p.f1_ref as Contractor
from t1 as p
inner join t3 as i on p.d1 = i.d1 and p.f1_tableIndex = i.f3_tableIndex and p.f1_ref = i.f3_ref";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void EqualityForUnionAndSingleReferenceColumns()
        {
            const string sourceSql = @"select p.Контрагент as Contractor
    from Документ.СписаниеСРасчетногоСчета as p
    JOIN Документ.ПоступлениеНаРасчетныйСчет as i ON p.Контрагент = i.Контрагент";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences  f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Документ.ПоступлениеНаРасчетныйСчет t3 Main
    Контрагент Single f3_ref Справочник.Контрагенты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    p.f1_ref as Contractor
from t1 as p
inner join t3 as i on p.d1 = i.d1 and p.f1_tableIndex = E'\\x000000D2' and p.f1_ref = i.f3_ref";

            CheckTranslate(mappings, sourceSql, expectedResult);

            const string sourceSql2 = @"select p.Контрагент as Contractor
    from Документ.СписаниеСРасчетногоСчета as p
    JOIN Документ.ПоступлениеНаРасчетныйСчет as i ON i.Контрагент = p.Контрагент";
            const string expectedResult2 =
                @"select
    p.f1_ref as Contractor
from t1 as p
inner join t3 as i on p.d1 = i.d1 and E'\\x000000D2' = p.f1_tableIndex and i.f3_ref = p.f1_ref";
            CheckTranslate(mappings, sourceSql2, expectedResult2);
        }

        [Test]
        public void CorrectCrashForInvalidUseOfPresentationFunction()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(testRef.Договор) as TestContract
    from Справочник.Тестовый as testRef";
            const string mappings = @"Справочник.Тестовый t1 Main
    Договор Single с1 Документ.ПоступлениеТоваровУслуг
Документ.ПоступлениеТоваровУслуг t2 Main
    Ссылка Single с2
    Наименование Single c3";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                CheckTranslate(mappings, sourceSql, ""));
            Assert.That(exception.Message,
                Is.EqualTo("[ПРЕДСТАВЛЕНИЕ] is only supported for [Перечисления,Справочники] but is applied to [Документ.ПоступлениеТоваровУслуг]"));
        }

        [Test]
        public void AddAreaToJoin()
        {
            const string sourceSql =
                @"выбрать contractors.Наименование as ContractorName,contractors.Родитель.Наименование as ParentName из
Справочник.Контрагенты as contractors
left join Справочник.КонтактныеЛица as contacts on contractors.ОсновноеКонтактноеЛицо = contacts.Ссылка";
            const string mappings = @"Справочник.Контрагенты t1 Main
    Ссылка Single c6
    Наименование Single c1
    ОбластьДанныхОсновныеДанные Single c2
    ОсновноеКонтактноеЛицо Single c3
    Родитель Single c4 Справочник.Контрагенты
Справочник.КонтактныеЛица t2 Main
    Ссылка Single c7
    ОбластьДанныхОсновныеДанные Single c5";
            const string expectedResult = @"select
    contractors.c1 as ContractorName,
    contractors.__nested_field0 as ParentName
from (select
    __nested_table0.c1,
    __nested_table1.c1 as __nested_field0,
    __nested_table0.c2,
    __nested_table0.c3
from t1 as __nested_table0
left join t1 as __nested_table1 on __nested_table1.c2 = __nested_table0.c2 and __nested_table1.c6 = __nested_table0.c4) as contractors
left join t2 as contacts on contractors.c2 = contacts.c5 and contractors.c3 = contacts.c7";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Having_Simple()
        {
            const string sourceSql = @"
select БухСчет, count(Идентификатор) from Документы.ПоступленияНаРасчтеныйСчет
group by БухСчет
having count(Идентификатор) > 10";
            const string mappings = @"Документы.ПоступленияНаРасчтеныйСчет documentsTable0 Main
    БухСчет Single accountingCodeColumn
    Идентификатор Single idColumn";
            const string expectedResult = @"select
    accountingCodeColumn as БухСчет,
    count(idColumn)
from documentsTable0
group by accountingCodeColumn
having count(idColumn) > 10";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void OrderBy_Simple()
        {
            const string source =
                @"select СчетУчетаРасчетовСКонтрагентом, count(Номер) from Документ.ПоступлениеНаРасчетныйСчет
group by СчетУчетаРасчетовСКонтрагентом
order by count(Номер) desc";

            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет documentsTable0 Main
    СчетУчетаРасчетовСКонтрагентом Single accountingCodeColumn
    Номер Single numberColumn";

            const string expected =
                @"select
    accountingCodeColumn as четУчетаРасчетовСКонтрагентом,
    count(numberColumn)
from documentsTable0
group by accountingCodeColumn
order by count(numberColumn) desc";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void CanUseDistinctInAggregateFunction()
        {
            const string sourceSql = @"select a, count(distinct b)
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    a Single f1
    b Single f2";
            const string expectedResult = @"select
    f1 as a,
    count(distinct f2)
from t1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Negation()
        {
            const string sourceSql = @"select -sum(a) from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    a Single f1";
            const string expectedResult = @"select
     -sum(f1)
from t1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void CanSelectFieldMultipleTimesWithDifferentCase()
        {
            const string sourceSql = @"select Ссылка, ссылка
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    Ссылка Single c1
    ОбластьДанныхОсновныеДанные Single c2";
            const string expectedResult = @"select
    __subquery0.c1 as Ссылка,
    __subquery0.c1 as ссылка
from (select
    __nested_table0.c1
from t1 as __nested_table0
where __nested_table0.c2 in (10, 20)) as __subquery0";
            CheckTranslate(mappings, sourceSql, expectedResult, 10, 20);
        }

        [Test]
        public void CheckIsReferenceObjectName()
        {
            const string sourceSql = @"select ИНН as CounterpartyInn
    from Справочник.Контрагенты
    where Владелец Ссылка Справочник.НеизвестныйСправочник";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    Владелец Single Справочник.Контрагенты";

            var exception = Assert.Throws<InvalidOperationException>(() => CheckTranslate(mappings, sourceSql, null));
            Assert.That(exception.Message,
                Is.EqualTo("operator [Ссылка] has unknown object name [Справочник.НеизвестныйСправочник]"));
        }

        [Test]
        public void CanUseDateTimeFunctionWithTime()
        {
            const string sourceSql = @"select *
    from Документ.ПоступлениеТоваровУслуг
    where Дата >= ДатаВремя(2016, 10, 11, 12, 13, 14)";
            const string mappings = @"Документ.ПоступлениеТоваровУслуг t1 Main
    Дата Single c1";
            const string expectedResult = @"select
    *
from t1
where c1 >= cast('2016-10-11 12:13:14' as timestamp)";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Null()
        {
            const string sourceSql = @"select null x from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select
    null as x
from t1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void AddDefaultAliases()
        {
            const string sourceSql = @"select ИНН,Владелец.ИНН,ИмяСвойстваПоДлинеПревышающееОграничениеПосгресаВ31Символ.ИНН
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    Владелец Single c2 Справочник.Контрагенты
    ОбластьДанныхОсновныеДанные Single c3
    ССылка Single c4
    ИмяСвойстваПоДлинеПревышающееОграничениеПосгресаВ31Символ Single c5 Справочник.Контрагенты";
            const string expectedResult = @"select
    __subquery0.c1 as ИНН,
    __subquery0.__nested_field0 as Владелец_ИНН,
    __subquery0.__nested_field1 as аничениеПосгресаВ31Символ_ИНН
from (select
    __nested_table0.c1,
    __nested_table1.c1 as __nested_field0,
    __nested_table2.c1 as __nested_field1
from t1 as __nested_table0
left join t1 as __nested_table1 on __nested_table1.c3 = __nested_table0.c3 and __nested_table1.c4 = __nested_table0.c2
left join t1 as __nested_table2 on __nested_table2.c3 = __nested_table0.c3 and __nested_table2.c4 = __nested_table0.c5) as __subquery0";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
    }
}