using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.AssemblyPublicizer;
using HarmonyLib;
using JECS;
using JimmysUnityUtilities;
using LogicAPI.Modding;
using LogicWorld.SharedCode.Modding.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SkysUltimateModApi.Shared;

[HarmonyPatch]
public static class UltimateModCompiler
{
    public static string ApiModID;
    public static string[] PublicizedAssemblyNames;
    public static MetadataReference[] PublicizedAssemblyReferences;
    // Stripping the files is nice in theory but it makes the de-comp useless </3
    // Feel free to turn it back on though if you prefer
    const bool DoStrip = false;

    public static void Initialize(ModManifest manifest)
    {
        ApiModID = manifest.ID;
        new Harmony(manifest.ID).PatchAll();

        var dir = new FileInfo(typeof(LogicAPI.Game).Assembly.Location).Directory;
        dir.CreateSubdirectory(manifest.ID); // I think jecs would handle this anyways but who cares

        var cacheFilePath = new FileInfo(Path.Join(dir.FullName, manifest.ID, "cache_meta.jecs"));
        var cachedAssemblyList = cacheFilePath.Exists ? new ReadOnlyDataFile(cacheFilePath.FullName).GetAsObject<AssemblyCacheManifest>() : default;
        var saveAfterLoad = false;

        if (!cacheFilePath.Exists || cachedAssemblyList.GameVersion != LogicAPI.Game.Version || cachedAssemblyList.ModVersion != manifest.Version)
        {
            // We only publicize the logic files because those are generally the relevant ones
            var names = dir.EnumerateFiles("logic*.dll", new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }).Select(file => file.Name);
            // And the server, because they couldn't just name it LogicServer.dll to make my life easy!!!
            if (new FileInfo(Path.Join(dir.FullName, "Server.dll")).Exists)
                names = names.Append("Server.dll");

            cachedAssemblyList = new AssemblyCacheManifest(LogicAPI.Game.Version, manifest.Version, [.. names]);

            // Will overwrite existing files
            foreach (var name in cachedAssemblyList.AssemblyNames)
                AssemblyPublicizer.Publicize(Path.Combine(dir.FullName, name), Path.Combine(dir.FullName, manifest.ID, name), new() { Strip = DoStrip });

            saveAfterLoad = true;
        }

        
        PublicizedAssemblyNames = [.. cachedAssemblyList.AssemblyNames.Select(name => name.RemoveFromEndIfPresent(".dll"))];
        PublicizedAssemblyReferences = [.. cachedAssemblyList.AssemblyNames.Select(name => MetadataReference.CreateFromFile(Path.Combine(dir.FullName, manifest.ID, name)))];

        // We dont want to save this above in case the previous line fails
        // Technically that probably wouldn't be an issue but it might make debugging harder
        if (saveAfterLoad)
            new DataFile(cacheFilePath.FullName).SaveAsObject(cachedAssemblyList);
    }


    [HarmonyPatch(typeof(ModCompiler), nameof(ModCompiler.Compile))]
    [HarmonyPrefix]
    public static bool CompilePatch(string name, IModFiles files, ModSide side, out ModCompiler.CompileResult __result)
    {
        __result = default;
        if (!files.TryGetFile("Manifest.jecs", out var manifestFile))
            return true; // should never happen

        var dependencies = manifestFile.ReadAsJecs().GetAsObject<ModManifest>().GetDependenciesForSide(side);
        if (!dependencies.Contains(ApiModID))
            return true; // ignore any mod that hasn't asked us to mess with it

        __result = CompileWithPassedReferences(name, files, side, GetReferences(files, side), compilationOptions);

        return false;
    }

    public static MetadataReference[] GetReferences(IModFiles files, ModSide side) => [
            .. AppDomain.CurrentDomain.GetAssemblies()
                        .Where(o => !o.IsDynamic)
                        .Where(o => !FileSystemName.MatchesSimpleExpression("logic*", Path.GetFileName(o.Location), ignoreCase: true))
                        .Where(o => !FileSystemName.MatchesSimpleExpression("server.dll", Path.GetFileName(o.Location), ignoreCase: true))
                        .Where(o => !string.IsNullOrEmpty(o.Location))
                        .Select(o => MetadataReference.CreateFromFile(o.Location)),
            .. ProjectParser.GetReferences(files, side),
            .. PublicizedAssemblyReferences,
        ];

    public static Assembly LoadAndAddAttributes(string assemblyPath)
    {
        // We need to modify the file before it is loaded because unloading is no longer supported.
        // Im sure we could do something more clever, but for now just shove in every dll.
        IgnoresAccessChecksToAttributeGenerator.AddToAssembly(assemblyPath, PublicizedAssemblyNames);
        return Assembly.LoadFile(assemblyPath);
    }
    // public static ConstructorInfo NewAttribute = typeof(AsyncIteratorStateMachineAttribute).g

    // This is a reverse patch so it takes the code from the base game and shoves it into our code
    // (and then modifies it using our transpiler)
    [HarmonyPatch(typeof(ModCompiler), nameof(ModCompiler.Compile))]
    [HarmonyReversePatch]
    public static ModCompiler.CompileResult CompileWithPassedReferences(string name, IModFiles files, ModSide side, MetadataReference[] references, CSharpCompilationOptions compilationOptions)
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();

            // this is the opcode where the reference collection starts
            var callIndex = instructionList.FindIndex(inst => inst.Calls(((Func<Assembly[]>)AppDomain.CurrentDomain.GetAssemblies).Method));
            // this is the opcode where everything is done and the final value is saved to a register
            var saveIndex = instructionList.FindIndex(callIndex, inst => inst.opcode == OpCodes.Stloc_S);
            // this is the opcode that creates the compilation settings
            var optionsIndex = instructionList.FindIndex(i => i.opcode == OpCodes.Newobj && (i?.operand as ConstructorInfo)?.DeclaringType == typeof(CSharpCompilationOptions));
            // this is the opcode that loads the final assembly
            var loadIndex = instructionList.FindLastIndex(inst => inst.Calls(((Func<string, Assembly>)Assembly.LoadFile).Method));

            // All we need to do is slice out the bits we don't like and replace them with our own.
            return
            [
                .. InstructionRange(0, callIndex - 1), // keep everything before the bit we care about
                CodeInstruction.LoadArgument(3), // load the MetadataReference[]
                .. InstructionRange(saveIndex, optionsIndex + 1),
                new CodeInstruction(OpCodes.Pop), // discard the settings object we just created
                CodeInstruction.LoadArgument(4), // load the CSharpCompilationOptions
                .. InstructionRange(optionsIndex + 1, loadIndex), // keep everything after the bit we care about
                CodeInstruction.Call(typeof(UltimateModCompiler), nameof(LoadAndAddAttributes)),
                .. InstructionRange(loadIndex + 1, instructionList.Count), // keep everything after the bit we care about
            ];
            // slice operators do not work in unity's version of c# </3
            IEnumerable<CodeInstruction> InstructionRange(int from, int to) => Enumerable.Range(from, to - from).Select(i => instructionList[i]);
        }

        // make compiler happy
        _ = Transpiler(null);
        return default;
    }

    public static readonly CSharpCompilationOptions compilationOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        allowUnsafe: true
    );

    public record struct AssemblyCacheManifest(Version GameVersion, Version ModVersion, string[] AssemblyNames);
    
}
