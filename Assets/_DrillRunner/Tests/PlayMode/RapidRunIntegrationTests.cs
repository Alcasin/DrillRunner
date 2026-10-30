using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Unity.Profiling;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private TrackCatalog stateCatalog;
        private void ForceFixtureEnd() { motor.Energy.Consume(1000000,1); motor.Simulate(0.001f); }
        private void Tick(float dt = 1f/120) { motor.Simulate(dt); track.UpdateCoverage(motor.Motion.Position.z,motor.ContactTarget); }
        private void Until(Func<bool> condition, int limit = 10000)
        { for (var i = 0; i < limit && !condition(); i++) Tick(); Assert.That(condition(),Is.True,"State="+motor.Motion.State); }
        [UnityTest] public IEnumerator ProductionStateConsumptionIsExclusiveSnapshottedAndAllowsBlockedEscape()
        {
            Configure(1,0,1337); var wood = motor.RegisteredObstacles.First(o=>o.Definition.MaterialId=="wood");
            foreach (var target in motor.RegisteredObstacles) target.gameObject.SetActive(target==wood); wood.transform.position = new Vector3(0,1,9);
            var profile = flow.Balance.ForForm(ProgressionConfig.PocketForm);
            Launch(); var before = motor.Energy.Remaining; Tick(0.25f); Assert.That(before-motor.Energy.Remaining,Is.EqualTo(profile.Moving*0.25f).Within(0.001));
            motor.Drilling.MovingEnergyPerSecond=999; motor.Drilling.DrillingBaseEnergyPerSecond=999;
            before=motor.Energy.Remaining; Tick(0.01f); Assert.That(before-motor.Energy.Remaining,Is.EqualTo(profile.Moving*0.01f).Within(0.001));
            Until(()=>motor.Motion.State==RunState.Drilling); var hp=wood.Durability.Remaining; before=motor.Energy.Remaining;
            var x=motor.Motion.Position.x; Steer(2); Tick(0.15f);
            Assert.That(before-motor.Energy.Remaining,Is.EqualTo(profile.DrillingRate(wood.Definition.DrillingEnergyPerSecond)*0.15f).Within(0.001)); Assert.That(hp-wood.Durability.Remaining,Is.EqualTo(50*0.15f).Within(0.02));
            Assert.That(motor.Motion.Position.x,Is.EqualTo(x).Within(0.001)); Assert.That(flow.Run.Rewards.ObstacleCoins,Is.Zero);
            ForceFixtureEnd(); Configure(1,0,1337); stateCatalog=LegacyTemplateFixture.Restore(track,hud);
            var stone=motor.RegisteredObstacles.Single(o=>o.Definition.MaterialId=="stone");
            foreach(var target in motor.RegisteredObstacles)target.gameObject.SetActive(target==stone); stone.transform.position=new Vector3(0,1,8);
            Launch(); Until(()=>motor.Motion.State==RunState.Blocked); before=motor.Energy.Remaining; Tick(0.35f);
            Assert.That(before-motor.Energy.Remaining,Is.EqualTo(profile.Blocked*0.35f).Within(0.001)); Assert.That(stone.Durability.Remaining,Is.EqualTo(240));
            Steer(2.05f); Until(()=>motor.Motion.State==RunState.Moving); Assert.That(motor.Energy.Remaining,Is.GreaterThan(motor.Energy.Capacity - profile.Moving * 0.5f - profile.Blocked * 0.65f));
            ForceFixtureEnd(); yield return null;
        }
        [UnityTest] public IEnumerator SafeModerateAndGuardedCopperRoutesUseActualCostsAndReactionAwareFootprintClearance()
        {
            var rows=new List<string>{"stage,route,drillSeconds,drillEnergy,coins,copper"};
            for(var stage=1;stage<=8;stage++) for(var route=0;route<3;route++)
            {
                ConfigureLevels(stage, 0, stage == 1 ? 1 : 0, 0, 1337); var library=track.Encounters;var sequence=new EncounterSequence(1337,library,EncounterEnvelope.From(motor));var ordinal=0;
                while(sequence.Next().SectionId!="OreGuardRight"&&ordinal++<1000){} Assert.That(ordinal,Is.LessThan(1000));
                var start=track.EncounterStartZ+ordinal*40; track.UpdateCoverage(start,null); var section=track.ActiveSections.Single(s=>s.StartZ==start);
                var wood=section.Obstacles.Single(o=>o.Definition.MaterialId=="wood");var brick=section.Obstacles.Single(o=>o.Definition.MaterialId=="brick");var ore=section.Obstacles.Single(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper);
                var targetX=route==1?0.8f:-0.65f; motor.Motion.ResolvePosition(new Vector3(targetX,0,start),true);motor.transform.position=motor.Motion.Position;
                Launch();var clearedAt=-1.0;var ticks=0;
                while(motor.Motion.Position.z<start+41.2f&&motor.Motion.State!=RunState.RunEnded&&ticks++<10000)
                {
                    if(route==2&&wood.Durability.IsDestroyed&&motor.Motion.Position.z-1.15f>brick.WorldBounds.max.z+0.08f)
                    {
                        if(clearedAt<0)clearedAt=motor.Diagnostics.Seconds;
                        if(motor.Diagnostics.Seconds-clearedAt>=library.ReactionSeconds)targetX=0.8f;
                    }
                    Steer(targetX);Tick(); Assert.That(motor.Motion.State,Is.Not.EqualTo(RunState.Blocked));
                    if(!ore.Durability.IsDestroyed)Assert.That(flow.Run.Rewards.Copper,Is.Zero);
                }
                Assert.That(motor.Motion.Position.z,Is.GreaterThanOrEqualTo(start+41.2f));
                var expectedTargets=route==0?new[]{wood}:route==1?new[]{brick,ore}:new[]{wood,ore};
                var expected=expectedTargets.Sum(o=>o.Definition.Durability/(motor.DrillPower*motor.Drilling.DamagePerPower)*Rate(o));
                Assert.That(motor.Diagnostics.DrillingEnergy,Is.EqualTo(expected).Within(0.005)); Assert.That(flow.Run.Rewards.Copper,Is.EqualTo(route==0?0:3));
                Assert.That(flow.Run.Rewards.ObstacleCoins,Is.EqualTo(route==1?50:25));
                if(stage==1)Assert.That(expected,Is.EqualTo(route==0?4.34:route==1?15:10.1).Within(0.01));
                rows.Add(string.Join(",",new object[]{stage,route==0?"Wood":route==1?"BrickOre":"WoodThenOre",motor.Diagnostics.DrillingSeconds,motor.Diagnostics.DrillingEnergy,flow.Run.Rewards.ObstacleCoins,flow.Run.Rewards.Copper}
                    .Select(v=>v is IFormattable f?f.ToString(null,System.Globalization.CultureInfo.InvariantCulture):v.ToString())));
                File.WriteAllLines("Logs/M9A-M8R4-RouteCosts.csv",rows);ForceFixtureEnd();yield return null;
            }
        }
        [UnityTest] public IEnumerator DepletionResultsAndRapidRetryBankOnceAndKeepTheSandboxIsolated()
        {
            var original=ProgressCodec.Encode(memory.Load().Progress);Configure(1,0,1337);var ended=0;Action count=()=>ended++;motor.RunEnded+=count;
            yield return Drive(Policy.Balanced);var completed=flow.Run.Rewards;var saved=flow.Service.Snapshot;var position=motor.Motion.Position;
            for(var i=0;i<20;i++)Tick(0.1f);Assert.That(ended,Is.EqualTo(1));Assert.That(motor.Motion.Position,Is.EqualTo(position));
            Assert.That(flow.RetryBank(),Is.True);Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(saved.Coins));yield return null;
            Assert.That(hud.SummaryText.gameObject.activeSelf,Is.True);Assert.That(hud.SummaryText.text,Does.Contain(completed.Coins+" Coins"));
            Assert.That(hud.RestartButton.GetComponentInChildren<Text>().text,Is.EqualTo("RETRY"));Assert.That(Object.FindFirstObjectByType<PresentationAudio>().LoopCue,Is.EqualTo(-1));
            hud.RestartButton.onClick.Invoke();var next=flow.Run.Rewards;for(var i=0;i<12;i++)hud.RestartButton.onClick.Invoke();
            Assert.That(flow.Run.Rewards,Is.SameAs(next));Assert.That(next,Is.Not.SameAs(completed));Assert.That(next.Coins,Is.Zero);
            Assert.That(flow.Service.Snapshot.IssuedAttempt,Is.EqualTo(saved.IssuedAttempt+1));Assert.That(flow.Service.Snapshot.Coins,Is.EqualTo(saved.Coins));
            Assert.That(ProgressCodec.Encode(memory.Load().Progress),Is.EqualTo(original));motor.RunEnded-=count;yield return null;
            Assert.That(flow.OpenWorkshop(),Is.True);var level=flow.Service.Snapshot.Level(UpgradeCategory.EnergyCapacity);flow.View.PurchaseButtons[1].onClick.Invoke();
            Assert.That(flow.Service.Snapshot.Level(UpgradeCategory.EnergyCapacity),Is.EqualTo(level+1));yield return null;
            flow.View.StartButton.onClick.Invoke();var fromWorkshop=flow.Run.Rewards;for(var i=0;i<12;i++)flow.View.StartButton.onClick.Invoke();
            Assert.That(flow.Run.Rewards,Is.SameAs(fromWorkshop));Assert.That(motor.Energy.Capacity,Is.EqualTo(30));Assert.That(motor.Drilling.MovingEnergyPerSecond,Is.EqualTo(3.35f));
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,Is.EqualTo(5));Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(5));
            yield return Load();Assert.That(flow.Service.Snapshot.Coins,Is.Zero);Assert.That(flow.Service.Snapshot.Copper,Is.Zero);Assert.That(flow.Service.Snapshot.PowerLevel,Is.Zero);Assert.That(flow.IsSandbox,Is.False);
        }
        [UnityTest] public IEnumerator NaturalUpdateEpisodesRenderTheShortRunResultsUpgradeAndImmediateNextAttempt()
        {
            var rows=new List<string>{"stage,level,seconds,distance,coins,copper,banked"};
            var counters=new List<string>{"stage,level,samples,meanMainNs,peakMainNs,peakDraws,peakGcBytes,peakSections,peakTargets"};
            foreach(var profile in new[]{new[]{1,0},new[]{1,2},new[]{2,0},new[]{2,5},new[]{6,2}})
            {
                Configure(profile[0],profile[1],1337);var label="Evo"+profile[0]+"-L"+profile[1];motor.enabled=true;
                var expectedCarry=flow.Balance.ForForm(ProgressionConfig.PocketForm).BoostDecayByDriveLevel[0]/flow.Balance.ForForm(flow.Service.ActiveForm.FormId).BoostDecayByDriveLevel[profile[1]];
                Assert.That(flow.View.Rows[2].text,Does.Contain("Coast \u00d7"+expectedCarry.ToString("0.##")));
                using(var framing=new LiveFrameTarget(Object.FindFirstObjectByType<RunnerCamera>().GetComponent<Camera>(),hud.GetComponent<Canvas>()))
                using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
                using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
                using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
                try
                {
                    motor.Motion.Press();yield return new WaitForSecondsRealtime(0.5f);yield return Capture(label+"-Charging");motor.Motion.Release();
                    var wallStart=Time.realtimeSinceStartup;var moving=false;var drilling=false;var copper=false;
                    long totalMain=0,peakMain=0,peakDraws=0,peakGc=0;var samples=0;var peakSections=0;var peakTargets=0;
                    while(motor.Motion.State!=RunState.RunEnded)
                    {
                        aim=Choose(Policy.Balanced);Steer(aim);
                        if(motor.Diagnostics.Seconds>=1&&samples<60)
                        {samples++;totalMain+=main.LastValue;peakMain=Math.Max(peakMain,main.LastValue);peakDraws=Math.Max(peakDraws,draws.LastValue);peakGc=Math.Max(peakGc,gc.LastValue);}
                        peakSections=Math.Max(peakSections,track.ActiveSections.Count);peakTargets=Math.Max(peakTargets,motor.RegisteredObstacles.Count);
                        if(!moving&&motor.Diagnostics.Seconds>=1){moving=true;yield return Capture(label+"-Moving");}
                        if(!drilling&&profile[0]<=2&&motor.Motion.State==RunState.Drilling){drilling=true;yield return Capture(label+"-Drilling");}
                        if(!copper&&flow.Run.Rewards.Copper>0){copper=true;yield return Capture(label+"-Copper");}
                        Assert.That(Time.realtimeSinceStartup-wallStart,Is.LessThan(60),"Test workload guard; never an in-game timer.");yield return null;
                    }
                    Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);Assert.That(flow.IsBanked,Is.True);Assert.That(motor.Diagnostics.Seconds,Is.LessThan(46));
                    yield return null;yield return Capture(label+"-Results");
                    rows.Add(string.Join(",",new object[]{profile[0],profile[1],motor.Diagnostics.Seconds,flow.Run.Rewards.Distance,flow.Run.Rewards.Coins,flow.Run.Rewards.Copper,flow.IsBanked}
                        .Select(v=>v is IFormattable f?f.ToString(null,System.Globalization.CultureInfo.InvariantCulture):v.ToString())));File.WriteAllLines("Logs/M9A-M8R4-NaturalEpisodes.csv",rows);
                    counters.Add(string.Join(",",profile[0],profile[1],samples,totalMain/Math.Max(1,samples),peakMain,peakDraws,peakGc,peakSections,peakTargets));File.WriteAllLines("Logs/M9A-M8R4-Profiler.csv",counters);
                    Assert.That(peakSections,Is.LessThanOrEqualTo(10));Assert.That(peakTargets,Is.LessThanOrEqualTo(40));
                    Assert.That(hud.SummaryText.gameObject.activeSelf,Is.True);Assert.That(Object.FindFirstObjectByType<PresentationAudio>().LoopCue,Is.EqualTo(-1));
                    flow.OpenWorkshop();if(profile[1]<5)Assert.That(flow.Purchase(UpgradeCategory.EnergyCapacity),Is.True);yield return Capture(label+"-Workshop");
                    if(profile[0]==1&&profile[1]==0)
                    {
                        var old=flow.Run.Rewards;flow.View.StartButton.onClick.Invoke();motor.GetComponent<DrillPointerInput>().enabled=false;
                        Assert.That(flow.Run.Rewards,Is.Not.SameAs(old));Assert.That(motor.Energy.Capacity,Is.EqualTo(30));
                        motor.Motion.Press();yield return new WaitForSecondsRealtime(0.2f);motor.Motion.Release();yield return new WaitForSecondsRealtime(0.8f);yield return Capture(label+"-ImmediateNextAttempt");
                        while(motor.Motion.State!=RunState.RunEnded){aim=Choose(Policy.Balanced);Steer(aim);yield return null;}
                        Assert.That(motor.Diagnostics.EndedByEnergy,Is.True);
                    }
                }
                finally{motor.enabled=false;}
            }
            foreach(var level in new[]{0,5})
            {Configure(8,level,1337);flow.OpenWorkshop();yield return Capture("Evo8-L"+level+"-Workshop");}
        }
        private sealed class LiveFrameTarget : IDisposable
        {
            private readonly Camera camera;private readonly Canvas canvas;private readonly RenderTexture target,previous;private readonly RenderMode mode;private readonly Camera uiCamera;private readonly float plane;
            public LiveFrameTarget(Camera camera,Canvas canvas)
            {
                this.camera=camera;this.canvas=canvas;previous=camera.targetTexture;mode=canvas.renderMode;uiCamera=canvas.worldCamera;plane=canvas.planeDistance;
                target=new RenderTexture(540,960,24);camera.targetTexture=target;camera.aspect=9f/16;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.5f;
            }
            public void Dispose()
            {canvas.renderMode=mode;canvas.worldCamera=uiCamera;canvas.planeDistance=plane;camera.targetTexture=previous;camera.ResetAspect();target.Release();Object.Destroy(target);}
        }
        private IEnumerator Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)yield break;
            var follower=Object.FindFirstObjectByType<RunnerCamera>();if(!motor.enabled)follower.Snap();var camera=follower.GetComponent<Camera>();var canvas=hud.GetComponent<Canvas>();
            var texture=new RenderTexture(540,960,24);var image=new Texture2D(540,960,TextureFormat.RGB24,false);var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;
            var oldCamera=canvas.worldCamera;var oldPlane=canvas.planeDistance;var oldActive=RenderTexture.active;
            try
            {
                camera.targetTexture=texture;camera.aspect=9f/16;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=0.5f;
                yield return new WaitForSecondsRealtime(0.08f);yield return null;yield return null;
                RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes("Logs/M9A-M8R4-"+name+".png",image.EncodeToPNG());
            }
            finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;camera.targetTexture=oldTarget;camera.ResetAspect();RenderTexture.active=oldActive;texture.Release();Object.Destroy(texture);Object.Destroy(image);}
        }
    }
}
