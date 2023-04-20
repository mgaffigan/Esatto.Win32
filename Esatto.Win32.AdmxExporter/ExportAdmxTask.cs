using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.AdmxExporter
{
    [LoadInSeparateAppDomain]
    public class ExportAdmx : AppDomainIsolatedTask
    {
        [Required]
        public string InputAssembly { get; set; }

        public string OutputDirectory { get; set; }

        public string OutputFilename { get; set; }

        public string OutputXml { get; set; }

        public string OutputAdmx { get; set; }

        public string OutputAdml { get; set; }

        private string InputBaseDir;

        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(InputAssembly))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrWhiteSpace(InputAssembly)", "value");
            }
            if (OutputDirectory == null
                && (OutputXml == null || OutputAdmx == null || OutputAdml == null))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrWhiteSpace(InputAssembly)", "value");
            }

            InputAssembly = Path.GetFullPath(InputAssembly);
            OutputFilename = OutputFilename ?? Path.GetFileNameWithoutExtension(InputAssembly);
            if (OutputDirectory != null)
            {
                OutputDirectory = Path.GetFullPath(OutputDirectory);
            }
            OutputXml = OutputXml == null
                ? Path.Combine(OutputDirectory, OutputFilename + ".xml")
                : Path.GetFullPath(OutputXml);
            OutputAdmx = OutputAdmx == null
                ? Path.Combine(OutputDirectory, OutputFilename + ".admx")
                : Path.GetFullPath(OutputAdmx);
            OutputAdml = OutputAdml == null
                ? Path.Combine(OutputDirectory, OutputFilename + ".adml")
                : Path.GetFullPath(OutputAdml);

            InputBaseDir = Path.GetDirectoryName(Path.GetFullPath(InputAssembly));

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            try
            {
                var asm = Assembly.LoadFrom(InputAssembly);
                Program.Export(asm, OutputXml, OutputAdmx, OutputAdml);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading assembly to export '{InputAssembly}' to '{OutputXml}'");
                throw ex;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            }

            return true;
        }

        private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                var proposedDllPath = Path.Combine(InputBaseDir, assemblyName.Name + ".dll");

                if (File.Exists(proposedDllPath))
                {
                    return Assembly.LoadFrom(proposedDllPath);
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error loading assembly {args.Name} referenced by {args.RequestingAssembly.FullName} in export of '{InputAssembly}' to '{OutputXml}'");
                throw ex;
            }
        }
    }
}
