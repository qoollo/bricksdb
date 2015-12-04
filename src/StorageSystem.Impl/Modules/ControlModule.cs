using System;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.Modules
{
    internal abstract class FunctionHandlerBase
    {
        public TResult Execute<TValue, TResult>(TValue value)
        {
            return (TResult) Execute((object)value);
        }

        public void Execute<TValue>(TValue value)
        {
            Execute((object)value);
        }

        protected abstract object Execute(object value);
    }

    internal class FunctionHandlerSync<TValue, TResult> : FunctionHandlerBase
    {
        private readonly Func<TValue, TResult> _func;        

        public FunctionHandlerSync(Func<TValue, TResult> func)
        {
            _func = func;
        }

        public FunctionHandlerSync(Func<TResult> func)
            : this(value => func())
        {
        }

        public FunctionHandlerSync(Action<TValue> func, Func<TResult> defaultResult)
            : this(value =>
            {
                func(value);
                return defaultResult();
            })
        {
        }

        public FunctionHandlerSync(Action func, Func<TResult> defaultResult)
            : this(value =>
            {
                func();
                return defaultResult();
            })
        {
        }

        public TResult Execute(TValue value)
        {

            return _func(value);
        }

        protected override object Execute(object value)
        {
            return Execute(value is TValue ? (TValue) value : default(TValue));
        }
    }

    internal class FunctionHandlerAsync<TValue> : FunctionHandlerBase
    {
        private readonly Action<TValue> _action;

        public FunctionHandlerAsync(Action<TValue> action)
        {
            _action = action;
        }

        public void Execute(TValue value)
        {
            _action(value);
        }

        protected override object Execute(object value)
        {
            Execute(value is TValue ? (TValue) value : default(TValue));
            return null;
        }
    }

    internal abstract class QueueHandlerBase
    {
        public string Name { set; get; }
        private readonly List<Type> _types = new List<Type>();

        public bool IsInnerType(Type type)
        {
            return _types.Contains(type);
        }

        public void RegistrateSync<TData>(Action<TData> action)
        {
            RegistrateInner(typeof(TData), new FunctionHandlerAsync<TData>(action));
            _types.Add(typeof(TData));
        }

        protected abstract void RegistrateInner(Type type, FunctionHandlerBase handler);
        public abstract void Start(bool isForceStart = false, QueueConfiguration configuration = null);
        public TResult Execute<TValue, TResult>(TValue value)
        {
            return (TResult)Execute(value);
        }

        protected abstract object Execute<TValue>(TValue value);
    }

    internal class QueueHandler<TBase, TResult>:QueueHandlerBase where TBase : class
    {
        private readonly Dictionary<Type, FunctionHandlerBase> _funcs = new Dictionary<Type, FunctionHandlerBase>();
        private readonly QueueWithParam<TBase> _queue;
        private readonly Func<TResult> _defaultResult;

        public QueueHandler(QueueWithParam<TBase> queue, Func<TResult> defaultResult)
        {
            _queue = queue;
            _defaultResult = defaultResult;
        }

        private void Process(TBase value)
        {
            FunctionHandlerBase handler;
            if (_funcs.TryGetValue(value.GetType(), out handler))
                handler.Execute(value);
        }

        public override void Start(bool isForceStart = false, QueueConfiguration configuration = null)
        {
            if (isForceStart)
                _queue.RegistrateWithStart(configuration, Process);
            else if (configuration != null)
                _queue.Registrate(configuration, Process);
            else
                _queue.Registrate(Process);
        }

        protected override void RegistrateInner(Type type, FunctionHandlerBase handler)
        {
            _funcs.Add(type, handler);
        }        

        protected override object Execute<TValue>(TValue value)
        {
            _queue.Add(value as TBase);
            return _defaultResult();
        }
    }

    public abstract class ControlModule:IDisposable
    {
        private readonly Dictionary<Type, FunctionHandlerBase> _sync = new Dictionary<Type, FunctionHandlerBase>();
        private readonly Dictionary<string, QueueHandlerBase>  _async = new Dictionary<string, QueueHandlerBase>();

        internal void RegistrateSync<TValue, TResult>(Func<TValue, TResult> func)
        {
            _sync.Add(typeof (TValue), new FunctionHandlerSync<TValue, TResult>(func));
        }

        internal void RegistrateSync<TValue, TResult>(Func<TResult> func)
        {
            _sync.Add(typeof(TValue), new FunctionHandlerSync<TValue, TResult>(func));
        }

        internal void RegistrateSync<TValue, TResult>(Action<TValue> action, Func<TResult> defaultResult)
        {
            _sync.Add(typeof(TValue), new FunctionHandlerSync<TValue, TResult>(action, defaultResult));
        }

        internal void RegistrateSync<TValue, TResult>(Action action, Func<TResult> defaultResult)
        {
            _sync.Add(typeof(TValue), new FunctionHandlerSync<TValue, TResult>(action, defaultResult));
        }

        internal void RegistrateAsync<TValue, TBase, TResult>(QueueWithParam<TBase> queue, Action<TValue> action,
            Func<TResult> defaultResult = null) where TValue:TBase where TBase : class
        {            
            QueueHandlerBase handler;
            if (_async.TryGetValue(queue.Name, out handler))
                handler.RegistrateSync(action);
            else
            {
                if(defaultResult == null)
                    throw new NullReferenceException("Default result mustn't be null");

                handler = new QueueHandler<TBase, TResult>(queue, defaultResult);
                handler.RegistrateSync(action);
                _async.Add(queue.Name, handler);
            }
        }

        internal TResult Execute<TValue, TResult>(TValue value)
            where  TValue:class
        {
            FunctionHandlerBase handler;
            if (_sync.TryGetValue(value.GetType(), out handler))
                return handler.Execute<TValue, TResult>(value);
         
            return ExecuteAsync<TValue, TResult>(value);
        }

        internal TResult ExecuteSync<TValue, TResult>(TValue value)
            where TValue : class
        {
            FunctionHandlerBase handler;
            if (_sync.TryGetValue(value.GetType(), out handler))
                return handler.Execute<TValue, TResult>(value);

            throw new NotImplementedException();
        }

        internal TResult ExecuteAsync<TValue, TResult>(TValue value)
            where TValue : class
        {
            var handler = _async.Values.FirstOrDefault(x => x.IsInnerType(value.GetType()));
            if (handler != null)
                return handler.Execute<TValue, TResult>(value);
            
            throw new NotImplementedException();
        }

        internal void StartAsync(QueueConfiguration configuration = null)
        {
            _async.Values.ToList().ForEach(x => x.Start(false, configuration));
        }

        public virtual void Build()
        {
        }

        public virtual void Start()
        {            
        }

        protected virtual void Dispose(bool isUserCall)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
