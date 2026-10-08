using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

class Program
{
    static void Main()
    {
        string path = @"D:\New folder\steam\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll";
        using var fs = File.OpenRead(path);
        using var peReader = new PEReader(fs);
        var mr = peReader.GetMetadataReader();

        foreach (var handle in mr.TypeDefinitions)
        {
            var typeDef = mr.GetTypeDefinition(handle);
            string typeName = mr.GetString(typeDef.Name);

            if (typeName == "PlayerController" || typeName == "PlayerHealth")
            {
                Console.WriteLine($"\n=== {typeName} ===");
                foreach (var fHandle in typeDef.GetFields())
                {
                    var f = mr.GetFieldDefinition(fHandle);
                    string name = mr.GetString(f.Name);
                    var attrs = f.Attributes;
                    Console.WriteLine($"Field: {attrs} -> {name}");
                }
                foreach (var mHandle in typeDef.GetMethods())
                {
                    var m = mr.GetMethodDefinition(mHandle);
                    string name = mr.GetString(m.Name);
                    var attrs = m.Attributes;
                    Console.WriteLine($"Method: {attrs} -> {name}");
                }
            }
        }
    }
}
