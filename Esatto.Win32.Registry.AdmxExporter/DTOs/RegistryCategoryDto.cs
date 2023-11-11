using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Esatto.Win32.Registry.AdmxExporter
{
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
        public string? ParentUuid { get; set; }

        public RegistryCategoryDto(string path, string name, string uuid, string? parentUuid)
        {
            this.FullPath = path;
            this.Name = name;
            this.Uuid = uuid;
            this.ParentUuid = parentUuid;
        }

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
            string? prevPath = null;
            foreach (var name in PartsForPath(s))
            {
                if (cpath == "")
                {
                    cpath = name;
                }
                else
                {
                    cpath += "\\" + name;
                }

                var uuid = Program.CalculateMD5Hash(cpath);
                yield return new RegistryCategoryDto(cpath, name, uuid, prevPath);
                prevPath = uuid;
            }
        }
    }
}
