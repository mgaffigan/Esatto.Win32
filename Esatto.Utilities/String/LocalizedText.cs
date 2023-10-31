using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Esatto.Utilities
{
    [DataContract(Namespace = "urn:esatto:localization")]
    [ImmutableObject(true)]
    public class LocalizedText
    {
        [DataMember]
        public string Message { get; private set; }

        public CultureInfo Culture { get; private set; }

        [DataMember]
#pragma warning disable IDE0051 // Remove unused private members
        private string CultureName
#pragma warning restore IDE0051 // Remove unused private members
        {
            get => Culture.IetfLanguageTag;
            set => Culture = CultureInfo.GetCultureInfoByIetfLanguageTag(value);
        }

        public LocalizedText(string message)
            : this(message, CultureInfo.CurrentUICulture)
        {
        }

        public LocalizedText(string message, CultureInfo culture)
        {
            this.Culture = culture ?? throw new ArgumentNullException(nameof(culture), "Contract assertion not met: culture != null");
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public override string ToString() => Message;
    }
}
