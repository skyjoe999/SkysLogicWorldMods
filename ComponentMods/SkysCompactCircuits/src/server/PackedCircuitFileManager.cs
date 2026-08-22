using System;
using System.IO;
using LogicLog;
using LogicWorld.SharedCode.BinaryStuff;
using SkysCompactCircuits.Shared;
using SkysGeneralLib.Server;

namespace SkysCompactCircuits.Server;

public static class PackedCircuitFileManager
{
    private static readonly ILogicLogger Logger = LogicLogger.For("Packed Circuit File Manager");
    public static object SaveLock = new(); // No clue if this is necessary but I based this on how the base game does it so it's here.

    public static string DataPath => Path.Combine(Services.ISaveManager.ActiveSaveDirectory, "ExtraData", "SkysCompactCircuits.PackedCircuitDatas.bin");

    public static void ReadFromDisk()
    {
        if (!new FileInfo(DataPath).Exists)
            File.Create(DataPath).Close();

        Logger.Trace($"Loading packed circuits from disk");
        using var reader = new FileByteReader(DataPath);
        PackedCircuitManager.DeserializeData(reader);
    }

    public static void AppendNewIndex(int index, byte[] data)
    {
        var writer = new ByteWriter();
        using (var reader = new FileByteReader(DataPath))
        {
            var count = reader.ReadInt32();

            writer.Write(count + 1);
            for (var i = 0; i < count; i++)
                writer.Write(reader.ReadInt32()).Write(reader.ReadByteArray());

            writer.Write(index).Write(data);
        }
        var bytes = writer.Finish();

        lock (SaveLock)
        {
            File.WriteAllBytes(DataPath + ".tmp", bytes);
            File.Move(DataPath + ".tmp", DataPath, true);
        }
    }

    public static void WriteToDisk()
    {
        Logger.Trace($"Writing packed circuits to disk");
        var bytes = PackedCircuitManager.SerializeData();
        Directory.CreateDirectory(Path.Combine(Services.ISaveManager.ActiveSaveDirectory, "ExtraData"));
        lock (SaveLock)
        {
            File.WriteAllBytes(DataPath + ".tmp", bytes);
            File.Move(DataPath + ".tmp", DataPath, true);
        }
    }

    public static void TryConvertLegacyFiles()
    {
        var legacyPath = Path.Combine(Services.ISaveManager.ActiveSaveDirectory, "ExtraData", "SkysCompactCircuits.PackedCircuitDatas.jecs");

        if (!new FileInfo(legacyPath).Exists)
            return; // Nope, no files.
        if (new FileInfo(DataPath).Exists)
            throw new("Both binary and base64 data exists for Compact Circuits. This should never happen. Possibly there was a problem with the steam cloud? If you are unsure, load a backup or contact the mod author to avoid data loss.");

        // Logger.Info(new JECS.DataFile(legacyPath).GetAtPath<string>(path: "Data"));
        var bytes = Convert.FromBase64String(new JECS.DataFile(legacyPath).GetAtPath<string>(path: "Data"));

        Directory.CreateDirectory(Path.Combine(Services.ISaveManager.ActiveSaveDirectory, "ExtraData"));
        lock (SaveLock)
        {
            File.WriteAllBytes(DataPath + ".converted.tmp", bytes);
            File.Move(DataPath + ".converted.tmp", DataPath, false);
            File.Delete(legacyPath);
        }
    }
}
