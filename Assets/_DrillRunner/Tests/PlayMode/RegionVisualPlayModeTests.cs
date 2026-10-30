using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        [UnityTest,Timeout(240000)] public IEnumerator QuarryNormalUpdateEpisodesCaptureMaterialsIronResultsAndPerformance()
        {
            var pictures=new HashSet<string>();var rows=new List<string>{"stage,level,seconds,distance,coins,iron,peakSections,peakTargets,peakDrawCalls,peakMainNs,peakGcBytes"};
            foreach(var p in new[]{new[]{3,0},new[]{3,5},new[]{4,0},new[]{4,5}})
            {
                ConfigureQuarry(p[0],p[1],1337);motor.enabled=true;
                using(var framing=new LiveFrameTarget(Object.FindFirstObjectByType<RunnerCamera>().GetComponent<Camera>(),hud.GetComponent<Canvas>()))
                using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
                using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
                using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
                try
                {
                    motor.Motion.Press();yield return new WaitForSecondsRealtime(1.15f);motor.Motion.Release();float begin=Time.realtimeSinceStartup;
                    int sections=0,targets=0;long peakDraws=0,peakMain=0,peakGc=0;
                    while(motor.Motion.State!=RunState.RunEnded)
                    {
                        aim=Choose(Policy.Power);Steer(aim);sections=Math.Max(sections,track.ActiveSections.Count);targets=Math.Max(targets,motor.RegisteredObstacles.Count);peakDraws=Math.Max(peakDraws,draws.LastValue);peakMain=Math.Max(peakMain,main.LastValue);peakGc=Math.Max(peakGc,gc.LastValue);
                        string id=motor.ContactTarget!=null?motor.ContactTarget.Definition.MaterialId:null;
                        if(id!=null&&motor.Motion.State==RunState.Drilling&&pictures.Add(id))
                        {motor.enabled=false;yield return Capture("Quarry-Contact-"+id);motor.enabled=true;}
                        if(motor.Diagnostics.Seconds>2&&pictures.Add("moving")){motor.enabled=false;yield return Capture("Quarry-Gameplay");motor.enabled=true;}
                        if(flow.Run.Rewards.Iron>0&&pictures.Add("iron-earned")){motor.enabled=false;yield return Capture("Quarry-Iron-Reward");motor.enabled=true;}
                        Assert.That(Time.realtimeSinceStartup-begin,Is.LessThan(90));yield return null;
                    }
                    Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(flow.IsBanked,Is.True);Assert.That(flow.Run.Rewards.Iron,Is.GreaterThan(0));yield return null;
                    Assert.That(hud.SummaryText.text,Does.Contain("Iron"));yield return Capture("Quarry-Evo"+p[0]+"-L"+p[1]+"-Results");flow.OpenWorkshop();yield return null;yield return Capture("Quarry-Evo"+p[0]+"-L"+p[1]+"-Workshop");
                    rows.Add(EconomyCsv(p[0],p[1],motor.Diagnostics.Seconds,flow.Run.Rewards.Distance,flow.Run.Rewards.Coins,flow.Run.Rewards.Iron,sections,targets,peakDraws,peakMain,peakGc));File.WriteAllLines("Logs/M9A-LiveEpisodes.csv",rows);
                    Assert.That(sections,Is.LessThanOrEqualTo(10));Assert.That(targets,Is.LessThanOrEqualTo(40));Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,Is.EqualTo(5));Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
                }
                finally{motor.enabled=false;}
            }
            foreach(string id in new[]{"granite","plating","reinforced","iron_ore"})Assert.That(pictures,Does.Contain(id),"Capture each new real material interaction.");
        }
        [UnityTest] public IEnumerator RegionalPartialIronRetrySwitchingAndReloadPreserveSelectedRunAndBalances()
        {
            yield return MiddleStart(3);Assert.That(flow.SelectRegion(RegionIds.Quarry),Is.True);var selected=flow.Service.Snapshot;yield return Load();Assert.That(flow.Service.Snapshot.SelectedRegion,Is.EqualTo(RegionIds.Quarry));
            Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=1337;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;Launch();
            bool partial=false;int ticks=0;
            while(!partial&&motor.Motion.State!=RunState.RunEnded&&ticks++<2000)
            {
                aim=Choose(Policy.Resource);Steer(aim);motor.Simulate(1f/60);track.UpdateCoverage(motor.Motion.Position.z,motor.ContactTarget);
                var t=motor.ContactTarget;if(t!=null&&t.Definition.RewardKind==ObstacleRewardKind.Iron&&t.Durability.Fraction<1&&t.Durability.Fraction>0)partial=true;
            }
            Assert.That(partial,Is.True);Assert.That(flow.Run.Rewards.Iron,Is.Zero);Assert.That(flow.Service.Snapshot.Iron,Is.Zero);
            yield return Drive(Policy.Resource,true,0.05f,true);Assert.That(last.Iron,Is.GreaterThan(0));var ledger=flow.Run.Rewards;var bank=flow.Service.Snapshot;
            Assert.That(flow.RetryBank(),Is.True);Assert.That(ProgressCodec.Encode(flow.Service.Snapshot),Is.EqualTo(ProgressCodec.Encode(bank)));
            yield return null;hud.RestartButton.onClick.Invoke();motor.GetComponent<DrillPointerInput>().enabled=false;Assert.That(track.RegionId,Is.EqualTo(RegionIds.Quarry));Assert.That(flow.Run.Rewards.Iron,Is.Zero);Assert.That(ledger.Iron,Is.EqualTo(bank.Iron));
            observedFront=null;decisionAt=0;blockedAt=-1;aim=0;yield return Drive(Policy.Balanced);flow.OpenWorkshop();Assert.That(flow.SelectRegion(RegionIds.Yard),Is.True);Assert.That(flow.StartRun(),Is.True);Assert.That(track.RegionId,Is.EqualTo(RegionIds.Yard));
            observedFront=null;decisionAt=0;blockedAt=-1;aim=0;yield return Drive(Policy.Resource);Assert.That(flow.Service.Snapshot.Iron,Is.GreaterThanOrEqualTo(bank.Iron));
            var saved=flow.Service.Snapshot;yield return Load();Assert.That(flow.Service.Snapshot.SelectedRegion,Is.EqualTo(RegionIds.Yard));Assert.That(flow.Service.Snapshot.Iron,Is.EqualTo(saved.Iron));Assert.That(flow.Service.Snapshot.BestDistance(RegionIds.Quarry),Is.GreaterThan(0));Assert.That(flow.Service.Snapshot.BestDistance(RegionIds.Yard),Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator QuarryHardnessRejectsPlatingUntilActualPurchasedPowerUnlocksIt()
        {
            foreach(int level in new[]{0,3})
            {
                ConfigureQuarry(3,level,1337);var template=track.Catalog.Variants.SelectMany(v=>v.Obstacles).First(o=>o.Definition.MaterialId=="plating");
                foreach(var o in motor.RegisteredObstacles.ToArray()){motor.UnregisterObstacle(o);o.gameObject.SetActive(false);}
                var target=Object.Instantiate(template,new Vector3(0,1,12),Quaternion.identity);motor.RegisterObstacle(target);Launch();
                int ticks=0;while(motor.Motion.State!=RunState.Blocked&&!target.Durability.IsDestroyed&&ticks++<1500)motor.Simulate(1f/120);
                if(level==0)
                {
                    Assert.That(motor.Motion.State,Is.EqualTo(RunState.Blocked));Assert.That(target.Durability.Remaining,Is.EqualTo(420));Assert.That(flow.Run.Rewards.ObstacleCoins,Is.Zero);
                    for(int i=0;i<120;i++){Steer(-2.05f);motor.Simulate(1f/120);}Assert.That(motor.Motion.State,Is.EqualTo(RunState.Moving));
                }
                else{Assert.That(motor.DrillPower,Is.EqualTo(26));Assert.That(target.Durability.IsDestroyed,Is.True);Assert.That(flow.Run.Rewards.ObstacleCoins,Is.EqualTo(600));}
                motor.UnregisterObstacle(target);Object.Destroy(target.gameObject);motor.Energy.Consume(1000000,1);motor.Simulate(0.001f);flow.OpenWorkshop();yield return null;
            }
        }
        [UnityTest] public IEnumerator ResourceGoalAwareMiddleJourneysKeepAvoidanceControlsAndEarnEveryRequirement()
        {
            var rows=new List<string>{"family,startStage,attempt,region,actualPolicy,seconds,distance,coins,iron,copper,purchases,wallet,ironBalance,copperBalance,evolved"};
            foreach(int stage in new[]{3,4})foreach(int family in new[]{8100,9200,10300})
            {
                yield return MiddleStart(stage);int attempt=0;
                while(flow.Service.ActiveForm.Stage==stage&&attempt<100)
                {
                    var p=flow.Service.Snapshot;var f=flow.Service.ActiveForm;long iron=f.RequiredResources.Where(c=>c.Resource==EvolutionResource.Iron).Sum(c=>c.Amount),copper=f.RequiredResources.Where(c=>c.Resource==EvolutionResource.Copper).Sum(c=>c.Amount);
                    string id=p.Iron>=iron&&p.Copper<copper?RegionIds.Yard:RegionIds.Quarry;bool seek=id==RegionIds.Quarry?p.Iron<iron:p.Copper<copper;var policy=seek?Policy.Resource:Policy.Balanced;
                    Assert.That(flow.SelectRegion(id),Is.True);Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=family+ ++attempt;hud.Restart();observedFront=null;decisionAt=0;blockedAt=-1;aim=0;
                    yield return Drive(policy,seek);flow.OpenWorkshop();int bought=BuyEconomyUpgrades(Policy.Resource);bool evolved=flow.Evolution.Ready;
                    rows.Add(EconomyCsv(family,stage,attempt,id,policy,last.Seconds,last.Distance,last.Coins,last.Iron,last.Copper,bought,flow.Service.Snapshot.Coins,flow.Service.Snapshot.Iron,flow.Service.Snapshot.Copper,evolved));File.WriteAllLines("Logs/M9A-GoalAwareJourneys.csv",rows);
                    if(evolved){Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(1);}yield return null;
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(stage+1));
            }
        }
    }
}
