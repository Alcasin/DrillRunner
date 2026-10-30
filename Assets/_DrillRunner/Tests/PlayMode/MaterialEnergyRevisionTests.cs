using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private const string EnergyPressureHeader="mode,region,stage,level,seed,policy,seconds,distance,coins,quartz,alloyCore,energyShard,normalCompleted,oreCompleted,totalCompleted,partialContacts,movingEnergy,drillingEnergy,blockedEnergy,movingSeconds,drillingSeconds,blockedSeconds";
        private void ExportEnergyPressure(List<string> rows,List<string> contacts,string mode,int stage,int level,int seed,Policy policy)
        {
            var d=motor.Diagnostics;var measured=d.DrillingContacts.ToArray();int normal=measured.Count(c=>c.Completed&&c.Kind==ObstacleRewardKind.Coins),ore=measured.Count(c=>c.Completed&&MaterialDefinition.IsResource(c.Kind));var run=flow.Run.Rewards;
            Assert.That(normal+ore,Is.EqualTo(d.Destroyed));Assert.That(measured.Sum(c=>c.Energy),Is.EqualTo(d.DrillingEnergy).Within(0.001));
            rows.Add(EconomyCsv(mode,track.RegionId,stage,level,seed,policy,d.Seconds,run.Distance,run.Coins,run.Quartz,run.AlloyCore,run.EnergyShard,normal,ore,normal+ore,measured.Count(c=>!c.Completed),d.MovingEnergy,d.DrillingEnergy,d.BlockedEnergy,d.MovingSeconds,d.DrillingSeconds,d.BlockedSeconds));
            foreach(var c in measured)contacts.Add(EconomyCsv(mode,track.RegionId,stage,level,seed,policy,c.MaterialId,c.MaximumHp,c.Completed,c.Seconds,c.Energy,c.TotalEnergyAtCompletion));
            if(mode!="Live"){File.WriteAllLines("Logs/M9BR-Profiles.csv",rows);File.WriteAllLines("Logs/M9BR-Contacts.csv",contacts);}
        }
        [UnityTest,Timeout(600000)] public IEnumerator MaterialEnergyRevisionComparesActualBeforeAfterCostsCountsAndNexus()
        {
            var rows=new List<string>{EnergyPressureHeader};var contacts=new List<string>{"mode,region,stage,level,seed,policy,material,hp,completed,drillingSeconds,drillingEnergy,totalEnergyAtCompletion"};
            bool revised=flow.Regions.Find(RegionIds.Advanced).DrillingCosts.Length>0;
            foreach(string mode in revised?new[]{"M9B","M9BR"}:new[]{"M9B"})
            foreach(string region in new[]{RegionIds.Advanced,RegionIds.Energy})
            foreach(var profile in region==RegionIds.Advanced?new[]{new[]{5,0},new[]{5,2},new[]{5,5},new[]{6,0},new[]{6,5}}:new[]{new[]{7,0},new[]{7,5},new[]{8,0},new[]{8,5}})
            foreach(int seed in new[]{1337,904,2026,8101,9201})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
            {
                ConfigureAdvanced(region,profile[0],profile[1],seed);
                if(mode=="M9B"){motor.ApplyMaterialDrillingCosts(Array.Empty<MaterialDrillingCost>());hud.Restart();}
                advancedGoal=region==RegionIds.Energy?ObstacleRewardKind.EnergyShard:profile[0]>=6?ObstacleRewardKind.AlloyCore:ObstacleRewardKind.Quartz;
                yield return Drive(policy);ExportEnergyPressure(rows,contacts,mode,profile[0],profile[1],seed,policy);
                Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(motor.Diagnostics.Seconds,Is.LessThan(65));yield return null;
            }
            if(revised)
            {
                var historicalNexus=rows.Where(r=>r.StartsWith("M9B,"+RegionIds.Energy+",")).Select(r=>r.Substring(r.IndexOf(',')+1)).ToArray();
                var currentNexus=rows.Where(r=>r.StartsWith("M9BR,"+RegionIds.Energy+",")).Select(r=>r.Substring(r.IndexOf(',')+1)).ToArray();
                Assert.That(currentNexus,Is.EqualTo(historicalNexus),"Shared materials must retain exact Nexus outcomes.");
                // Regression medians for this informed policy; no production count/timer termination exists.
                foreach(var expected in new[]{new[]{0,2,3},new[]{2,3,4},new[]{5,4,6}})
                {
                    var counts=rows.Skip(1).Select(r=>r.Split(',')).Where(r=>r[0]=="M9BR"&&r[1]==RegionIds.Advanced&&r[2]=="5"&&r[3]==expected[0].ToString()&&r[5]=="Balanced").Select(r=>int.Parse(r[14])).OrderBy(n=>n).ToArray();
                    Assert.That(counts.Length,Is.EqualTo(5));Assert.That(counts[2],Is.InRange(expected[1],expected[2]));
                }
            }
        }
    }
}
