using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Collector.Comparer;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Tests.TestWriter
{
    internal class TestDbImplModule : DbImplModule<TestCommand, TestCommand, TestDbReader>
    {
        public List<int> Data = new List<int>();
        public List<TestCommand> Meta = new List<TestCommand>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public int Local
        {
            get
            {
                _lock.EnterReadLock();
                int count = Meta.Count(x => x.Local && !x.IsDeleted);
                _lock.ExitReadLock();
                return count;
            }
        }

        public int Deleted
        {
            get
            {
                _lock.EnterReadLock();
                int count = Meta.Count(x => x.IsDeleted);
                _lock.ExitReadLock();
                return count;
            }
        }

        public int Remote
        {
            get
            {
                _lock.EnterReadLock();
                int count = Meta.Count(x => !x.Local && !x.IsDeleted);
                _lock.ExitReadLock();
                return count;
            }
        }

        public int DeleteMeta = 0;
        public int Delete = 0;

        public override RemoteResult ExecuteNonQuery(TestCommand command)
        {
            _lock.EnterWriteLock();

            switch (command.Command)
            {
                case "create":
                    Data.Add(command.Key);
                    break;
                case "update":
                    if (!Data.Exists(x => x == command.Key))
                    {
                        Data.Add(command.Key);
                    }

                    break;
                case "updatemeta":
                    if (!Meta.Exists(x => x.Key == command.Key))
                    {
                        Meta.Add(new TestCommand { Key = command.Key, Local = command.Local });
                    }
                    else
                    {
                        int index = Meta.FindIndex(x => x.Key == command.Key);
                        Meta[index].IsDeleted = command.IsDeleted;
                        Meta[index].Local = command.Local;
                    }
                    break;
                case "delete":
                    Data.Remove(command.Key);
                    Delete++;
                    break;
                case "createmeta":
                    Meta.Add(new TestCommand {Key = command.Key, Local = command.Local, Hash = command.Hash});
                    break;
                case "deletemeta":
                    Meta.RemoveAll(x => x.Key == command.Key);
                    DeleteMeta++;
                    break;
                case "setdatadeleted":
                    Meta.Find(x => x.Key == command.Key).IsDeleted = true;
                    Meta.Find(x => x.Key == command.Key).DeleteTime = DateTime.Now;
                    break;
                case "setdatanotdeleted":
                    Meta.Find(x => x.Key == command.Key).IsDeleted = false;
                    break;
                case "metaselect":
                    Meta.Find(x => x.Key == command.Key).IsDeleted = false;
                    break;
            }
            _lock.ExitWriteLock();

            return new SuccessResult();
        }

        public override DbReader<TestDbReader> CreateReader(TestCommand command)
        {
            DbReader<TestDbReader> ret = null;
            _lock.EnterWriteLock();

            if (command.Command.Contains("ReadWithDeleteAndLocal"))
            {
                var split = command.Command.Split(new[] { "%" }, StringSplitOptions.RemoveEmptyEntries);
                command.Command = "asc2";
                command.Local = bool.Parse(split[1]);
                command.IsDeleted = bool.Parse(split[2]);
            }

            switch (command.Command)
            {
                case "readMeta":
                case "read":
                    var meta1 = Meta.Find(x => x.Hash == command.Hash && x.IsDeleted == command.IsDeleted);
                    ret = meta1 == null
                        ? new TestDbReader(new List<int>(), new List<TestCommand>())
                        : new TestDbReader(command.Key, Meta.Find(x => x.Hash == command.Hash));
                    break;
                case "ReadAllElementsAndMergeWhereStatemenetForKey":
                    var meta = Meta.Where(x =>
                    {
                        if (command.Support == 10)
                            return x.Key == command.Key && x.IsDeleted == command.IsDeleted;

                        if (command.Local)
                        {
                            if (x.IsDeleted == command.IsDeleted)
                                return true;
                            return false;
                        }

                        if (!x.Local && x.IsDeleted == command.IsDeleted)
                            return true;
                        return false;
                    }).ToList();
                    var data = Data.Where(x => meta.Exists(w => w.Key == x)).ToList();
                    ret = new TestDbReader(data, meta);
                    break;
                case "desc":
                    var meta2 = Meta.FindAll(x => x.IsDeleted == command.IsDeleted).ToList();
                    meta2.Sort((x, y) => IntComparer.Compare(x.Key, y.Key));
                    var data2 = Data.Where(x => meta2.Exists(w => w.Key == x)).ToList();
                    ret = new TestDbReader(data2, meta2, true);
                    break;
                case "asc":
                    var meta3 = Meta.FindAll(x => x.IsDeleted == command.IsDeleted).ToList();
                    meta3.Sort((x, y) => IntComparer.Compare(x.Key, y.Key));
                    var data3 = Data.Where(x => meta3.Exists(w => w.Key == x)).ToList();
                    ret = new TestDbReader(data3, meta3, false, true);
                    break;
                case "asc2":
                    var meta4 = Meta.FindAll(x => x.IsDeleted == command.IsDeleted && x.Local == command.Local).ToList();
                    meta4.Sort((x, y) => -IntComparer.Compare(x.Key, y.Key));
                    var data4 = Data.Where(x => meta4.Exists(w => w.Key == x)).ToList();
                    ret = new TestDbReader(data4, meta4, false, true);
                    break;
            }

            _lock.ExitWriteLock();
            return ret;
        }

        public override RentedElementMonitor<TestCommand> RentConnectionInner()
        {
            throw new Exception();
        }
    }
}
