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
        // Additive schema-1 extension. Zero denotes a valid original M4 Pocket profile.
        public int FormSchema, WorkshopPowerLevel, WorkshopEnergyLevel, WorkshopDriveLevel;
        public PlayerProgress Copy() => (PlayerProgress)MemberwiseClone();
        public int Level(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.DrillPower: return FormId == ProgressionConfig.WorkshopForm ? WorkshopPowerLevel : PowerLevel;
                case UpgradeCategory.EnergyCapacity: return FormId == ProgressionConfig.WorkshopForm ? WorkshopEnergyLevel : EnergyLevel;
                case UpgradeCategory.DrivePower: return FormId == ProgressionConfig.WorkshopForm ? WorkshopDriveLevel : DriveLevel;
                default: throw new ArgumentException("Unknown upgrade category.");
            }
        }
        internal void Increase(UpgradeCategory category)
        {
            if (FormId == ProgressionConfig.WorkshopForm)
            {
                switch (category)
                {
                    case UpgradeCategory.DrillPower: WorkshopPowerLevel++; return;
                    case UpgradeCategory.EnergyCapacity: WorkshopEnergyLevel++; return;
                    case UpgradeCategory.DrivePower: WorkshopDriveLevel++; return;
                    default: throw new ArgumentException("Unknown upgrade category.");
                }
            }
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
            if (Version != 1 || (FormId != ProgressionConfig.PocketForm && FormId != ProgressionConfig.WorkshopForm) ||
                FormSchema < 0 || FormSchema > 1 || !Guid.TryParseExact(ProfileId, "N", out _) ||
                Coins < 0 || Copper < 0 || Revision < 0 || IssuedAttempt < 0 || BankedAttempt < 0 || BankedAttempt > IssuedAttempt ||
                PowerLevel < 0 || PowerLevel > config.Power.MaximumLevel || EnergyLevel < 0 || EnergyLevel > config.Energy.MaximumLevel ||
                DriveLevel < 0 || DriveLevel > config.Drive.MaximumLevel)
                throw new ArgumentException("Invalid or unsupported player progression; original save preserved.");
            var evolved = FormId == ProgressionConfig.WorkshopForm;
            if (WorkshopPowerLevel < 0 || WorkshopEnergyLevel < 0 || WorkshopDriveLevel < 0 ||
                (!evolved && (WorkshopPowerLevel != 0 || WorkshopEnergyLevel != 0 || WorkshopDriveLevel != 0)) ||
                (evolved && (FormSchema != 1 || config.NextForm == null || PowerLevel != config.Power.MaximumLevel ||
                    EnergyLevel != config.Energy.MaximumLevel || DriveLevel != config.Drive.MaximumLevel ||
                    WorkshopPowerLevel > config.NextForm.Power.MaximumLevel || WorkshopEnergyLevel > config.NextForm.Energy.MaximumLevel ||
                    WorkshopDriveLevel > config.NextForm.Drive.MaximumLevel)))
                throw new ArgumentException("Invalid evolution history or form-specific upgrade levels; save preserved.");
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
