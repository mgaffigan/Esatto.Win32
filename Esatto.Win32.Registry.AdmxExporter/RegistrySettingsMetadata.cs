using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Esatto.Win32.Registry;
using Mono.Cecil.Cil;

namespace Esatto.Win32.Registry.AdmxExporter
{
    internal static class TypeConstants
    {
        public const string RegistrySettingsTypeName = "Esatto.Win32.Registry.RegistrySettings";
    }

    public sealed class RegistrySettingsMetadata
    {

        public TypeDefinition SettingsType { get; }
        public string SoftwarePath { get; }
        public string PolicyPath { get; }
        public string Category { get; }
        public string DisplayName { get; }
        public string? Description { get; }

        public RegistrySettingsMetadata(TypeDefinition tRegistrySettings)
        {
            if (tRegistrySettings == null)
            {
                throw new ArgumentNullException(nameof(tRegistrySettings));
            }

            var ctorType = IsParameterized(tRegistrySettings);
            SettingsType = tRegistrySettings;
            DisplayName = tRegistrySettings.CustomAttributes.GetOrDefault<DisplayNameAttribute>()?.DisplayName ?? tRegistrySettings.Name;
            Description = tRegistrySettings.CustomAttributes.GetOrDefault<DescriptionAttribute>()?.Description;

            var relPath = GetRelPath(tRegistrySettings);
            SoftwarePath = $@"SOFTWARE\{relPath}";
            PolicyPath = $@"SOFTWARE\Policies\{relPath}";
            Category = tRegistrySettings.CustomAttributes.GetOrDefault<CategoryAttribute>()?.Category ?? relPath;

            var settings = new List<RegistrySettingMetadata>();
            foreach (var prop in tRegistrySettings.Properties)
            {
                if (RegistrySettingMetadata.TryGet(prop, out var setting))
                {
                    settings.Add(setting);
                }
            }
            Settings = settings;
        }

        private static string GetRelPath(TypeDefinition tRegistrySettings)
        {
            var attr = tRegistrySettings.CustomAttributes.GetOrDefault<RegistrySettingsAttribute>();
            if (attr is not null) return attr.Path;

            var ctor = GetConstructor(tRegistrySettings);

            // Get the string passed to the base constructor
            /*
                .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
                {
                    .maxstack 8
                    L_0000: ldarg.0 
                    L_0001: ldstr "SOFTWARE\\Company\\Product"
                    L_0006: call instance void [Esatto.Win32.Registry]Esatto.Win32.Registry.RegistrySettings::.ctor(string)
                    L_000b: nop 
                    L_000c: nop 
                    L_000d: ret 
                }
            */

            var inst = ctor.Body.Instructions;
            const string useAttrInstruction = "Apply [RegistrySettings(\"path\")] or add a nullary public constructor calling base(\"path\").";
            var call = inst.SingleOrDefault(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference mr
                && mr.Name == ".ctor" && mr.DeclaringType == tRegistrySettings.BaseType)
                ?? throw new InvalidOperationException($"No base(\"path\") call found in '{tRegistrySettings.FullName}..ctor()'.  {useAttrInstruction}");
            var ldstr = inst.Single(i => i.OpCode == OpCodes.Ldstr && i.Next == call)
                ?? throw new InvalidOperationException($"No ldstr for base(\"path\") call found in '{tRegistrySettings.FullName}..ctor()'.  {useAttrInstruction}");
            return ((string?)ldstr.Operand)
                ?? throw new InvalidOperationException($"Path cannot be null in '{tRegistrySettings.FullName}..ctor()'.  {useAttrInstruction}");
        }

        private static MethodDefinition GetConstructor(TypeDefinition tRegistrySettings)
        {
            var defaultCtor = tRegistrySettings.GetConstructors()
                .Where(md => md.IsPublic && !md.IsStatic && md.Parameters.Count == 0)
                .SingleOrDefault();
            if (defaultCtor is null)
            {
                throw new InvalidOperationException($"Type {tRegistrySettings} is not default constructible.  " +
                    "Apply [RegistrySettings(\"path\")] or add a nullary public constructor calling base(\"path\").");
            }
            else return defaultCtor;
        }

        public static IEnumerable<RegistrySettingsMetadata> GetAllSettings(AssemblyDefinition asm)
        {
            return (
                from module in asm.Modules
                from type in module.Types
                    // !Abstract == non-static (https://stackoverflow.com/questions/28592681/in-what-way-is-a-static-class-implicitly-abstract)
                where IsRegistrySettingsType(type)
                select new RegistrySettingsMetadata(type)
            ).ToList();
        }

        private static bool IsRegistrySettingsType(TypeDefinition type)
        {
            return type.IsPublic && !type.IsAbstract
                && (
                    type.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(RegistrySettingsAttribute).FullName)
                    || type.BaseType.Name == TypeConstants.RegistrySettingsTypeName
                );
        }

        public static bool IsParameterized(TypeDefinition tRegistrySettings)
        {
            if (!IsRegistrySettingsType(tRegistrySettings))
            {
                throw new ArgumentOutOfRangeException(nameof(tRegistrySettings), "Type does not inherit from RegistrySettings or have RegistrySettingsAttribute");
            }

            return tRegistrySettings.CustomAttributes.GetOrDefault<RegistrySettingsAttribute>()
                ?.IsParameterized ?? false;
        }

