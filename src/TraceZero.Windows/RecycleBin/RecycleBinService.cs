using System.Runtime.InteropServices;
using TraceZero.Application.Cleaning;

namespace TraceZero.Windows.RecycleBin;

/// <summary>
/// Implémentation Windows de <see cref="IRecycleBinService"/> via l'API Shell (SHQueryRecycleBin /
/// SHEmptyRecycleBin). Aucune suppression de chemin brute n'est effectuée.
/// </summary>
public sealed class RecycleBinService : IRecycleBinService
{
    private const int S_OK = 0;

    [Flags]
    private enum RecycleFlags : uint
    {
        NoConfirmation = 0x00000001,
        NoProgressUi = 0x00000002,
        NoSound = 0x00000004,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBinW(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBinW(IntPtr hwnd, string? pszRootPath, RecycleFlags dwFlags);

    public long GetUsedBytes() => Query().i64Size;

    public long GetItemCount() => Query().i64NumItems;

    public long Empty()
    {
        var before = Query().i64Size;
        var flags = RecycleFlags.NoConfirmation | RecycleFlags.NoProgressUi | RecycleFlags.NoSound;

        // pszRootPath null = toutes les corbeilles de tous les volumes.
        var result = SHEmptyRecycleBinW(IntPtr.Zero, null, flags);

        // S_OK ou "corbeille déjà vide" sont des succès.
        return result == S_OK ? before : 0;
    }

    private static SHQUERYRBINFO Query()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        var result = SHQueryRecycleBinW(null, ref info);
        return result == S_OK ? info : new SHQUERYRBINFO { cbSize = info.cbSize };
    }
}
