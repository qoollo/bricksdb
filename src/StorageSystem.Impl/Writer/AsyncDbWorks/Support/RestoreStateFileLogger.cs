using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreStateFileLogger
    {
        public string TableName { get; private set; }
        public bool IsModelUpdate { get; private set; }
        public List<RestoreServer> RestoreServers { get; private set; }
        public RestoreStateHelper StateHelper { get; private set; }

        public RestoreStateFileLogger(string filename, RestoreStateHelper stateHelper, string tableName,
            bool isModelUpdate, List<RestoreServer> restoreServers) : this(filename)
        {
            Contract.Requires(stateHelper != null);            
            TableName = tableName;
            IsModelUpdate = isModelUpdate;            
            StateHelper = stateHelper;
            RestoreServers = restoreServers;
        }

        public RestoreStateFileLogger(string filename, RestoreStateHelper stateHelper)
            : this(filename)
        {
            Contract.Requires(stateHelper != null);
            StateHelper = stateHelper;
        }

        public RestoreStateFileLogger(string filename)
        {
            Contract.Requires(!string.IsNullOrEmpty(filename));
            _filename = filename;
        }

        private readonly string _filename;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void SetRestoreDate(string tableName, bool isModelUpdate, List<RestoreServer> restoreServers)
        {
            _lock.EnterWriteLock();

            TableName = tableName;
            IsModelUpdate = isModelUpdate;            
            RestoreServers = restoreServers;
            
            _lock.ExitWriteLock();
        }

        public void Save()
        {
            _lock.EnterWriteLock();
            if(IsNeedRestore())
                SaveInner();
            else
                RemoveFile();
            _lock.ExitWriteLock();
        }

        public bool Load()
        {
            _lock.EnterWriteLock();
            try
            {
                var formatter = new XmlSerializer(typeof (RestoreSaveHelper));
                var stream = new FileStream(_filename, FileMode.Open);
                var load = (RestoreSaveHelper) formatter.Deserialize(stream);

                RestoreServers = load.RestoreServers;
                StateHelper = new RestoreStateHelper(load.State);
                TableName = load.TableName;
                IsModelUpdate = load.IsModelUpdate;

                stream.Close();

                return true;
            }
            catch (FileNotFoundException e)
            {
                Logger.Logger.Instance.DebugFormat(e, "file name = {0}", _filename);
            }
            catch (System.Security.SecurityException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Logger.Instance.DebugFormat(e, "file name = {0}", _filename);
            }
            catch (IOException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _filename);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return false;
        }

        public bool IsNeedRestore()
        {
            return StateHelper.State != RestoreState.Restored && (RestoreServers != null && RestoreServers.Count != 0);
        }

        private void SaveInner()
        {
            try
            {
                var save = new RestoreSaveHelper(StateHelper.State, RestoreServers, IsModelUpdate, TableName);
                var formatter = new XmlSerializer(save.GetType());
                var stream = new FileStream(_filename, FileMode.Create);
                formatter.Serialize(stream, save);
                stream.Close();
            }
            catch (FileNotFoundException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (System.Security.SecurityException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (DirectoryNotFoundException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (IOException e)
            {
                Logger.Logger.Instance.ErrorFormat(e, "file name = {0}", _filename);
            }            
        }

        private void RemoveFile()
        {
            File.Delete(_filename);
        }        
    }

    [Serializable]
    [DataContract]
    public class RestoreSaveHelper
    {        
        public RestoreSaveHelper()
        {
        }

        public RestoreSaveHelper(RestoreState state, IEnumerable<RestoreServer> servers, bool isModelUpdate,
            string tableName)
        {
            IsModelUpdate = isModelUpdate;
            TableName = tableName;
            State = state;
            if (servers != null)
                RestoreServersSave = servers.Select(x => new RestoreServerSave(x)).ToList();
        }

        [DataMember]
        [XmlAttribute("IsModelUpdate")]
        public bool IsModelUpdate { get; set; }
        
        [DataMember]
        [XmlAttribute("TableName")]
        public string TableName { get; set; }

        [DataMember]
        [XmlAttribute("RestoreState")]
        public RestoreState State { get; set; }

        [DataMember]
        [XmlArray("RestoreServers")]
        public List<RestoreServerSave> RestoreServersSave { get; set; }

        [XmlIgnore]
        public List<RestoreServer> RestoreServers { get { return RestoreServersSave.Select(x => x.Convert()).ToList(); }}
    }
}
