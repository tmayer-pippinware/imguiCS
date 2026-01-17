using System.IO.Compression;

namespace BinaryToCompressedC;

internal static class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: BinaryToCompressedC <input-file> [symbol]");
            return;
        }

        string inputPath = args[0];
        string symbol = args.Length > 1 ? args[1] : Path.GetFileNameWithoutExtension(inputPath);
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return;
        }

        byte[] data = File.ReadAllBytes(inputPath);
        using var ms = new MemoryStream();
        using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            ds.Write(data, 0, data.Length);
        }
        var compressed = ms.ToArray();
        var writer = new StreamWriter(Console.OpenStandardOutput());
        writer.WriteLine($"// Compressed binary of {Path.GetFileName(inputPath)}");
        writer.WriteLine($"static const unsigned int {symbol}_size = {data.Length};");
        writer.WriteLine($"static const unsigned int {symbol}_compressed_size = {compressed.Length};");
        writer.WriteLine($"static const unsigned char {symbol}_data[] = {{");
        for (int i = 0; i < compressed.Length; i++)
        {
            writer.Write($"0x{compressed[i]:X2}");
            if (i != compressed.Length - 1)
                writer.Write(", ");
            if ((i + 1) % 16 == 0)
                writer.WriteLine();
        }
        writer.WriteLine("\n};");
        writer.Flush();
    }
}
