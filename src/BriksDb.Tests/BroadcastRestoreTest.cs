﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    public class BroadcastRestoreTest:TestBase
    {
        private InnerData InnerData(int i)
        {
            var ev = new InnerData(new Transaction(
                HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "default")
            {
                OperationName = OperationName.Create,
                OperationType = OperationType.Async
            })
            {
                Data = CommonDataSerializer.Serialize(i),
                Key = CommonDataSerializer.Serialize(i),
                Transaction = { Distributor = new ServerId("localhost", distrServer1) }
            };
            ev.Transaction.TableName = "Int";
            return ev;
        }

        //[Theory]
        //[InlineData(50)]
        public void Writer_CheckData_TwoServers(int count)
        {
            var filename = nameof(Writer_CheckData_TwoServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            {
                CreateHashFile(filename, 2);

                _distrTest.Build(1, distrServer1, distrServer12, filename);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);

                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, 1);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

                    _distrTest.Input.ProcessAsync(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                _writer2.Distributor.Restore(RestoreState.BroadcastRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }
    }
}