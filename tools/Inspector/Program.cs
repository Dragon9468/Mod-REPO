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
        var caType = asmDef.MainModule.GetType("CameraAim");
        
        var pc = asmDef.MainModule.GetType("PlayerController");
        var caAwake = caType.Methods.FirstOrDefault(m => m.Name == "Awake");
        if (caAwake != null)
        {
            Console.WriteLine("\n=== CameraAim Awake IL Instructions ===");
            foreach (var inst in caAwake.Body.Instructions) Console.WriteLine($"  {inst.OpCode} {inst.Operand}");
        }
    }
}
