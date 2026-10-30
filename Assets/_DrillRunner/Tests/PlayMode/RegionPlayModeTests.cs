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
        private void ConfigureQuarry(int stage,int level,int seed)
        {
            flow.UseTemporaryStage(stage,1000000,0,0,0,0,0,level==5);
            if(level>0&&level<5)foreach(UpgradeCategory c in Enum.GetValues(typeof(UpgradeCategory)))for(int i=0;i<level;i++)Assert.That(flow.Purchase(c),Is.True);
            Assert.That(flow.SelectRegion(RegionIds.Quarry),Is.True);Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;
            flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=seed;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
        }
        private IEnumerator MiddleStart(int stage)
        {
            flow.UseTemporaryStage(stage,0,0,0,0,0,0,false);memory=new MemoryProgressStore(flow.Service.Snapshot);yield return Load();Assert.That(flow.Service.Snapshot.Coins+flow.Service.Snapshot.Iron+flow.Service.Snapshot.Copper,Is.Zero);
        }
        [UnityTest] public IEnumerator RegionSelectionLocksDeferredContentPersistsAndDoesNotTouchRealQaStore()
        {
            var original=ProgressCodec.Encode(memory.Load().Progress);var menu=flow.GetComponent<RegionWorkshopView>();
            flow.UseTemporaryStage(2,1000,0,0,0,0,0,false);menu.Selector.onClick.Invoke();yield return null;Assert.That(menu.Choices[1].interactable,Is.False);yield return Capture("Region-Locked-Evo2");
            Assert.That(flow.SelectRegion(RegionIds.Quarry),Is.False);flow.UseTemporaryStage(3,1000,0,0,0,0,0,false);if(!menu.IsExpanded)menu.Selector.onClick.Invoke();yield return null;
            Assert.That(menu.Choices[1].interactable,Is.True);Assert.That(menu.Choices[2].interactable,Is.False);Assert.That(menu.Choices[3].interactable,Is.False);yield return Capture("Region-Unlocked-Evo3");
            Assert.That(flow.SelectRegion(RegionIds.Quarry),Is.True);Assert.That(flow.StartRun(),Is.True);Launch();Assert.That(flow.SelectRegion(RegionIds.Yard),Is.False);Assert.That(track.RegionId,Is.EqualTo(RegionIds.Quarry));
            yield return Drive(Policy.Resource,true,0.05f,true);Assert.That(last.Iron,Is.GreaterThanOrEqualTo(6));Assert.That(flow.Run.Rewards.Copper,Is.Zero);Assert.That(flow.Service.Snapshot.BestDistance(RegionIds.Quarry),Is.EqualTo(last.Distance));
            Assert.That(flow.OpenWorkshop(),Is.True);Assert.That(flow.SelectRegion(RegionIds.Yard),Is.True);Assert.That(flow.StartRun(),Is.True);Assert.That(track.RegionId,Is.EqualTo(RegionIds.Yard));
            yield return Drive(Policy.Balanced);yield return Capture("Yard-Gameplay-Results");
            Assert.That(flow.Service.Snapshot.BestDistance(RegionIds.Yard),Is.GreaterThan(0));Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(original));
            yield return Load();Assert.That(flow.Service.Snapshot.Coins+flow.Service.Snapshot.Iron,Is.Zero);
        }
        [UnityTest] public IEnumerator QuarryPoliciesMeasureActualMaterialsEnergyAndIron()
        {
            var rows=new List<string>{Sample.Header+",region,graniteBreaks,platingBreaks,reinforcedBreaks,ironBreaks"};
            foreach(var p in new[]{new[]{3,0},new[]{3,2},new[]{3,5},new[]{4,0},new[]{4,2},new[]{4,5},new[]{5,0},new[]{8,5}})
            foreach(int seed in new[]{1337,904,2026,8101,9201})foreach(Policy policy in Enum.GetValues(typeof(Policy)))
            {
                ConfigureQuarry(p[0],p[1],seed);var broken=new Dictionary<string,int>();Action<DrillObstacle> count=o=>{string id=o.Definition.MaterialId;broken[id]=broken.TryGetValue(id,out var n)?n+1:1;};motor.ObstacleBroken+=count;
                try{yield return Drive(policy);}finally{motor.ObstacleBroken-=count;}
                int Count(string id)=>broken.TryGetValue(id,out var n)?n:0;
                rows.Add(last.Csv("M9A")+EconomyCsv("",RegionIds.Quarry,Count("granite"),Count("plating"),Count("reinforced"),Count("iron_ore")));File.WriteAllLines("Logs/M9A-QuarryProfiles.csv",rows);
                Assert.That(last.Seconds,Is.LessThan(60));Assert.That(track.ActiveSections.Count,Is.LessThanOrEqualTo(10));Assert.That(motor.RegisteredObstacles.Count,Is.LessThanOrEqualTo(40));
                if(policy==Policy.Resource)Assert.That(last.Iron,Is.GreaterThanOrEqualTo(6));if(policy==Policy.Safe)Assert.That(last.ObstacleCoins+last.Iron,Is.Zero);yield return null;
            }
        }
        [UnityTest,Timeout(600000)] public IEnumerator UnfundedMiddleStagesMeasureCoinIronAndCopperProgression()
        {
            var rows=new List<string>{"family,strategy,startStage,attempt,stage,region,seconds,distance,coins,iron,copper,drillingEnergy,distanceCoins,obstacleCoins,walletBefore,walletAfterBank,purchases,walletAfterPurchases,ironBalance,copperBalance,evolved"};
            foreach(int start in new[]{3,4})foreach(int family in new[]{8100,9200,10300})foreach(Policy strategy in Enum.GetValues(typeof(Policy)))
            {
                yield return MiddleStart(start);int attempts=0;
                while(flow.Service.ActiveForm.Stage==start&&attempts<100)
                {
                    var before=flow.Service.Snapshot;var form=flow.Service.ActiveForm;long needIron=form.RequiredResources.Where(c=>c.Resource==EvolutionResource.Iron).Sum(c=>c.Amount);long needCopper=form.RequiredResources.Where(c=>c.Resource==EvolutionResource.Copper).Sum(c=>c.Amount);
                    string region=before.Iron>=needIron&&before.Copper<needCopper?RegionIds.Yard:RegionIds.Quarry;Assert.That(flow.SelectRegion(region),Is.True);
                    Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=family+ ++attempts;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
                    var actual=strategy==Policy.Safe&&AllCurrentUpgradesMaximum()?Policy.Resource:strategy;bool seek=region==RegionIds.Quarry?before.Iron<needIron:before.Copper<needCopper;
                    yield return Drive(actual,seek);Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(before.Coins+last.Coins));flow.OpenWorkshop();long bank=flow.Service.Snapshot.Coins;int bought=BuyEconomyUpgrades(strategy);bool evolved=flow.Evolution.Ready;
                    rows.Add(EconomyCsv(family,strategy,start,attempts,start,region,last.Seconds,last.Distance,last.Coins,last.Iron,last.Copper,last.DrillEnergy,last.DistanceCoins,last.ObstacleCoins,before.Coins,bank,bought,flow.Service.Snapshot.Coins,flow.Service.Snapshot.Iron,flow.Service.Snapshot.Copper,evolved));File.WriteAllLines("Logs/M9A-MiddleJourneys.csv",rows);
                    if(evolved){Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(1);}yield return null;
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(start+1));Assert.That(flow.Service.Snapshot.Quartz+flow.Service.Snapshot.AlloyCore+flow.Service.Snapshot.EnergyShard,Is.Zero);
            }
        }
        [UnityTest,Timeout(600000)] public IEnumerator ThreeFullyUnfundedYardAndQuarryJourneysReachEvoFive()
        {
            var rows=new List<string>{"family,attempt,stage,region,seconds,distance,coins,copper,iron,purchases,wallet,ironBalance,copperBalance,evolved"};
            foreach(int family in new[]{8100,9200,10300})
            {
                memory=new MemoryProgressStore();yield return Load();int attempt=0;
                while(flow.Service.ActiveForm.Stage<5&&attempt<150)
                {
                    var before=flow.Service.Snapshot;var form=flow.Service.ActiveForm;long iron=form.RequiredResources.Where(c=>c.Resource==EvolutionResource.Iron).Sum(c=>c.Amount);long copper=form.RequiredResources.Where(c=>c.Resource==EvolutionResource.Copper).Sum(c=>c.Amount);
                    string region=form.Stage<3||before.Iron>=iron&&before.Copper<copper?RegionIds.Yard:RegionIds.Quarry;Assert.That(flow.SelectRegion(region),Is.True);
                    Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=family+ ++attempt;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
                    yield return Drive(Policy.Balanced,region==RegionIds.Quarry?before.Iron<iron:before.Copper<copper);flow.OpenWorkshop();int bought=BuyEconomyUpgrades(Policy.Balanced);bool evolved=flow.Evolution.Ready;
                    rows.Add(EconomyCsv(family,attempt,form.Stage,region,last.Seconds,last.Distance,last.Coins,last.Copper,last.Iron,bought,flow.Service.Snapshot.Coins,flow.Service.Snapshot.Iron,flow.Service.Snapshot.Copper,evolved));File.WriteAllLines("Logs/M9A-FullJourneys.csv",rows);
                    if(evolved){Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(1);}yield return null;
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(5));Assert.That(flow.SelectRegion(RegionIds.Advanced),Is.True);Assert.That(flow.Service.Snapshot.Quartz+flow.Service.Snapshot.AlloyCore+flow.Service.Snapshot.EnergyShard,Is.Zero);
            }
        }
        [UnityTest] public IEnumerator QuarryBypassesSupportMinimumUpgradedAndReturningDrillsAtBothDriveExtremes()
        {
            var library=flow.Regions.Find(RegionIds.Quarry).Encounters;
            foreach(var p in new[]{new[]{3,0},new[]{3,5},new[]{4,0},new[]{4,5},new[]{8,5}})
            {
                ConfigureQuarry(p[0],p[1],1337);
                foreach(var target in library.Patterns)
                {
                    hud.Restart();var sequence=new EncounterSequence(flow.Run.Rewards.Seed,library,EncounterEnvelope.From(motor));int ordinal=0;while(sequence.Next().SectionId!=target.Section.SectionId&&ordinal++<1500){}
                    Assert.That(ordinal,Is.LessThan(1500));float start=track.EncounterStartZ+ordinal*40;track.UpdateCoverage(start,null);float entry=target.BypassX>0?-2.15f:2.15f;motor.Motion.ResolvePosition(new Vector3(entry,0,start),true);motor.transform.position=motor.Motion.Position;Launch();
                    float time=0;int ticks=0;while(motor.Motion.Position.z<start+41.2f&&ticks++<5000)
                    {
                        if(time>=library.ReactionSeconds)Steer(target.BypassX);motor.Simulate(1f/60);track.UpdateCoverage(motor.Motion.Position.z,motor.ContactTarget);time+=1f/60;
                        Assert.That(motor.Motion.State,Is.EqualTo(RunState.Moving),target.name+" / EVO "+p[0]);
                    }
                    Assert.That(motor.Motion.Position.z,Is.GreaterThanOrEqualTo(start+41.2f));Assert.That(flow.Run.Rewards.Iron+flow.Run.Rewards.ObstacleCoins,Is.Zero);yield return null;
                }
                motor.Energy.Consume(1000000,1);motor.Simulate(0.001f);flow.OpenWorkshop(); // Diagnostic fixture cleanup; excluded from progression exports.
            }
        }
    }
}
