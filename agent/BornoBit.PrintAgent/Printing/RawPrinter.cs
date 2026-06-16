using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BornoBit.PrintAgent.Printing;

/// <summary>
/// Sends raw ESC/POS bytes straight to a Windows print spooler queue (RAW datatype),
/// bypassing the printer driver's graphics rendering. This is the only reliable way to
/// drive thermal receipt printers shared via the Windows spooler.
/// </summary>
[SupportedOSPlatform("windows")]
public static class RawPrinter
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFOW
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinterW(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinterW(IntPtr hPrinter, int level, ref DOCINFOW di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetDefaultPrinterW([Out] System.Text.StringBuilder? pszBuffer, ref int pcchBuffer);

    /// <summary>Resolves the effective printer name: the given one, or the Windows default if blank.</summary>
    public static string ResolvePrinterName(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return requested;

        int size = 0;
        GetDefaultPrinterW(null, ref size); // first call: ask for the required buffer length
        if (size <= 0)
            return "";
        var sb = new System.Text.StringBuilder(size);
        return GetDefaultPrinterW(sb, ref size) ? sb.ToString() : "";
    }

    public static void Send(string printerName, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("No printer name resolved (and no Windows default printer is set).");

        if (!OpenPrinterW(printerName, out var hPrinter, IntPtr.Zero))
            throw new Win32Exception($"OpenPrinter('{printerName}')");

        var unmanaged = Marshal.AllocCoTaskMem(data.Length);
        try
        {
            var di = new DOCINFOW
            {
                pDocName = "BornoBit Receipt",
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinterW(hPrinter, 1, ref di))
                throw new Win32Exception("StartDocPrinter");
            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new Win32Exception("StartPagePrinter");

                Marshal.Copy(data, 0, unmanaged, data.Length);
                if (!WritePrinter(hPrinter, unmanaged, data.Length, out var written) || written != data.Length)
                    throw new Win32Exception($"WritePrinter (wrote {written}/{data.Length})");

                EndPagePrinter(hPrinter);
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(unmanaged);
            ClosePrinter(hPrinter);
        }
    }

    private sealed class Win32Exception(string op)
        : Exception($"{op} failed (Win32 error {Marshal.GetLastWin32Error()}).");
}
