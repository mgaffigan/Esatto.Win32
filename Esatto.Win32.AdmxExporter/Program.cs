using Esatto.Win32.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace Esatto.Win32.AdmxExporter
{
    class Program
    {
        [DataContract(Namespace = "urn:esatto:registry", Name = "RegistrySettings")]
        public sealed class RegistrySettingsDto
        {
            [DataMember]
            public string AssemblyName { get; set; }

            [DataMember]
            public string Uuid { get; set; }

            [DataMember]
            public List<RegistrySettingsKey> Keys { get; set; }

            [DataMember]
            public List<RegistryCategoryDto> Categories { get; set; }
        }

        internal sealed class CategoryUuidComparer : IEqualityComparer<RegistryCategoryDto>
        {
            public bool Equals(RegistryCategoryDto x, RegistryCategoryDto y)
            {
                return x?.Uuid == y?.Uuid;
            }

            public int GetHashCode(RegistryCategoryDto obj)
            {
                return obj.Uuid.GetHashCode();
            }
        }

        [DataContract(Namespace = "urn:esatto:registry", Name = "Category")]
        public sealed class RegistryCategoryDto
        {
            [DataMember]
            public string FullPath { get; set; }
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string Uuid { get; set; }
            [DataMember]
            public string ParentUuid { get; set; }

            public static string[] PartsForPath(string s)
            {
                var ss = s.Split(new[] { '\\', '/', '.' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < ss.Length; i++)
                {
                    ss[i] = ss[i].Trim();
                }

                return ss;
            }

            public static string NormalizePath(string s) => string.Join("\\", PartsForPath(s));

            public static IEnumerable<RegistryCategoryDto> ForPath(string s)
            {
                var cpath = "";
                string prevPath = null;
                foreach (var n in PartsForPath(s))
                {
                    if (cpath == "")
                    {
                        cpath = n;
                    }
                    else
                    {
                        cpath += "\\" + n;
                    }

                    yield return new RegistryCategoryDto()
                    {
                        FullPath = cpath,
                        Name = n,
                        ParentUuid = prevPath,
                        Uuid = prevPath = CalculateMD5Hash(cpath),
                    };
                }
            }
        }

        [DataContract(Namespace = "urn:esatto:registry", Name = "Key")]
        public sealed class RegistrySettingsKey
        {
            public RegistrySettingsKey(RegistrySettingsMetadata t)
            {
                this.SettingsTypeName = t.SettingsType.FullName;
                this.ConstructorParameter = t.ConstructorParameter;
                this.SoftwarePath = t.SoftwarePath;
                this.PolicyPath = t.PolicyPath;
                this.Uuid = CalculateMD5Hash(SoftwarePath + SettingsTypeName);

                this.DisplayName = t.DisplayName;
                this.Description = t.Description;
                this.Category = RegistryCategoryDto.ForPath(t.Category).Last();

                this.Settings = t.Settings.Select(s => new RegistrySettingValue(SoftwarePath, s)).ToList();
                foreach (var setting in Settings)
                {
                    if (setting.ParentSettingName != null)
                    {
                        setting.ParentSettingUuid = Settings.FirstOrDefault(f => f.Name == setting.ParentSettingName)?.Uuid;
                    }
                }
            }

            [DataMember]
            public string SettingsTypeName { get; set; }
            [DataMember]
            public string ConstructorParameter { get; set; }
            [DataMember]
            public string SoftwarePath { get; set; }
            [DataMember]
            public string PolicyPath { get; set; }

            [DataMember]
            public string Uuid { get; set; }

            [DataMember]
            public string DisplayName { get; set; }
            [DataMember]
            public string Description { get; set; }
            [DataMember]
            public RegistryCategoryDto Category { get; set; }

            [DataMember]
            public List<RegistrySettingValue> Settings { get; set; }
        }

        [DataContract(Namespace = "urn:esatto:registry", Name = "Setting")]
        public sealed class RegistrySettingValue
        {
            public RegistrySettingValue(string parentPath, RegistrySettingMetadata s)
            {
                this.Name = s.Name;
                this.DisplayName = s.DisplayName;
                this.Description = s.Description;
                this.ParentSettingName = s.ParentSettingName;
                this.PropertyType = s.PropertyType.Name;
                this.ValueKind = s.ValueKind.ToString();
                this.DefaultValue = GetStringForValue(s.DefaultValue);
                this.Uuid = CalculateMD5Hash(parentPath + Name);
            }

            private static string GetStringForValue(object defaultValue)
            {
                if (defaultValue is string[] arr)
                {
                    return string.Join(Environment.NewLine, arr);
                }
                else return defaultValue?.ToString();
            }

            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string DisplayName { get; set; }
            [DataMember]
            public string Description { get; set; }
            [DataMember]
            public string ParentSettingName { get; set; }
            [DataMember]
            public string ParentSettingUuid { get; set; }
            [DataMember]
            public string PropertyType { get; set; }
            [DataMember]
            public string ValueKind { get; set; }
            [DataMember]
            public string DefaultValue { get; set; }
            [DataMember]
            public string Uuid { get; set; }
        }

        static void Main(string[] args)
        {
            string assemblyPath = args[0];
            string outPath = args[1];
            var xmlPath = outPath + ".xml";
            var admxPath = outPath + ".admx";
            var admlPath = Path.GetFullPath(outPath + ".adml");
            var admlDir = Path.Combine(Path.GetDirectoryName(admlPath), "en-US");
            admlPath = Path.Combine(admlDir, Path.GetFileName(admlPath));

            Directory.CreateDirectory(admlDir);

            try
            {
                var asm = Assembly.LoadFrom(assemblyPath);
                Export(asm, xmlPath, admxPath, admlPath);
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.Error.WriteLine($"Unexpected exception while loading target file:\r\n{ex}");
                foreach (var lex in ex.LoaderExceptions)
                {
                    Console.Error.WriteLine($"Detail of customization assembly load failure:\r\n{lex}");
                }
                Environment.Exit(-10);
            }
        }

        public static void Export(Assembly asm, string xmlPath, string admxPath, string admlPath)
        {
            var types = RegistrySettingsMetadata.GetAllSettings(asm).ToArray();
            var categories = types.SelectMany(t => RegistryCategoryDto.ForPath(t.Category)).Distinct(new CategoryUuidComparer()).ToList();
            var asmName = asm.GetName().Name;
            var doc = new RegistrySettingsDto()
            {
                AssemblyName = asmName,
                Uuid = CalculateMD5Hash(asmName),
                Categories = categories,
                Keys = types.Select(t => new RegistrySettingsKey(t)).ToList()
            };

            using (var fs = File.Open(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                var dcs = new DataContractSerializer(typeof(RegistrySettingsDto));
                dcs.WriteObject(fs, doc);
            }

            Translate(xmlPath, admxPath, "admx.xslt");
            Translate(xmlPath, admlPath, "adml.xslt");
        }

        private static void Translate(string xmlPath, string admxPath, string xslName)
        {
            var xslt = new XslCompiledTransform();
            var assembly = typeof(Program).Assembly;
            using (var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{xslName}"))
            {
                using (var xmlReader = XmlReader.Create(stream))
                {
                    xslt.Load(xmlReader);
                }
            }

            using (var xi = XmlReader.Create(File.Open(xmlPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            using (var xo = XmlWriter.Create(File.Open(admxPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
            {
                xslt.Transform(xi, xo);
            }
        }

        private static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
