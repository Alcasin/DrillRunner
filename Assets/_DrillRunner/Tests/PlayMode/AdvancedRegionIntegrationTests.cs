using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        [UnityTest] public IEnumerator AdvancedOresRejectPartialDamageBankOnceAndPreserveTheTemporaryStore()
        {
            var original=ProgressCodec.Encode(memory.Load().Progress);
            foreach(var kind in new[]{ObstacleRewardKind.Quartz,ObstacleRewardKind.AlloyCore,ObstacleRewardKind.EnergyShard})
            {
                string region=kind==ObstacleRewardKind.EnergyShard?RegionIds.Energy:RegionIds.Advanced;ConfigureAdvanced(region,kind==ObstacleRewardKind.Quartz?5:kind==ObstacleRewardKind.AlloyCore?6:7,0,1337);advancedGoal=kind;
                Launch();bool partial=false;int ticks=0;
                while(!partial&&motor.Motion.State!=RunState.RunEnded&&ticks++<10000)
                {
                    aim=Choose(Policy.Resource);Steer(aim);Tick(1f/120);
                    var t=motor.ContactTarget;partial=t!=null&&t.Definition.RewardKind==kind&&t.Durability.Fraction>0&&t.Durability.Fraction<1;
                }
                Assert.That(partial,Is.True);Assert.That(flow.Run.Rewards.Resource(kind),Is.Zero);Assert.That(flow.Service.Snapshot.Resource((EvolutionResource)((int)kind-1)),Is.Zero);
                yield return Capture("M9B-Partial-"+kind);yield return Drive(Policy.Resource,true,0.05f,true);var bank=flow.Service.Snapshot;
                Assert.That(flow.Run.Rewards.Resource(kind),Is.GreaterThan(0));Assert.That(bank.Resource((EvolutionResource)((int)kind-1)),Is.EqualTo(flow.Run.Rewards.Resource(kind)));
                Assert.That(flow.RetryBank(),Is.True);Assert.That(ProgressCodec.Encode(flow.Service.Snapshot),Is.EqualTo(ProgressCodec.Encode(bank)));yield return null;
                Assert.That(hud.SummaryText.text,Does.Contain(ResourceCost.Name((EvolutionResource)((int)kind-1))));yield return Capture("M9B-"+kind+"-Results");
                Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(original));flow.OpenWorkshop();
            }
            yield return Load();Assert.That(flow.Service.Snapshot.Quartz+flow.Service.Snapshot.AlloyCore+flow.Service.Snapshot.EnergyShard,Is.Zero);Assert.That(flow.IsSandbox,Is.False);
        }
        [UnityTest] public IEnumerator AdvancedBypassesUseActualReactionAndFullFootprintAtBaseAndMaxDrive()
        {
            foreach(string region in new[]{RegionIds.Advanced,RegionIds.Energy})foreach(int level in new[]{0,5})
            {
                ConfigureAdvanced(region,region==RegionIds.Advanced?5:7,level,1337);var library=track.Encounters;
                foreach(var target in library.Patterns)
                {
                    hud.Restart();var sequence=new EncounterSequence(1337,library,EncounterEnvelope.From(motor));int ordinal=0;while(sequence.Next().SectionId!=target.Section.SectionId&&ordinal++<1500){}
                    Assert.That(ordinal,Is.LessThan(1500));float start=track.EncounterStartZ+ordinal*40;track.UpdateCoverage(start,null);
                    motor.Motion.ResolvePosition(new Vector3(target.BypassX>0?-2.15f:2.15f,0,start),true);motor.transform.position=motor.Motion.Position;Launch();float seconds=0;int ticks=0;
                    while(motor.Motion.Position.z<start+41.2f&&ticks++<5000)
                    {
                        if(seconds>=library.ReactionSeconds)Steer(target.BypassX);Tick(1f/60);seconds+=1f/60;Assert.That(motor.Motion.State,Is.EqualTo(RunState.Moving),target.name);
                    }
                    Assert.That(motor.Motion.Position.z,Is.GreaterThanOrEqualTo(start+41.2f));Assert.That(motor.Diagnostics.Destroyed,Is.Zero);yield return null;
                }
                ForceFixtureEnd();flow.OpenWorkshop();
            }
        }
        [UnityTest] public IEnumerator QuarryCenteredGraniteAlignmentBlockAndPlatingThresholdHaveDistinctHudReasons()
        {
            foreach(int scenario in new[]{0,1,2,3})
            {
                ConfigureQuarry(3,scenario==3?3:0,1337);string id=scenario<2?"granite":"plating";
                var template=track.Catalog.Variants.SelectMany(s=>s.Obstacles).First(o=>o.Definition.MaterialId==id);
                foreach(var o in motor.RegisteredObstacles.ToArray()){motor.UnregisterObstacle(o);o.gameObject.SetActive(false);}
                var target=Object.Instantiate(template,new Vector3(scenario==1?1:0,1,12),Quaternion.identity);motor.RegisterObstacle(target);Launch();int ticks=0;
                while(motor.Motion.State!=RunState.Blocked&&!target.Durability.IsDestroyed&&ticks++<3000)motor.Simulate(1f/120);
                if(scenario==1||scenario==2)
                {
                    Assert.That(motor.Motion.State,Is.EqualTo(RunState.Blocked));Assert.That(target.Durability.Remaining,Is.EqualTo(target.Definition.Durability));Assert.That(motor.ContactIsTooHard,Is.EqualTo(scenario==2));yield return null;
                    var serialized=new UnityEditor.SerializedObject(hud);var hint=(Text)serialized.FindProperty("hintText").objectReferenceValue;
                    Assert.That(hint.text,Does.Contain(scenario==2?"Requires more power":"Align the drill tip"));
                    // Explicit-clock fixtures can reach BLOCKED before the bounded 10 Hz numeric HUD refresh.
                    yield return new WaitForSecondsRealtime(0.12f);
                    yield return Capture("M9B-Quarry-Blocked-"+(scenario==2?"Hardness":"Alignment"));
                    for(int i=0;i<120;i++){Steer(-2.05f);motor.Simulate(1f/120);}Assert.That(motor.Motion.State,Is.EqualTo(RunState.Moving));
                }
                else{Assert.That(motor.DrillPower,Is.EqualTo(scenario==0?20:26));Assert.That(target.Durability.IsDestroyed,Is.True);Assert.That(flow.Run.Rewards.ObstacleCoins,Is.EqualTo(scenario==0?300:600));}
                motor.UnregisterObstacle(target);Object.Destroy(target.gameObject);ForceFixtureEnd();flow.OpenWorkshop();yield return null;
            }
        }
        [UnityTest] public IEnumerator FourRegionalRecordsAndAdvancedBanksSurviveSwitchRetryAndReload()
        {
            yield return MiddleStart(7);
            foreach(string region in RegionIds.All)
            {
                BeginRegionalAttempt(region,1337);SetResourceGoal(region);yield return Drive(Policy.Resource);var ledger=flow.Run.Rewards;
                Assert.That(flow.Service.Snapshot.BestDistance(region),Is.EqualTo(ledger.Distance));var saved=ProgressCodec.Encode(flow.Service.Snapshot);Assert.That(flow.RetryBank(),Is.True);Assert.That(ProgressCodec.Encode(flow.Service.Snapshot),Is.EqualTo(saved));flow.OpenWorkshop();yield return null;
            }
            var before=flow.Service.Snapshot;yield return Load();var after=flow.Service.Snapshot;
            Assert.That(after.Revision,Is.EqualTo(before.Revision+1));Assert.That(after.IssuedAttempt,Is.EqualTo(before.IssuedAttempt+1));
            // Scene entry legitimately issues one Ready attempt. Every balance, record, ownership field and bank receipt must remain exact.
            after.Revision=before.Revision;after.IssuedAttempt=before.IssuedAttempt;Assert.That(ProgressCodec.Encode(after),Is.EqualTo(ProgressCodec.Encode(before)));
            Assert.That(flow.Service.Snapshot.RegionRecords.Length,Is.EqualTo(4));Assert.That(flow.Service.Snapshot.SelectedRegion,Is.EqualTo(RegionIds.Energy));
            Assert.That(Object.FindObjectsByType<DrillMotor>(FindObjectsSortMode.None).Length,Is.EqualTo(1));Assert.That(Object.FindObjectsByType<TrackStreamer>(FindObjectsSortMode.None).Length,Is.EqualTo(1));Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        [UnityTest,Timeout(300000)] public IEnumerator AdvancedNormalUpdateCapturesResourcesRoutesEvolutionAndQuantumWithBoundedEffects()
        {
            var pictures=new HashSet<string>();var rows=new List<string>{"region,stage,level,seconds,distance,coins,quartz,alloyCore,energyShard,sections,targets,draws,mainNs,gcBytes"};
            foreach(var profile in new[]{new[]{5,0},new[]{6,5},new[]{7,0},new[]{8,5}})
            {
                string region=profile[0]<7?RegionIds.Advanced:RegionIds.Energy;ConfigureAdvanced(region,profile[0],profile[1],1337);advancedGoal=profile[0]==6?ObstacleRewardKind.AlloyCore:profile[0]>=7?ObstacleRewardKind.EnergyShard:ObstacleRewardKind.Quartz;
                flow.OpenWorkshop();var menu=flow.GetComponent<RegionWorkshopView>();menu.Selector.onClick.Invoke();yield return null;yield return Capture("M9B-Selector-Evo"+profile[0]);menu.Selector.onClick.Invoke();flow.StartRun();motor.GetComponent<DrillPointerInput>().enabled=false;motor.enabled=true;
                using(var framing=new LiveFrameTarget(Object.FindFirstObjectByType<RunnerCamera>().GetComponent<Camera>(),hud.GetComponent<Canvas>()))
                using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
                try
                {
                    motor.Motion.Press();yield return new WaitForSecondsRealtime(1.15f);motor.Motion.Release();float begin=Time.realtimeSinceStartup;int sections=0,targets=0;long draw=0,peakMain=0,peakGc=0;
                    while(motor.Motion.State!=RunState.RunEnded)
                    {
                        aim=Choose(Policy.Power);Steer(aim);sections=Math.Max(sections,track.ActiveSections.Count);targets=Math.Max(targets,motor.RegisteredObstacles.Count);draw=Math.Max(draw,draws.LastValue);peakMain=Math.Max(peakMain,main.LastValue);peakGc=Math.Max(peakGc,gc.LastValue);
                        string id=motor.ContactTarget!=null?motor.ContactTarget.Definition.MaterialId:null;
                        if(id!=null&&motor.Motion.State==RunState.Drilling&&pictures.Add(id)){motor.enabled=false;yield return Capture("M9B-Contact-"+id);motor.enabled=true;}
                        if(motor.Diagnostics.Seconds>2&&pictures.Add("moving"+profile[0])){motor.enabled=false;yield return Capture("M9B-Evo"+profile[0]+"-Gameplay");motor.enabled=true;}
                        foreach(var kind in new[]{ObstacleRewardKind.Quartz,ObstacleRewardKind.AlloyCore,ObstacleRewardKind.EnergyShard})if(flow.Run.Rewards.Resource(kind)>0&&pictures.Add("reward"+kind)){motor.enabled=false;yield return Capture("M9B-Reward-"+kind);motor.enabled=true;}
                        Assert.That(Time.realtimeSinceStartup-begin,Is.LessThan(90));yield return null;
                    }
                    Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(flow.IsBanked,Is.True);yield return null;yield return Capture("M9B-Evo"+profile[0]+"-Results");var run=flow.Run.Rewards;
                    rows.Add(EconomyCsv(region,profile[0],profile[1],motor.Diagnostics.Seconds,run.Distance,run.Coins,run.Quartz,run.AlloyCore,run.EnergyShard,sections,targets,draw,peakMain,peakGc));File.WriteAllLines("Logs/M9B-LiveEpisodes.csv",rows);
                    Assert.That(sections,Is.LessThanOrEqualTo(10));Assert.That(targets,Is.LessThanOrEqualTo(40));Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,Is.EqualTo(5));Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(5));Assert.That(Object.FindFirstObjectByType<PresentationEffects>().ParticleCount,Is.LessThanOrEqualTo(112));
                    flow.OpenWorkshop();yield return null;yield return Capture("M9B-Evo"+profile[0]+"-Workshop");
                }
                finally{motor.enabled=false;}
            }
            foreach(var id in new[]{"crystal_composite","energy_alloy","quartz_ore","alloy_core","energy_shard"})Assert.That(pictures,Does.Contain(id));
            foreach(var kind in new[]{ObstacleRewardKind.Quartz,ObstacleRewardKind.AlloyCore,ObstacleRewardKind.EnergyShard})Assert.That(pictures,Does.Contain("reward"+kind));
            flow.UseTemporaryStage(7,1000,0,0,0,12,30,true);flow.OpenWorkshop();Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(0.3f);yield return Capture("M9B-Evolving-Quantum");yield return new WaitForSecondsRealtime(0.8f);Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(8));Assert.That(flow.Evolution.HasNext,Is.False);yield return Capture("M9B-Quantum-Unlocked");
        }
    }
}
