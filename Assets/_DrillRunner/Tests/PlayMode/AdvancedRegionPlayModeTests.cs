using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private ObstacleRewardKind advancedGoal=ObstacleRewardKind.Quartz;
        private float ChooseAdvanced(Policy policy,bool collect)
        {
            var section=track.ActiveSections.FirstOrDefault(s=>motor.Motion.Position.z>=s.StartZ&&motor.Motion.Position.z<s.EndZ);if(section==null)return aim;
            var targets=section.Obstacles.Where(o=>o.IsSolid&&o.WorldBounds.max.z>=motor.Motion.Position.z-1.15f).ToArray();var front=targets.OrderBy(o=>o.WorldBounds.min.z).FirstOrDefault();
            if(front!=observedFront){observedFront=front;decisionAt=motor.Diagnostics.Seconds+0.35;}
            if(motor.Motion.State==RunState.Blocked){if(blockedAt<0)blockedAt=motor.Diagnostics.Seconds;if(motor.Diagnostics.Seconds-blockedAt<0.35)return aim;return Free(targets);}blockedAt=-1;
            if(motor.Diagnostics.Seconds<decisionAt)return aim;if(policy==Policy.Safe)return Free(targets);
            float reserve=Mathf.Max(1,motor.Drilling.MovingEnergyPerSecond*0.61f);
            // Balanced/Power aim at one configured node per attempt. Pure Resource keeps mining feasible nodes.
            int nodePayout=flow.SelectedRegion.Materials.First(m=>m.RewardKind==advancedGoal).RewardAmount;
            bool seek=collect&&(policy==Policy.Resource||flow.Run.Rewards.Resource(advancedGoal)<nodePayout);
            if(seek)
            {
                foreach(var ore in targets.Where(o=>o.Definition.RewardKind==advancedGoal&&o.WorldBounds.min.z<motor.Motion.Position.z+32))
                {
                    float x=ore.WorldBounds.center.x;var line=targets.Where(o=>o.WorldBounds.min.z<=ore.WorldBounds.max.z&&EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)).ToArray();
                    if(line.All(o=>o.Definition.Hardness<=motor.DrillPower&&Aligned(x,o))&&Cost(line)+reserve<motor.Energy.Remaining&&Cost(line)<motor.Energy.Capacity*0.65f)return x;
                }
            }
            if(policy!=Policy.Resource&&front!=null)
            {
                var group=targets.Where(o=>o.WorldBounds.min.z<front.WorldBounds.min.z+3&&!MaterialDefinition.IsResource(o.Definition.RewardKind));
                var ordered=policy==Policy.Power?group.OrderByDescending(o=>o.Definition.Hardness):group.OrderBy(o=>o.Definition.Hardness);
                foreach(var target in ordered)
                {
                    float x=target.WorldBounds.center.x;var line=targets.Where(o=>o.WorldBounds.min.z<=target.WorldBounds.max.z&&EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)).ToArray();
                    if(line.All(o=>o.Definition.Hardness<=motor.DrillPower&&Aligned(x,o))&&Cost(line)+reserve<motor.Energy.Remaining&&Cost(line)<motor.Energy.Capacity*0.4f)return x;
                }
            }
            return Free(targets);
        }
        private void ConfigureAdvanced(string region,int stage,int level,int seed)
        {
            flow.UseTemporaryStage(stage,1000000,0,0,0,0,0,level==5);
            if(level>0&&level<5)foreach(UpgradeCategory c in Enum.GetValues(typeof(UpgradeCategory)))for(int i=0;i<level;i++)Assert.That(flow.Purchase(c),Is.True);
            BeginRegionalAttempt(region,seed);advancedGoal=region==RegionIds.Energy?ObstacleRewardKind.EnergyShard:ObstacleRewardKind.Quartz;
        }
        private void BeginRegionalAttempt(string region,int seed)
        {
            Assert.That(flow.SelectRegion(region),Is.True);Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;
            flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=seed;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
        }
        [UnityTest,Timeout(600000)] public IEnumerator AdvancedRegionPoliciesMeasureActualDurationCoinsAndAllResources()
        {
            var rows=new List<string>{Sample.Header+",region,quartz,alloyCore,energyShard"};
            foreach(var region in new[]{RegionIds.Advanced,RegionIds.Energy})
            foreach(var stage in region==RegionIds.Advanced?new[]{5,6,7,8}:new[]{7,8})foreach(var level in new[]{0,5})
            foreach(int seed in new[]{1337,904,2026})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
            {
                ConfigureAdvanced(region,stage,level,seed);if(region==RegionIds.Advanced&&stage>=6)advancedGoal=ObstacleRewardKind.AlloyCore;
                yield return Drive(policy);var run=flow.Run.Rewards;
                rows.Add(last.Csv("M9B")+EconomyCsv("",region,run.Quartz,run.AlloyCore,run.EnergyShard));File.WriteAllLines("Logs/M9B-AdvancedProfiles.csv",rows);
                Assert.That(last.Seconds,Is.LessThan(65));Assert.That(track.ActiveSections.Count,Is.LessThanOrEqualTo(10));Assert.That(motor.RegisteredObstacles.Count,Is.LessThanOrEqualTo(40));
                if(policy==Policy.Resource)Assert.That(run.Resource(advancedGoal),Is.GreaterThan(0));if(policy==Policy.Safe)Assert.That(run.ObstacleCoins+run.Quartz+run.AlloyCore+run.EnergyShard,Is.Zero);
                yield return null;
            }
        }
        private string NeededRegion()
        {
            var p=flow.Service.Snapshot;int stage=flow.Service.ActiveForm.Stage;
            if(stage<3)return RegionIds.Yard;
            if(stage<5){long need=flow.Service.ActiveForm.RequiredResources.Where(c=>c.Resource==EvolutionResource.Iron).Sum(c=>c.Amount);return p.Iron>=need&&stage==4&&p.Copper<12?RegionIds.Yard:RegionIds.Quarry;}
            if(stage<7)return RegionIds.Advanced;
            return p.AlloyCore<12?RegionIds.Advanced:RegionIds.Energy;
        }
        private bool SetResourceGoal(string region)
        {
            var p=flow.Service.Snapshot;var form=flow.Service.ActiveForm;
            if(region==RegionIds.Yard)return p.Copper<form.RequiredResources.Where(c=>c.Resource==EvolutionResource.Copper).Sum(c=>c.Amount);
            if(region==RegionIds.Quarry)return p.Iron<form.RequiredResources.Where(c=>c.Resource==EvolutionResource.Iron).Sum(c=>c.Amount);
            if(region==RegionIds.Energy){advancedGoal=ObstacleRewardKind.EnergyShard;return p.EnergyShard<30;}
            bool quartz=p.Quartz<(form.Stage==5?40:form.Stage==6?20:0);advancedGoal=quartz?ObstacleRewardKind.Quartz:ObstacleRewardKind.AlloyCore;
            return quartz||p.AlloyCore<(form.Stage==6?24:form.Stage==7?12:0);
        }
        private IEnumerator Journey(int family,Policy strategy,int startStage,List<string> rows,string path)
        {
            int attempts=0;
            while(flow.Service.ActiveForm.Stage<8&&attempts<650)
            {
                int stage=flow.Service.ActiveForm.Stage;string region=NeededRegion();var before=flow.Service.Snapshot;
                BeginRegionalAttempt(region,family+ ++attempts);bool seek=SetResourceGoal(region);
                // Explicit controls: Safe mines only after MAX to satisfy the resource gate. Resource switches
                // to balanced normal mining once its current resource goal is met; the pure Ore policy is in the matrix.
                var actual=strategy==Policy.Safe&&AllCurrentUpgradesMaximum()?Policy.Resource:strategy==Policy.Resource&&!seek?Policy.Balanced:strategy;
                yield return Drive(actual,seek);var run=flow.Run.Rewards;Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(before.Coins+run.Coins));
                Assert.That(flow.Service.Snapshot.Quartz,Is.EqualTo(before.Quartz+run.Quartz));Assert.That(flow.Service.Snapshot.AlloyCore,Is.EqualTo(before.AlloyCore+run.AlloyCore));Assert.That(flow.Service.Snapshot.EnergyShard,Is.EqualTo(before.EnergyShard+run.EnergyShard));
                flow.OpenWorkshop();int bought=BuyEconomyUpgrades(strategy);bool evolved=flow.Evolution.Ready;
                rows.Add(EconomyCsv(family,strategy,startStage,attempts,stage,region,last.Seconds,last.Distance,last.Coins,last.Copper,last.Iron,run.Quartz,run.AlloyCore,run.EnergyShard,last.Drilling,last.DrillEnergy,bought,flow.Service.Snapshot.Coins,flow.Service.Snapshot.Quartz,flow.Service.Snapshot.AlloyCore,flow.Service.Snapshot.EnergyShard,evolved,actual));File.WriteAllLines(path,rows);
                if(evolved){Assert.That(AllCurrentUpgradesMaximum(),Is.True);Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(0.95f);Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(stage+1));}
                yield return null;
            }
            Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(8));Assert.That(flow.IsSandbox,Is.False);Assert.That(flow.Evolution.HasNext,Is.False);
        }
        private const string JourneyHeader="family,strategy,startStage,attempt,stage,region,seconds,distance,coins,copper,iron,quartz,alloyCore,energyShard,drillingSeconds,drillingEnergy,purchases,wallet,quartzBalance,alloyBalance,shardBalance,evolved,actualPolicy";
        [UnityTest,Timeout(600000)] public IEnumerator FullyUnfundedFreshProfilesReachQuantumThroughAllFourRegions()
        {
            var rows=new List<string>{JourneyHeader};
            foreach(int family in new[]{8100,9200,10300})
            {
                memory=new MemoryProgressStore();yield return Load();Assert.That(flow.Service.Snapshot.Coins+flow.Service.Snapshot.Copper+flow.Service.Snapshot.Iron+flow.Service.Snapshot.Quartz+flow.Service.Snapshot.AlloyCore+flow.Service.Snapshot.EnergyShard,Is.Zero);
                yield return Journey(family,Policy.Balanced,1,rows,"Logs/M9B-FullJourneys.csv");
                Assert.That(flow.Service.Snapshot.RegionRecords.Length,Is.EqualTo(4));
            }
        }
        [UnityTest,Timeout(1200000)] public IEnumerator UnfundedAdvancedPoliciesExposeProgressionAndBypassGrind()
        {
            var rows=new List<string>{JourneyHeader};
            foreach(int family in new[]{8100,9200,10300})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
            {
                // Prior ownership selects this experiment's initial stage; current Coins/resources/upgrades are zero.
                yield return MiddleStart(5);Assert.That(flow.IsSandbox,Is.False);yield return Journey(family,policy,5,rows,"Logs/M9B-AdvancedJourneys.csv");
            }
        }
    }
}
