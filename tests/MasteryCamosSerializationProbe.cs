// Run under Unity 5.6's Mono, against the installed reproduction-profile libraries.
// Exercises the real MasteryCamos quest-state JSON serialization, without launching VR.
using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

public static class MasteryCamosSerializationProbe
{
    public static int Main(string[] args)
    {
        try
        {
            string profile = args[0];
            string gameManaged = args[1];
            string libraries = Path.Combine(profile, "BepInEx/plugins/HQB-Repro-Runtime-Libraries");
            string plugins = Path.Combine(profile, "BepInEx/plugins");
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs request)
            {
                string filename = new AssemblyName(request.Name).Name + ".dll";
                string direct = Path.Combine(gameManaged, filename);
                if (File.Exists(direct)) return Assembly.LoadFrom(direct);
                string core = Path.Combine(profile, "BepInEx/core/" + filename);
                if (File.Exists(core)) return Assembly.LoadFrom(core);
                string[] matches = Directory.GetFiles(plugins, filename, SearchOption.AllDirectories);
                if (matches.Length == 1) return Assembly.LoadFrom(matches[0]);
                throw new FileNotFoundException("Missing or ambiguous profile dependency: " + request.Name);
            };
            // Explicit paths prevent this test silently borrowing these missing game libraries
            // from the host framework, as a DefaultAssemblyResolver audit did previously.
            foreach (string name in new[] { "Mono.Data.Tds", "System.Transactions", "System.Xml.Linq", "System.Data" })
            {
                Assembly library = Assembly.LoadFrom(Path.Combine(libraries, name + ".dll"));
                if (Path.GetFullPath(library.Location) != Path.GetFullPath(Path.Combine(libraries, name + ".dll")))
                    throw new Exception("Library resolved outside the test profile: " + library.Location);
                Console.WriteLine("PROFILE_LIBRARY: " + library.Location);
            }
            return Probe(profile, libraries);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static int Probe(string profile, string libraries)
    {
        Assembly mastery = Assembly.LoadFrom(Path.Combine(profile, "BepInEx/plugins/NGA-MasteryCamos/NGAMasteryCamos.dll"));
        Type stateType = mastery.GetType("NGA.MasteryCamos+QuestsState", true);
        object state = Activator.CreateInstance(stateType);
        stateType.GetMethod("InitNow").Invoke(state, null);
        string json = JsonConvert.SerializeObject(state, Formatting.Indented);
        object restored = JsonConvert.DeserializeObject(json, stateType);
        foreach (string field in new[] { "questDictionary", "unlockedCamos", "masteryCamosUnlocked" })
            if (stateType.GetField(field).GetValue(restored) == null)
                throw new Exception("Round-trip lost " + field);
        Console.WriteLine("JSON_LIBRARY: " + typeof(JsonConvert).Assembly.Location);
        Console.WriteLine("PASS: Real MasteryCamos.QuestsState initialized, serialized, and deserialized under Unity Mono.");
        return 0;
    }
}
