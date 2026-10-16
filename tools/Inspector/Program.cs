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
        Console.WriteLine("\n=== CameraBob ===");
        var cb = asmDef.MainModule.GetType("CameraBob");
        if (cb != null)
        {
            foreach (var f in cb.Fields) Console.WriteLine($"  cb Field: {(f.IsPublic ? "pub" : "priv")} {f.FieldType.Name} {f.Name}");
            foreach (var p in cb.Properties) Console.WriteLine($"  cb Prop: {p.PropertyType.Name} {p.Name}");
        }

        Console.WriteLine("\n=== CameraJump ===");
        var cj = asmDef.MainModule.GetType("CameraJump");
        if (cj != null)
        {
            foreach (var f in cj.Fields) Console.WriteLine($"  cj Field: {(f.IsPublic ? "pub" : "priv")} {f.FieldType.Name} {f.Name}");
            foreach (var p in cj.Properties) Console.WriteLine($"  cj Prop: {p.PropertyType.Name} {p.Name}");
        }

        Console.WriteLine("\n=== CameraShake ===");
        var cs = asmDef.MainModule.GetType("CameraShake");
        if (cs != null)
        {
            foreach (var f in cs.Fields) Console.WriteLine($"  cs Field: {(f.IsPublic ? "pub" : "priv")} {f.FieldType.Name} {f.Name}");
        }

        Console.WriteLine("\n=== ItemEquippable ===");
        var ie2 = asmDef.MainModule.GetType("ItemEquippable");
        if (ie2 != null)
        {
            foreach (var f in ie2.Fields) Console.WriteLine($"  ie Field: {(f.IsPublic ? "pub" : "priv")} {f.FieldType.Name} {f.Name}");
            foreach (var p in ie2.Properties) Console.WriteLine($"  ie Prop: {p.PropertyType.Name} {p.Name}");
        }
        return;
    }
}
