using System.Linq;
using NUnit.Framework;
using Simple1C.Tests.Metadata1C.ПланыВидовХарактеристик;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests.Integration
{
    internal class CharacteristicsTest : COMDataContextTestBase
    {
        [Test]
        public void Test()
        {
            var разделыДатЗапредаИзменений = dataContext.Select<РазделыДатЗапретаИзменения>()
                .ToArray();
            Assert.That(разделыДатЗапредаИзменений.Length, Is.GreaterThan(0));
            var item = разделыДатЗапредаИзменений
                .SingleOrDefault(x => x.Наименование == "Бухгалтерский учет");
            Assert.That(item, Is.Not.Null);
            Assert.That(item.ТипЗначения, Is.EquivalentTo(new[] {typeof(Организации)}));
        }
    }
}