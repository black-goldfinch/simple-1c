using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class SubqueryTest : TranslationTestBase
    {
        [Test]
        public void CanSelectAllInSubquery()
        {
            const string source = "select t.ИНН, t.Наименование from (select * from Справочник.Контрагенты) t";
            const string mappings = @"Справочник.Контрагенты contractors0 Main
    ИНН Single inn
    Наименование Single name";

            const string expected = @"select
    t.inn as ИНН,
    t.name as Наименование
from (select
    *
from contractors0) as t";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinInsideSubquery()
        {
            const string source = @"select 
    subquery.contractName, 
    subquery.inn,
    docs.Номер
    from Справочник.Контрагенты contractorsOuter
    left join (select 
        contracts.Наименование as contractName,
        contractorsInner.ИНН as inn,
        contractorsInner.Ссылка as contractorId
    from Справочник.Контрагенты contractorsInner
    left join Справочник.ДоговорыКонтрагентов contracts on contractorsInner.Ссылка = contracts.Владелец) as subquery on subquery.contractorId = contractorsOuter.Ссылка
    left join Документ.ПоступлениеНаРасчетныйСчет docs on docs.Контрагент = contractorsOuter.Ссылка";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contracts1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractors2 Main
    Ссылка Single id
    Наименование Single name
    ИНН Single inn
    ОбластьДанныхОсновныеДанные Single mainData
Документ.ПоступлениеНаРасчетныйСчет docs3 Main
    Контрагент Single contractorId Справочник.Контрагенты
    Номер Single number
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    subquery.contractName as contractName,
    subquery.inn as inn,
    docs.number as Номер
from contractors2 as contractorsOuter
left join (select
    contracts.name as contractName,
    contractorsInner.inn as inn,
    contractorsInner.id as contractorId,
    contractorsInner.mainData as ОбластьДанныхОсновныеДанные
from contractors2 as contractorsInner
left join contracts1 as contracts on contractorsInner.mainData = contracts.mainData and contractorsInner.id = contracts.contractorId) as subquery on contractorsOuter.mainData = subquery.ОбластьДанныхОсновныеДанные and subquery.contractorId = contractorsOuter.id
left join docs3 as docs on contractorsOuter.mainData = docs.mainData and docs.contractorId = contractorsOuter.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinOnSubquery()
        {
            const string source = @"
select 
    contracts.Наименование, 
    contractor.Наименование 
from (select 
        Наименование, 
        Ссылка 
    from Справочник.Контрагенты as contractors) contractor
 join Справочник.ДоговорыКонтрагентов contracts on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.name as Наименование_2
from (select
    contractors.name,
    contractors.id,
    contractors.mainData as ОбластьДанныхОсновныеДанные
from contractorsTable2 as contractors
where contractors.mainData in (10, 200)) as contractor
inner join contractsTable1 as contracts on contractor.ОбластьДанныхОсновныеДанные = contracts.mainData and contracts.contractorId = contractor.id
where contracts.mainData in (10, 200)";
            CheckTranslate(mappings, source, expected, 10, 200);
        }

        [Test]
        public void JoinOnSubqueryWithAreaColumn()
        {
            const string source = @"
select
    contracts.Наименование,
    contractor.Наименование
from (select
        Наименование,
        Ссылка,
        ОбластьДанныхОсновныеДанные
    from Справочник.Контрагенты as contractors) contractor
 join Справочник.ДоговорыКонтрагентов contracts on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.name as Наименование_2
from (select
    contractors.name,
    contractors.id,
    contractors.mainData
from contractorsTable2 as contractors) as contractor
inner join contractsTable1 as contracts on contractor.mainData = contracts.mainData and contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinOnSubqueryWithAliasedAreaColumn()
        {
            const string source = @"
select
    contracts.Наименование,
    contractor.Наименование
from (select
        Наименование,
        Ссылка,
        ОбластьДанныхОсновныеДанные MD
    from Справочник.Контрагенты as contractors) contractor
 join Справочник.ДоговорыКонтрагентов contracts on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.name as Наименование_2
from (select
    contractors.name,
    contractors.id,
    contractors.mainData as MD
from contractorsTable2 as contractors) as contractor
inner join contractsTable1 as contracts on contractor.MD = contracts.mainData and contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinToSubqueryWithAreaColumn()
        {
            const string source = @"
select
    contracts.Наименование,
    contractor.Наименование
from Справочник.ДоговорыКонтрагентов contracts
 join (select
        Наименование,
        Ссылка,
        ОбластьДанныхОсновныеДанные
    from Справочник.Контрагенты as contractors) contractor on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.name as Наименование_2
from contractsTable1 as contracts
inner join (select
    contractors.name,
    contractors.id,
    contractors.mainData
from contractorsTable2 as contractors) as contractor on contracts.mainData = contractor.mainData and contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinToSubqueryWithAreaAliasedColumn()
        {
            const string source = @"
select
    contracts.Наименование,
    contractor.Наименование
from Справочник.ДоговорыКонтрагентов contracts
 join (select
        Наименование,
        Ссылка,
        ОбластьДанныхОсновныеДанные MD
    from Справочник.Контрагенты as contractors) contractor on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.name as Наименование_2
from contractsTable1 as contracts
inner join (select
    contractors.name,
    contractors.id,
    contractors.mainData as MD
from contractorsTable2 as contractors) as contractor on contracts.mainData = contractor.MD and contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinToSubqueryWithGroupByClause()
        {
            const string source = @"
select
    contracts.Наименование,
    contractor.Наименование
from Справочник.ДоговорыКонтрагентов contracts
 join (select
        МАКСИМУМ(contractors.Наименование) AS Наименование,
        contractors.Ссылка
    from Справочник.Контрагенты as contractors
    group by contractors.Ссылка) contractor on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.Наименование as Наименование_2
from contractsTable1 as contracts
inner join (select
    max(contractors.name) as Наименование,
    contractors.id,
    contractors.mainData as ОбластьДанныхОсновныеДанные
from contractorsTable2 as contractors
group by contractors.id,contractors.mainData) as contractor on contracts.mainData = contractor.ОбластьДанныхОсновныеДанные and contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinToSubqueryWithUnion()
        {
            const string source = @"
select
    contracts.Наименование,
    contractor.Наименование
from Справочник.ДоговорыКонтрагентов contracts
 join (
    select
        contractors.Наименование,
        contractors.Ссылка
    from Справочник.Контрагенты as contractors
    UNION ALL
    select
        individual.Наименование,
        individual.Ссылка
    from Справочник.ФизЛица as individual
) contractor on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
Справочник.ФизЛица individuals Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name as Наименование,
    contractor.name as Наименование_2
from contractsTable1 as contracts
inner join (select
    contractors.name,
    contractors.id,
    contractors.mainData as ОбластьДанныхОсновныеДанные
from contractorsTable2 as contractors

union all

select
    individual.name,
    individual.id,
    individual.mainData as ОбластьДанныхОсновныеДанные
from individuals as individual) as contractor on contracts.mainData = contractor.ОбластьДанныхОсновныеДанные and contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinToSubqueryWithUnion2()
        {
            const string source = @"
 select
	income.инн,
	income.Ссылка as counterpartId,
	contract.Ссылка as contractId,
	представление(contract.видДоговора) as contractType,
	представление(income.видДоговора) as counterpartType
from Справочник.Контрагенты AS income
inner join Справочник.ДоговорыКонтрагентов contract on contract.Владелец = income.ссылка

union all

 select
	outcome.инн,
	outcome.Ссылка as counterpartId,
	contract.Ссылка as contractId,
	представление(contract.видДоговора) as contractType,
	представление(income.видДоговора) as counterpartType
from Справочник.Контрагенты AS outcome
inner join Справочник.ДоговорыКонтрагентов contract on contract.Владелец = outcome.ссылка
";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ВидДоговора Single type Перечисление.ВидДоговора
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    ВидДоговора Single type Перечисление.ВидДоговора
    Наименование Single name
    ИНН Single inn
    ОбластьДанныхОсновныеДанные Single mainData
Перечисление.ВидДоговора t2 Main
    ССылка Single f3
    Порядок Single f4";

            const string expected = @"
select
	income.inn as инн,
	income.id as counterpartId,
	contract.id as contractId,
	contract.__nested_field0 as contractType,
	income.__nested_field1 as counterpartType
from (select
	__nested_table0.inn,
	__nested_table0.id,
	__nested_table2.enumValueName as __nested_field1,
	__nested_table0.mainData
from contractorsTable2 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f3 = __nested_table0.type
left join simple1c.enumMappings as __nested_table2 on __nested_table2.enumName = 'виддоговора' and __nested_table2.orderIndex = __nested_table1.f4) as income
inner join (select
	__nested_table3.id,
	__nested_table5.enumValueName as __nested_field0,
	__nested_table3.mainData,
	__nested_table3.contractorId
from contractsTable1 as __nested_table3
left join t2 as __nested_table4 on __nested_table4.f3 = __nested_table3.type
left join simple1c.enumMappings as __nested_table5 on __nested_table5.enumName = 'виддоговора' and __nested_table5.orderIndex = __nested_table4.f4) as contract on income.mainData = contract.mainData and contract.contractorId = income.id

union all

select
	outcome.inn as инн,
	outcome.id as counterpartId,
	contract.id as contractId,
	contract.__nested_field2 as contractType,
	income.__nested_field1 as counterpartType
from contractorsTable2 as outcome
inner join (select
	__nested_table6.id,
	__nested_table8.enumValueName as __nested_field2,
	__nested_table6.mainData,
	__nested_table6.contractorId
from contractsTable1 as __nested_table6
left join t2 as __nested_table7 on __nested_table7.f3 = __nested_table6.type
left join simple1c.enumMappings as __nested_table8 on __nested_table8.enumName = 'виддоговора' and __nested_table8.orderIndex = __nested_table7.f4) as contract on outcome.mainData = contract.mainData and contract.contractorId = outcome.id";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void SelectFromSubqueryWithAreas()
        {
            const string source = "select ИНН, Наименование_Alias from " +
                                  "(select ИНН, Наименование as Наименование_Alias " +
                                  "from Справочник.Контрагенты) t";

            const string mappings = @"Справочник.Контрагенты contractors0 Main
    ИНН Single inn
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected =
                @"select
    t.inn as ИНН,
    t.Наименование_Alias as Наименование_Alias
from (select
    inn,
    name as Наименование_Alias
from contractors0
where mainData in (10, 20, 30)) as t";
            CheckTranslate(mappings, source, expected, 10, 20, 30);
        }

        [Test]
        public void SubqueryInFilterExpressionRefersToOuterTable()
        {
            const string source = @"select Номер from Документ.ПоступлениеНаРасчетныйСчет dOuter 
    where СуммаДокумента in 
    (select СуммаДокумента from Документ.ПоступлениеНаРасчетныйСчет where Номер <> dOuter.Номер)";

            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    Номер Single number
    СуммаДокумента Single sum";

            const string expected = @"select
    dOuter.number as Номер
from documents1 as dOuter
where dOuter.sum in (select
    sum
from documents1
where number <> dOuter.number)";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void SubqueryUsesNestedPropertyOfOuterTable()
        {
            const string source = @"select * from Документ.ПоступлениеНаРасчетныйСчет as cOuter 
    where Контрагент.Наименование in (select Наименование from 
                    Справочник.Контрагенты cInner 
                    where cOuter.ДоговорКонтрагента.Наименование like cInner.Наименование )";
            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    Ссылка Single id
    Контрагент Single contractorId Справочник.Контрагенты
    ДоговорКонтрагента Single contractId Справочник.ДоговорыКонтрагентов
    ОбластьДанныхОсновныеДанные Single mainData
Справочник.Контрагенты contractors2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
Справочник.ДоговорыКонтрагентов contracts3 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";
            const string expected = @"select
    *
from (select
    __nested_table1.name as __nested_field0,
    __nested_table2.name as __nested_field1
from documents1 as __nested_table0
left join contractors2 as __nested_table1 on __nested_table1.mainData = __nested_table0.mainData and __nested_table1.id = __nested_table0.contractorId
left join contracts3 as __nested_table2 on __nested_table2.mainData = __nested_table0.mainData and __nested_table2.id = __nested_table0.contractId) as cOuter
where cOuter.__nested_field0 in (select
    cInner.name
from contractors2 as cInner
where cOuter.__nested_field1 like cInner.name)";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void UseSubqueryInFilterExpression()
        {
            const string source = @"select * from Документ.ПоступлениеНаРасчетныйСчет 
                            where ИННКонтрагента in (select ИНН from Справочник.Контрагенты where ИННВведенКорректно = true)";
            const string mappings = @"Справочник.Контрагенты contractors0 Main
    ИНН Single inn
    ИННВведенКорректно Single innIsCorrect
Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    ИННКонтрагента Single contractorInn";

            const string expected = @"select
    *
from documents1
where contractorInn in (select
    inn
from contractors0
where innIsCorrect = true)";
            CheckTranslate(mappings, source, expected);
        }
    }
}