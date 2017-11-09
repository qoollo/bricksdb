using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Qoollo.Impl.Modules.Config
{
    public interface ISettingsReader
    {
        T Load<T>() where T : class, new();
        T LoadSection<T>() where T : class, new();
        object Load(Type type);
        object LoadSection(Type type);
    }

    public class SettingsReader : ISettingsReader
    {
        private readonly string _configurationFilePath;
        private readonly string _sectionNameSuffix;

        private string _jsonFile;
        private dynamic _jsonData;

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new SettingsReaderContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };


        public SettingsReader(string configurationFilePath, string sectionNameSuffix = "Configuration")
        {
            _configurationFilePath = configurationFilePath;
            _sectionNameSuffix = sectionNameSuffix;
        }

        public void Start()
        {
            if (!File.Exists(_configurationFilePath))
                throw new FileNotFoundException($"file {_configurationFilePath} not found");

            _jsonFile = File.ReadAllText(_configurationFilePath);
            _jsonData = JsonConvert.DeserializeObject<dynamic>(_jsonFile, JsonSerializerSettings);
        }

        public T Load<T>() where T : class, new() => Load(typeof(T)) as T;

        public T LoadSection<T>() where T : class, new() => LoadSection(typeof(T)) as T;

        public object Load(Type type)
        {
            if (!File.Exists(_configurationFilePath))
                return Activator.CreateInstance(type);

            return JsonConvert.DeserializeObject(_jsonFile, type, JsonSerializerSettings);
        }

        public object LoadSection(Type type)
        {
            var section = ToCamelCase(type.Name.Replace(_sectionNameSuffix, string.Empty));
            var settingsSection = _jsonData[section];

            return settingsSection == null
                ? Activator.CreateInstance(type)
                : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(settingsSection), type,
                    JsonSerializerSettings);
        }

        private class SettingsReaderContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(p => CreateProperty(p, memberSerialization))
                    .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(f => CreateProperty(f, memberSerialization)))
                    .ToList();
                props.ForEach(p =>
                {
                    p.Writable = true;
                    p.Readable = true;
                });

                return props;
            }
        }

        private static string ToCamelCase(string text)
            => string.IsNullOrWhiteSpace(text) ? string.Empty : $"{text.ToLowerInvariant()}";
    }
}
