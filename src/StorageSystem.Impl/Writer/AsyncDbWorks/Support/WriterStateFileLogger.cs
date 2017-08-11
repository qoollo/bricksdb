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
    internal class WriterStateFileLogger
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public List<RestoreServer> RestoreServers { get; private set; } = new List<RestoreServer>();

        public RestoreState WriterState => _writerState;
        private RestoreState _writerState;

        public RestoreState RestoreStateRun { get; private set; }

        public RestoreType RestoreType { get; private set; }

        public WriterStateFileLogger(string filename)
        {
            Contract.Requires(!string.IsNullOrEmpty(filename));
            _filename = filename;
        }

        private readonly string _filename;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void SetRestoreDate(RestoreType type, RestoreState runState, List<RestoreServer> restoreServers)
        {
            _lock.EnterWriteLock();

            RestoreStateRun = runState;
            RestoreType = type;
            RestoreServers = restoreServers;

            _lock.ExitWriteLock();
        }

        #region Writer state

        public bool IsNeedRestore()
        {
            return RestoreType != RestoreType.None;
        }

        public void DistributorSendState(RestoreState state)
        {
            LocalSendState(state);
        }

        private void LocalSendState(RestoreState state)
        {
            _writerState = state;
        }

        public void ModelUpdate()
        {
            LocalSendState(RestoreState.FullRestoreNeed);
        }

        #endregion

        #region Save/Load

        public void Save()
        {
            _lock.EnterWriteLock();
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
                _writerState = load.State;
                RestoreType = load.Mode;
                RestoreStateRun = load.RunState;

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

        private void SaveInner()
        {
            try
            {
                var save = new RestoreSaveHelper(RestoreType, WriterState, RestoreStateRun, RestoreServers);
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
            _lock.EnterWriteLock();
            File.Delete(_filename);
            _lock.ExitWriteLock();            
        }

        #endregion

    }

    [Serializable]
    [DataContract]
    public class RestoreSaveHelper
    {        
        public RestoreSaveHelper()
        {
        }

        public RestoreSaveHelper(RestoreType mode, RestoreState state, RestoreState runState, IEnumerable<RestoreServer> servers)
        {
            RunState = runState;
            State = state;
            Mode = mode;
            if (servers != null)
                RestoreServersSave = servers.Select(x => new RestoreServerSave(x)).ToList();
        }
        
        [DataMember]
        [XmlAttribute("RestoreState")]
        public RestoreState State { get; set; }

        [DataMember]
        [XmlAttribute("RestoreStateRun")]
        public RestoreState RunState { get; set; }

        [DataMember]
        [XmlAttribute("RestoreType")]
        public RestoreType Mode { get; set; }

        [DataMember]
        [XmlArray("RestoreServers")]
        public List<RestoreServerSave> RestoreServersSave { get; set; }

        [XmlIgnore]
        public List<RestoreServer> RestoreServers { get { return RestoreServersSave.Select(x => x.Convert()).ToList(); }}
    }
}
