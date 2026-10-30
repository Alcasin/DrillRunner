using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class AdvancedRegionSetup
    {
        public const string Data = RegionSetup.Data + "/Advanced";
        public const string Prefabs = FoundationSetup.Root + "/Prefabs/AdvancedRegions";
        private static Material Steel => PresentationAssets.Metal;
        private static Material Dark => PresentationAssets.Dark;
        private static Material Purple => Paint("RefineryPurple",new Color(0.37f,0.23f,0.5f),0.3f);
        private static Material Cyan => Paint("RefineryCyan",new Color(0.2f,0.84f,0.83f),0.2f);
        private static Material Gold => Paint("AlloyGold",new Color(0.96f,0.64f,0.2f),0.55f);
        private static Material Navy => Paint("NexusNavy",new Color(0.12f,0.21f,0.31f),0.6f);
        private static Material Blue => Paint("NexusBlue",new Color(0.35f,0.69f,0.98f),0.25f);
        private readonly struct Place
        {
            public readonly string Material; public readonly float X,Z,Width;
            public Place(string material,float x,float z,float width=0.9f){Material=material;X=x;Z=z;Width=width;}
        }
        [MenuItem("Tools/DrillRunner/Setup M9-B Advanced Regions")]
        public static void Setup()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before advanced-region setup.");
            var loaded=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(FoundationSetup.ScenePath);
            if(loaded.IsValid()&&loaded.isLoaded&&loaded.isDirty)throw new InvalidOperationException("Save scene edits first; custom work is preserved.");
            var regions=AssetDatabase.LoadAssetAtPath<RegionCatalog>(RegionSetup.CatalogPath);
            if(regions==null)throw new InvalidOperationException("Run the existing M9-A integration before advanced setup.");
            if(regions.Find(RegionIds.Advanced).Implemented&&regions.Find(RegionIds.Energy).Implemented)
            {Validate(regions);Debug.Log("M9-B already configured; all authored files preserved.");return;}
            Folder(RegionSetup.Data,"Advanced");Folder(FoundationSetup.Root+"/Prefabs","AdvancedRegions");
            Define("CrystalComposite","crystal_composite","Crystal Composite",50,1500,38,16,ObstacleRewardKind.Coins,new Color(0.64f,0.4f,0.8f));
            Define("EnergyAlloy","energy_alloy","Energy Alloy",100,2200,55,32,ObstacleRewardKind.Coins,new Color(0.2f,0.4f,0.56f));
            Define("QuartzOre","quartz_ore","Quartz Ore",50,650,30,4,ObstacleRewardKind.Quartz,new Color(0.4f,0.94f,0.94f));
            // Accessible at EVO 5 but slower/costlier than Quartz; EVO 6's Power makes its core efficient.
            Define("AlloyCore","alloy_core","Alloy Core",52,1800,55,3,ObstacleRewardKind.AlloyCore,new Color(1,0.7f,0.25f));
            Define("EnergyShard","energy_shard","Energy Shard Ore",120,1200,48,2,ObstacleRewardKind.EnergyShard,new Color(0.35f,0.7f,1));
            foreach(var name in new[]{"CrystalComposite","EnergyAlloy","QuartzOre","AlloyCore","EnergyShard"})Obstacle(name);
            Build(regions.Find(RegionIds.Advanced),false);Build(regions.Find(RegionIds.Energy),true);
            FinishMaterials();Validate(regions);AssetDatabase.SaveAssets();
            Debug.Log("M9-B integrated: Crystal Refinery and Energy Nexus, three real resource sources, two normal tiers. Existing scene, pacing, prices and saves preserved.");
        }
        public static void Validate(RegionCatalog regions)
        {
            regions.Validate();RegionSetup.Validate(regions);
            foreach(var id in new[]{RegionIds.Advanced,RegionIds.Energy})
            {
                var r=regions.Find(id);GameplayVarietySetup.Validate(r.Encounters);
                if(!SectionSequence.IsSafe(r.Introduction,r.Track,0.85f))throw new ArgumentException("Unsafe regional introduction.");
                if(r.Environment==null||r.Environment.GetComponentsInChildren<Collider>(true).Length!=0)throw new ArgumentException("Dressing must be collider-free.");
                foreach(var p in r.Encounters.Patterns)
                {
                    if(p.Section.GetComponentsInChildren<Collider>(true).Length!=p.Section.Obstacles.Length+1)throw new ArgumentException("Unexpected regional collider.");
                    foreach(var o in p.Section.Obstacles)if(MaterialDefinition.IsResource(o.Definition.RewardKind)&&!r.Resources.Contains((EvolutionResource)((int)o.Definition.RewardKind-1)))throw new ArgumentException("Foreign regional resource.");
                }
            }
        }
        // Explicit measured configuration: no shared material or earlier-region edits.
        [MenuItem("Tools/DrillRunner/Apply M9-B-R Refinery Drilling Costs")]
        public static void ApplyRefineryDrillingCosts()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before balance authoring.");
            var regions=AssetDatabase.LoadAssetAtPath<RegionCatalog>(RegionSetup.CatalogPath);
            Validate(regions);
            var refinery=regions.Find(RegionIds.Advanced);
            var measured=new[]{new MaterialDrillingCost("crystal_composite",8),new MaterialDrillingCost("quartz_ore",8),new MaterialDrillingCost("alloy_core",9),new MaterialDrillingCost("energy_alloy",9)};
            if(refinery.DrillingCosts.Length>0)
            {
                if(refinery.DrillingCosts.SequenceEqual(measured)){Debug.Log("M9-B-R already configured; all authored files preserved.");return;}
                // Only migrate the exact first measured M9-B-R candidate; retain any owner tuning.
                var firstCandidate=(MaterialDrillingCost[])measured.Clone();firstCandidate[0].Multiplier=10;
                if(!refinery.DrillingCosts.SequenceEqual(firstCandidate))throw new InvalidOperationException("Custom Refinery drilling costs preserved; review before replacing.");
            }
            refinery.DrillingCosts=measured;refinery.Validate();EditorUtility.SetDirty(refinery);AssetDatabase.SaveAssetIfDirty(refinery);
            Debug.Log("M9-B-R: Refinery-only active drilling coefficients applied. Raw materials and all other regions preserved.");
        }
        // Historical M9-B reward authoring pass; touches only its five material definitions.
        public static void ApplyMeasuredBalance()
        {
            string[] names={"CrystalComposite","EnergyAlloy","QuartzOre","AlloyCore","EnergyShard"};int[] original={28,56,4,3,2},revised={16,32,4,3,2};
            for(int i=0;i<names.Length;i++)
            {
                var d=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Data+"/"+names[i]+".asset");
                if(d==null||d.RewardAmount!=original[i]&&d.RewardAmount!=revised[i])throw new InvalidOperationException("Custom tuning preserved: "+names[i]);
            }
            for(int i=0;i<names.Length;i++)
            {
                var d=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Data+"/"+names[i]+".asset");if(d.RewardAmount==revised[i])continue;
                d.RewardAmount=revised[i];EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
            }
            FinishMaterials();Setup();Debug.Log("M9-B measured reward revision applied; existing-region economy and all gameplay costs unchanged.");
        }
        private static void FinishMaterials()
        {
            // Restrained self-lit crystal/conduit surfaces; no bloom, real-time lights, extra passes or geometry.
            foreach(var material in new[]{Cyan,Blue})
            {
                if(material.IsKeywordEnabled("_EMISSION"))continue;
                material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",material.GetColor("_BaseColor")*0.2f);
                EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            }
        }
        private static void Build(RegionDefinition region,bool nexus)
        {
            string prefix=nexus?"Nexus":"Refinery";Environment(prefix,nexus);
            var track=ScriptableObject.CreateInstance<TrackCatalog>();track.InitialPower=nexus?125:52;
            track.ResourceKind=nexus?ObstacleRewardKind.EnergyShard:ObstacleRewardKind.Quartz;
            track.Fallback=Section(prefix+"Open",prefix,nexus);
            var library=ScriptableObject.CreateInstance<EncounterLibrary>();library.Catalog=track;library.ResourceKind=track.ResourceKind;
            library.FirstSelectionCopper=true;library.OpeningCopperBySelection=1;library.StartingOreEnergyBudget=500;library.RicherFromSection=8;
            var patterns=new List<EncounterDefinition>();string normal=nexus?"EnergyAlloy":"CrystalComposite",ore=nexus?"EnergyShard":"QuartzOre";
            foreach(int side in new[]{-1,1})
            {
                string suffix=side<0?"Left":"Right";float x=side*0.95f,bypass=-side*2.05f;
                patterns.Add(Pattern(prefix+"Flow"+suffix,prefix,nexus,EncounterCategory.EasyFlow,EncounterArrangement.FlowArc,0,bypass,
                    new Place(normal,x,nexus?20:19),new Place(normal,x,nexus?32:29)));
                patterns.Add(Pattern(prefix+"Dense"+suffix,prefix,nexus,EncounterCategory.DenseDrilling,EncounterArrangement.SplitCluster,2,bypass,
                    new Place(normal,side*0.2f,nexus?19:20,1.1f),new Place(normal,x,nexus?25:28),new Place(nexus?"CrystalComposite":"ReinforcedBlock",x,nexus?31:34)));
                // Calm opening resource line. The Refinery offers both resources in each resource-bearing pattern.
                patterns.Add(Pattern(prefix+"Resource"+suffix,prefix,nexus,EncounterCategory.RiskReward,EncounterArrangement.OreChoice,0,bypass,
                    new Place(ore,x,nexus?21:19),new Place(nexus?ore:"AlloyCore",x,nexus?31:27),new Place(normal,nexus?-side*0.6f:x,nexus?26:34,nexus?0.6f:0.9f)));
                patterns.Add(Pattern(prefix+"Guard"+suffix,prefix,nexus,EncounterCategory.RiskReward,EncounterArrangement.GuardedChoice,2,bypass,
                    new Place(normal,x,nexus?20:19),new Place(ore,x,nexus?27:26),new Place(nexus?ore:"AlloyCore",x,nexus?34:33)));
                patterns.Add(Pattern(prefix+"Escape"+suffix,prefix,nexus,EncounterCategory.PrecisionEscape,EncounterArrangement.StoneStagger,2,bypass,
                    new Place("EnergyAlloy",side*(nexus?0.3f:0.15f),nexus?21:19,1.15f),new Place(nexus?"CrystalComposite":normal,x,nexus?30:29)));
                patterns.Add(Pattern(prefix+"Power"+suffix,prefix,nexus,EncounterCategory.PowerTest,EncounterArrangement.PowerFork,1,bypass,
                    new Place("EnergyAlloy",x,nexus?20:22),new Place("CrystalComposite",-side*0.55f,nexus?28:22,0.65f)));
            }
            patterns.Add(Pattern(prefix+"Recovery",prefix,nexus,EncounterCategory.Recovery,EncounterArrangement.QuietDeck,0,-2.05f));
            patterns.Add(Pattern(prefix+"RecoveryResource",prefix,nexus,EncounterCategory.Recovery,EncounterArrangement.OreChoice,0,-2.05f,
                new Place(ore,1,20),new Place(nexus?ore:"AlloyCore",1,30)));
            library.Patterns=patterns.ToArray();track.Variants=patterns.Select(p=>p.Section).ToArray();
            AssetDatabase.CreateAsset(track,Data+"/"+prefix+"Track.asset");AssetDatabase.CreateAsset(library,Data+"/"+prefix+"Encounters.asset");
            region.DisplayName=nexus?"Energy Nexus":"Crystal Refinery";region.Identity=nexus?"Reactor towers, steel conduits and blue power cells":"Quartz extraction, purple chambers and cyan conveyors";
            region.Implemented=true;region.Track=track;region.Encounters=library;region.Introduction=Section(prefix+"Introduction",prefix,nexus,new Place(normal,0,25,1));
            region.Environment=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+prefix+"Environment.prefab");
            region.Materials=(nexus?new[]{"CrystalComposite","EnergyAlloy","EnergyShard"}:new[]{"CrystalComposite","EnergyAlloy","QuartzOre","AlloyCore"}).Select(n=>AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Data+"/"+n+".asset")).ToArray();
            region.Resources=nexus?new[]{EvolutionResource.EnergyShard}:new[]{EvolutionResource.Quartz,EvolutionResource.AlloyCore};
            region.Sky=nexus?new Color(0.36f,0.47f,0.58f):new Color(0.69f,0.73f,0.81f);EditorUtility.SetDirty(region);
        }
        private static void Define(string name,string id,string display,float hardness,float hp,float rate,int amount,ObstacleRewardKind kind,Color color)
        {
            string path=Data+"/"+name+".asset";if(AssetDatabase.LoadAssetAtPath<MaterialDefinition>(path)!=null)return;
            var d=ScriptableObject.CreateInstance<MaterialDefinition>();d.MaterialId=id;d.DisplayName=display;d.Hardness=hardness;d.Durability=hp;d.DrillingEnergyPerSecond=rate;d.RewardAmount=amount;d.RewardKind=kind;d.FeedbackColor=color;d.Validate();AssetDatabase.CreateAsset(d,path);
        }
        private static Material Paint(string name,Color color,float metal)=>PresentationAssets.Material("M9B"+name,color,metal);
        private static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,Mesh mesh=null)=>PresentationAssets.Part(parent,name,position,size,material,mesh);
        private static Mesh Gem=>PresentationAssets.MeshAsset("M9BFacetedCrystal",()=>
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            var ring=new[]{new Vector3(-0.5f,0,0),new Vector3(0,0,0.5f),new Vector3(0.5f,0,0),new Vector3(0,0,-0.5f)};
            for(int i=0;i<4;i++)foreach(int end in new[]{-1,1}){int start=vertices.Count;vertices.Add(Vector3.up*end*0.5f);vertices.Add(ring[end>0?i:(i+1)%4]);vertices.Add(ring[end>0?(i+1)%4:i]);triangles.AddRange(new[]{start,start+1,start+2});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        });
        private static void Obstacle(string name)
        {
            string path=Prefabs+"/Obstacle"+name+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            var root=new GameObject(name);root.transform.localScale=new Vector3(1,2,2.4f);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh=PresentationAssets.Bevel;
                root.AddComponent<MeshRenderer>().sharedMaterial=name=="CrystalComposite"?Purple:name=="EnergyAlloy"?Navy:Dark;
                root.AddComponent<BoxCollider>();var obstacle=root.AddComponent<DrillObstacle>();var data=new SerializedObject(obstacle);data.FindProperty("definition").objectReferenceValue=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Data+"/"+name+".asset");data.ApplyModifiedProperties();
                var details=new GameObject("Resource Surface").transform;details.SetParent(root.transform,false);
                if(name=="QuartzOre"||name=="EnergyShard")
                {
                    for(int i=0;i<3;i++)
                    {var p=Part(details,name=="QuartzOre"?"Quartz Spear":"Energy Shard",new Vector3((i-1)*0.27f,0.05f+(i==1?0.1f:0),-0.52f),new Vector3(0.26f,i==1?0.82f:0.58f,0.28f),name=="QuartzOre"?Cyan:Blue,Gem);p.transform.localRotation=Quaternion.Euler(0,0,(i-1)*-12);}
                    Part(details,"Lower Ore Collar",new Vector3(0,-0.37f,-0.53f),new Vector3(0.88f,0.12f,0.1f),Steel);
                }
                else if(name=="AlloyCore")
                {
                    foreach(float y in new[]{-0.3f,0.3f})Part(details,"Containment Rail",new Vector3(0,y,-0.55f),new Vector3(0.8f,0.12f,0.13f),Steel);
                    var core=Part(details,"Amber Core",new Vector3(0,0,-0.58f),new Vector3(0.55f,0.48f,0.2f),Gold);core.transform.localRotation=Quaternion.Euler(0,0,45);
                    Part(details,"Core Center",new Vector3(0,0,-0.7f),new Vector3(0.18f,0.18f,0.08f),Dark);
                }
                else
                {
                    foreach(float x in new[]{-0.37f,0.37f})Part(details,"Armored Rail",new Vector3(x,0,-0.53f),new Vector3(0.1f,0.87f,0.1f),Steel);
                    for(int i=0;i<3;i++){var p=Part(details,name=="CrystalComposite"?"Composite Inlay":"Energy Conductor",new Vector3(0,(i-1)*0.26f,-0.54f),new Vector3(0.57f,0.08f,0.05f),name=="CrystalComposite"?Cyan:Blue);p.transform.localRotation=Quaternion.Euler(0,0,name=="CrystalComposite"?22:-15);}
                }
                PresentationWorld.Combine(details,"M9B"+name+"Details");var cracks=new GameObject[3];
                for(int i=0;i<3;i++){cracks[i]=Part(details,"Damage "+i,new Vector3((i-1)*0.18f,0,-0.76f),new Vector3(0.025f,0.64f,0.015f),Dark);cracks[i].transform.localRotation=Quaternion.Euler(0,0,27*i-24);cracks[i].SetActive(false);}
                var appearance=root.AddComponent<ObstacleAppearance>();data=new SerializedObject(appearance);data.FindProperty("details").objectReferenceValue=details.gameObject;Set(data.FindProperty("cracks"),cracks);data.ApplyModifiedProperties();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static void Environment(string prefix,bool nexus)
        {
            string path=Prefabs+"/"+prefix+"Environment.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;var root=new GameObject(prefix+" Machinery");
            try
            {
                Part(root.transform,"Facility Ground",new Vector3(0,-0.62f,20),new Vector3(28,0.4f,40),Paint(prefix+"Ground",nexus?new Color(0.22f,0.3f,0.38f):new Color(0.58f,0.59f,0.68f),0),PresentationAssets.Cube);
                foreach(int side in new[]{-1,1})
                {
                    if(!nexus)
                    {
                        foreach(float z in new[]{8f,29f})
                        {
                            Part(root.transform,"Refinement Chamber",new Vector3(side*5.7f,1.8f,z),new Vector3(2.6f,3.6f,3.1f),Purple);
                            Part(root.transform,"Cyan Chamber Window",new Vector3(side*4.35f,1.8f,z),new Vector3(0.12f,2.3f,1.8f),Cyan);
                            Part(root.transform,"Processing Cap",new Vector3(side*5.7f,3.7f,z),new Vector3(3,0.45f,3.5f),Steel);
                            for(int i=0;i<3;i++)Part(root.transform,"Extraction Crystal",new Vector3(side*(7.8f+i*0.5f),0.9f+i*0.2f,z+2),new Vector3(0.7f,2+i*0.3f,0.9f),Cyan,Gem);
                        }
                        Part(root.transform,"Crystal Conveyor",new Vector3(side*4.9f,0.7f,19),new Vector3(1.3f,0.5f,13),Dark);
                        for(int i=0;i<7;i++){Part(root.transform,"Conveyor Slat",new Vector3(side*4.9f,1,13+i*1.8f),new Vector3(1.32f,0.14f,0.24f),Steel);if(i%2==0)Part(root.transform,"Conveyed Quartz",new Vector3(side*4.9f,1.3f,13+i*1.8f),new Vector3(0.35f,0.6f,0.5f),Cyan,Gem);}
                    }
                    else
                    {
                        foreach(float z in new[]{8f,28f})
                        {
                            Part(root.transform,"Reactor Plinth",new Vector3(side*6,0.3f,z),new Vector3(3.2f,0.6f,4.5f),Navy);
                            Part(root.transform,"Reactor Column",new Vector3(side*6,2.4f,z),new Vector3(1.9f,4.2f,2.4f),Dark);
                            foreach(float y in new[]{0.9f,2.3f,3.8f})Part(root.transform,"Reactor Steel Collar",new Vector3(side*6,y,z),new Vector3(2.6f,0.3f,3),Steel);
                            Part(root.transform,"Vertical Power Channel",new Vector3(side*4.98f,2.3f,z),new Vector3(0.13f,3.8f,0.8f),Blue);
                            Part(root.transform,"Reactor Core",new Vector3(side*6,4.9f,z),new Vector3(0.9f,1.8f,1.1f),Blue,Gem);
                        }
                        for(int i=0;i<3;i++)Part(root.transform,"Energy Conduit",new Vector3(side*(3.7f+i*0.35f),0.15f,20),new Vector3(0.16f,0.2f,40),i==1?Blue:Steel);
                        Part(root.transform,"Power Cabinet",new Vector3(side*5,0.8f,18),new Vector3(1.5f,1.6f,3),Navy);
                        Part(root.transform,"Cabinet Status",new Vector3(side*4.2f,0.95f,18),new Vector3(0.04f,0.22f,1.5f),Gold);
                    }
                }
                PresentationWorld.Combine(root.transform,"M9B"+prefix+"Environment");PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static TrackSection Section(string id,string prefix,bool nexus,params Place[] places)
        {
            string path=Prefabs+"/"+id+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing.GetComponent<TrackSection>();var root=new GameObject(id);
            try
            {
                var surface=Part(root.transform,"Surface",new Vector3(0,-0.25f,20),new Vector3(6,0.5f,40),Paint(prefix+"Deck",nexus?new Color(0.26f,0.35f,0.43f):new Color(0.43f,0.43f,0.55f),0.15f),PresentationAssets.Cube);surface.AddComponent<BoxCollider>();
                foreach(int side in new[]{-1,1})Part(root.transform,"Boundary",new Vector3(side*2.97f,0.012f,20),new Vector3(0.06f,0.012f,40),nexus?Blue:Cyan,PresentationAssets.Cube);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+prefix+"Environment.prefab"),root.transform);
                var obstacles=new List<DrillObstacle>();foreach(var p in places)
                {
                    string prefab=Prefabs+"/Obstacle"+p.Material+".prefab";if(p.Material=="ReinforcedBlock")prefab=RegionSetup.Prefabs+"/ObstacleReinforcedBlock.prefab";
                    var target=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab),root.transform);target.transform.localPosition=new Vector3(p.X,1,p.Z);target.transform.localScale=new Vector3(p.Width,2,2.4f);obstacles.Add(target.GetComponent<DrillObstacle>());
                }
                var entrance=new GameObject("Entrance").transform;entrance.SetParent(root.transform,false);var exit=new GameObject("Exit").transform;exit.SetParent(root.transform,false);exit.localPosition=Vector3.forward*40;
                var section=root.AddComponent<TrackSection>();var data=new SerializedObject(section);data.FindProperty("sectionId").stringValue=id;data.FindProperty("surface").objectReferenceValue=surface.GetComponent<BoxCollider>();data.FindProperty("entrance").objectReferenceValue=entrance;data.FindProperty("exit").objectReferenceValue=exit;Set(data.FindProperty("obstacles"),obstacles.ToArray());data.ApplyModifiedProperties();return PrefabUtility.SaveAsPrefabAsset(root,path).GetComponent<TrackSection>();
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static EncounterDefinition Pattern(string id,string prefix,bool nexus,EncounterCategory category,EncounterArrangement arrangement,int pressure,float bypass,params Place[] places)
        {
            var d=ScriptableObject.CreateInstance<EncounterDefinition>();d.Section=Section(id,prefix,nexus,places);d.Category=category;d.Arrangement=arrangement;d.Pressure=pressure;d.MinimumOrdinal=pressure==2?1:0;d.EarlyWeight=3;d.Weight=3;d.LateWeight=3;d.BypassX=bypass;d.Intent=prefix+": optional mining, independent full-footprint bypass, resource choices and pressure relief.";AssetDatabase.CreateAsset(d,Data+"/"+id+".asset");return d;
        }
        private static void Set<T>(SerializedProperty p,T[] values)where T:Object{p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
        private static void Folder(string parent,string name){if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);}
    }
}
