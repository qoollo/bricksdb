using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreStateFileLogger
    {
        public List<RestoreServer> RestoreServers { get; private set; }
        public RestoreStateHelper StateHelper { get; private set; }

        public RestoreStateFileLogger(string filename, RestoreStateHelper stateHelper,
            List<RestoreServer> restoreServers) : this(filename)
        {
            Contract.Requires(stateHelper != null);
            Contract.Requires(restoreServers != null);
            StateHelper = stateHelper;
            RestoreServers = restoreServers;
        }

        public RestoreStateFileLogger(string filename)
        {
            Contract.Requires(!string.IsNullOrEmpty(filename));
            _filename = filename;
        }

        private readonly string _filename;

        public void Save()
        {
            try
            {
                var save = new RestoreSaveHelper(StateHelper.State, RestoreServers);
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

        public void Load()
        {
            try
            {
                var formatter = new XmlSerializer(typeof (RestoreSaveHelper));
                var stream = new FileStream(_filename, FileMode.Open);
                var save = (RestoreSaveHelper) formatter.Deserialize(stream);
                RestoreServers = save.RestoreServers;
                StateHelper = new RestoreStateHelper(save.State);
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
    }

    [Serializable]
    [DataContract]
    public class RestoreSaveHelper
    {
        public RestoreSaveHelper()
        {
        }

        public RestoreSaveHelper(RestoreState state, IEnumerable<RestoreServer> servers)
        {
            State = state;
            RestoreServersSave = servers.Select(x => new RestoreServerSave(x)).ToList();
        }

        [DataMember]
        [XmlAttribute("Restore state")]
        public RestoreState State { get; set; }

        [DataMember]
        [XmlArray("Restore servers")]
        public List<RestoreServerSave> RestoreServersSave { get; set; }

        [XmlIgnore]
        public List<RestoreServer> RestoreServers { get { return RestoreServersSave.Select(x => x.Convert()).ToList(); }}
    }
}
