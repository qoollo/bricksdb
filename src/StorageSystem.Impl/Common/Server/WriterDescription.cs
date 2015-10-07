﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.Server
{
    public class WriterDescription:ServerId
    {
        public bool IsAvailable { get; private set; }

        public bool IsServerRestored { get { return RestoreState == RestoreState.Restored; } }

        public RestoreState RestoreState { get; private set; }

        public string StateString
        {
            get
            {
                return string.Format("{0}. Restore state: {1}. Available: {2}. {3}", ToString(),
                    Enum.GetName(typeof(RestoreState), RestoreState), IsAvailable, GetInnerState());
            }
        }

        public WriterDescription(string host,  int port)
            : base(host,  port)
        {
            IsAvailable = true;
            RestoreState = RestoreState.Restored;
        }

        public WriterDescription(ServerId server) : this(server.RemoteHost, server.Port)
        {
            _stateInfo = new ConcurrentDictionary<string, string>();
        }

        private readonly ConcurrentDictionary<string, string> _stateInfo;

        private string GetInnerState()
        {
            var keys = _stateInfo.Keys;
            string result = string.Empty;
            
            foreach (var key in keys)
            {
                string value;
                if (_stateInfo.TryGetValue(key, out value))
                    result += ". " + value;
            }

            return result;
        }

        public void NotAvailable()
        {
            IsAvailable = false;
            UpdateState(RestoreState.SimpleRestoreNeed);
        }

        public void Available()
        {
            IsAvailable = true;
        }

        public void UpdateModel()
        {
            UpdateState(RestoreState.FullRestoreNeed);
        }

        public void UpdateState(RestoreState state)
        {
            switch (RestoreState)
            {
                case RestoreState.Restored:
                case RestoreState.SimpleRestoreNeed:
                    RestoreState = state;
                    break;
                case RestoreState.FullRestoreNeed:
                    if (state != RestoreState.SimpleRestoreNeed)
                        RestoreState = state;
                    break;
            }
        }

        public void SetInfoMessage(string tag, string message)
        {
            _stateInfo.AddOrUpdate(tag, message, (s, s1) => s1);
        }
    }
}
