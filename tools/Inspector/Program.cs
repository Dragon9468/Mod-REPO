using System;
using System.IO;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
        string managedDir = @"D:\New folder\steam\steamapps\common\REPO\REPO_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            string shortName = new AssemblyName(e.Name).Name + ".dll";
            string p = Path.Combine(managedDir, shortName);
            if (File.Exists(p)) return Assembly.Load(File.ReadAllBytes(p));
            return null;
        };

        var asm = Assembly.Load(File.ReadAllBytes(Path.Combine(managedDir, "Assembly-CSharp.dll")));

        var itemType = asm.GetType("Item");
        if (itemType != null)
        {
            Console.WriteLine("=== Item ===");
            foreach (var f in itemType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Console.WriteLine($"Field: {f.FieldType} {f.Name}");
            foreach (var p in itemType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Console.WriteLine($"Property: {p.PropertyType} {p.Name}");
        }

        var playerType = asm.GetType("PlayerAvatar");
        if (playerType != null)
        {
            Console.WriteLine("=== PlayerAvatar inventory/item methods ===");
            foreach (var m in playerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.ToLower().Contains("item") || m.Name.ToLower().Contains("inv") || m.Name.ToLower().Contains("give") || m.Name.ToLower().Contains("add"))
                    Console.WriteLine($"Method: {m.ReturnType} {m.Name}({string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType + " " + p.Name))})");
            }
        }

        var statsType = asm.GetType("StatsManager");
        if (statsType != null)
        {
            var m = statsType.GetMethod("LoadItemsFromFolder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (m != null)
            {
                var body = m.GetMethodBody();
                if (body != null)
                {
                    byte[] il = body.GetILAsByteArray();
                    for (int i = 0; i < il.Length - 4; i++)
                    {
                        // 0x72 is ldstr opcode
                        if (il[i] == 0x72)
                        {
                            int token = BitConverter.ToInt32(il, i + 1);
                            try
                            {
                                string s = asm.ManifestModule.ResolveString(token);
                                Console.WriteLine($"LoadItemsFromFolder string: \"{s}\"");
                            }
                            catch { }
                        }
                    }
                }
            }

            var itemFetch = statsType.GetMethod("ItemFetchName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (itemFetch != null)
            {
                Console.WriteLine($"=== ItemFetchName calls ===");
                var body = itemFetch.GetMethodBody();
                if (body != null)
                {
                    byte[] il = body.GetILAsByteArray();
                    for (int i = 0; i < il.Length - 4; i++)
                    {
                        // 0x28 is call, 0x6f is callvirt
                        if (il[i] == 0x28 || il[i] == 0x6f)
                        {
                            int token = BitConverter.ToInt32(il, i + 1);
                            try
                            {
                                var method = asm.ManifestModule.ResolveMethod(token);
                                Console.WriteLine($"Calls: {method.DeclaringType?.Name}.{method.Name}");
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        var shopType = asm.GetType("ShopManager");
        if (shopType != null)
        {
            Console.WriteLine("=== ShopManager Methods with 'Spawn' or 'Item' ===");
            foreach (var m in shopType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                try
                {
                    if (m.Name.Contains("Spawn") || m.Name.Contains("Item") || m.Name.Contains("Buy"))
                        Console.WriteLine($"Method: {m.Name}");
                }
                catch { }
            }
        }

        var itemVolType = asm.GetType("ItemVolume");
        if (itemVolType != null)
        {
            Console.WriteLine("=== ItemVolume Methods ===");
            foreach (var m in itemVolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                try
                {
                    Console.WriteLine($"Method: {m.Name}");
                }
                catch { }
            }
        }

        var spotType = asm.GetType("InventorySpot");
        if (spotType != null)
        {
            Console.WriteLine("=== InventorySpot Fields & Methods ===");
            foreach (var f in spotType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Console.WriteLine($"Field: {f.FieldType?.Name} {f.Name}");
            foreach (var m in spotType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                try { Console.WriteLine($"Method: {m.Name}"); } catch { }
            }
        }

        var secretEnum = asm.GetType("SemiFunc+itemSecretShopType");
        if (secretEnum != null)
        {
            Console.WriteLine("=== SemiFunc+itemSecretShopType values ===");
            foreach (var name in Enum.GetNames(secretEnum))
                Console.WriteLine($"secretShopType: {name}");
        }
    }
}
