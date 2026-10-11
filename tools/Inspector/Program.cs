using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main()
    {
        string managedDir = @"D:\New folder\steam\steamapps\common\REPO\REPO_Data\Managed";
        var asmDef = AssemblyDefinition.ReadAssembly(Path.Combine(managedDir, "Assembly-CSharp.dll"));
        var ep = asmDef.MainModule.GetType("EnemyParent");
        var esType = asmDef.MainModule.GetType("EnemyState");
        if (esType != null)
        {
            Console.WriteLine("\n=== EnemyState Enum Values ===");
            foreach (var f in esType.Fields)
            {
                if (f.IsStatic) Console.WriteLine($"  EnemyState: {f.Name} = {f.Constant}");
            }
        }

        return;
    }
}
