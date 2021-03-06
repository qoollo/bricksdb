﻿using System;
using System.Diagnostics.Contracts;
using System.Threading;
using Qoollo.Impl.Common.Support;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Modules.Pools
{
    internal class CommonPool<T> : DynamicPoolManager<T> where T : class
    {
        private CreateElementDelegate<T> _createElementDelegate;
        private Func<T, bool> _isValidElementFunc;
        private Action<T> _destroyElementAction;

        public CommonPool(CreateElementDelegate<T> createElementDelegate, Func<T, bool> isValidElemetFunc,
            Action<T> destroyElementAction, int maxElemCount, int trimPeriod, string name)
            : base(1, maxElemCount, name, trimPeriod)
        {
            Contract.Requires(createElementDelegate != null);
            Contract.Requires(isValidElemetFunc != null);
            Contract.Requires(destroyElementAction!=null);
            _createElementDelegate = createElementDelegate;
            _isValidElementFunc = isValidElemetFunc;
            _destroyElementAction = destroyElementAction;
        }

        protected override bool CreateElement(out T elem, int timeout, CancellationToken token)
        {
            return _createElementDelegate(out elem, timeout, token);
        }

        protected override bool IsValidElement(T elem)
        {
            return _isValidElementFunc(elem);
        }

        protected override void DestroyElement(T elem)
        {
            _destroyElementAction(elem);
        }
    }
}
