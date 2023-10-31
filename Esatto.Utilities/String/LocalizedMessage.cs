using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Esatto.Utilities
{
    [DataContract(Namespace = "urn:esatto:localization")]
    [JsonConverter(typeof(LocalizedMessageJsonConverter))]
    public class LocalizedMessage
    {
        [DataMember]
        public Collection<LocalizedText> Translations { get; private set; }

        public LocalizedMessage()
        {
            this.Translations = new Collection<LocalizedText>();
        }

        public LocalizedMessage(IEnumerable<LocalizedText> translations)
            : this()
        {
            foreach (var text in translations)
                Translations.Add(text);
        }

        public LocalizedMessage(string message)
            : this()
        {
            this.Translations.Add(new LocalizedText(message));
        }

        public bool IsNullOrEmpty
        {
            get
            {
                return !Translations.Any(t => string.IsNullOrWhiteSpace(t.Message));
            }
        }

        public override string ToString()
        {
            return ToString(CultureInfo.CurrentCulture);
        }

        public string ToString(CultureInfo cultureInfo)
        {
            if (this.Translations.Count == 1)
            {
                return this.Translations[0].ToString();
            }

            var targetInCulture = Translations.FirstOrDefault(f => f.Culture.Equals(cultureInfo));
            if (targetInCulture != null)
            {
                return targetInCulture.ToString();
            }

            CultureInfo currentParent = cultureInfo.Parent;
            while (currentParent != CultureInfo.InvariantCulture)
            {
                var exactMatch = Translations.FirstOrDefault(f => f.Culture.Equals(currentParent));
                if (exactMatch != null)
                    return exactMatch.ToString();

                var closeMatch = Translations.FirstOrDefault(f => !f.Culture.IsNeutralCulture && f.Culture.Parent.Equals(currentParent));
                if (closeMatch != null)
                    return closeMatch.ToString();
            }

            var first = Translations.FirstOrDefault();
            if (first != null)
                return first.ToString();

            return base.ToString()!;
        }

#if NET
        [return: NotNullIfNotNull("message")]
        public static implicit operator LocalizedMessage?(string? message)
#else
        public static implicit operator LocalizedMessage(string message)
#endif
        {
            if (message == null)
                return null!;

            return new LocalizedMessage(message);
        }

#if NET
        [return: NotNullIfNotNull("message")]
        public static implicit operator string?(LocalizedMessage? message)
#else
        public static implicit operator string(LocalizedMessage message)
#endif
        {
            if (message == null)
                return null!;

            return message.ToString(CultureInfo.CurrentCulture)!;
        }
    }

    public class LocalizedMessageJsonConverter : JsonConverter<LocalizedMessage>
    {
        public override LocalizedMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            else if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var dictionary = new LocalizedMessageBuilder();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                // Get the key.
                if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                var key = reader.GetString() ?? throw new JsonException("Invalid type for LocalizedMessage key");
                reader.Read();
                var value = reader.GetString() ?? throw new JsonException("Invalid type for LocalizedMessage value");
                dictionary.Add(key, value);
            }

            // loop should have encountered EndObject
            throw new JsonException("Unexpected end of object");
        }

        public override void Write(Utf8JsonWriter writer, LocalizedMessage value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value.Translations)
            {
                writer.WriteString(kvp.Culture.IetfLanguageTag, kvp.Message);
            }
            writer.WriteEndObject();
        }
    }

    [TypeConverter(typeof(LocalizedMessageBuilderConverter))]
    [SuppressMessage("Design", "CA1710", Justification = "Legacy name")]
    public class LocalizedMessageBuilder : IDictionary<string, string>, IDictionary<CultureInfo, string>
    {
        private readonly Dictionary<CultureInfo, string> b = new();

        public LocalizedMessageBuilder()
            : base()
        {
            // nop
        }

        public LocalizedMessageBuilder(string defaultValue)
            : this()
        {
            this["default"] = defaultValue;
        }

        public string this[string key]
        {
            get => b[CultureInfoForString(key)];
            set => b[CultureInfoForString(key)] = value;
        }

        private static CultureInfo CultureInfoForString(string key)
        {
            if ("default".Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.CurrentCulture;
            }

            return CultureInfo.GetCultureInfoByIetfLanguageTag(key);
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly => false;
        ICollection<string> IDictionary<string, string>.Keys => b.Keys.Select(key => key.IetfLanguageTag).ToArray();
        ICollection<string> IDictionary<string, string>.Values => b.Values;

        public void Add(string key, string value) => b.Add(CultureInfoForString(key), value);
        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
            => ((ICollection<KeyValuePair<CultureInfo, string>>)b)
            .Contains(new KeyValuePair<CultureInfo, string>(CultureInfoForString(item.Key), item.Value));

        public bool ContainsKey(string key) => b.ContainsKey(CultureInfoForString(key));

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            var kvp = (ICollection<KeyValuePair<CultureInfo, string>>)b;
            var intermediate = new KeyValuePair<CultureInfo, string>[array.Length];
            kvp.CopyTo(intermediate, arrayIndex);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new KeyValuePair<string, string>(intermediate[i].Key.IetfLanguageTag, intermediate[i].Value);
            }
        }

        public bool Remove(string key) => b.Remove(CultureInfoForString(key));

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
            => ((ICollection<KeyValuePair<CultureInfo, string>>)b)
            .Remove(new KeyValuePair<CultureInfo, string>(CultureInfoForString(item.Key), item.Value));

        public int Count => b.Count;

        public ICollection<CultureInfo> Keys => ((IDictionary<CultureInfo, string>)b).Keys;

        public ICollection<string> Values => ((IDictionary<CultureInfo, string>)b).Values;

        public bool IsReadOnly => ((ICollection<KeyValuePair<CultureInfo, string>>)b).IsReadOnly;

        public string this[CultureInfo key] { get => ((IDictionary<CultureInfo, string>)b)[key]; set => ((IDictionary<CultureInfo, string>)b)[key] = value; }

        public void Clear() => b.Clear();

