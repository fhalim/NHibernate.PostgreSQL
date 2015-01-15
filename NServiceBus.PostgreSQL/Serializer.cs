﻿namespace NServiceBus.PostgreSQL
{
    using System;
    using Newtonsoft.Json;

    class Serializer
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        public string Serialize(Object o)
        {
            return JsonConvert.SerializeObject(o, JsonSerializerSettings);
        }

        public T Deserialize<T>(string contents)
        {
            return JsonConvert.DeserializeObject<T>(contents, JsonSerializerSettings);
        }
    }
}
