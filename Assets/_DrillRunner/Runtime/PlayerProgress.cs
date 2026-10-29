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
        // Versioned additive extension; the legacy fields and durable receipts retain their original meaning.
        public int EvolutionSchema;
        public long Iron, Quartz, AlloyCore, EnergyShard;
        public FormUpgradeRecord[] AdvancedForms = Array.Empty<FormUpgradeRecord>();
        public PlayerProgress Copy()
        {
            var copy = (PlayerProgress)MemberwiseClone();
            copy.AdvancedForms = AdvancedForms == null ? null : Array.ConvertAll(AdvancedForms, r => r?.Copy()); return copy;
        }
        public long Resource(EvolutionResource resource)
        {
            switch (resource)
            {
                case EvolutionResource.Copper: return Copper;
                case EvolutionResource.Iron: return Iron;
                case EvolutionResource.Quartz: return Quartz;
                case EvolutionResource.AlloyCore: return AlloyCore;
                case EvolutionResource.EnergyShard: return EnergyShard;
                default: throw new ArgumentException("Unknown resource identifier.");
            }
        }
        internal void Spend(ResourceCost cost)
        {
            switch (cost.Resource)
            {
                case EvolutionResource.Copper: Copper = checked(Copper - cost.Amount); break;
                case EvolutionResource.Iron: Iron = checked(Iron - cost.Amount); break;
                case EvolutionResource.Quartz: Quartz = checked(Quartz - cost.Amount); break;
                case EvolutionResource.AlloyCore: AlloyCore = checked(AlloyCore - cost.Amount); break;
                case EvolutionResource.EnergyShard: EnergyShard = checked(EnergyShard - cost.Amount); break;
                default: throw new ArgumentException("Unknown resource identifier.");
            }
        }
        internal void EnterForm(string id)
        {
            FormId = id; FormSchema = 1; EvolutionSchema = 1;
            if (id == ProgressionConfig.WorkshopForm) WorkshopPowerLevel = WorkshopEnergyLevel = WorkshopDriveLevel = 0;
            else
            {
                var history = AdvancedForms ?? Array.Empty<FormUpgradeRecord>(); var records = new FormUpgradeRecord[history.Length + 1];
                Array.Copy(history, records, history.Length); records[history.Length] = new FormUpgradeRecord { FormId = id }; AdvancedForms = records;
            }
        }
        public int Level(UpgradeCategory category)
        {
            if (!Enum.IsDefined(typeof(UpgradeCategory), category)) throw new ArgumentException("Unknown upgrade category.");
            if (FormId != ProgressionConfig.PocketForm && FormId != ProgressionConfig.WorkshopForm)
            {
                foreach (var record in AdvancedForms ?? Array.Empty<FormUpgradeRecord>()) if (record != null && record.FormId == FormId) return record.Level(category);
                throw new ArgumentException("Missing form upgrade history.");
            }
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
            if (FormId != ProgressionConfig.PocketForm && FormId != ProgressionConfig.WorkshopForm)
            {
                foreach (var record in AdvancedForms) if (record.FormId == FormId)
                {
                    if (category == UpgradeCategory.DrillPower) record.Power++; else if (category == UpgradeCategory.EnergyCapacity) record.Energy++; else record.Drive++; return;
                }
                throw new ArgumentException("Missing current-form history.");
            }
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
            var active = config.ForForm(FormId); var stage = active.Stage;
            if (Version != 1 || stage == 0 ||
                FormSchema < 0 || FormSchema > 1 || !Guid.TryParseExact(ProfileId, "N", out _) ||
                Coins < 0 || Copper < 0 || Revision < 0 || IssuedAttempt < 0 || BankedAttempt < 0 || BankedAttempt > IssuedAttempt ||
                PowerLevel < 0 || PowerLevel > config.Power.MaximumLevel || EnergyLevel < 0 || EnergyLevel > config.Energy.MaximumLevel ||
                DriveLevel < 0 || DriveLevel > config.Drive.MaximumLevel)
                throw new ArgumentException("Invalid or unsupported player progression; original save preserved.");
            var evolved = stage >= 2;
            if (WorkshopPowerLevel < 0 || WorkshopEnergyLevel < 0 || WorkshopDriveLevel < 0 ||
                (!evolved && (WorkshopPowerLevel != 0 || WorkshopEnergyLevel != 0 || WorkshopDriveLevel != 0)) ||
                (evolved && (FormSchema != 1 || config.NextForm == null || PowerLevel != config.Power.MaximumLevel ||
                    EnergyLevel != config.Energy.MaximumLevel || DriveLevel != config.Drive.MaximumLevel ||
                    WorkshopPowerLevel > config.NextForm.Power.MaximumLevel || WorkshopEnergyLevel > config.NextForm.Energy.MaximumLevel ||
                    WorkshopDriveLevel > config.NextForm.Drive.MaximumLevel)))
                throw new ArgumentException("Invalid evolution history or form-specific upgrade levels; save preserved.");
            var records = AdvancedForms ?? Array.Empty<FormUpgradeRecord>();
            if (EvolutionSchema < 0 || EvolutionSchema > 1 || Iron < 0 || Quartz < 0 || AlloyCore < 0 || EnergyShard < 0 ||
                (EvolutionSchema == 0 && (stage > 2 || records.Length != 0 || Iron != 0 || Quartz != 0 || AlloyCore != 0 || EnergyShard != 0)) ||
                records.Length != Math.Max(0, stage - 2) || (stage > 2 && (WorkshopPowerLevel != config.NextForm.Power.MaximumLevel ||
                    WorkshopEnergyLevel != config.NextForm.Energy.MaximumLevel || WorkshopDriveLevel != config.NextForm.Drive.MaximumLevel)))
                throw new ArgumentException("Invalid advanced evolution schema, resources, or ownership history; save preserved.");
            for (var i = 0; i < records.Length; i++)
            {
                var record = records[i]; var form = config.ForForm(ProgressionConfig.FormIds[i + 2]);
                if (record == null || record.FormId != form.FormId) throw new ArgumentException("Missing, duplicate, or out-of-order form history.");
                foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
                {
                    var level = record.Level(category); var maximum = form.Table(category).MaximumLevel;
                    if (level < 0 || level > maximum || (i < records.Length - 1 && level != maximum)) throw new ArgumentException("Invalid purchased form levels.");
                }
            }
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
