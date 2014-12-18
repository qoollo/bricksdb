using System;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules.Db.Exceptions;

namespace Qoollo.Impl.Modules.Db.Impl
{
    public abstract class DbReader<TReader>:ControlModule
    {
        public abstract TReader Reader { get; }

        public bool IsFail;

        protected DbReader()
        {
            IsFail = false;
        }

        public override void Start()
        {
            try
            {
                StartInner();
                IsFail = !IsValidRead();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
                IsFail = true;
            }
        }

        public object GetValue(int index)
        {
            if(IsFail)
                throw new ReaderIsFailException(Errors.DbReaderIsFail);

            try
            {
                return GetValueInner(index);
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");                
                throw new ReaderIsFailException(e.Message);
            }
        }

        public object GetValue(string index)
        {
            if (IsFail)
                throw new ReaderIsFailException(Errors.DbReaderIsFail);

            try
            {
                return GetValueInner(index);
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
                throw new ReaderIsFailException(e.Message);
            }
        }        

        public abstract bool IsCanRead { get; }

        public void ReadNext()
        {
            if (IsFail)
                throw new ReaderIsFailException(Errors.DbReaderIsFail);

            try
            {
                ReadNextInner();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
                throw new ReaderIsFailException(e.Message);
            }
        }

        public int CountFields()
        {
            if (IsFail)
                throw new ReaderIsFailException(Errors.DbReaderIsFail);

            try
            {
                return CountFieldsInner();
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
                throw new ReaderIsFailException(e.Message);
            }
        }

        protected abstract int CountFieldsInner();

        protected abstract void ReadNextInner();

        protected abstract object GetValueInner(int index);

        protected abstract object GetValueInner(string index);

        protected abstract bool IsValidRead();

        protected abstract void StartInner();
    }
}
