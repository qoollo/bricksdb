using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.Common.HashFile
{
    internal class HashMap:IDisposable
    {
        public List<HashMapRecord> Map { get; protected set; }
        public List<ControllerDescription> Servers { get; private set; }
        public List<HashMapRecord> AvailableMap { get; private set; } 
        private HashMapConfiguration _configuration;

        public HashMap(HashMapConfiguration configuration)
        {
            Contract.Requires(configuration!=null);
            _configuration = configuration;
            Map = new List<HashMapRecord>(configuration.CountSlices);    
            AvailableMap = new List<HashMapRecord>();
            Servers = new List<ControllerDescription>();
        }

        #region Start work

        public void CreateMap()
        {
            Map.Clear();
            Servers.Clear();
            AvailableMap.Clear();

            switch (_configuration.Mode)
            {
                case HashMapCreationMode.CreateNew:
                    CreateNewMap();
                    CreateNewFile();
                    PrepareServerList();
                    break;
                case HashMapCreationMode.ReadFromFile:
                    ReadFromFile();
                    PrepareServerList();
                    CreateAvailableMap();
                    break;
            }
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

        private void CreateNewMap()
        {
            var maxVal = BigInteger.Parse("100000000000000000000000", NumberStyles.HexNumber) * 0x10 - 1;
            var minVal = BigInteger.Parse("0", NumberStyles.HexNumber);
            var step = maxVal / _configuration.CountSlices;
            BigInteger current;
            string min, cur;

            for (int i = 0; i < _configuration.CountSlices - 1; i++)
            {
                current = minVal + step;

                min = i == 0 ? "000000000000000000000000" : minVal.ToString("x2");
                cur = current.ToString("x2");

                Map.Add(new HashMapRecord(min, cur));

                minVal = current;
            }

            min = _configuration.CountSlices == 1 ? "000000000000000000000000" : minVal.ToString("x2");
            cur = maxVal.ToString("x2");

            Map.Add(new HashMapRecord(min, cur));
        }

        protected void CreateNewFile()
        {
            try
            {
                var formatter = new XmlSerializer(Map.GetType());
                //IFormatter formatter = new BinaryFormatter();
                var stream = new FileStream(_configuration.Filename, FileMode.Create);
                formatter.Serialize(stream, Map);
                stream.Close();
            }
            catch (FileNotFoundException e)
            {
                Logger.Logger.Instance.ErrorFormat(e,"file name = {0}", _configuration.Filename);
            }
            catch (System.Security.SecurityException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
            catch (IOException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
        }

        private void ReadFromFile()
        {
            try
            {
                var formatter = new XmlSerializer(Map.GetType());                
                var stream = new FileStream(_configuration.Filename, FileMode.Open);
                Map =  (List<HashMapRecord>) formatter.Deserialize(stream);
                stream.Close();
            }
            catch (FileNotFoundException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
            catch (System.Security.SecurityException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
            catch (IOException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _configuration.Filename);
            }
        }

        private void PrepareServerList()
        {
            foreach (var record in Map)
            {                
                record.Prepare(_configuration.Type);
                var s = Servers.FirstOrDefault(x => x.Equals(record.ServerId));
                if(s==null)
                    Servers.Add(record.ServerId);
                else
                    record.SetServer(s);
            }
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

        public List<HashMapRecord> GetLocalMap(ServerId server)
        {
            var ret = new List<HashMapRecord>();

            for (int i = 0; i < Map.Count; i++)
            {
                if (Map[i].Save.Equals(server))
                {

                    var list = Copy(i, _configuration.CountReplics);

                    var intersect = ret.Intersect(list);
                    if(intersect.Count()!=0)
                    {
                        //TODO в системен не хватает серверов для полного хранения реплик
                        Logger.Logger.Instance.Error("Need more servres to store data");
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

        public void Dispose()
        {
            //TODO save to file
        }
    }
}
