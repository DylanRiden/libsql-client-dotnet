using System.Runtime.InteropServices;
using System.Text;

namespace Libsql.Client.Ado.Extensions;

internal static class CustomMarshal
{
    public static unsafe string PtrToStringUTF8(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return string.Empty;
        int len = 0;
        while (Marshal.ReadByte(ptr, len) != 0) ++len;
        byte[] buffer = new byte[len];
        Marshal.Copy(ptr, buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer);
    }
}