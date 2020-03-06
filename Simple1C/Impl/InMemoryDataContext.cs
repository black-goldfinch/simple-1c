using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    public class InMemoryDataContext : IDataContext
    {
        private readonly Dictionary<Type, InMemoryEntityCollection> committed =
            new Dictionary<Type, InMemoryEntityCollection>();

        private readonly TypeRegistry typeRegistry;

        public InMemoryDataContext(Assembly mappingsAssembly)
        {
            typeRegistry = new TypeRegistry(mappingsAssembly);
        }

        public Type GetTypeOrNull(string configurationName)
        {
            return typeRegistry.GetTypeOrNull(configurationName);
        }

        public int GetRevision(Type type)
        {
            InMemoryEntityCollection collection;
            return committed.TryGetValue(type, out collection) ? collection.revision : 0;
        }

        public IQueryable<T> Select<T>(string sourceName = null)
        {
            Func<Type, InMemoryEntity, object> entityCreator;
            if (typeof(Abstract1CEntity).IsAssignableFrom(typeof(T)))
                entityCreator = CreateAbstractEntity;
            else if (typeof(Constant).IsAssignableFrom(typeof(T)))
                entityCreator = CreateConstantEntity;
            else
                throw new InvalidOperationException($"invalid entity type [{typeof(T)}]");

            return Collection(typeof(T)).list
                .Select(x => (T) entityCreator(typeof(T), x))
                .AsQueryable();
        }

        private static object CreateAbstractEntity(Type type, InMemoryEntity entity)
        {
            var result = (Abstract1CEntity) FormatterServices.GetUninitializedObject(type);
            result.Controller = new EntityController(entity.revision);
            return result;
        }

        private static object CreateConstantEntity(Type type, InMemoryEntity entity)
        {
            var result = (Constant) FormatterServices.GetUninitializedObject(type);
            result.ЗначениеНетипизированное =
                entity.revision.TryLoadValue(nameof(result.ЗначениеНетипизированное), typeof(object), out var item)
                    ? item
                    : throw new InvalidOperationException($"Constant [{type.Name}] value is empty");
            return result;
        }

        public void Save(object entity)
        {
            if (entity is IEnumerable collection)
            {
                foreach (var item in collection)
                {
                    try
                    {
                        SaveEntity(item);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException(
                            $"Error when saving entity [{item.GetType().FormatName()}] of collection", e);
                    }
                }
            }
            else SaveEntity(entity);
        }

        private void SaveEntity(object entity)
        {
            switch (entity)
            {
                case null:
                    throw new InvalidOperationException("entity is null");
                case Abstract1CEntity abstract1CEntity:
                    var entitiesToSave = new List<Abstract1CEntity>();
                    abstract1CEntity.Controller.PrepareToSave(abstract1CEntity, entitiesToSave);
                    foreach (var e in entitiesToSave)
                        SaveAbstract(e, false);
                    break;
                case Constant constant:
                    SaveConstant(constant);
                    break;
                default:
                    const string messageFormat = "invalid entity type [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        entity.GetType().FormatName()));
            }
        }

        private InMemoryEntity SaveConstant(Constant entity)
        {
            var entityType = entity.GetType();

            var collection = Collection(entityType);
            if (collection.list.Count > 1)
                throw new InvalidOperationException($"Whoops, constant [{entityType}] was duplicated");

            var inMemoryEntity = new InMemoryEntity
            {
                entityType = entityType
            };

            var existingItem = collection.list.SingleOrDefault();
            inMemoryEntity.revision =
                new InMemoryEntityRevision(inMemoryEntity, existingItem?.revision, new Dictionary<string, object>
                {
                    [nameof(Constant.ЗначениеНетипизированное)] = entity.ЗначениеНетипизированное
                });

            collection.list.Clear();
            collection.list.Add(inMemoryEntity);
            collection.revision++;
            return inMemoryEntity;
        }

        private InMemoryEntity SaveAbstract(Abstract1CEntity entity, bool isTableSection)
        {
            if (entity == null)
                return null;

            var changed = entity.Controller.Changed;
            if (changed != null)
            {
                var keys = changed.Keys.ToArray();
                foreach (var k in keys)
                {
                    var abstract1CEntity = changed[k] as Abstract1CEntity;
                    if (abstract1CEntity != null)
                    {
                        changed[k] = SaveAbstract(abstract1CEntity, false);
                        continue;
                    }

                    var list = changed[k] as IList;
                    if (list != null)
                    {
                        changed[k] = ConvertList(list);
                        continue;
                    }

                    var syncList = changed[k] as SyncList;
                    if (syncList != null)
                        changed[k] = ConvertList(syncList.Current);
                }
            }

            InMemoryEntity inMemoryEntity;
            if (!entity.Controller.IsNew)
            {
                var inmemoryEntityRevision = (InMemoryEntityRevision) entity.Controller.ValueSource;
                inMemoryEntity = inmemoryEntityRevision.inMemoryEntity;
                if (changed != null)
                {
                    if (!isTableSection)
                    {
                        var configurationName = ConfigurationName.Get(entity.GetType());
                        if (configurationName.Scope == ConfigurationScope.Документы)
                        {
                            AssignNewGuid(entity, changed, "ВерсияДанных");
                        }
                    }

                    inMemoryEntity.revision =
                        new InMemoryEntityRevision(inMemoryEntity, inmemoryEntityRevision, changed);
                    Collection(entity.GetType()).revision++;
                }
            }
            else
            {
                if (changed == null)
                    changed = new Dictionary<string, object>();
                inMemoryEntity = new InMemoryEntity();
                var revision = new InMemoryEntityRevision(inMemoryEntity, null, changed);
                inMemoryEntity.entityType = entity.GetType();
                inMemoryEntity.revision = revision;
                if (!isTableSection)
                {
                    var configurationName = ConfigurationName.Get(entity.GetType());
                    if (configurationName.Scope == ConfigurationScope.Справочники)
                    {
                        AssignNewGuid(entity, changed, "Код");
                        AssignNewGuid(entity, changed, "ВерсияДанных");
                    }
                    else if (configurationName.Scope == ConfigurationScope.Документы)
                    {
                        AssignNewGuid(entity, changed, "Номер");
                        AssignNewGuid(entity, changed, "ВерсияДанных");
                    }

                    if (entity.Controller.IsNew && configurationName.HasReference)
                    {
                        var idProperty = entity.GetType().GetProperty(EntityHelpers.idPropertyName);
                        if (idProperty == null)
                            throw new InvalidOperationException("assertion failure");
                        AssignValue(entity, changed, idProperty, Guid.NewGuid());
                    }

                    var inMemoryEntityCollection = Collection(entity.GetType());
                    inMemoryEntityCollection.revision++;
                    inMemoryEntityCollection.list.Add(inMemoryEntity);
                }
            }

            entity.Controller.ResetDirty(inMemoryEntity.revision);
            return inMemoryEntity;
        }

        private IList ConvertList(IList newList)
        {
            var result = new List<InMemoryEntity>(newList.Count);
            foreach (var l in newList)
                result.Add(SaveAbstract((Abstract1CEntity) l, true));
            return result;
        }

        private static void AssignNewGuid(Abstract1CEntity target, Dictionary<string, object> committed,
            string property)
        {
            if (committed.ContainsKey(property))
                return;
            var codeProperty = target.GetType().GetProperty(property);
            if (codeProperty == null)
                return;
            AssignValue(target, committed, codeProperty, Guid.NewGuid().ToString());
        }

        private static void AssignValue(Abstract1CEntity target, Dictionary<string, object> committed,
            PropertyInfo property, object value)
        {
            committed[property.Name] = value;
            var oldTrackChanges = target.Controller.TrackChanges;
            target.Controller.TrackChanges = false;
            try
            {
                property.SetMethod.Invoke(target, new[] {value});
            }
            finally
            {
                target.Controller.TrackChanges = oldTrackChanges;
            }
        }

        private InMemoryEntityCollection Collection(Type type)
        {
            return committed.GetOrAdd(type, t => new InMemoryEntityCollection {list = new List<InMemoryEntity>()});
        }

        private class InMemoryEntityRevision : IValueSource
        {
            public readonly InMemoryEntity inMemoryEntity;
            private readonly InMemoryEntityRevision previous;
            private readonly Dictionary<string, object> properties;

            public InMemoryEntityRevision(InMemoryEntity inMemoryEntity, InMemoryEntityRevision previous,
                Dictionary<string, object> properties)
            {
                this.inMemoryEntity = inMemoryEntity;
                this.previous = previous;
                this.properties = properties;
                Writable = true;
            }

            public object GetBackingStorage()
            {
                return inMemoryEntity;
            }

            public bool Writable { get; private set; }

            public bool TryLoadValue(string name, Type type, out object result)
            {
                result = null;
                var rev = this;
                while (rev != null)
                {
                    if (rev.properties.TryGetValue(name, out result))
                    {
                        result = Convert(type, result);
                        return true;
                    }

                    rev = rev.previous;
                }

                return false;
            }

            private static object Convert(Type type, object value)
            {
                if (type == typeof(object))
                {
                    if (value is InMemoryEntity entity)
                    {
                        return typeof(Abstract1CEntity).IsAssignableFrom(entity.entityType)
                            ? CreateAbstractEntity(entity.entityType, entity)
                            : value;
                    }

                    return value;
                }

                if (typeof(IList).IsAssignableFrom(type))
                {
                    var oldList = (IList) value;
                    var itemType = type.GetGenericArguments()[0];
                    var newList = ListFactory.Create(itemType, null, oldList.Count);
                    foreach (InMemoryEntity l in oldList)
                        newList.Add(CreateAbstractEntity(itemType, l));
                    return newList;
                }

                return typeof(Abstract1CEntity).IsAssignableFrom(type) && value != null
                    ? CreateAbstractEntity(type, (InMemoryEntity) value)
                    : value;
            }
        }

        private class InMemoryEntity
        {
            public Type entityType;
            public InMemoryEntityRevision revision;
        }

        private class InMemoryEntityCollection
        {
            public List<InMemoryEntity> list;
            public int revision;
        }
    }
}