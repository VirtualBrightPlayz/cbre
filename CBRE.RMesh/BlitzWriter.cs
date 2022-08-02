using System;
using System.IO;
using System.Text;

namespace CBRE.RMesh; 

public class BlitzWriter : IDisposable {
    private readonly FileStream fileStream;
    private readonly BinaryWriter binaryWriter;

    public BlitzWriter(string filePath) {
        fileStream = new FileStream(filePath, FileMode.Create);
        binaryWriter = new BinaryWriter(fileStream);
    }

    public void WriteByte(byte b)
        => binaryWriter.Write(b);
    
    //Little-endian 32-bit signed integer
    public void WriteInt(Int32 i) {
        byte[] bytes = BitConverter.GetBytes(i);
        if (!BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        WriteByte(bytes[0]);
        WriteByte(bytes[1]);
        WriteByte(bytes[2]);
        WriteByte(bytes[3]);
        // UInt32 value = unchecked((UInt32)i);
        // WriteByte((byte)((value >> 0) & 0xff));
        // WriteByte((byte)((value >> 8) & 0xff));
        // WriteByte((byte)((value >> 16) & 0xff));
        // WriteByte((byte)((value >> 24) & 0xff));
    }
    
    //32-bit (single precision) floating point number
    public void WriteFloat(Single f) {
        byte[] bytes = BitConverter.GetBytes(f);
        if (!BitConverter.IsLittleEndian) {
            Array.Reverse(bytes);
        }
        WriteByte(bytes[0]);
        WriteByte(bytes[1]);
        WriteByte(bytes[2]);
        WriteByte(bytes[3]);
        // WriteInt(BitConverter.SingleToInt32Bits(f));
    }
    
    //Int32 length of string + however many bytes that length says
    public void WriteString(string s) {
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        WriteInt(bytes.Length);
        foreach (byte b in bytes) {
            WriteByte(b);
        }
    }

    public void Dispose() {
        fileStream.Dispose();
        binaryWriter.Dispose();
    }
}
