using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Contracts.DebugEvents;

/// <summary>
/// Writes decoded batches as JSON Lines (design D7): one complete JSON object per line, lines end with <c>\n</c>, no
/// wrapping array, no separators, no header. Counts lines and UTF-8 bytes, and sets <see cref="CapReached"/> after the
/// whole line that brings the total to the cap. Plain C#, no Unity types; the clock is injected.
/// </summary>
internal sealed class DebugEventLineWriter
{
    /// <summary>The file size at which the recorder stops writing (64 MiB).</summary>
    public const long DefaultCapBytes = 64L * 1024 * 1024;

    /// <summary>Kind of the line that reports dropped events.</summary>
    public const string DroppedKind = "debug-events-dropped";

    private readonly TextWriter _writer;
    private readonly Func<double> _secondsSinceStart;
    private readonly long _capBytes;

    /// <param name="writer">Destination. Lines are written with <c>\n</c> regardless of platform.</param>
    /// <param name="secondsSinceStart">Clock for <c>t</c>, seconds since game start.</param>
    /// <param name="existingLines">Lines already in the destination (append), so <c>n</c> keeps rising.</param>
    /// <param name="existingBytes">Bytes already in the destination (append), counted against the cap.</param>
    /// <param name="capBytes">Size at which writing stops. Defaults to 64 MiB.</param>
    public DebugEventLineWriter(
        TextWriter writer,
        Func<double> secondsSinceStart,
        long existingLines = 0,
        long existingBytes = 0,
        long capBytes = DefaultCapBytes)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _secondsSinceStart = secondsSinceStart ?? throw new ArgumentNullException(nameof(secondsSinceStart));
        _capBytes = capBytes;
        Lines = existingLines;
        Bytes = existingBytes;
        CapReached = existingBytes >= capBytes;
    }

    /// <summary>Lines written so far, including any that were already in the file.</summary>
    public long Lines { get; private set; }

    /// <summary>UTF-8 bytes written so far, including any that were already in the file.</summary>
    public long Bytes { get; private set; }

    /// <summary>True once a whole line brought the total to the cap. Nothing is written after that.</summary>
    public bool CapReached { get; private set; }

    /// <summary>
    /// Writes the dropped-count line (when the batch reports drops) and one line per event, in order. Does nothing
    /// once <see cref="CapReached"/> is true.
    /// </summary>
    /// <param name="reporter">Reporting mod id, used when the batch itself names none.</param>
    /// <param name="batch">The decoded batch.</param>
    public void Write(string reporter, DebugEventBatch batch)
    {
        if (CapReached || batch == null)
        {
            return;
        }

        var sender = string.IsNullOrEmpty(batch.Reporter) ? reporter : batch.Reporter;
        if (batch.Dropped > 0)
        {
            WriteLine(new JObject
            {
                ["n"] = Lines + 1,
                ["t"] = _secondsSinceStart(),
                ["at"] = UtcNowIso(),
                ["source"] = "sdk",
                ["reporter"] = sender,
                ["kind"] = DroppedKind,
                ["fields"] = new JObject { ["count"] = batch.Dropped }
            });
        }

        foreach (var record in batch.Events)
        {
            if (CapReached)
            {
                return;
            }

            var line = new JObject
            {
                ["n"] = Lines + 1,
                ["t"] = _secondsSinceStart(),
                ["at"] = string.IsNullOrEmpty(record.At) ? UtcNowIso() : record.At,
                ["source"] = "sdk",
                ["reporter"] = sender,
                ["kind"] = record.Kind
            };
            if (record.Detail != null)
            {
                line["detail"] = record.Detail;
            }

            if (record.Fields != null)
            {
                line["fields"] = record.Fields;
            }

            WriteLine(line);
        }
    }

    /// <summary>Flushes the destination. The recorder calls it after each batch.</summary>
    public void Flush() => _writer.Flush();

    private void WriteLine(JObject line)
    {
        // Newtonsoft escapes control characters inside strings, so the serialized object never holds a raw line break.
        var json = JsonConvert.SerializeObject(line, Formatting.None);
        _writer.Write(json);
        _writer.Write('\n');
        Lines++;
        Bytes += Encoding.UTF8.GetByteCount(json) + 1;
        if (Bytes >= _capBytes)
        {
            CapReached = true;
        }
    }

    private static string UtcNowIso() => DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
}
