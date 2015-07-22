using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace libshadowsocks_test
{
    public static class AssemblyHelper
    {
        public static Assembly GetAssembly(string[] referencedAssemblies, string path)
        {
            var provider = CodeDomProvider.CreateProvider("cs");
            var cp = new CompilerParameters();
            cp.IncludeDebugInformation = true;
            cp.GenerateExecutable = false;
            cp.GenerateInMemory = true;
            cp.ReferencedAssemblies.AddRange(new[]{ 
                "System.dll", 
                "System.Core.dll", 
                "Microsoft.CSharp.dll",
                "System.Configuration.dll",
                "System.Data.dll", 
                "System.Data.DataSetExtensions.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Windows.Forms.dll",
            });

            cp.ReferencedAssemblies.AddRange(referencedAssemblies);

            var sources = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            var result = provider.CompileAssemblyFromFile(cp, sources);
            if (result.Errors.HasErrors)
            {
                Trace.WriteLine(result.Output.Cast<string>().Aggregate((a, b) => a + Environment.NewLine + b));
                throw new Exception("Compile Failed");
            }

            return result.CompiledAssembly;
        }

        public static IList<T1> GetObjects<T1, T2>(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => t.IsDefined(typeof(T2))).Select(t => (T1)Activator.CreateInstance(t)).ToList();
        }

        public static IList<Type> GetTypes<T>(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => t.IsDefined(typeof(T))).ToList();
        }

        public static T GetObject<T>(Type t)
        {
            return (T)Activator.CreateInstance(t);
        }
    }
}
