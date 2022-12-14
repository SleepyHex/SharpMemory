using System.Runtime.InteropServices;
using System.Text;
using static SharpMemory.Native.NativeData;
namespace SharpMemory;
public class WriteFunctions
{
    uint PROT_PAGE_READWRITE => 0x04; //https://learn.microsoft.com/en-us/windows/win32/Memory/memory-protection-constants

    public bool Write<T>(Address address, T value, bool useVirtualProtect = true)
    {
        if(value == null)
            return false;

        int size = 0;
        byte[] byteDataToWrite;

        if(typeof(T) == typeof(bool))
            byteDataToWrite = BitConverter.GetBytes((bool)(object)value);
        else if(typeof(T) == typeof(char))
            byteDataToWrite = BitConverter.GetBytes((char)(object)value);
        else
        {
            size = Marshal.SizeOf(typeof(T));
            byteDataToWrite = new byte[size];

            var gcHandle = GCHandle.Alloc(value, GCHandleType.Pinned);
            Marshal.Copy(gcHandle.AddrOfPinnedObject(), byteDataToWrite, 0, size);
            gcHandle.Free();
        }

        return byteDataToWrite == null ? false : WriteByteArray(address.value, byteDataToWrite, useVirtualProtect);
    }

    public bool WriteByteArray(long address, byte[] value, bool useVirtualProtect = true)
    {
        if(!SharpMem.Inst.IsConnectedToProcess)
            return false;

        var procHandle = SharpMem.Inst.ProcessHandle;
        bool bResult = false;
        uint originalPageProtection = 0;

        try
        {
            if(useVirtualProtect) VirtualProtectEx(procHandle, (IntPtr)address, (uint)value.Length, PROT_PAGE_READWRITE, out originalPageProtection);

            bResult = WriteProcessMemory(procHandle, (IntPtr)address, value, (uint)value.Length, out uint bytesWritten);

            if(useVirtualProtect) VirtualProtectEx(procHandle, (IntPtr)address, (uint)value.Length, originalPageProtection, out uint dummy);
        }
        catch { return false; }
        return bResult;
    }
     
    public bool WriteStringAscii(long address, string text, bool useVirtualProtect = true) => WriteByteArray(address, Encoding.ASCII.GetBytes(text), useVirtualProtect);

    public bool WriteStringUnicode(long address, string text, bool useVirtualProtect = true) => WriteByteArray(address, Encoding.Unicode.GetBytes(text), useVirtualProtect);

    public bool WriteNop(long address, int numOfBytes, bool useVirtualProtect = true)
    {
        byte[] nopBuffer = new byte[numOfBytes];
        for(int i = 0; i < numOfBytes; i++)
            nopBuffer[i] = 0x90;

        return WriteByteArray(address, nopBuffer, useVirtualProtect);
    }
}