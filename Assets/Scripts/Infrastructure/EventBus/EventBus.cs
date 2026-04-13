using System;
using System.Collections.Generic;
using R3;

namespace FoldingFate.Infrastructure.EventBus
{
    public class EventBus : IDisposable
    {
        private readonly Dictionary<Type, object> _subjects = new();

        public void Publish<T>(T message)
        {
            if (_subjects.TryGetValue(typeof(T), out var subject))
            {
                ((Subject<T>)subject).OnNext(message);
            }
        }

        public Observable<T> Receive<T>()
        {
            if (!_subjects.TryGetValue(typeof(T), out var subject))
            {
                subject = new Subject<T>();
                _subjects[typeof(T)] = subject;
            }
            return ((Subject<T>)subject).AsObservable();
        }

        public void Dispose()
        {
            foreach (var subject in _subjects.Values)
            {
                if (subject is IDisposable disposable)
                    disposable.Dispose();
            }
            _subjects.Clear();
        }
    }
}