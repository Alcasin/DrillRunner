using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DrillRunner.Editor
{
    // Authored balance migration only. No profile, transaction or run-time state is touched.
    public static class UltraShortRunSetup
    {
        public const int DistanceCoinsPerPayout = 6;
        public static readonly float[][] Capacity = {
            new[]{25f,30,35,40,45,55}, new[]{90f,100,110,120,130,140},
            new[]{190f,205,220,240,260,280}, new[]{300f,320,340,365,390,420},
            new[]{440f,465,490,520,550,580}, new[]{600f,630,665,700,740,780},
            new[]{800f,845,890,940,990,1040}, new[]{1080f,1140,1200,1265,1330,1400} };
        private static readonly float[][] PreviousCapacity = {
            new[]{100f,115,130,145,160,180}, new[]{200f,220,240,260,280,300},
            new[]{320f,345,370,400,430,460}, new[]{480f,510,540,575,610,650},
            new[]{680f,720,760,805,850,900}, new[]{950f,1000,1060,1120,1180,1250},
            new[]{1320f,1390,1460,1540,1620,1700}, new[]{1800f,1890,1980,2080,2190,2300} };
        private static readonly float[] PreviousMoving = {6,9,14,20,27,32,43,57};
        public static readonly float[] Moving = {3.35f,7.1f,12.5f,17,23,29,36,45};
        public static readonly float[] Blocked = {3.7f,8.1f,13.5f,18,24,30,37,46};
        public static readonly float[] Drilling = {2.7f,2.5f,2.7f,2.9f,3.1f,3.4f,3.6f,3.9f};
        [MenuItem("Tools/DrillRunner/Setup M8-R2 Ultra Short Runs")]
        public static void Setup() => RapidRunSetup.Setup();
        [MenuItem("Tools/DrillRunner/Setup M8-R3 Fast Upgrades")]
        public static void SetupR3() => Setup();
        [MenuItem("Tools/DrillRunner/Setup M8-R4 Coin Economy")]
        public static void SetupR4() => Setup();
        private static void ApplyCoinEconomy(RapidRunBalance balance)
        {
            if (balance.Revision >= 7) return; // Preserve later owner tuning on reruns.
            if (balance.Rewards.DistancePerPayout != 5 || balance.Rewards.ObstacleCoinMultiplier != 25 ||
                (balance.Rewards.CoinsPerPayout != 12 && balance.Rewards.CoinsPerPayout != DistanceCoinsPerPayout))
                throw new InvalidOperationException("M8-R4 preserved custom reward tuning. Review before applying the economy migration.");
            balance.Rewards.CoinsPerPayout = DistanceCoinsPerPayout; balance.Revision = 7;
            balance.Validate(); EditorUtility.SetDirty(balance); AssetDatabase.SaveAssetIfDirty(balance);
            Debug.Log("M8-R4: 6 Coins per completed 5m; normal rewards, Copper, prices, all gameplay tuning and player saves preserved.");
        }
        private static float[] R2Capacity(int i) => i == 0 ? new[]{25f,35,45,55,65,80} :
            i == 1 ? new[]{90f,105,120,140,160,180} : Capacity[i];
        private static void ApplyEarlyUpgradePacing(ProgressionConfig root, RapidRunBalance balance)
        {
            if (balance.Revision >= 6) return; // Preserve subsequent owner Inspector tuning.
            for (int i=0;i<2;i++)
            {
                var form=root.ForForm(ProgressionConfig.FormIds[i]);
                if (!form.Energy.Values.SequenceEqual(R2Capacity(i)) && !form.Energy.Values.SequenceEqual(Capacity[i]))
                    throw new InvalidOperationException("M8-R3 preserved custom early Capacity values for "+form.DisplayName);
            }
            for (int i=0;i<2;i++)
            {
                var form=root.ForForm(ProgressionConfig.FormIds[i]);form.Energy.Values=(float[])Capacity[i].Clone();EditorUtility.SetDirty(form);
            }
            root.Validate();balance.Revision=6;EditorUtility.SetDirty(balance);
            for (int i=0;i<2;i++)AssetDatabase.SaveAssetIfDirty(root.ForForm(ProgressionConfig.FormIds[i]));
            AssetDatabase.SaveAssetIfDirty(balance);
            Debug.Log("M8-R3: early Capacity growth reduced; base Pocket, operating costs, Power/Drive, prices/rewards and EVO 3-8 preserved. No player profile accessed.");
        }
        public static void Apply(ProgressionConfig root, RapidRunBalance balance)
        {
            if (balance.Revision >= 5) { ApplyEarlyUpgradePacing(root, balance); ApplyCoinEconomy(balance); return; } // Future Inspector tuning is never overwritten on reruns.
            bool calibrated = balance.Revision == 4;
            var priorMoving = calibrated ? new[]{3.3f,6.8f,12.5f,17,23,29,36,45} : PreviousMoving;
            var priorBlocked = new[]{3.6f,7.8f,13.5f,18,24,30,37,46};
            var priorDrilling = new[]{3.3f,3,3.2f,3.4f,3.6f,3.9f,4.2f,4.5f};
            // Preflight every owned table before writing anything. Preserve unfamiliar authored tuning.
            for (var i=0;i<8;i++)
            {
                var form=root.ForForm(ProgressionConfig.FormIds[i]); var profile=balance.ForForm(form.FormId);
                if (!form.Energy.Values.SequenceEqual(PreviousCapacity[i]) && !form.Energy.Values.SequenceEqual(Capacity[i]) && !form.Energy.Values.SequenceEqual(R2Capacity(i)))
                    throw new InvalidOperationException("M8-R2 preserved custom Capacity values for "+form.DisplayName);
                if (Mathf.Abs(profile.Moving-priorMoving[i])>0.001f || Mathf.Abs(profile.Blocked-(calibrated ? priorBlocked[i] : PreviousMoving[i]+1))>0.001f ||
                    Mathf.Abs(profile.DrillingBase-(calibrated ? priorDrilling[i] : PreviousMoving[i]+0.2f))>0.001f || Mathf.Abs(profile.MaterialScale-(calibrated ? 0.05f : 0.1f))>0.0001f)
                    throw new InvalidOperationException("M8-R2 preserved custom operating costs for "+form.DisplayName);
            }
            if (balance.Rewards.CoinsPerPayout!=(calibrated ? 12 : 9) || balance.Rewards.DistancePerPayout!=5 || balance.Rewards.ObstacleCoinMultiplier!=25)
                throw new InvalidOperationException("M8-R2 preserved custom reward tuning.");
            var library=AssetDatabase.LoadAssetAtPath<EncounterLibrary>(GameplayVarietySetup.LibraryPath);
            var open=library.Patterns.Where(p=>p.name.StartsWith("OreOpen",StringComparison.Ordinal)).ToArray();
            foreach (var pattern in open)
            {
                var ore=pattern.Section.Obstacles.Single(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper);
                if (Mathf.Abs(ore.transform.localPosition.z-22)>0.001f && Mathf.Abs(ore.transform.localPosition.z-19)>0.001f)
                    throw new InvalidOperationException("M8-R2 preserved custom Ore placement: "+pattern.name);
            }
            foreach (var pattern in open)
            {
                string path=AssetDatabase.GetAssetPath(pattern.Section); var contents=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var ore=contents.GetComponent<TrackSection>().Obstacles.Single(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper);
                    var at=ore.transform.localPosition; at.z=19; ore.transform.localPosition=at;
                    PrefabUtility.SaveAsPrefabAsset(contents,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            foreach (var guard in library.Patterns.Where(p=>p.name.StartsWith("OreGuard",StringComparison.Ordinal)))
            {
                guard.MinimumOrdinal=3;
                guard.Intent="Optional Brick guard and Ore; early Capacity/Power upgrades make the route more affordable. A full-footprint bypass remains.";
                EditorUtility.SetDirty(guard); AssetDatabase.SaveAssetIfDirty(guard);
            }
            GameplayVarietySetup.Validate(library);
            for (var i=0;i<8;i++)
            {
                var form=root.ForForm(ProgressionConfig.FormIds[i]); var profile=balance.ForForm(form.FormId);
                form.Energy.Values=(float[])R2Capacity(i).Clone(); EditorUtility.SetDirty(form);
                profile.Moving=Moving[i]; profile.Blocked=Blocked[i]; profile.DrillingBase=Drilling[i]; profile.MaterialScale=0.05f;
            }
            balance.Rewards.CoinsPerPayout=12; balance.Revision=5;
            root.Validate(); balance.Validate(); EditorUtility.SetDirty(balance);
            for (var form=root;form!=null;form=form.NextForm) AssetDatabase.SaveAssetIfDirty(form);
            AssetDatabase.SaveAssetIfDirty(balance);
            ApplyEarlyUpgradePacing(root, balance);
            ApplyCoinEconomy(balance);
            Debug.Log("M8-R2: compact authoritative Capacity, fixed form operating costs and 12 Coins / 5m; all prices, Power, Drive and player save data preserved.");
        }
    }
}
