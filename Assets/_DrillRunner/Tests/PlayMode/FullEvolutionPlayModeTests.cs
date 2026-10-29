using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Profiling;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed class FullEvolutionPlayModeTests
    {
        private WorkshopController flow;
        private DrillMotor motor;
        private TrackStreamer track;
        private PrototypeHud hud;
        private MemoryProgressStore memory;
        private readonly List<Object> owned = new List<Object>();
        private readonly List<string> performance = new List<string> { "sample,meanMainNs,peakMainNs,peakDrawCalls,peakGcBytes" };
        private static string Csv(params object[] values) => string.Join(",",values.Select(v=>v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v.ToString()));
        [UnitySetUp] public IEnumerator Open()
        { memory = new MemoryProgressStore(); WorkshopController.StoreFactory = () => memory; yield return Load(); }
        private IEnumerator Load()
        {
            yield return HistoricalRunFixture.LoadScene(); yield return null;
            flow=Object.FindFirstObjectByType<WorkshopController>();motor=flow.Run.Motor;track=flow.Run.Track;hud=Object.FindFirstObjectByType<PrototypeHud>();
            motor.enabled=false;motor.GetComponent<DrillPointerInput>().enabled=false;Object.FindFirstObjectByType<DrillPresentation>().SendMessage("OnApplicationFocus",true);
            flow.OpenWorkshop();
        }
        [UnityTearDown] public IEnumerator Close()
        { yield return HistoricalRunFixture.UnloadScene();foreach(var item in owned)if(item!=null)Object.Destroy(item);owned.Clear();WorkshopController.StoreFactory=()=>new MemoryProgressStore(); }
        private void Temporary(int stage,bool max=false,long resources=0)
        { flow.UseTemporaryStage(stage,1000000,resources,resources,resources,resources,resources,max); }
        private void Launch()
        { motor.Motion.Press();motor.Simulate(1.5f);motor.Motion.Release();motor.Simulate(0.001f); }
        private void Steer(float x)
        {
            if(motor.Motion.State!=RunState.Moving&&motor.Motion.State!=RunState.Blocked)return;
            motor.Motion.CancelPointer();motor.Motion.Press();motor.Motion.Drag(new Vector2((x-motor.Motion.Position.x)*540/8,0),540);
        }
        private void Tick(float dt=1f/60)
        {motor.Simulate(dt);track.UpdateCoverage(motor.Motion.Position.z,motor.ContactTarget);}
        private void End()
        {motor.Energy.Consume(1000000,1);motor.Simulate(0.001f);Assert.That(motor.Motion.State,Is.EqualTo(RunState.RunEnded));}
        private void Until(Func<bool> condition,int limit=10000,float dt=1f/120)
        {for(var i=0;i<limit&&!condition();i++)Tick(dt);Assert.That(condition(),Is.True,"State="+motor.Motion.State);}
        [UnityTest] public IEnumerator EveryFormShowsTheCorrectWorkshopAndGameplayModelWithSharedAnimationAndLimits()
        {
            var stats=new List<string>{"stage,powerBase,powerMax,energyBase,energyMax,cruise,minBoostBase,minBoostMax,recoveryBase,recoveryMax,upgradeCoins,renderers,vertices"};
            for(var stage=1;stage<=8;stage++)
            {
                Temporary(stage);yield return null;var form=flow.Service.ActiveForm;
                Assert.That(flow.View.FormName.text,Does.Contain("EVO "+stage));Assert.That(flow.View.FormPreview.texture,Is.SameAs(form.PreviewImage));
                Assert.That(flow.Visuals.CurrentForm,Is.EqualTo(form.FormId));Assert.That(flow.View.Requirements.text,Does.Contain(stage==8?"FINAL FORM":"NEXT:"));
                if(stage==1)Assert.That(flow.View.ResourceSummary.text,Is.Empty);
                if(stage==3)Assert.That(flow.View.ResourceSummary.text,Does.Contain("Iron 0"));
                yield return Capture("Evo"+stage+"-Workshop");Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;
                flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=1337;hud.Restart();
                Assert.That(motor.DrillPower,Is.EqualTo(form.Power.Values[0]));Assert.That(motor.Energy.Capacity,Is.EqualTo(form.Energy.Values[0]));
                Assert.That(motor.transform.Cast<Transform>().Count(t=>t.gameObject.activeSelf&&(t.name=="Visuals"||t.name.EndsWith(" Visual"))),Is.EqualTo(1));
                if(stage>=3)Assert.That(flow.View.Rows[2].text,Does.Contain("Launch"));
                Assert.That(motor.Motion.SpeedCeiling,Is.EqualTo(form.SpeedCeiling));Assert.That(flow.Visuals.ActiveVisual.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(Object.FindObjectsByType<DrillMotor>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
                Assert.That(motor.GetComponent<BoxCollider>().size,Is.EqualTo(new Vector3(1.7f,1.3f,3)));
                Assert.That(flow.Visuals.ActiveVisual.transform.Find("Drill Rotor"),Is.Not.Null);Assert.That(flow.Visuals.ActiveVisual.transform.Find("Motor Housing"),Is.Not.Null);
                motor.Motion.Press();motor.Simulate(0.75f);yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(Object.FindFirstObjectByType<DrillPresentation>().RotorSpeed,Is.GreaterThan(120));
                motor.Motion.Release();motor.Simulate(0.001f);Steer(2.05f);Until(()=>motor.Motion.Position.z>=12);
                yield return Capture("Evo"+stage+"-Gameplay");Assert.That(motor.Motion.Position.x,Is.GreaterThan(1.9f));
                var active=flow.Visuals.ActiveVisual;var renderers=active.GetComponentsInChildren<MeshRenderer>();
                stats.Add(Csv(stage,form.Power.Values[0],form.Power.Values[5],form.Energy.Values[0],form.Energy.Values[5],form.Drive.Values[0],form.MinimumBoost[0],form.MinimumBoost[5],form.Recovery[0],form.Recovery[5],
                    form.Power.Costs.Sum()+form.Energy.Costs.Sum()+form.Drive.Costs.Sum(),renderers.Length,active.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount)));
                End();Temporary(stage,true);
                if(stage==8){Assert.That(flow.View.Requirements.text,Does.Contain("ALL UPGRADES MAX"));yield return Capture("Evo8-MAX-Workshop");}
                Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;Launch();
                Assert.That(motor.Motion.ForwardSpeed,Is.LessThanOrEqualTo(26));Assert.That(motor.DrillPower,Is.EqualTo(form.Power.Values[5]));End();
                Assert.That(Object.FindFirstObjectByType<PresentationAudio>().GetComponentsInChildren<AudioSource>(true).Length,Is.EqualTo(5));
            }
            File.WriteAllLines("Logs/M8-Forms.csv",stats);
        }
        [UnityTest] public IEnumerator EveryFormActuallyDrillsWoodAndOreAndAwardsNothingForPartialDamage()
        {
            for(var stage=1;stage<=8;stage++)
            {
                Temporary(stage);Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;
                var wood=motor.RegisteredObstacles.First(o=>o.Definition.MaterialId=="wood");var ore=motor.RegisteredObstacles.First(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper);
                foreach(var item in motor.RegisteredObstacles)item.gameObject.SetActive(item==wood||item==ore);
                wood.transform.position=new Vector3(0,1,7);ore.transform.position=new Vector3(0,1,14);Launch();
                Until(()=>motor.Motion.State==RunState.Drilling,10000,0.001f);var energy=motor.Energy.Remaining;var hp=wood.Durability.Remaining;
                motor.Simulate((float)(wood.Durability.TimeToBreak(motor.DrillPower,motor.Drilling.DamagePerPower)*0.3));
                Assert.That(wood.Durability.Remaining,Is.LessThan(hp));Assert.That(wood.Durability.IsDestroyed,Is.False);Assert.That(flow.Run.Rewards.ObstacleCoins,Is.Zero);
                Assert.That(motor.Energy.Remaining,Is.LessThan(energy));var x=motor.Motion.Position.x;Steer(2);motor.Simulate(0.001f);Assert.That(motor.Motion.Position.x,Is.EqualTo(x).Within(0.001));
                if(stage==3||stage==8)yield return Capture("Evo"+stage+"-Drilling");
                Until(()=>wood.Durability.IsDestroyed,10000,0.001f);Assert.That(flow.Run.Rewards.ObstacleCoins,Is.EqualTo(1));
                Until(()=>motor.ContactTarget==ore,10000,0.001f);Assert.That(flow.Run.Rewards.Copper,Is.Zero);
                motor.Simulate((float)ore.Durability.TimeToBreak(motor.DrillPower,motor.Drilling.DamagePerPower)*0.25f);Assert.That(flow.Run.Rewards.Copper,Is.Zero);
                Until(()=>ore.Durability.IsDestroyed,10000,0.001f);Assert.That(flow.Run.Rewards.Copper,Is.EqualTo(3));End();
                yield return null;
            }
        }
        [UnityTest] public IEnumerator AllSevenSavedTransitionsSurviveInterruptedPresentationAndReloadIntoTheCorrectModel()
        {
            memory=new MemoryProgressStore(new PlayerProgress{Coins=1000000,Copper=1000,Iron=1000,Quartz=1000,AlloyCore=1000,EnergyShard=1000,EvolutionSchema=1});yield return Load();
            for(var stage=1;stage<8;stage++)
            {
                foreach(UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))while(flow.Purchase(category)){}
                Assert.That(flow.Evolution.Ready,Is.True);var current=flow.Service.Snapshot;var costs=flow.Service.ActiveForm.RequiredResources;
                Assert.That(flow.Evolve(),Is.True);var evolved=flow.Service.Snapshot;Assert.That(evolved.FormId,Is.EqualTo(ProgressionConfig.FormIds[stage]));
                Assert.That(flow.StartRun(),Is.False);for(var i=0;i<10;i++)Assert.That(flow.Evolve(),Is.False);
                foreach(var cost in costs)Assert.That(evolved.Resource(cost.Resource),Is.EqualTo(current.Resource(cost.Resource)-cost.Amount));
                if(stage==2||stage==7){yield return new WaitForSecondsRealtime(0.22f);yield return Capture("Structural-Evolution"+stage+"to"+(stage+1));}
                yield return Load();Assert.That(flow.IsEvolving,Is.False);Assert.That(flow.Visuals.CurrentForm,Is.EqualTo(evolved.FormId));Assert.That(flow.Service.Snapshot.Copper,Is.EqualTo(evolved.Copper));
                Assert.That(flow.Service.Snapshot.AdvancedForms.Length,Is.EqualTo(Math.Max(0,stage-1)));
            }
            Assert.That(flow.View.EvolveButton.gameObject.activeSelf,Is.False);Assert.That(flow.Evolution.HasNext,Is.False);
            yield return Capture("Structural-Final-Evo8-Workshop");
        }
        [UnityTest] public IEnumerator EveryFormAtBaseAndMaxCanReachAllAuthoredBypassesAfterTheReactionAllowance()
        {
            var library=track.Encounters;
            for(var stage=1;stage<=8;stage++)foreach(var max in new[]{false,true})
            {
                Temporary(stage,max);
                foreach(var target in library.Patterns)
                {
                    hud.Restart();var sequence=new EncounterSequence(flow.Run.Rewards.Seed,library,EncounterEnvelope.From(motor));var ordinal=0;
                    while(sequence.Next().SectionId!=target.Section.SectionId&&ordinal++<1000){}
                    Assert.That(ordinal,Is.LessThan(1000));var start=track.EncounterStartZ+ordinal*40;track.UpdateCoverage(start,null);
                    var entry=target.BypassX>0?-2.15f:2.15f;motor.Motion.ResolvePosition(new Vector3(entry,0,start),true);motor.transform.position=motor.Motion.Position;Launch();
                    var elapsed=0f;var ticks=0;
                    while(motor.Motion.Position.z<start+41.2f&&ticks++<5000)
                    {
                        if(elapsed>=library.ReactionSeconds)Steer(target.BypassX);Tick();elapsed+=1f/60;
                        Assert.That(motor.Motion.State,Is.EqualTo(RunState.Moving),"EVO "+stage+" "+target.name);
                    }
                    Assert.That(motor.Motion.Position.z,Is.GreaterThanOrEqualTo(start+41.2f));Assert.That(flow.Run.Rewards.Copper,Is.Zero);Assert.That(flow.Run.Rewards.ObstacleCoins,Is.Zero);yield return null;
                }
                End();
            }
        }
        [UnityTest] public IEnumerator TemporaryStagesResourcesPurchasesBankingAndMissingRequirementsNeverTouchTheOriginalStore()
        {
            var before=ProgressCodec.Encode(memory.Load().Progress);Temporary(4,true);Assert.That(flow.Evolution.Ready,Is.False);
            var blocked=ProgressCodec.Encode(flow.Service.Snapshot);Assert.That(flow.Evolve(),Is.False);Assert.That(ProgressCodec.Encode(flow.Service.Snapshot),Is.EqualTo(blocked));
            Assert.That(flow.View.ResourceSummary.text,Does.Contain("Iron 0"));Temporary(6,true,1000);Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(flow.Purchase(UpgradeCategory.DrillPower),Is.True);Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;Launch();Tick(0.5f);End();
            Assert.That(flow.IsBanked,Is.True);Assert.That(flow.IsSandbox,Is.True);Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(before));
            yield return Load();Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(1));Assert.That(flow.Service.Snapshot.Iron,Is.Zero);Assert.That(flow.Service.Snapshot.Coins,Is.Zero);Assert.That(flow.IsSandbox,Is.False);
        }
        private float Policy(bool seek)
        {
            var section=track.ActiveSections.FirstOrDefault(s=>motor.Motion.Position.z>=s.StartZ&&motor.Motion.Position.z<s.EndZ);if(section==null)return -2.05f;
            var obstacles=section.Obstacles.Where(o=>o.IsSolid&&o.WorldBounds.max.z>=motor.Motion.Position.z-1.15f).ToArray();
            if(seek&&flow.Run.Rewards.Copper==0)foreach(var ore in obstacles.Where(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper&&o.WorldBounds.min.z<motor.Motion.Position.z+32))
            {
                var x=ore.WorldBounds.center.x;var cost=0f;var valid=true;
                foreach(var obstacle in obstacles.Where(o=>o.WorldBounds.min.z<=ore.WorldBounds.max.z&&EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)))
                {valid&=obstacle.Definition.Hardness<=motor.DrillPower;cost+=obstacle.Durability.Remaining/(motor.DrillPower*motor.Drilling.DamagePerPower)*obstacle.Definition.DrillingEnergyPerSecond;}
                if(valid&&cost<motor.Energy.Capacity*0.28f&&cost+8<motor.Energy.Remaining)return x;
            }
            var candidates=new[]{-2.05f,2.05f,0f}.OrderBy(x=>Mathf.Abs(x-motor.Motion.Position.x)).ToArray();
            foreach(var x in candidates)if(obstacles.All(o=>!EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)))return x;
            if(obstacles.Length>0)
            {var first=obstacles.Min(o=>o.WorldBounds.min.z);var group=obstacles.Where(o=>o.WorldBounds.min.z<first+3).ToArray();foreach(var x in candidates)if(group.All(o=>!EncounterSafety.OverlapsX(x,0.85f,o.WorldBounds)))return x;}
            return track.ActiveEncounter!=null?track.ActiveEncounter.BypassX:-2.1f;
        }
        [UnityTest] public IEnumerator TwoUnfundedWorkshopYardJourneysReachIndustrialWithoutAdvancedResourceSources()
        {
            var lines=new List<string>{"family,run,stage,distance,earnedCoins,earnedCopper,bankedCoins,bankedCopper"};
            foreach(var family in new[]{8100,9200})
            {
                memory=new MemoryProgressStore();yield return Load();var attempts=0;var evolutionRuns=new List<int>();
                while(flow.Service.ActiveForm.Stage<3&&attempts<20)
                {
                    Assert.That(flow.StartRun(),Is.True);motor.GetComponent<DrillPointerInput>().enabled=false;flow.Run.UseFixedSeed=true;flow.Run.ConfiguredSeed=family+ ++attempts;hud.Restart();Launch();var ticks=0;
                    var needCopper=flow.Service.Snapshot.Copper<flow.Service.ActiveForm.RequiredResources.Where(c=>c.Resource==EvolutionResource.Copper).Sum(c=>c.Amount);
                    while(motor.Motion.State!=RunState.RunEnded&&ticks++<15000){Steer(Policy(needCopper));Tick(0.1f);Assert.That(motor.Motion.State,Is.Not.EqualTo(RunState.Blocked));if(ticks%250==0)yield return null;}
                    Assert.That(motor.Motion.State,Is.EqualTo(RunState.RunEnded));Assert.That(flow.IsBanked,Is.True);
                    var p=flow.Service.Snapshot;lines.Add(string.Join(",",family,attempts,flow.Service.ActiveForm.Stage,flow.Run.Rewards.Distance.ToString("0.0",CultureInfo.InvariantCulture),flow.Run.Rewards.Coins,flow.Run.Rewards.Copper,p.Coins,p.Copper));
                    File.WriteAllLines("Logs/M8-ProductionJourneys.csv",lines);flow.OpenWorkshop();
                    foreach(var category in new[]{UpgradeCategory.EnergyCapacity,UpgradeCategory.DrivePower,UpgradeCategory.DrillPower})while(flow.Purchase(category)){}
                    if(flow.Evolution.Ready){evolutionRuns.Add(attempts);Assert.That(flow.Evolve(),Is.True);yield return new WaitForSecondsRealtime(1.0f);}
                }
                Assert.That(flow.Service.ActiveForm.Stage,Is.EqualTo(3));Assert.That(evolutionRuns.Count,Is.EqualTo(2));Assert.That(evolutionRuns[0],Is.InRange(5,8));
                Assert.That(evolutionRuns[1]-evolutionRuns[0],Is.InRange(5,8));Assert.That(flow.Service.Snapshot.Iron+flow.Service.Snapshot.Quartz+flow.Service.Snapshot.AlloyCore+flow.Service.Snapshot.EnergyShard,Is.Zero);
                Assert.That(flow.Evolution.Ready,Is.False);Assert.That(flow.Service.ActiveForm.RequiredResources.Single().Resource,Is.EqualTo(EvolutionResource.Iron));
            }
        }
        [UnityTest] public IEnumerator AllVisualFormsRetainStoneHardnessBlockedEscapeAndBoundedPresentationSystems()
        {
            var legacy=LegacyTemplateFixture.Restore(track,hud);if(legacy!=null)owned.Add(legacy);
            for(var stage=1;stage<=8;stage++)
            {
                Temporary(stage);flow.StartQaRun(5,500);motor.GetComponent<DrillPointerInput>().enabled=false;
                var stone=motor.RegisteredObstacles.Single(o=>o.Definition.MaterialId=="stone");foreach(var obstacle in motor.RegisteredObstacles)obstacle.gameObject.SetActive(obstacle==stone);
                stone.transform.position=new Vector3(0,1,7);Launch();Until(()=>motor.Motion.State==RunState.Blocked);Assert.That(stone.Durability.Remaining,Is.EqualTo(stone.Durability.Maximum));
                Steer(2.05f);Until(()=>motor.Motion.State==RunState.Moving);Assert.That(motor.Motion.Position.x,Is.GreaterThan(1.5f));End();
                Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(5));yield return null;
            }
        }
        private IEnumerator Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)yield break;
            var follower=Object.FindFirstObjectByType<RunnerCamera>();follower.Snap();var camera=follower.GetComponent<Camera>();var canvas=hud.GetComponent<Canvas>();
            var texture=new RenderTexture(540,960,24);var image=new Texture2D(540,960,TextureFormat.RGB24,false);
            var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldPlane=canvas.planeDistance;var oldActive=RenderTexture.active;
            try
            {
                camera.targetTexture=texture;camera.aspect=9f/16;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.5f;
                yield return new WaitForSecondsRealtime(0.12f);yield return null;yield return null;RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes("Logs/M8-"+name+".png",image.EncodeToPNG());
                if(name.EndsWith("-Gameplay"))
                {
                    using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
                    using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
                    using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
                    {
                        yield return null;yield return null;long total=0,peakMain=0,peakDraws=0,peakGc=0;
                        for(var i=0;i<30;i++){yield return null;total+=main.LastValue;peakMain=Math.Max(peakMain,main.LastValue);peakDraws=Math.Max(peakDraws,draws.LastValue);peakGc=Math.Max(peakGc,gc.LastValue);}
                        Assert.That(peakDraws,Is.GreaterThan(0));performance.Add(string.Join(",",name,total/30,peakMain,peakDraws,peakGc));File.WriteAllLines("Logs/M8-RenderProfiler.csv",performance);
                    }
                }
            }
            finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;camera.targetTexture=oldTarget;camera.ResetAspect();RenderTexture.active=oldActive;texture.Release();Object.Destroy(texture);Object.Destroy(image);}
        }
    }
}
