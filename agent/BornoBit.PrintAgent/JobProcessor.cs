using System.Collections.Concurrent;
using System.Runtime.Versioning;
using BornoBit.PrintAgent.Printing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BornoBit.PrintAgent;

/// <summary>
/// Turns a <see cref="PrintJobRequest"/> into bytes and pushes them to the spooler.
/// De-duplicates by <see cref="PrintJobRequest.JobId"/> so a server retry (or an at-least-once
/// hub redelivery) never double-prints — the second hit returns Deduplicated=true without printing.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class JobProcessor(IOptions<PrintAgentConfig> options, ILogger<JobProcessor> logger)
{
    private readonly PrintAgentConfig _cfg = options.Value;

    // Bounded recent-job memory: JobId -> insertion order, evicting the oldest past the cap.
    private readonly ConcurrentDictionary<Guid, byte> _seen = new();
    private readonly ConcurrentQueue<Guid> _order = new();
    private const int SeenCapacity = 512;

    public PrintJobResponse Process(PrintJobRequest job)
    {
        if (job.JobId != Guid.Empty && !MarkNew(job.JobId))
        {
            logger.LogInformation("Job {JobId} already printed — skipping (dedup).", job.JobId);
            return new PrintJobResponse(job.JobId, "Ok", Deduplicated: true, "Already printed.");
        }

        try
        {
            var copies = Math.Clamp(job.Copies <= 0 ? 1 : job.Copies, 1, 5);

            byte[] bytes;
            string label, printerCandidate;

            if (job.KitchenTicket is { } kot)
            {
                bytes = KitchenTicketRenderer.Render(kot, _cfg.PaperWidthChars, _cfg.FullCut);
                label = $"KOT_{kot.OrderNumber}";
                printerCandidate = Pick(job.PrinterName, _cfg.KitchenPrinterName, _cfg.ReceiptPrinterName);
            }
            else if (job.Receipt is { } receipt)
            {
                var openDrawer = job.OpenCashDrawer && _cfg.AllowCashDrawer;
                bytes = ReceiptRenderer.Render(receipt, _cfg.PaperWidthChars, openDrawer, _cfg.FullCut, job.IsReprint);
                label = $"RECEIPT_{receipt.OrderNumber}";
                printerCandidate = Pick(job.PrinterName, _cfg.ReceiptPrinterName);
            }
            else
            {
                return new PrintJobResponse(job.JobId, "Error", false, "Job had neither a receipt nor a kitchen ticket.");
            }

            if (_cfg.IsFileMode)
            {
                var path = WriteToDisk(job.JobId, label, bytes);
                logger.LogInformation("FILE MODE: wrote {Label} to {Path} (open the .txt to preview).", label, path);
            }
            else
            {
                var printer = RawPrinter.ResolvePrinterName(printerCandidate);
                SendCopies(printer, bytes, copies);
                logger.LogInformation("Printed {Label} to '{Printer}'.", label, printer);
            }

            return new PrintJobResponse(job.JobId, "Ok", Deduplicated: false);
        }
        catch (Exception ex)
        {
            // Drop it from the dedup set so the server's retry can try again.
            Forget(job.JobId);
            logger.LogError(ex, "Failed to print job {JobId}.", job.JobId);
            return new PrintJobResponse(job.JobId, "Error", false, ex.Message);
        }
    }

    private static void SendCopies(string printer, byte[] bytes, int copies)
    {
        for (var i = 0; i < copies; i++)
            RawPrinter.Send(printer, bytes);
    }

    /// <summary>
    /// Test sink: writes the raw ESC/POS bytes (<c>.bin</c>, for replay to a real printer later) and a
    /// control-code-stripped readable preview (<c>.txt</c>). Returns the folder it wrote into.
    /// </summary>
    private string WriteToDisk(Guid jobId, string label, byte[] bytes)
    {
        var folder = string.IsNullOrWhiteSpace(_cfg.OutputFolder)
            ? Path.Combine(Path.GetTempPath(), "BornoBitPrintAgent")
            : _cfg.OutputFolder;
        Directory.CreateDirectory(folder);

        var stem = $"{Sanitize(label)}_{jobId:N}";
        var binPath = Path.Combine(folder, stem + ".bin");
        var txtPath = Path.Combine(folder, stem + ".txt");

        File.WriteAllBytes(binPath, bytes);
        File.WriteAllText(txtPath, ToReadable(bytes));
        return folder;
    }

    /// <summary>Keep printable ASCII + newlines; drop ESC/GS control bytes so the .txt reads like the receipt.</summary>
    private static string ToReadable(byte[] bytes)
    {
        var sb = new System.Text.StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            if (b == 0x0A) sb.Append('\n');
            else if (b >= 0x20 && b < 0x7F) sb.Append((char)b);
        }
        return sb.ToString();
    }

    private static string Sanitize(string s) =>
        string.Concat(s.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '-' : c));

    private static string Pick(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "";

    private bool MarkNew(Guid jobId)
    {
        if (!_seen.TryAdd(jobId, 0))
            return false;
        _order.Enqueue(jobId);
        while (_order.Count > SeenCapacity && _order.TryDequeue(out var old))
            _seen.TryRemove(old, out _);
        return true;
    }

    private void Forget(Guid jobId) => _seen.TryRemove(jobId, out _);
}
