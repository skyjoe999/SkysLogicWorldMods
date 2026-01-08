using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JECS;
using JECS.MemoryFiles;
using LogicAPI;
using LogicAPI.Modding;
using LogicLog;
using SkysChallengeSystem.Server.Challenges;
using SkysChallengeSystem.Server.Loaders;
using SkysChallengeSystem.Shared;

namespace SkysChallengeSystem.Server;

public static class ChallengeManager
{
    internal static ILogicLogger Logger;
    private static readonly Dictionary<string, ChallengeRecord> ChallengeRecords = [];
    private static readonly Dictionary<Type, ChallengeLoader> ChallengeLoaders = new() { [typeof(NullType)] = null };
    private static readonly Dictionary<string, Type> ChallengeRecordLoaderTypes = [];

    public static void RegisterChallenges(MetaMod mod) => RegisterChallenges(mod.Files, mod.Manifest);

    public static void RegisterChallenges(IModFiles modFiles, ModManifest manifest)
    {
        foreach (
            var modFile in
            modFiles.EnumerateFiles()
                .Where(f => ".jecs".Equals(f.Extension) && f.Path.StartsWith("challenges/"))
        ) RegisterChallenges(modFile, manifest);
    }

    public static void RegisterChallenges(ModFile modFile, ModManifest manifest)
    {
        Logger.Debug($"Loading mod file '{modFile.Path}' in mod-FS '{modFile.FileSystem.Path}'.");

        var file = new MemoryDataFile(modFile.ReadAllText());
        foreach (var key in file.GetTopLevelKeysInOrder())
        {
            // (error handling? In one of Sky's mods? Shocking!)
            var errorBase = new StringBuilder().Append($"Error loading challenges from '{modFile.Path}'");
            if (manifest != null) errorBase.Append($" in mod '{manifest.ID}'");
            errorBase.Append(':').AppendLine().Append('\t');

            // Server specific part
            var dynamicRecord = (DynamicRecord)ReadChallengeFromFileAs(typeof(DynamicRecord));

            if (dynamicRecord.Loader is null)
                throw error($"Challenge '{key}' must have loader");

            var loader = GetOrAddChallengeLoader(dynamicRecord.Loader, errorBase);

            // now that we have the loader we can load it properly
            var challengeRecord = ReadChallengeFromFileAs(loader.RecordType);


            if (!TryRegisterChallenge(challengeRecord))
                throw error($"Duplicate challenge with path {challengeRecord.FullPath}");

            // if this fails, there is a duplicate and the caller will throw an error so just ignore
            ChallengeRecordLoaderTypes.TryAdd(challengeRecord.FullPath, dynamicRecord.Loader);

            continue;

            ChallengeRecord ReadChallengeFromFileAs(Type type)
            {
                if (!typeof(ChallengeRecord).IsAssignableFrom(type))
                    throw error($"Type '{type?.Name ?? "Null"}' does not inherit type '{nameof(ChallengeRecord)}'");
                if (!file.TryGetNonGeneric(type, key, out var obj))
                    throw error($"Could not load '{key}' as type '{type.Name}'");

                var record = (ChallengeRecord)obj;
                record.Name = key;
                record.Mod = manifest;
                return record;
            }

            Exception error(string msg) => new(errorBase.Append(msg).ToString());
        }
    }

    public static bool TryRegisterChallenge(ChallengeRecord challengeRecord)
        => ChallengeRecords.TryAdd(challengeRecord.FullPath, challengeRecord);

    public static ChallengeRecord GetRecord(string FullPath)
        => ChallengeRecords.GetValueOrDefault(FullPath);

    public static string[] GetChallengePaths()
        => ChallengeRecords.Keys.ToArray();

    private static ChallengeLoader GetOrAddChallengeLoader(Type type, StringBuilder errorBase)
    {
        var loader = GetLoader(type);
        if (loader is not null) return loader;

        if (!type.IsAssignableTo(typeof(ChallengeLoader)))
            throw error($"Loader '{type.Name}' does not inherit type '{nameof(ChallengeLoader)}'");

        loader = (ChallengeLoader)Activator.CreateInstance(type);
        if (loader is null)
            throw error($"Could not instantiate loader of type '{type.Name}'");
        ChallengeLoaders.Add(type, loader); // Should never fail because we just added it

        if (!loader.RecordType.IsAssignableTo(typeof(ChallengeRecord)))
            throw error($"RecordType '{loader.RecordType}' does not inherit type '{nameof(ChallengeRecord)}'");

        return loader;
        Exception error(string msg) => new(errorBase.Append(msg).ToString());
    }

    public static ChallengeLoader GetLoader(Type type) => ChallengeLoaders.GetValueOrDefault(type);

    public static ChallengeLoader GetLoader(string FullPath)
        => ChallengeLoaders.GetValueOrDefault(ChallengeRecordLoaderTypes.GetValueOrDefault(FullPath, typeof(NullType)));

    public static Challenge LoadChallenge(string FullPath)
    {
        var record = GetRecord(FullPath);
        return record is null ? null : GetLoader(FullPath).LoadChallenge(record);
    }

    private class NullType;

    // ReSharper disable ClassNeverInstantiated.Local
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    private record DynamicRecord : ChallengeRecord
    {
        [SaveThis] public Type Loader { get; set; }
    }
}
