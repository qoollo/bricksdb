using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Common.HashFile
{
    internal class HashMap:ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public List<HashMapRecord> Map { get; protected set; }
        public List<WriterDescription> Servers { get; }
        public List<HashMapRecord> AvailableMap { get; private set; }
        public string Filename { get; private set; }

        public HashMap(StandardKernel kernel, HashFileType type)
            :base(kernel)
        {
            _type = type;
            Map = new List<HashMapRecord>();    
            AvailableMap = new List<HashMapRecord>();
            Servers = new List<WriterDescription>();
        }

        public HashMap(StandardKernel kernel, HashFileType type, string filename)
            :this(kernel, type)
        {
            Filename = filename;
        }

        private readonly HashFileType _type;
        private int _countReplic;

        public override void Start()
        {
            var config = Kernel.Get<ICommonConfiguration>();
            _countReplic = config.CountReplics;
            Filename = config.HashFilename;
        }

        #region Start work

        public void CreateMap()
        {
            Map.Clear();
            Servers.Clear();
            AvailableMap.Clear();

            ReadFromFile();
            PrepareServerList();
            CreateAvailableMap();
        }        

        public void CreateMapFromDistributor(List<Tuple<ServerId, string, string>> servers)
        {
            Map.Clear();
            Servers.Clear();
            AvailableMap.Clear();

            Map = new List<HashMapRecord>(servers.Select(x =>
                new HashMapRecord(x.Item2, x.Item3)
                {
                    Save = new SavedServerId(x.Item1.RemoteHost, x.Item1.Port, x.Item1.Port)
                }
                ));

            PrepareServerList();
            CreateAvailableMap();
        }       

        private void ReadFromFile()
        {
            try
            {
                var formatter = new XmlSerializer(Map.GetType());                
                var stream = new FileStream(Filename, FileMode.Open);
                Map =  (List<HashMapRecord>) formatter.Deserialize(stream);
                stream.Close();
            }
            catch (FileNotFoundException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
            catch (System.Security.SecurityException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
            catch (IOException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
        }

        protected void PrepareServerList()
        {
            foreach (var record in Map)
            {                
                record.Prepare(_type);
                var s = Servers.FirstOrDefault(x => x.Equals(record.ServerId));
                if(s==null)
                    Servers.Add(record.ServerId);
                else
                    record.SetServer(s);
            }
        }

        protected void CreateNewFile()
        {
            try
            {
                var formatter = new XmlSerializer(Map.GetType());
                var stream = new FileStream(Filename, FileMode.Create);
                formatter.Serialize(stream, Map);
                stream.Close();
            }
            catch (FileNotFoundException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
            catch (System.Security.SecurityException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
            catch (IOException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", Filename);
            }
        }

        public void CreateNewMapWithFile(List<HashMapRecord> servers)
        {
            Map = servers;
            CreateNewFile();
            PrepareServerList();
        }

        #endregion

        #region Public

        public void UpdateFileModel()
        {
            ReadFromFile();
            Servers.Clear();
            PrepareServerList();
            CreateAvailableMap();
        }

        public void CreateAvailableMap()
        {
            AvailableMap = new List<HashMapRecord>(Map.Where(x => x.ServerId.IsAvailable));
        }

        public List<ServerId> GetUnavailableMap()
        {
            return
                new List<ServerId>(Map.Where(x => !x.ServerId.IsAvailable).Select(x => x.ServerId)).Distinct().ToList();
        }

        public List<ServerId> GetAvailableMap()
        {
            return
                new List<ServerId>(Map.Where(x => x.ServerId.IsAvailable).Select(x => x.ServerId)).Distinct().ToList();
        }

        public List<HashMapRecord> GetHashMap(ServerId server)
        {
            var ret = new List<HashMapRecord>();

            for (int i = 0; i < Map.Count; i++)
            {
                if (Map[i].Save.Equals(server))
                {

                    var list = Copy(i, _countReplic);

                    var intersect = ret.Intersect(list);
                    if (intersect.Count() != 0)
                    {
                        _logger.Error("Need more servres to store data");
                    }
                    ret.AddRange(list);
                }
            }

            return ret;            
        }

        private List<HashMapRecord> Copy(int pos, int count)
        {
            var ret = new List<HashMapRecord>();
            while (count != 0)
            {
                ret.Add(Map[pos]);
                pos = pos - 1 < 0 ? Map.Count - 1 : pos - 1;
                count--;
            }
            return ret;
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            //TODO save to file
        }
    }
}
