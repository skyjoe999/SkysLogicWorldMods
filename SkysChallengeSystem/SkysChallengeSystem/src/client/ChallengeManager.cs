using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JECS.MemoryFiles;
using LogicAPI;
using LogicAPI.Modding;
using LogicLog;
using SkysChallengeSystem.Shared;

namespace SkysChallengeSystem.Client;

public static class ChallengeManager
{
    internal static ILogicLogger Logger;
    private static readonly Dictionary<string, ChallengeRecord> ChallengeRecords = [];

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

            var challengeRecord = ReadChallengeFromFileAs(typeof(ChallengeRecord));

            if (!TryRegisterChallenge(challengeRecord))
                throw error($"Duplicate challenge with path {challengeRecord.FullPath}");

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
}
