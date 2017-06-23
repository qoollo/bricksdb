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
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public List<RestoreServer> RestoreServers { get; private set; }
        public RestoreStateHolder StateHolder { get; private set; }

        public RestoreState RestoreState { get; private set; }
        public RestoreType RestoreMode { get; private set; }

        public RestoreStateFileLogger(string filename, RestoreStateHolder stateHolder)
            : this(filename)
        {
            Contract.Requires(stateHolder != null);
            StateHolder = stateHolder;
        }

        public RestoreStateFileLogger(string filename)
        {
            Contract.Requires(!string.IsNullOrEmpty(filename));
            _filename = filename;
        }

        private readonly string _filename;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void SetRestoreDate(RestoreType mode, RestoreState state, List<RestoreServer> restoreServers)
        {
            _lock.EnterWriteLock();

            RestoreState = state;
            RestoreMode = mode;
            RestoreServers = restoreServers;

            _lock.ExitWriteLock();
        }

        public void Save()
        {
            _lock.EnterWriteLock();
            if(IsNeedSave())
                SaveInner();
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
                StateHolder = new RestoreStateHolder(load.State);
                RestoreState = load.State;
                RestoreMode = load.Mode;

                stream.Close();

                return true;
            }
            catch (FileNotFoundException e)
            {
                _logger.DebugFormat(e, "file name = {0}", _filename);
            }
            catch (System.Security.SecurityException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.DebugFormat(e, "file name = {0}", _filename);
            }
            catch (IOException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", _filename);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return false;
        }

        public bool IsNeedRestore()
        {
            return StateHolder.State != RestoreState.Restored && RestoreServers != null && RestoreServers.Count != 0;
        }

        private bool IsNeedSave()
        {
            return StateHolder.State != RestoreState.Restored;
        }

        private void SaveInner()
        {
            try
            {
                var save = new RestoreSaveHelper(RestoreMode, StateHolder.State, RestoreServers);
                var formatter = new XmlSerializer(save.GetType());
                var stream = new FileStream(_filename, FileMode.Create);
                formatter.Serialize(stream, save);
                stream.Close();
            }
            catch (FileNotFoundException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (System.Security.SecurityException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", _filename);
            }
            catch (IOException e)
            {
                _logger.ErrorFormat(e, "file name = {0}", _filename);
            }            
        }

        public void RemoveFile()
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

        public RestoreSaveHelper(RestoreType mode, RestoreState state, IEnumerable<RestoreServer> servers)
        {            
            State = state;
            Mode = mode;
            if (servers != null)
                RestoreServersSave = servers.Select(x => new RestoreServerSave(x)).ToList();
        }
        
        [DataMember]
        [XmlAttribute("RestoreState")]
        public RestoreState State { get; set; }

        [DataMember]
        [XmlAttribute("RestoreMode")]
        public RestoreType Mode { get; set; }

        [DataMember]
        [XmlArray("RestoreServers")]
        public List<RestoreServerSave> RestoreServersSave { get; set; }

        [XmlIgnore]
        public List<RestoreServer> RestoreServers { get { return RestoreServersSave.Select(x => x.Convert()).ToList(); }}
    }
}
