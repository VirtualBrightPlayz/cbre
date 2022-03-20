using System;
using System.IO;
using System.Text;

namespace CBRE.RMesh; 

public class BlitzReader : IDisposable {
    private readonly FileStream fileStream;
    private readonly BinaryReader binaryReader;
    
    public BlitzReader(string filePath) {
        fileStream = new FileStream(filePath, FileMode.Open);
        binaryReader = new BinaryReader(fileStream);
    }

    public byte ReadByte()
        => binaryReader.ReadByte();
    
    //Little-endian 32-bit signed integer
    public Int32 ReadInt() {
        byte b0 = ReadByte();
        byte b1 = ReadByte();
        byte b2 = ReadByte();
        byte b3 = ReadByte();
        UInt32 result =
            b0
            | (UInt32)(b1 << 8)
            | (UInt32)(b2 << 16)
            | (UInt32)(b3 << 24);
        return unchecked((Int32)result);
    }
    
    //32-bit (single precision) floating point number
    public Single ReadFloat() {
        Int32 intRepresentation = ReadInt();
        return BitConverter.Int32BitsToSingle(intRepresentation);
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
