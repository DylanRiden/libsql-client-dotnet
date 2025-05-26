using System;
using System.Runtime.InteropServices; // For Marshal
using Libsql.Client.Ado.Exceptions; // Assumed namespace for LibsqlException
using Libsql.Client.Ado.Extensions; // Assumed namespace for CustomMarshal

namespace Libsql.Client.Ado
{
    // Represents the error message pointer OUT parameter from the native library
    internal unsafe struct NativeError : IDisposable
    {
        // We don't store the pointer directly anymore, it's passed as an out param each time.
        // public byte* Ptr; // Removed

        // Use this signature for functions that provide the error pointer directly
        public void ThrowIfFailed(int exitCode, byte* errorPtr, string message)
        {
            const int LIBSQL_OK = 0; // Assuming 0 is success

            if (exitCode == LIBSQL_OK)
            {
                // Success, errorPtr should be null. If not, it might indicate an issue,
                // but we don't throw. We also don't free it as it wasn't our allocation.
                 if (errorPtr != null)
                 {
                     // This shouldn't happen on success according to typical C API patterns.
                     // Log a warning? For now, ignore it, but don't free it.
                     Console.Error.WriteLine($"Warning: Native call succeeded (Code {exitCode}) but returned non-null error pointer: {(IntPtr)errorPtr}");
                 }
                return;
            }

            // Failure path
            string errorMsg = "Unknown error";
            IntPtr errorIntPtr = (IntPtr)errorPtr; // For marshalling/freeing

            if (errorIntPtr != IntPtr.Zero)
            {
                try
                {
                    // We received an error message pointer. Convert it.
                    errorMsg = CustomMarshal.PtrToStringUTF8(errorIntPtr);
                }
                catch (Exception ex)
                {
                    errorMsg = $"Failed to marshal native error string: {ex.Message}";
                }
                finally
                {
                    // We are responsible for freeing this pointer using the binding's free function.
                    try
                    {
                        Bindings.libsql_free_string(errorPtr);
                    }
                    catch (Exception freeEx)
                    {
                         // Log if freeing fails, but proceed with throwing the original error
                         Console.Error.WriteLine($"Warning: Failed to free native error string pointer {(IntPtr)errorPtr}: {freeEx.Message}");
                    }
                }
            }

            // Construct the final exception message
            string fullMessage = string.IsNullOrEmpty(errorMsg) || errorMsg == "Unknown error"
                ? $"{message} (Code: {exitCode})"
                : $"{message}: {errorMsg} (Code: {exitCode})";

            // Throw the exception
            throw new LibsqlException(fullMessage, exitCode);
        }

         // No Ptr field means nothing instance-specific to dispose.
         // The freeing happens inside ThrowIfFailed now.
        public void Dispose()
        {
            // No-op needed here as the pointer is handled immediately in ThrowIfFailed
        }
    }
}