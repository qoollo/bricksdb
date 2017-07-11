using System.IO;
using Qoollo.Impl.Modules.Config;
using Qoollo.Tests.Support;
using Xunit;

namespace Qoollo.Tests
{
    public class ConfigTests:TestBase
    {

        private void CreateFile(string filename)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine($@"{{ {GetQueue()} }}");
            }
        }

        private string GetQueue()
        {
            return $@"""queue"": {{ 
    {GetSingle("writerdistributor")},
    {GetSingle("WriterInput")},
    {GetSingle("WriterInputRollback")},
    {GetSingle("WriterRestore")},
    {GetSingle("WriterRestorePackage")},
    {GetSingle("WriterTimeout")},
    {GetSingle("WriterTransactionAnswer")},

    {GetSingle("DistributorDistributor")},
    {GetSingle("DistributorTransaction")},
    {GetSingle("DistributorTransactionCallback")},

    {GetSingle("ProxyDistributor")},
    {GetSingle("ProxyInput")},
    {GetSingle("ProxyInputOther")},
        }} ";
        }

        private string GetSingle(string name)
        {
            return $@"""{name.ToLower()}"": {{ {GetParam("countthreads", 1)}, {GetParam("maxsize", 1)} }}";
        }

        private string GetParam(string name, object value)
        {
            var strValue = value.ToString();
            if (value is string)
                strValue = $@"""{strValue}""";
            return $@"""{name}"":{strValue}";
        }

        [Fact]
        public void SomeTest()
        {
            var filename = nameof(SomeTest) + ".json";
            using (new FileCleaner(filename, true))
            {
                CreateFile(filename);
                var settings = new SettingsModule(_kernel, filename);
                settings.Start();
            }            
        }
    }
}