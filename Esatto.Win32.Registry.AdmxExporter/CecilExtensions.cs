using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Esatto.Win32.Registry.AdmxExporter
{
    static class CecilExtensions
    {
        public static T? GetOrDefault<T>(this IEnumerable<CustomAttribute> attrs)
            where T : Attribute 
            => attrs.AttributesOfType<T>().FirstOrDefault();

        public static IEnumerable<T> AttributesOfType<T>(this IEnumerable<CustomAttribute> attrs, params string[] otherNames)
            where T : Attribute
        {
            foreach (var ca in attrs.Where(ca => ca.AttributeType.FullName == typeof(T).FullName || otherNames.Contains(ca.AttributeType.FullName)))
            {
                var ctor = typeof(T).GetConstructors()
                    .FirstOrDefault(c =>
                    {
                        var cParams = c.GetParameters();
                        if (cParams.Length != ca.Constructor.Parameters.Count)
                        {
                            return false;
                        }

                        for (int i = 0; i < ca.Constructor.Parameters.Count; i++)
                        {
                            if (cParams[i].ParameterType.FullName != ca.Constructor.Parameters[i].ParameterType.FullName)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    ?? throw new InvalidOperationException("Cannot find constructor");

                var args = ca.ConstructorArguments.GetValues();
                args = args.CastNestedArrayTypes(ctor.GetParameters().Select(p => p.ParameterType).ToArray());
                yield return (T)ctor.Invoke(args);
            }
        }

        public static object[] GetValues(this IList<CustomAttributeArgument> args)
        {
            var result = new object[args.Count];
            for (var i = 0; i < args.Count; i++)
            {
                var c = args[i].Value;
                if (c is IList<CustomAttributeArgument> nested)
                {
                    c = nested.GetValues();
                }
                result[i] = c;
            }
            return result;
        }

        public static object[] CastNestedArrayTypes(this object[] obj, Type[] desiredTypes)
        {
            var result = obj.ToArray();
            for (int i = 0; i < desiredTypes.Length; i++)
            {
                var t = desiredTypes[i].GetElementType();
                if (t != null)
                {
                    var arr = (Array)obj[i];
                    var newArray = Array.CreateInstance(t, arr.Length);
                    arr.CopyTo(newArray, 0);
                    result[i] = newArray;
                }
            }
            return result;
        }
    }
}