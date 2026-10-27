using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DrillRunner
{
    [Serializable]
    public sealed class PlayerProgress
    {
        public int Version = 1;
        public string ProfileId = Guid.NewGuid().ToString("N");
        public string FormId = ProgressionConfig.PocketForm;
        public long Revision, Coins, Copper, IssuedAttempt, BankedAttempt;
        public int PowerLevel, EnergyLevel, DriveLevel;
        public PlayerProgress Copy() => (PlayerProgress)MemberwiseClone();
        public int Level(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.DrillPower: return PowerLevel;
                case UpgradeCategory.EnergyCapacity: return EnergyLevel;
                case UpgradeCategory.DrivePower: return DriveLevel;
                default: throw new ArgumentException("Unknown upgrade category.");
            }
        }
        internal void Increase(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.DrillPower: PowerLevel++; break;
                case UpgradeCategory.EnergyCapacity: EnergyLevel++; break;
                case UpgradeCategory.DrivePower: DriveLevel++; break;
                default: throw new ArgumentException("Unknown upgrade category.");
            }
        }
        public void Validate(ProgressionConfig config)
        {
            if (Version != 1 || FormId != ProgressionConfig.PocketForm || !Guid.TryParseExact(ProfileId, "N", out _) ||
                Coins < 0 || Copper < 0 || Revision < 0 || IssuedAttempt < 0 || BankedAttempt < 0 || BankedAttempt > IssuedAttempt ||
                PowerLevel < 0 || PowerLevel > config.Power.MaximumLevel || EnergyLevel < 0 || EnergyLevel > config.Energy.MaximumLevel ||
                DriveLevel < 0 || DriveLevel > config.Drive.MaximumLevel)
                throw new ArgumentException("Invalid or unsupported player progression; original save preserved.");
        }
    }

    /// <summary>Integrity detects truncation/accidental corruption; this is not anti-cheat or encryption.</summary>
    public static class ProgressCodec
    {
        [Serializable] private sealed class Document { public int Version; public string Payload, Checksum; }
        private static string Hash(string payload)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
        public static string Encode(PlayerProgress progress)
        {
            var payload = JsonUtility.ToJson(progress);
            return JsonUtility.ToJson(new Document { Version = 1, Payload = payload, Checksum = Hash(payload) }, true);
        }
        public static bool TryDecode(string json, out PlayerProgress progress, out string error)
        {
            progress = null; error = "Malformed, incomplete, or unsupported save; original files preserved.";
            if (string.IsNullOrWhiteSpace(json) || json.Length > 65536) return false;
            try
            {
                var document = JsonUtility.FromJson<Document>(json);
                if (document == null || document.Version != 1 || string.IsNullOrEmpty(document.Payload) || document.Checksum != Hash(document.Payload)) return false;
                progress = JsonUtility.FromJson<PlayerProgress>(document.Payload);
                if (progress == null || progress.Version != 1 || string.IsNullOrEmpty(progress.ProfileId) || string.IsNullOrEmpty(progress.FormId))
                { progress = null; return false; }
                error = null; return true;
            }
            catch (ArgumentException) { return false; }
        }
    }
}
