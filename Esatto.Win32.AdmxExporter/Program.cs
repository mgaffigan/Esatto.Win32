using Esatto.Win32.Registry;
using Microsoft.Win32;
using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace Esatto.Win32.Registry.AdmxExporter;

class Program
{
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
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath);
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

    public static void Export(AssemblyDefinition asm, string xmlPath, string admxPath, string admlPath)
    {
        var types = RegistrySettingsMetadata.GetAllSettings(asm).ToArray();
        var categories = types.SelectMany(t => RegistryCategoryDto.ForPath(t.Category)).Distinct(new CategoryUuidComparer()).ToList();
        var asmName = asm.Name.FullName;
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

    public static string CalculateMD5Hash(string input)
    {
        // step 1, calculate MD5 hash from input
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(input));

        // step 2, convert byte array to hex string
        var sb = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
