using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using DeconzToMqtt.Model;
using Newtonsoft.Json;

namespace DeconzToMqtt.Persistence
{
    public abstract class DeconzRepository
    {
        public abstract IEnumerable<DeconzItem> GetAllDeconzItems();

        public abstract string Serialize(DeconzItem item);
    }

    public abstract class DeconzRepository<TDeconzItem> : DeconzRepository
    where TDeconzItem : DeconzItem
    {

        private readonly Dictionary<int, TDeconzItem> _cache;
        private readonly string _apiKey;
        private readonly Uri _baseAddress;
        private readonly JsonSerializer _jsonSerializer;

        public DeconzRepository(string apiKey, Uri baseAddress)
        {
            _cache = new Dictionary<int, TDeconzItem>();
            _apiKey = apiKey;
            _baseAddress = baseAddress;
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new DeconzItemListConverter<TDeconzItem>());
            InitializeSerializerSettings(settings);
            _jsonSerializer = JsonSerializer.Create(settings);
        }


        protected abstract string EndpointName { get; }

        public override IEnumerable<DeconzItem> GetAllDeconzItems()
            => GetAll();

        public IEnumerable<TDeconzItem> GetAll()
        {
            foreach (var item in QueryApi<TDeconzItem[]>(EndpointName))
            {
                CreateHock(item);
                _cache[item.Id] = item;
                yield return item;
            }
        }

        public TDeconzItem GetById(int id)
        {
            if (_cache.ContainsKey(id))
            {
                return _cache[id];
            }

            var result = QueryApi<TDeconzItem>(EndpointName + "/" + id);
            CreateHock(result);

            _cache[id] = result;
            return result;
        }

        public void NotifyChange(int id)
        {
            if (_cache.ContainsKey(id))
            {
                var toRefresh = _cache[id];
                var refreshedEntity = QueryApi<TDeconzItem>(EndpointName + "/" + id, toRefresh.ETag);
                if (refreshedEntity != null)
                {
                    _cache[id] = refreshedEntity;
                }
                else
                {
                    _cache.Remove(id);
                }
            }
        }

        public override string Serialize(DeconzItem item)
            => Serialize((TDeconzItem) item);

        public string Serialize(TDeconzItem sensor)
        {
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                var jsonWriter = new JsonTextWriter(streamWriter);
                _jsonSerializer.Serialize(jsonWriter, sensor);
                jsonWriter.Flush();
                streamWriter.Flush();

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        protected virtual void CreateHock(TDeconzItem item)
        { }

        protected virtual void InitializeSerializerSettings(JsonSerializerSettings settings)
        { }


        private TResult QueryApi<TResult>(string uriExtension, string etag = null)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/" + _apiKey + "/" + uriExtension))
            {
                client.BaseAddress = _baseAddress;

                if (!string.IsNullOrEmpty(etag))
                {
                    request.Headers.TryAddWithoutValidation("If-None-Match", etag);
                    var s = request.Headers.IfNoneMatch;
                }

                var sensorResult = client.SendAsync(request).Result;

                if (sensorResult.IsSuccessStatusCode)
                {
                    using (var stream = sensorResult.Content.ReadAsStreamAsync().Result)
                    using (var streamReader = new StreamReader(stream))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        return _jsonSerializer.Deserialize<TResult>(jsonReader);
                    }
                }

                return default(TResult);
            }
        }
    }
}
