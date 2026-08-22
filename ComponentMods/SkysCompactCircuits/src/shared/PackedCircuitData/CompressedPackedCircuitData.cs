using System.IO;
using System.IO.Compression;
using LogicWorld.SharedCode.BinaryStuff;

namespace SkysCompactCircuits.Shared;

public class CompressedPackedCircuitData(FullPackedCircuitData reference) : DeferredPackedCircuitData(reference)
{
    public CompressedPackedCircuitData(IPackedCircuitData reference) : this(reference is FullPackedCircuitData full ? full : new FullPackedCircuitData(reference)) { }

    public override byte[] Encode()
    {
        ByteWriter writer = new();
        writer.WriteObject(IPackedCircuitData.Mode.Compressed)
            .Write(Zip(Reference.Encode()));
        return writer.Finish();
    }

    public static CompressedPackedCircuitData Decode(ByteReader reader)
    {
        using MemoryByteReader innerReader = new(Zip(reader.ReadByteArray(), decompress: true));
        IPackedCircuitData.AcceptModes((IPackedCircuitData.Mode)innerReader.ReadByte(), IPackedCircuitData.Mode.Full);
        return new(FullPackedCircuitData.Decode(innerReader));
    }

    public static byte[] Zip(byte[] bytes, int index = 0, int? count = null, bool decompress = false)
    {
        using var input = new MemoryStream(bytes, index, count ?? (bytes.Length - index));
        using var output = new MemoryStream();
        using (var zipStream = new GZipStream(decompress ? input : output, decompress ? CompressionMode.Decompress : CompressionMode.Compress))
        {
            // This cannot be a simple using statement, we need the dispose method to be called before the return.
            if (decompress)
                zipStream.CopyTo(output);
            else
                input.CopyTo(zipStream);
        }

        return output.ToArray();
    }
}
