using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Text;

namespace Employee
{
    public class Json<T>
    {
        private Encoding m_encoding = Encoding.UTF8;
        private List<T> m_data = new List<T>();

        public Json() { }

        public Json(Encoding encoding)
        {
            m_encoding = encoding;
        }

        public Json(string json)
        {
            Parse(json);
        }

        public void Add(T o)
        {
            if (o != null) {
                m_data.Add(o);
            }
        }

        public void Clear()
        {
            m_data.Clear();
        }

        public override string ToString()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            IsoDateTimeConverter timeConverter = new IsoDateTimeConverter {
                DateTimeFormat = "yyyy-MM-dd HH:mm:ss"
            };
            settings.ContractResolver = new LowercaseContractResolver();
            settings.Converters.Add(timeConverter);

            string json = string.Empty;
            if (m_data.Count > 1) {
                json = JsonConvert.SerializeObject(m_data.ToArray(), Formatting.Indented, settings); ;
            } else if (m_data.Count == 1) {
                JsonConvert.SerializeObject(m_data[0], Formatting.Indented, settings);
            }
            return json;
        }

        public T[] ToArray()
        {
            return m_data.ToArray();
        }

        public void Parse(string json)
        {
            if (Check(json)) {
                if (JsonConvert.DeserializeObject(json, typeof(T)) is T data) {
                    m_data.Add(data);
                }
            }
        }

        public void ParseArray(string json)
        {
            if (Check(json)) {
                if (JsonConvert.DeserializeObject(json, typeof(List<T>)) is List<T> data) {
                    m_data.AddRange(data);
                }
            }
        }

        private bool Check(string json)
        {
            bool b = false;
            if (!string.IsNullOrEmpty(json)) {
                json = json.Trim();
                if ((json.StartsWith("{") && json.EndsWith("}")) ||
                    (json.StartsWith("[") && json.EndsWith("]"))) {
                    try {
                        JToken.Parse(json);
                        b = true;
                    } catch {
                        b = false;
                    }
                } else {
                    b = false;
                }
            }
            return b;
        }
    }

    internal class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }
}