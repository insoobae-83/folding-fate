using System;
using System.Collections.Generic;

namespace FoldingFate.Core
{
    public class Entity
    {
        public string Id { get; }
        public EntityType Type { get; }
        public string DisplayName { get; }
        private readonly Dictionary<Type, IEntityComponent> _components = new();

        public Entity(string id, EntityType type, string displayName)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Type = type;
        }

        public void Add<T>(T component) where T : class, IEntityComponent
        {
            if (_components.TryGetValue(typeof(T), out var existing))
                existing.Owner = null;

            _components[typeof(T)] = component;
            component.Owner = this;
        }

        public T Get<T>() where T : class, IEntityComponent
        {
            _components.TryGetValue(typeof(T), out var component);
            return component as T;
        }

        public bool Has<T>() where T : class, IEntityComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        public bool Remove<T>() where T : class, IEntityComponent
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                component.Owner = null;
                return _components.Remove(typeof(T));
            }
            return false;
        }
    }
}
