using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Elasticsearch
{
    public class JsonNetSerializer : IElasticsearchSerializer
    {
        private readonly JsonSerializer _serializer;
        private readonly Func<JsonSerializer, object, Formatting, string> _serializeToString;

        public JsonNetSerializer(
            IConnectionSettingsValues connectionSettings,
            Func<JsonSerializerSettings, JsonSerializerSettings> settingsModifier,
            Func<JsonSerializer, object, Formatting, string> serializeToString)
        {
            connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            settingsModifier = settingsModifier ?? throw new ArgumentNullException(nameof(settingsModifier));
            _serializeToString = serializeToString ?? throw new ArgumentNullException(nameof(serializeToString));

            var settings = settingsModifier(new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            });

            _serializer = JsonSerializer.Create(settings);
        }

        public object Deserialize(Type type, Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return _serializer.Deserialize(jsonReader, type);
        }

        public T Deserialize<T>(Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            return _serializer.Deserialize<T>(jsonReader);
        }

        public Task<object> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Deserialize(type, stream));
        }

        public Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Deserialize<T>(stream));
        }

        public void Serialize<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.Indented)
        {
            var format = formatting == SerializationFormatting.Indented ? Formatting.Indented : Formatting.None;
            var serialized = _serializeToString(_serializer, data, format);
            var bytes = Encoding.UTF8.GetBytes(serialized);
            stream.Write(bytes, 0, bytes.Length);
        }

        public Task SerializeAsync<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.Indented, CancellationToken cancellationToken = default)
        {
            Serialize(data, stream, formatting);
            return Task.CompletedTask;
        }
    }
} 