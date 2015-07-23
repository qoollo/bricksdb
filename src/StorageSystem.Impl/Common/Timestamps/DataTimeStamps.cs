using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Qoollo.Impl.Common.Timestamps
{
    [DataContract]
    public class DataTimeStamps
    {
        public string TimeStamps { get { return _timeStamps; } }
        class DataTimeStampRecord
        {
            public string Name { get; private set; }
            public long ElapsedMilliseconds { get; private set; }

            public DataTimeStampRecord(string name, long elapsedMilliseconds)
            {
                Name = name;
                ElapsedMilliseconds = elapsedMilliseconds;
            }
        }

        public DataTimeStamps(bool enable)
        {            
            _enable = enable;
            _timer = new Stopwatch();
        }

        [DataMember]
        private readonly bool _enable;
        private readonly Stopwatch _timer;
        [DataMember]
        private string _timeStamps = string.Empty;

        public void MakeStamp(string stampName)
        {
            if (!_enable)
                return;

            _timer.Stop();
            AddStamp(stampName, _timer.ElapsedMilliseconds);
            _timer.Start();            
        }

        public void AddStamps(DataTimeStamps stamps)
        {
            if (!_enable || stamps == null)
                return;
            _timeStamps += stamps.TimeStamps.Trim() + "\"}\n";
        }

        public void StartMeasure(string module)
        {
            if (!_enable)
                return;
            _timeStamps += string.Format("{{\"Module\":\"{0}\",\"Stamps\":\n\"", module);
            _timer.Restart();
        }

        public void StopMeasure()
        {
            if (!_enable)
                return;
            _timeStamps += "\"}";
            _timer.Stop();
        }

        private void AddStamp(string stampName, long mls)
        {
            if (!_enable)
                return;
            _timeStamps += JsonConvert.SerializeObject(new DataTimeStampRecord(stampName, mls)) + "\n";
        }
    }
}

