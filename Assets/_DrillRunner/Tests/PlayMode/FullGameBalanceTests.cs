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
        private enum GameStyle { Balanced, Resource, Power, Safe, Casual, Cautious }
        private System.Random casualRandom;
        private DrillObstacle casualFront,casualIgnored;
        private double casualDecision,casualBlocked=-1;
        private bool casualLowReward;
        private const string GameHeader="family,style,attempt,stage,region,seed,powerLevel,energyLevel,driveLevel,seconds,distance,coins,copper,iron,quartz,alloyCore,energyShard,normalCompleted,oreCompleted,movingEnergy,drillingEnergy,blockedEnergy,movingSeconds,drillingSeconds,blockedSeconds,affordable,purchases,powerAfter,energyAfter,driveAfter,wallet,max,resourcesReady,ready,visited,peakSections,peakTargets";
        private static string PrimaryRegion(int stage)=>stage<3?RegionIds.Yard:stage<5?RegionIds.Quarry:stage<7?RegionIds.Advanced:RegionIds.Energy;
        private float VisibleBypass(DrillObstacle[] targets)
        {
            foreach(float x in new[]{-2.05f,2.05f}.OrderBy(x=>Mathf.Abs(x-motor.Motion.Position.x)))
                if(targets.All(o=>!EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)))return x;
            return motor.Motion.Position.x>0?-2.05f:2.05f;
        }
        private void BeginCasual(int seed)
        {
            casualRandom=new System.Random(unchecked(seed*31+1709));casualFront=casualIgnored=null;casualDecision=0;casualBlocked=-1;casualLowReward=casualRandom.Next(8)==0;
        }
        private float ChooseCasual(bool seek)
        {
            // Only visible geometry, 22 m ahead, delayed observation, occasional skipped nodes/poor alignment.
            // No access to the encounter plan, future layout, generator RNG, or future rewards.
            var targets=track.ActiveSections.SelectMany(s=>s.Obstacles).Where(o=>o.IsSolid&&o.WorldBounds.max.z>=motor.Motion.Position.z-1.15f&&o.WorldBounds.min.z<motor.Motion.Position.z+22).ToArray();
            if(motor.Motion.State==RunState.Blocked)
            {
                if(casualBlocked<0)casualBlocked=motor.Diagnostics.Seconds;
                if(motor.Diagnostics.Seconds-casualBlocked<0.65)return aim;
                casualIgnored=motor.ContactTarget;return VisibleBypass(targets);
            }
            casualBlocked=-1;
            var front=targets.OrderBy(o=>o.WorldBounds.min.z).FirstOrDefault();
            if(front!=casualFront){casualFront=front;casualDecision=motor.Diagnostics.Seconds+0.55;}
            if(motor.Diagnostics.Seconds<casualDecision)return aim;
            if(front==null||front==casualIgnored||casualLowReward)return VisibleBypass(targets);
            casualDecision=motor.Diagnostics.Seconds+0.55;
            int choice=casualRandom.Next(100);if(choice<18)return VisibleBypass(targets);
            var goal=track.RegionId==RegionIds.Yard?ObstacleRewardKind.Copper:track.RegionId==RegionIds.Quarry?ObstacleRewardKind.Iron:advancedGoal;
            var target=seek&&choice<78?targets.FirstOrDefault(o=>o!=casualIgnored&&o.Definition.RewardKind==goal):null;
            if(target==null)target=targets.FirstOrDefault(o=>o!=casualIgnored&&!MaterialDefinition.IsResource(o.Definition.RewardKind));
            if(target==null||target.Definition.Hardness>motor.DrillPower*(choice>90?1.15f:1)||Cost(new[]{target})*0.85f>motor.Energy.Remaining)return VisibleBypass(targets);
            return Mathf.Clamp(target.WorldBounds.center.x+(choice>84?0.6f:0),-2.15f,2.15f);
        }
        private IEnumerator CasualAttempt(bool seek,int seed)
        {
            BeginCasual(seed);Launch();int ticks=0;
            while(motor.Motion.State!=RunState.RunEnded&&ticks++<12000)
            {
                aim=ChooseCasual(seek);Steer(aim);Tick(0.05f);if(ticks%250==0)yield return null;
            }
            Assert.That(motor.Diagnostics.EndedByEnergy&&flow.IsBanked,Is.True);Assert.That(motor.Diagnostics.Consumed,Is.EqualTo(motor.Energy.Capacity).Within(0.001));
        }
        private float ChooseCautious(bool seek)
        {
            var section=track.ActiveSections.FirstOrDefault(s=>motor.Motion.Position.z>=s.StartZ&&motor.Motion.Position.z<s.EndZ);if(section==null)return aim;
            var targets=section.Obstacles.Where(o=>o.IsSolid&&o.WorldBounds.max.z>=motor.Motion.Position.z-1.15f).ToArray();var front=targets.OrderBy(o=>o.WorldBounds.min.z).FirstOrDefault();
            if(front!=observedFront){observedFront=front;decisionAt=motor.Diagnostics.Seconds+0.5;}
            if(motor.Motion.State==RunState.Blocked){if(blockedAt<0)blockedAt=motor.Diagnostics.Seconds;if(motor.Diagnostics.Seconds-blockedAt<0.5)return aim;return Free(targets);}blockedAt=-1;
            if(motor.Diagnostics.Seconds<decisionAt)return aim;
            var goal=track.RegionId==RegionIds.Yard?ObstacleRewardKind.Copper:track.RegionId==RegionIds.Quarry?ObstacleRewardKind.Iron:advancedGoal;
            var choices=seek&&flow.Run.Rewards.Resource(goal)==0?targets.Where(o=>o.Definition.RewardKind==goal&&o.WorldBounds.min.z<motor.Motion.Position.z+28):Enumerable.Empty<DrillObstacle>();
            foreach(var target in choices.Concat(targets.Where(o=>o.Definition.RewardKind==ObstacleRewardKind.Coins&&front!=null&&o.WorldBounds.min.z<front.WorldBounds.min.z+3).OrderBy(o=>Cost(new[]{o}))))
            {
                float x=target.WorldBounds.center.x;var line=targets.Where(o=>o.WorldBounds.min.z<=target.WorldBounds.max.z&&EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)).ToArray();float cost=Cost(line);
                if(line.All(o=>o.Definition.Hardness<=motor.DrillPower&&Aligned(x,o))&&cost+motor.Drilling.MovingEnergyPerSecond*0.8f<motor.Energy.Remaining&&cost<motor.Energy.Capacity*(MaterialDefinition.IsResource(target.Definition.RewardKind)?0.55f:0.25f))return x;
            }
            return Free(targets);
        }
        private IEnumerator CautiousAttempt(bool seek)
        {
            Launch();int ticks=0;while(motor.Motion.State!=RunState.RunEnded&&ticks++<12000){aim=ChooseCautious(seek);Steer(aim);Tick(0.05f);if(ticks%250==0)yield return null;}
            Assert.That(motor.Diagnostics.EndedByEnergy&&flow.IsBanked,Is.True);Assert.That(motor.Diagnostics.Consumed,Is.EqualTo(motor.Energy.Capacity).Within(0.001));
        }
        private int BuyGameUpgrades(GameStyle style)
        {
            if(style!=GameStyle.Casual)return BuyEconomyUpgrades(style==GameStyle.Power?Policy.Power:style==GameStyle.Resource?Policy.Resource:style==GameStyle.Safe?Policy.Safe:Policy.Balanced);
            int purchases=0;
            while(AffordableUpgradeCount()>0)
            {
                var p=flow.Service.Snapshot;var categories=Enum.GetValues(typeof(UpgradeCategory)).Cast<UpgradeCategory>().Where(c=>p.Level(c)<5&&flow.Service.ActiveForm.Table(c).Costs[p.Level(c)]<=p.Coins).ToArray();
                Assert.That(flow.Purchase(categories[casualRandom.Next(categories.Length)]),Is.True);purchases++;
            }
            return purchases;
        }
        [UnityTest,Timeout(1200000)] public IEnumerator FullGameJourneysMeasureFivePoliciesPurchaseGapsAndLegitimateQuantumUpgrades()
        {yield return GameJourneys(false);}
        [UnityTest,Timeout(600000)] public IEnumerator FullGameCautiousMiningSeparatesReasonableSafetyFromPureAvoidance()
        {yield return GameJourneys(true);}
        private IEnumerator GameJourneys(bool cautiousOnly)
        {
            string path=cautiousOnly?"Logs/M10-CautiousJourneys.csv":"Logs/M10-Journeys.csv";
            var rows=new List<string>{GameHeader};
            File.WriteAllLines(path,rows);
            foreach(int family in new[]{8100,9200,10300})foreach(GameStyle style in cautiousOnly?new[]{GameStyle.Cautious}:new[]{GameStyle.Balanced,GameStyle.Resource,GameStyle.Power,GameStyle.Safe,GameStyle.Casual})
            {
                memory=new MemoryProgressStore();yield return Load();Assert.That(flow.Service.Snapshot.Coins,Is.Zero);Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(1));int attempt=0,totalPurchases=0;bool reachedQuantum=false;
                while((flow.Service.ActiveForm.Stage<8||style==GameStyle.Balanced&&!AllCurrentUpgradesMaximum())&&attempt<900)
                {
                    int stage=flow.Service.ActiveForm.Stage;if(stage==8&&!reachedQuantum){Assert.That(totalPurchases,Is.EqualTo(105));reachedQuantum=true;}
                    string region=stage==8?RegionIds.Energy:NeededRegion();var before=flow.Service.Snapshot;int seed=family+ ++attempt;
                    BeginRegionalAttempt(region,seed);bool seek=stage<8&&SetResourceGoal(region);
                    var visited=new HashSet<string>();int sections=0,targets=0;
                    Action observe=()=>{if(motor.Motion.State==RunState.Ready||motor.Motion.State==RunState.Charging)return;sections=Math.Max(sections,track.ActiveSections.Count);targets=Math.Max(targets,motor.RegisteredObstacles.Count);foreach(var s in track.ActiveSections)if(motor.Motion.Position.z>=s.StartZ&&motor.Motion.Position.z<s.EndZ)visited.Add(s.SectionId);};
                    motor.SimulationAdvanced+=observe;
                    try
                    {
                        if(style==GameStyle.Casual)yield return CasualAttempt(seek,seed);
                        else if(style==GameStyle.Cautious)yield return CautiousAttempt(seek);
                        else
                        {
                            var actual=style==GameStyle.Safe?(seek?Policy.Resource:Policy.Safe):style==GameStyle.Resource?(seek?Policy.Resource:Policy.Balanced):style==GameStyle.Power?Policy.Power:Policy.Balanced;
                            yield return Drive(actual,seek);
                        }
                    }
                    finally{motor.SimulationAdvanced-=observe;}
                    var earned=flow.Run.Rewards;var bank=flow.Service.Snapshot;Assert.That(bank.Coins,Is.EqualTo(before.Coins+earned.Coins));
                    foreach(EvolutionResource resource in Enum.GetValues(typeof(EvolutionResource)))Assert.That(bank.Resource(resource),Is.EqualTo(before.Resource(resource)+earned.Resource((ObstacleRewardKind)((int)resource+1))));
                    string receipt=ProgressCodec.Encode(bank);Assert.That(flow.RetryBank(),Is.True);Assert.That(ProgressCodec.Encode(flow.Service.Snapshot),Is.EqualTo(receipt));
                    Assert.That(flow.OpenWorkshop(),Is.True);int affordable=AffordableUpgradeCount(),bought=BuyGameUpgrades(style);totalPurchases+=bought;
                    var after=flow.Service.Snapshot;var d=motor.Diagnostics;var contacts=d.DrillingContacts.ToArray();bool ready=flow.Evolution.Ready;
                    rows.Add(EconomyCsv(family,style,attempt,stage,region,seed,before.Level(UpgradeCategory.DrillPower),before.Level(UpgradeCategory.EnergyCapacity),before.Level(UpgradeCategory.DrivePower),d.Seconds,earned.Distance,earned.Coins,earned.Copper,earned.Iron,earned.Quartz,earned.AlloyCore,earned.EnergyShard,contacts.Count(c=>c.Completed&&c.Kind==ObstacleRewardKind.Coins),contacts.Count(c=>c.Completed&&MaterialDefinition.IsResource(c.Kind)),d.MovingEnergy,d.DrillingEnergy,d.BlockedEnergy,d.MovingSeconds,d.DrillingSeconds,d.BlockedSeconds,affordable,bought,after.Level(UpgradeCategory.DrillPower),after.Level(UpgradeCategory.EnergyCapacity),after.Level(UpgradeCategory.DrivePower),after.Coins,AllCurrentUpgradesMaximum(),flow.Evolution.ResourcesAvailable,ready,string.Join("|",visited.OrderBy(s=>s)),sections,targets));
                    File.AppendAllText(path,rows[rows.Count-1]+Environment.NewLine);
                    Assert.That(d.EndedByEnergy,Is.True);Assert.That(d.Seconds,Is.LessThan(65));Assert.That(sections,Is.LessThanOrEqualTo(10));Assert.That(targets,Is.LessThanOrEqualTo(40));
                    if(ready){Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(0.95f);Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(stage+1));}
                    yield return null;
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(8),"Workload bound fails the diagnostic; never terminates a production run.");Assert.That(flow.IsSandbox,Is.False);
                Assert.That(totalPurchases,Is.EqualTo(style==GameStyle.Balanced?120:105));Assert.That(flow.Evolution.HasNext,Is.False);
            }
        }
        private float ChooseCoinRoute()
        {
            var section=track.ActiveSections.FirstOrDefault(s=>motor.Motion.Position.z>=s.StartZ&&motor.Motion.Position.z<s.EndZ);if(section==null)return aim;
            var targets=section.Obstacles.Where(o=>o.IsSolid&&o.WorldBounds.max.z>=motor.Motion.Position.z-1.15f).ToArray();var front=targets.OrderBy(o=>o.WorldBounds.min.z).FirstOrDefault();
            if(front!=observedFront){observedFront=front;decisionAt=motor.Diagnostics.Seconds+0.35;}
            if(motor.Motion.State==RunState.Blocked){if(blockedAt<0)blockedAt=motor.Diagnostics.Seconds;if(motor.Diagnostics.Seconds-blockedAt<0.35)return aim;return Free(targets);}blockedAt=-1;
            if(motor.Diagnostics.Seconds<decisionAt||front==null)return aim;
            var candidates=targets.Where(o=>o.WorldBounds.min.z<front.WorldBounds.min.z+3&&o.Definition.RewardKind==ObstacleRewardKind.Coins).OrderByDescending(o=>o.Definition.RewardAmount/(Cost(new[]{o})+1));
            foreach(var target in candidates)
            {
                float x=target.WorldBounds.center.x;var line=targets.Where(o=>o.WorldBounds.min.z<=target.WorldBounds.max.z&&EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)).ToArray();
                if(line.All(o=>o.Definition.Hardness<=motor.DrillPower&&Aligned(x,o))&&Cost(line)+motor.Drilling.MovingEnergyPerSecond*0.61f<motor.Energy.Remaining&&Cost(line)<motor.Energy.Capacity*0.4f)return x;
            }
            return Free(targets);
        }
        [UnityTest] public IEnumerator FullGameUiAuditRecordsPurchaseCallbacksAndMissingResourceGuidance()
        {
            var rows=new List<string>{"scenario,stage,powerLevel,coins,guidance"};
            ConfigureAdvanced(RegionIds.Yard,1,0,1337);flow.OpenWorkshop();
            flow.View.PurchaseButtons[0].onClick.Invoke();flow.View.PurchaseButtons[0].onClick.Invoke();
            var p=flow.Service.Snapshot;var table=flow.Service.ActiveForm.Power;
            Assert.That(p.Level(UpgradeCategory.DrillPower),Is.EqualTo(1),"Duplicate UI callbacks in one frame must buy once.");
            Assert.That(p.Coins,Is.EqualTo(1000000-table.Costs.Take(p.Level(UpgradeCategory.DrillPower)).Sum()));
            rows.Add(EconomyCsv("sameFramePowerCallbacks",1,p.Level(UpgradeCategory.DrillPower),p.Coins,""));
            yield return null;flow.View.PurchaseButtons[0].onClick.Invoke();Assert.That(flow.Service.Snapshot.Level(UpgradeCategory.DrillPower),Is.EqualTo(2));
            flow.View.PurchaseButtons[1].onClick.Invoke();flow.View.PurchaseButtons[2].onClick.Invoke();
            Assert.That(flow.Service.Snapshot.Level(UpgradeCategory.EnergyCapacity),Is.EqualTo(1));Assert.That(flow.Service.Snapshot.Level(UpgradeCategory.DrivePower),Is.EqualTo(1));
            foreach(int stage in new[]{4,5,6,7})
            {
                flow.UseTemporaryStage(stage,1000,0,stage==4?50:0,0,0,0,true);flow.OpenWorkshop();flow.SelectRegion(PrimaryRegion(stage));flow.View.Refresh();
                Assert.That(AllCurrentUpgradesMaximum(),Is.True);Assert.That(flow.Evolution.Ready,Is.False);
                Assert.That(flow.View.Requirements.text,Does.Contain("COLLECT"));Assert.That(flow.View.Requirements.text,Does.Not.Contain("MAX all upgrades"));
                rows.Add(EconomyCsv("missingResources",stage,5,flow.Service.Snapshot.Coins,flow.View.Requirements.text.Replace('\n','|')));
                if(stage==4||stage==7){yield return new WaitForSecondsRealtime(0.12f);yield return Capture("M10-Return-Goal-Evo"+stage);}
            }
            File.WriteAllLines("Logs/M10-UiAudit.csv",rows);yield return null;
        }
        [UnityTest,Timeout(600000)] public IEnumerator FullGameReturnRegionCoinRoutesMeasureFarmingOpportunityCosts()
        {
            var rows=new List<string>{"stage,level,region,seed,seconds,distance,coins,normal,ore,movingEnergy,drillingEnergy,blockedEnergy,materials"};
            foreach(int stage in new[]{3,4,5,6,7,8})foreach(int level in new[]{0,5})foreach(var region in flow.Regions.Regions.Where(r=>r.CanPlay(stage)).ToArray())foreach(int seed in new[]{1337,904,2026,8101,9201})
            {
                ConfigureAdvanced(region.Id,stage,level,seed);Launch();int ticks=0;
                while(motor.Motion.State!=RunState.RunEnded&&ticks++<12000){aim=ChooseCoinRoute();Steer(aim);Tick(0.05f);if(ticks%250==0)yield return null;}
                var d=motor.Diagnostics;var c=d.DrillingContacts.ToArray();var run=flow.Run.Rewards;
                rows.Add(EconomyCsv(stage,level,region.Id,seed,d.Seconds,run.Distance,run.Coins,c.Count(t=>t.Completed&&t.Kind==ObstacleRewardKind.Coins),c.Count(t=>t.Completed&&MaterialDefinition.IsResource(t.Kind)),d.MovingEnergy,d.DrillingEnergy,d.BlockedEnergy,string.Join("|",c.Where(t=>t.Completed).Select(t=>t.MaterialId))));File.WriteAllLines("Logs/M10-CoinRoutes.csv",rows);
                Assert.That(d.EndedByEnergy&&flow.IsBanked,Is.True);Assert.That(d.Consumed,Is.EqualTo(motor.Energy.Capacity).Within(0.001));yield return null;
            }
        }
    }
}