#if NET
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
#else
        public bool TryGetValue(string key, out string value)
#endif
            => b.TryGetValue(CultureInfoForString(key), out value);

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
            => ((IEnumerable<KeyValuePair<CultureInfo, string>>)b)
            .Select(k => new KeyValuePair<string, string>(k.Key.IetfLanguageTag, k.Value)).GetEnumerator();

#if NET
        [return: NotNullIfNotNull("message")]
        public static implicit operator LocalizedMessage?(LocalizedMessageBuilder? message)
#else
        public static implicit operator LocalizedMessage(LocalizedMessageBuilder message)
#endif
        {
            if (message == null)
                return null!;

            return new LocalizedMessage(
                message.b.Select(s => new LocalizedText(s.Value, s.Key))
            );
        }

        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Matching behavior of LocalizedMessage")]
        public override string ToString() => ((LocalizedMessage)this).ToString();

        public string ToString(CultureInfo culture) => ((LocalizedMessage)this).ToString(culture);

        public void Add(CultureInfo key, string value)
        {
            ((IDictionary<CultureInfo, string>)b).Add(key, value);
        }

        public bool ContainsKey(CultureInfo key)
        {
            return ((IDictionary<CultureInfo, string>)b).ContainsKey(key);
        }

        public bool Remove(CultureInfo key)
        {
            return ((IDictionary<CultureInfo, string>)b).Remove(key);
        }

        public bool TryGetValue(CultureInfo key,
#if NET
            [MaybeNullWhen(false)]
#endif
            out string value)
        {
            return ((IDictionary<CultureInfo, string>)b).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<CultureInfo, string> item)
        {
            ((ICollection<KeyValuePair<CultureInfo, string>>)b).Add(item);
        }

        public bool Contains(KeyValuePair<CultureInfo, string> item)
        {
            return ((ICollection<KeyValuePair<CultureInfo, string>>)b).Contains(item);
        }

        public void CopyTo(KeyValuePair<CultureInfo, string>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<CultureInfo, string>>)b).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<CultureInfo, string> item)
        {
            return ((ICollection<KeyValuePair<CultureInfo, string>>)b).Remove(item);
        }

        public IEnumerator<KeyValuePair<CultureInfo, string>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<CultureInfo, string>>)b).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)b).GetEnumerator();
        }
    }

    public class LocalizedMessageBuilderConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value is null) return null;
            if (value is string s) return new LocalizedMessageBuilder(s);
            return base.ConvertFrom(context, culture, value);
        }
    }
}
