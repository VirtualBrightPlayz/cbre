using System;
using System.IO;
using System.Text;

namespace CBRE.RMesh; 

public class BlitzReader : IDisposable {
    private readonly Stream fileStream;
    private readonly BinaryReader binaryReader;

    public BlitzReader(string filePath) {
        fileStream = new FileStream(filePath, FileMode.Open);
        binaryReader = new BinaryReader(fileStream);
    }

    public BlitzReader(Stream stream) {
        fileStream = stream;
        binaryReader = new BinaryReader(fileStream);
    }

    public byte ReadByte()
        => binaryReader.ReadByte();
    
    //Little-endian 32-bit signed integer
    public Int32 ReadInt() {
        byte[] bytes = new byte[4];
        bytes[0] = ReadByte();
        bytes[1] = ReadByte();
        bytes[2] = ReadByte();
        bytes[3] = ReadByte();

        if (!BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }

        return (Int32)BitConverter.ToUInt32(bytes);
    }
    
    //32-bit (single precision) floating point number
    public Single ReadFloat() {
        byte[] bytes = new byte[4];
        bytes[0] = ReadByte();
        bytes[1] = ReadByte();
        bytes[2] = ReadByte();
        bytes[3] = ReadByte();

        if (!BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }

        return BitConverter.ToSingle(bytes);
    }
    
    //Int32 length of string + however many bytes that length says
    public string ReadString() {
        int length = ReadInt();
        byte[] bytes = binaryReader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    public void Dispose() {
        fileStream.Dispose();
        binaryReader.Dispose();
    }
}