        public IReadOnlyList<RegistrySettingMetadata> Settings { get; }
    }

    public sealed class RegistrySettingMetadata
    {
        public string Name { get; }
        public string DisplayName { get; }
        public string? Description { get; }
        public string? ParentSettingName { get; }
        public string PropertyType { get; }
        public RegistryValueKind ValueKind { get; }
        public string? DefaultValue { get; }

        public RegistrySettingMetadata(PropertyDefinition p, RegistrySettingMetadataAttribute rsm)
        {
            Name = rsm.Name;
            ValueKind = rsm.Type;
            PropertyType = p.PropertyType.FullName;
            DefaultValue = rsm.DefaultValue;

            DisplayName = p.CustomAttributes.GetOrDefault<DisplayNameAttribute>()?.DisplayName ?? Name;
            Description = p.CustomAttributes.GetOrDefault<DescriptionAttribute>()?.Description;
            ParentSettingName = p.CustomAttributes.GetOrDefault<ChildSettingOfAttribute>()?.ParentName;
        }

        internal static bool TryGet(PropertyDefinition prop,
#if NET
            [MaybeNullWhen(false)]
#endif
            out RegistrySettingMetadata setting)
        {
            // static properties are not eligible
            if (!prop.HasThis)
            {
                setting = null!;
                return false;
            }

            var rsm = prop.CustomAttributes.GetOrDefault<RegistrySettingMetadataAttribute>();
            // If not explicit, only allow read/write props
            if (rsm is not null && prop.GetMethod is null || prop.SetMethod is null)
            {
                setting = null!;
                return false;
            }

            rsm ??= GetRsmFromMsilOrDefault(prop);
            setting = new RegistrySettingMetadata(prop, rsm);
            return true;
        }

        private static RegistrySettingMetadataAttribute GetRsmFromMsilOrDefault(PropertyDefinition prop)
        {
            var type = GetType(prop);
            var defaultRsm = new RegistrySettingMetadataAttribute(prop.Name, type);
            var body = prop.GetMethod.Body;
            // Converts things like ldc.i4.0 into ldc.i4 0
            body.SimplifyMacros();
            var inst = body.Instructions;

            /*
                public string A { get => GetXXX("A", null); }

                .method public hidebysig specialname 
                    instance string get_A () cil managed 
                {
                    // Method begins at RVA 0x20a4
                    // Code size 18 (0x12)
                    .maxstack 3
                    .locals init (
                        [0] string
                    )

                    IL_0000: nop
                    IL_0001: ldarg.0
                    IL_0002: ldstr "A"
                    IL_0007: ldnull or ldstr "bar" or ldc.i4 50000 or ldc.i4 0 (false) or ldc.i4 1 (true)
                    IL_0008: call instance string C::GetXXX(string, string)
                    IL_000d: stloc.0
                    IL_000e: br.s IL_0010

                    IL_0010: ldloc.0
                    IL_0011: ret
                } // end of method B::get_A
            */

            var call = inst.SingleOrDefault(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference mr
                && mr.Name.StartsWith("Get", StringComparison.Ordinal) && mr.DeclaringType.FullName == TypeConstants.RegistrySettingsTypeName);
            if (call is null) return defaultRsm;

            var ldDefault = call.Previous;
            if (!GetValueFromLoad(ldDefault, prop.PropertyType, out var defaultValue)) return defaultRsm;

            var ldName = ldDefault.Previous;
            if (!GetStringFromLoad(ldDefault, out var name)) return defaultRsm;
            if (name is null) return defaultRsm;

            return new RegistrySettingMetadataAttribute(name, type) { DefaultValue = defaultValue?.ToString() };
        }

        private static bool GetValueFromLoad(Instruction inst, TypeReference type, out object? defaultValue)
        {
            // ldnull or ldstr "bar" or ldc.i4 50000 or ldc.i4 0 (false) or ldc.i4 1 (true)
            if (inst.OpCode == OpCodes.Ldnull)
            {
                defaultValue = null;
                return true;
            }
            else if (inst.OpCode == OpCodes.Ldstr)
            {
                defaultValue = (string)inst.Operand;
                return true;
            }
            else if (inst.OpCode == OpCodes.Ldc_I4)
            {
                var iValue = (int)inst.Operand;
                if (type.FullName == typeof(bool).FullName)
                {
                    defaultValue = iValue != 0;
                    return true;
                }
                else if (type is TypeDefinition td && td.IsEnum && td.GetEnumUnderlyingType().FullName == typeof(int).FullName)
                {
                    // .field public static literal valuetype EnumName B = int32(1)
                    var name = td.Fields.FirstOrDefault(f => f.IsStatic && f.IsPublic && f.IsLiteral && iValue == (int)f.Constant)?.Name;
                    defaultValue = (object?)name ?? iValue;
                    return true;
                }
                else
                {
                    defaultValue = iValue;
                    return true;
                }
            }
            else
            {
                defaultValue = null;
                return false;
            }
        }

        private static bool GetStringFromLoad(Instruction ld, out string? value)
        {
            if (ld.OpCode == OpCodes.Ldstr)
            {
                value = (string?)ld.Operand;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private static RegistryValueKind GetType(PropertyDefinition propertyType)
        {
            if (propertyType.FullName == typeof(string).FullName
                || propertyType.FullName == typeof(Guid).FullName)
            {
                return RegistryValueKind.String;
            }
            else if (propertyType.FullName == typeof(int).FullName
                || propertyType.FullName == typeof(TimeSpan).FullName
                || propertyType.FullName == typeof(bool).FullName)
            {
                return RegistryValueKind.DWord;
            }
            else if (propertyType.FullName == typeof(string[]).FullName)
            {
                return RegistryValueKind.MultiString;
            }
            else
            {
                // Most of the time, this is correct. Exporter works better with this.
                // throw new NotSupportedException($"Unknown type {propertyType}");
                return RegistryValueKind.String;
            }
        }
    }
}
