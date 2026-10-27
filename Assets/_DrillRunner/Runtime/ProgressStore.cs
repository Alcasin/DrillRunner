using System;
using System.IO;
using System.Text;

namespace DrillRunner
{
    public sealed class ProgressLoad
    {
        public readonly PlayerProgress Progress;
        public readonly bool Writable;
        public readonly string Message;
        public ProgressLoad(PlayerProgress progress, bool writable, string message)
        { Progress = progress; Writable = writable; Message = message; }
    }

    public interface IProgressStore
    {
        ProgressLoad Load();
        bool TrySave(PlayerProgress progress, out string error);
    }

    public sealed class MemoryProgressStore : IProgressStore
    {
        private PlayerProgress saved;
        public MemoryProgressStore(PlayerProgress initial = null) { saved = initial?.Copy(); }
        public ProgressLoad Load() => new ProgressLoad(saved?.Copy() ?? new PlayerProgress(), true, "Temporary profile; no disk writes.");
        public bool TrySave(PlayerProgress progress, out string error) { saved = progress.Copy(); error = null; return true; }
    }

    /// <summary>One local profile: flushed staging file, atomic replace, previous valid revision backup, stale-writer protection.</summary>
    public sealed class FileProgressStore : IProgressStore
    {
        public string Path { get; }
        public string BackupPath => Path + ".bak";
        private string expected;
        private bool writable;
        public FileProgressStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !System.IO.Path.IsPathRooted(directory)) throw new ArgumentException("An explicit absolute save directory is required.");
            Path = System.IO.Path.Combine(System.IO.Path.GetFullPath(directory), "drillrunner-progress.json");
        }
        private static bool Read(string path, out string raw, out PlayerProgress progress, out string error)
        {
            raw = null; progress = null; error = "Save is missing.";
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length > 65536) { error = "Save exceeds the supported size; preserved."; return false; }
            raw = File.ReadAllText(path, Encoding.UTF8);
            return ProgressCodec.TryDecode(raw, out progress, out error);
        }
        public ProgressLoad Load()
        {
            writable = false; expected = null;
            try
            {
                if (Read(Path, out var raw, out var progress, out var error))
                { expected = raw; writable = true; return new ProgressLoad(progress, true, "Local progress loaded."); }
                if (!File.Exists(Path) && !File.Exists(BackupPath))
                { writable = true; return new ProgressLoad(new PlayerProgress(), true, "New Pocket Drill profile."); }
                var backupValid = Read(BackupPath, out _, out var backup, out _);
                return new ProgressLoad(backupValid ? backup : new PlayerProgress(), false,
                    error + (backupValid ? " Previous revision shown read-only. Use confirmed development recovery." : " Saving is disabled until recovery."));
            }
            catch (Exception exception) when (IoFailure(exception))
            { return new ProgressLoad(new PlayerProgress(), false, "Save unavailable: " + exception.Message); }
        }
        public bool TrySave(PlayerProgress progress, out string error)
        {
            error = null;
            if (!writable) { error = "Save is read-only; original data preserved."; return false; }
            var temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                using (var writeLock = new FileStream(Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    // Compare and replace under one exclusive writer lock, including multiple local instances.
                    var current = File.Exists(Path) ? File.ReadAllText(Path, Encoding.UTF8) : null;
                    if (current != expected) { error = "Save changed externally. Reload before continuing."; return false; }
                    var json = ProgressCodec.Encode(progress);
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        var bytes = new UTF8Encoding(false).GetBytes(json); stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
                    }
                    if (File.Exists(Path)) File.Replace(temporary, Path, BackupPath);
                    else File.Move(temporary, Path);
                    expected = json; return true;
                }
            }
            catch (Exception exception) when (IoFailure(exception)) { error = "Could not save progress: " + exception.Message; return false; }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (Exception exception) when (IoFailure(exception)) { /* Original and backup remain authoritative. */ }
            }
        }
        private static bool IoFailure(Exception e) => e is IOException || e is UnauthorizedAccessException || e is System.Security.SecurityException;

        // Called only by the Editor inspector after an explicit confirmation. Never auto-downgrade a save.
        public bool RecoverBackup(ProgressionConfig config, out string error)
        {
            error = null;
            try
            {
                if (!Read(BackupPath, out var raw, out var progress, out error)) return false;
                progress.Validate(config);
                if (File.Exists(Path)) File.Copy(Path, Path + ".rejected-" + Guid.NewGuid().ToString("N"), false);
                var recovery = Path + ".recover-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(recovery, raw, new UTF8Encoding(false));
                if (File.Exists(Path)) File.Replace(recovery, Path, null); else File.Move(recovery, Path);
                return true;
            }
            catch (Exception exception) when (IoFailure(exception) || exception is ArgumentException)
            { error = "Recovery failed; original files preserved: " + exception.Message; return false; }
        }
    }
}
