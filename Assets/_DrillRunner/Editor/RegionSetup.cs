using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class RegionSetup
    {
        public const string Data=FoundationSetup.Root+"/ScriptableObjects/Regions";
        public const string Prefabs=FoundationSetup.Root+"/Prefabs/Quarry";
        public const string CatalogPath=Data+"/Regions.asset";
        private static Material Rock=>PresentationAssets.Material("M9QuarryRock",new Color(0.44f,0.5f,0.57f),0.08f);
        private static Material Steel=>PresentationAssets.Material("M9QuarrySteel",new Color(0.28f,0.4f,0.49f),0.6f);
        private static Material Amber=>PresentationAssets.Material("M9QuarryAmber",new Color(0.97f,0.66f,0.19f),0.2f);
        private static Material Iron=>PresentationAssets.Material("M9IronSilver",new Color(0.73f,0.82f,0.87f),0.65f);
        private readonly struct Place
        {
            public readonly string Material;public readonly float X,Z,Width;
            public Place(string material,float x,float z,float width=0.9f){Material=material;X=x;Z=z;Width=width;}
        }
        [MenuItem("Tools/DrillRunner/Setup M9-A Regions and Quarry")]
        public static void Setup()=>SetupScene(FoundationSetup.ScenePath);
        public static void SetupScene(string path)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before region setup.");
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(!opened&&scene.isDirty)throw new InvalidOperationException("Save scene edits before setup; custom work is preserved.");
            var previous=SceneManager.GetActiveScene();if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var flow=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WorkshopController>(true)).Single();
                if(flow.Regions!=null){Validate(flow.Regions);if(flow.GetComponent<RegionWorkshopView>()==null)throw new InvalidOperationException("Region UI missing; preserve existing work and repair references.");Debug.Log("M9-A already configured; no authored bytes changed.");return;}
                Folder(FoundationSetup.Root+"/ScriptableObjects","Regions");Folder(FoundationSetup.Root+"/Prefabs","Quarry");
                var regions=AssetDatabase.LoadAssetAtPath<RegionCatalog>(CatalogPath);
                if(regions==null)
                {
                    MaterialAsset("Granite","granite",18,260,18,12,new Color(0.5f,0.58f,0.64f),ObstacleRewardKind.Coins);
                    MaterialAsset("MetalPlating","plating",26,420,24,24,new Color(0.3f,0.46f,0.55f),ObstacleRewardKind.Coins);
                    MaterialAsset("ReinforcedBlock","reinforced",40,620,32,40,new Color(0.23f,0.3f,0.36f),ObstacleRewardKind.Coins);
                    MaterialAsset("IronOre","iron_ore",18,240,20,6,new Color(0.69f,0.8f,0.86f),ObstacleRewardKind.Iron);
                    Environment();
                    foreach(var name in new[]{"Granite","MetalPlating","ReinforcedBlock","IronOre"})Obstacle(name);
                    var catalog=ScriptableObject.CreateInstance<TrackCatalog>();catalog.InitialPower=20;catalog.ResourceKind=ObstacleRewardKind.Iron;
                    catalog.Fallback=Section("QuarryOpen");
                    var library=ScriptableObject.CreateInstance<EncounterLibrary>();library.Catalog=catalog;library.ResourceKind=ObstacleRewardKind.Iron;
                    library.FirstSelectionCopper=true;library.OpeningCopperBySelection=1;library.StartingOreEnergyBudget=90;library.RicherFromSection=8;
                    var patterns=new List<EncounterDefinition>();
                    foreach(int side in new[]{-1,1})
                    {
                        string suffix=side<0?"Left":"Right";float bypass=-side*2.05f;
                        patterns.Add(Pattern("QuarryFlow"+suffix,EncounterCategory.EasyFlow,EncounterArrangement.FlowArc,0,0,bypass,new Place("Granite",side*0.65f,20),new Place("Granite",side*0.65f,26),new Place("Granite",side*0.65f,32)));
                        patterns.Add(Pattern("QuarryDense"+suffix,EncounterCategory.DenseDrilling,EncounterArrangement.SplitCluster,2,1,bypass,new Place("Granite",side*0.65f,20),new Place("MetalPlating",side*0.65f,26),new Place("Granite",side*0.65f,32)));
                        patterns.Add(Pattern("QuarryIronOpen"+suffix,EncounterCategory.RiskReward,EncounterArrangement.OreChoice,0,0,bypass,new Place("IronOre",side*0.95f,20),new Place("IronOre",side*0.95f,30),new Place("Granite",-side*0.55f,25,0.7f)));
                        patterns.Add(Pattern("QuarryIronGuard"+suffix,EncounterCategory.RiskReward,EncounterArrangement.GuardedChoice,2,1,bypass,new Place("Granite",side*0.8f,20),new Place("IronOre",side*0.8f,27),new Place("IronOre",side*0.8f,33)));
                        patterns.Add(Pattern("QuarryEscape"+suffix,EncounterCategory.PrecisionEscape,EncounterArrangement.StoneStagger,2,1,bypass,new Place("ReinforcedBlock",side*0.2f,21,1.2f),new Place("MetalPlating",side*0.95f,30,0.9f)));
                        patterns.Add(Pattern("QuarryPower"+suffix,EncounterCategory.PowerTest,EncounterArrangement.PowerFork,1,0,bypass,new Place("MetalPlating",side,22),new Place("Granite",-side*0.55f,22,0.7f),new Place("ReinforcedBlock",side*0.4f,30,1)));
                    }
                    patterns.Add(Pattern("QuarryRecovery",EncounterCategory.Recovery,EncounterArrangement.QuietDeck,0,0,-2.05f));
                    patterns.Add(Pattern("QuarryRecoveryIron",EncounterCategory.Recovery,EncounterArrangement.OreChoice,0,0,-2.05f,new Place("IronOre",1,21),new Place("IronOre",1,31)));
                    library.Patterns=patterns.ToArray();catalog.Variants=patterns.Select(p=>p.Section).ToArray();
                    AssetDatabase.CreateAsset(catalog,Data+"/QuarryTrack.asset");AssetDatabase.CreateAsset(library,Data+"/QuarryEncounters.asset");
                    var intro=Section("QuarryIntroduction",new Place("Granite",0,26,1.1f));
                    var yard=new SerializedObject(flow.Run.Track).FindProperty("starterTemplates").GetArrayElementAtIndex(0).objectReferenceValue as TrackSection;
                    var definitions=new[]{Definition(RegionIds.Yard,"Workshop Yard",1,true,flow.Run.Track.Catalog,flow.Run.Track.Encounters,yard),
                        Definition(RegionIds.Quarry,"Quarry / Scrapworks",3,true,catalog,library,intro),Definition(RegionIds.Advanced,"Advanced Works",5,false,null,null,null),Definition(RegionIds.Energy,"High Energy",7,false,null,null,null)};
                    definitions[0].Resources=new[]{EvolutionResource.Copper};definitions[1].Resources=new[]{EvolutionResource.Iron};
                    definitions[1].Environment=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/QuarryEnvironment.prefab");
                    definitions[1].Materials=new[]{"Granite","MetalPlating","ReinforcedBlock","IronOre"}.Select(n=>AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Data+"/"+n+".asset")).ToArray();
                    definitions[1].Sky=new Color(0.69f,0.74f,0.79f);definitions[2].Resources=new[]{EvolutionResource.Quartz,EvolutionResource.AlloyCore};definitions[3].Resources=new[]{EvolutionResource.EnergyShard};
                    foreach(var d in definitions){EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);}
                    regions=ScriptableObject.CreateInstance<RegionCatalog>();regions.Regions=definitions;AssetDatabase.CreateAsset(regions,CatalogPath);AssetDatabase.SaveAssets();
                }
                Validate(regions);var data=new SerializedObject(flow);data.FindProperty("regions").objectReferenceValue=regions;data.ApplyModifiedProperties();BuildUi(flow);
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save region integration.");
                Debug.Log("M9-A: two playable regions, four metadata definitions, Quarry materials/Iron and compact Workshop selector configured. No player profile accessed.");
            }
            finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened&&SceneManager.sceneCount>1)EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Validate(RegionCatalog catalog)
        {
            catalog.Validate();var quarry=catalog.Find(RegionIds.Quarry);GameplayVarietySetup.Validate(quarry.Encounters);
            if(!SectionSequence.IsSafe(quarry.Introduction,quarry.Track,0.85f))throw new ArgumentException("Unsafe Quarry introduction.");
            foreach(var p in quarry.Encounters.Patterns)
            {
                var boxes=p.Section.GetComponentsInChildren<Collider>(true);if(boxes.Length!=p.Section.Obstacles.Length+1)throw new ArgumentException("Decorative collider in Quarry.");
                if(p.Section.Obstacles.Any(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper))throw new ArgumentException("Quarry must not manufacture Copper.");
            }
        }
        private static RegionDefinition Definition(string id,string name,int stage,bool implemented,TrackCatalog track,EncounterLibrary encounters,TrackSection intro)
        {
            var d=ScriptableObject.CreateInstance<RegionDefinition>();d.Id=id;d.DisplayName=name;d.UnlockStage=stage;d.Implemented=implemented;d.Track=track;d.Encounters=encounters;d.Introduction=intro;d.Identity=id==RegionIds.Quarry?"Cool rock, silver Iron and warm industrial machinery":"";
            AssetDatabase.CreateAsset(d,Data+"/"+id+".asset");return d;
        }
        private static void MaterialAsset(string name,string id,float hardness,float hp,float rate,int amount,Color color,ObstacleRewardKind kind)
        {
            var path=Data+"/"+name+".asset";if(AssetDatabase.LoadAssetAtPath<MaterialDefinition>(path)!=null)return;
            var d=ScriptableObject.CreateInstance<MaterialDefinition>();d.MaterialId=id;d.DisplayName=name=="MetalPlating"?"Metal Plating":name=="ReinforcedBlock"?"Reinforced Block":name=="IronOre"?"Iron Ore":name;
            d.Hardness=hardness;d.Durability=hp;d.DrillingEnergyPerSecond=rate;d.RewardAmount=amount;d.RewardKind=kind;d.FeedbackColor=color;d.Validate();AssetDatabase.CreateAsset(d,path);
        }
        private static void Obstacle(string name)
        {
            string path=Prefabs+"/Obstacle"+name+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            var root=new GameObject("Quarry "+name);root.transform.localScale=new Vector3(1,2,2.4f);
            try
            {
                var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=PresentationAssets.Bevel;root.AddComponent<MeshRenderer>().sharedMaterial=name=="Granite"?Rock:name=="IronOre"?PresentationAssets.Material("M9OreHost",new Color(0.24f,0.31f,0.36f),0.1f):Steel;
                root.AddComponent<BoxCollider>();var o=root.AddComponent<DrillObstacle>();var data=new SerializedObject(o);data.FindProperty("definition").objectReferenceValue=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Data+"/"+name+".asset");data.ApplyModifiedProperties();
                var details=new GameObject("Industrial Surface").transform;details.SetParent(root.transform,false);
                if(name=="Granite")for(int i=0;i<4;i++){var part=PresentationAssets.Part(details,"Rock Facet",new Vector3((i%2-0.5f)*0.4f,(i/2-0.5f)*0.38f,-0.495f),new Vector3(0.46f,0.42f,0.08f),i%2==0?Rock:Iron);part.transform.localRotation=Quaternion.Euler(0,0,i*11-17);}
                else if(name=="IronOre")for(int i=0;i<7;i++){var part=PresentationAssets.Part(details,"Silver Iron Deposit",new Vector3((i%3-1)*0.26f,(i/3-1)*0.28f,-0.51f),new Vector3(0.18f,0.23f,0.15f),Iron);part.transform.localRotation=Quaternion.Euler(0,0,i*17+30);}
                else
                {
                    foreach(float x in new[]{-0.36f,0.36f})foreach(float y in new[]{-0.34f,0.34f})PresentationAssets.Part(details,"Structural Bolt",new Vector3(x,y,-0.52f),new Vector3(0.09f,0.09f,0.06f),Iron);
                    for(int i=0;i<3;i++)PresentationAssets.Part(details,name=="MetalPlating"?"Metal Rib":"Heavy Reinforcement",new Vector3(0,-0.26f+i*0.26f,-0.51f),new Vector3(0.76f,name=="MetalPlating"?0.045f:0.13f,0.05f),name=="MetalPlating"?Iron:Amber);
                }
                PresentationWorld.Combine(details,"M9"+name+"Detail");var cracks=new GameObject[3];
                for(int i=0;i<3;i++){cracks[i]=PresentationAssets.Part(details,"Damage "+i,new Vector3((i-1)*0.16f,0,-0.62f),new Vector3(0.018f,0.58f,0.01f),PresentationAssets.Dark);cracks[i].transform.localRotation=Quaternion.Euler(0,0,25*i-20);cracks[i].SetActive(false);}
                var appearance=root.AddComponent<ObstacleAppearance>();data=new SerializedObject(appearance);data.FindProperty("details").objectReferenceValue=details.gameObject;Set(data.FindProperty("cracks"),cracks);data.ApplyModifiedProperties();PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static void Environment()
        {
            string path=Prefabs+"/QuarryEnvironment.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;var root=new GameObject("Quarry Dressing");
            try
            {
                var ground=PresentationAssets.Material("M9QuarryGround",new Color(0.55f,0.6f,0.64f),0);
                PresentationAssets.Part(root.transform,"Excavation Ground",new Vector3(0,-0.65f,20),new Vector3(24,0.5f,40),ground,PresentationAssets.Cube);
                foreach(int side in new[]{-1,1})
                {
                    foreach(float z in new[]{5f,18,32})
                    {
                        var rock=PresentationAssets.Part(root.transform,"Quarry Rock Formation",new Vector3(side*7.2f,1.1f,z),new Vector3(3,3.3f+(z%3)*0.3f,5),Rock);rock.transform.localRotation=Quaternion.Euler(0,0,side*7);
                        PresentationAssets.Part(root.transform,"Reinforced Shoulder",new Vector3(side*3.8f,-0.1f,z),new Vector3(0.6f,0.5f,3),Steel);
                        PresentationAssets.Part(root.transform,"Amber Work Lamp",new Vector3(side*4.2f,0.9f,z),new Vector3(0.28f,0.24f,0.34f),Amber);
                    }
                    PresentationAssets.Part(root.transform,"Conveyor Structure",new Vector3(side*5.1f,0.7f,13),new Vector3(1.1f,0.45f,12),Steel);
                    for(int i=0;i<6;i++)PresentationAssets.Part(root.transform,"Conveyor Roller",new Vector3(side*5.1f,0.98f,8+i*1.8f),new Vector3(1.14f,0.13f,0.22f),Iron);
                    PresentationAssets.Part(root.transform,"Excavator Motor",new Vector3(side*5.2f,1.5f,28),new Vector3(1.7f,2,2.5f),Amber);
                    PresentationAssets.Part(root.transform,"Excavator Tower",new Vector3(side*5.1f,3,29),new Vector3(0.45f,5,0.45f),Steel);
                    PresentationAssets.Part(root.transform,"Hoist Beam",new Vector3(side*5.1f,5.2f,26.8f),new Vector3(0.6f,0.55f,5),Steel);
                }
                PresentationWorld.Combine(root.transform,"M9QuarryEnvironment");PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static TrackSection Section(string id,params Place[] placements)
        {
            string path=Prefabs+"/"+id+".prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing.GetComponent<TrackSection>();var root=new GameObject(id);
            try
            {
                var surface=PresentationAssets.Part(root.transform,"Surface",new Vector3(0,-0.25f,20),new Vector3(6,0.5f,40),PresentationAssets.Material("M9QuarryDeck",new Color(0.36f,0.43f,0.49f),0.18f),PresentationAssets.Cube);surface.AddComponent<BoxCollider>();
                foreach(int side in new[]{-1,1})PresentationAssets.Part(root.transform,"Boundary",new Vector3(side*2.97f,0.012f,20),new Vector3(0.06f,0.012f,40),Amber,PresentationAssets.Cube);
                var dressing=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/QuarryEnvironment.prefab"),root.transform);dressing.transform.localPosition=Vector3.zero;
                var targets=new List<DrillObstacle>();foreach(var p in placements)
                {
                    var ob=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Obstacle"+p.Material+".prefab"),root.transform);ob.transform.localPosition=new Vector3(p.X,1,p.Z);var size=ob.transform.localScale;size.x=p.Width;ob.transform.localScale=size;targets.Add(ob.GetComponent<DrillObstacle>());
                }
                var entrance=new GameObject("Entrance").transform;entrance.SetParent(root.transform,false);var exit=new GameObject("Exit").transform;exit.SetParent(root.transform,false);exit.localPosition=Vector3.forward*40;
                var section=root.AddComponent<TrackSection>();var data=new SerializedObject(section);data.FindProperty("sectionId").stringValue=id;data.FindProperty("surface").objectReferenceValue=surface.GetComponent<BoxCollider>();data.FindProperty("entrance").objectReferenceValue=entrance;data.FindProperty("exit").objectReferenceValue=exit;Set(data.FindProperty("obstacles"),targets.ToArray());data.ApplyModifiedProperties();return PrefabUtility.SaveAsPrefabAsset(root,path).GetComponent<TrackSection>();
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static EncounterDefinition Pattern(string name,EncounterCategory category,EncounterArrangement arrangement,int pressure,int minimum,float bypass,params Place[] places)
        {
            var d=ScriptableObject.CreateInstance<EncounterDefinition>();d.Section=Section(name,places);d.Category=category;d.Arrangement=arrangement;d.Pressure=pressure;d.MinimumOrdinal=minimum;d.EarlyWeight=minimum==0?3:1;d.Weight=3;d.LateWeight=3;d.BypassX=bypass;d.Intent="Quarry "+category+": optional industrial mining, an independent full-footprint bypass and early Iron decisions.";AssetDatabase.CreateAsset(d,Data+"/"+name+".asset");return d;
        }
        private static void BuildUi(WorkshopController flow)
        {
            var panel=flow.View.Panel.transform;var hud=Object.FindFirstObjectByType<PrototypeHud>();var sprite=PresentationAssets.Sprite("M6Rounded");
            var display=panel.Find("Pocket Drill Presentation").GetComponent<RectTransform>();Top(display,202,492,94);Top(flow.View.FormName.rectTransform,3,480,25);flow.View.FormName.fontSize=17;Top(flow.View.FormPreview.rectTransform,28,240,60);
            Top(flow.View.ResourceSummary.rectTransform,76,480,18);flow.View.ResourceSummary.fontSize=13;
            var selector=Button(panel,"M9 Region Selector",145,492,48,sprite);selector.GetComponentInChildren<Text>().fontSize=17;
            var drawer=Rect(panel,"M9 Region Choices",198,502,340);var background=drawer.gameObject.AddComponent<Image>();background.sprite=sprite;background.type=Image.Type.Sliced;background.color=new Color(0.86f,0.92f,0.93f);drawer.SetAsLastSibling();
            var buttons=new Button[4];var labels=new Text[4];for(int i=0;i<4;i++){buttons[i]=Button(drawer,"Region "+i,8+i*82,484,76,sprite);labels[i]=buttons[i].GetComponentInChildren<Text>();labels[i].fontSize=16;}
            var marker=Label(hud.transform,"M9 Region Marker",65,492,18,13);marker.color=new Color(0.24f,0.35f,0.39f);
            var view=flow.gameObject.AddComponent<RegionWorkshopView>();var data=new SerializedObject(view);data.FindProperty("flow").objectReferenceValue=flow;data.FindProperty("selector").objectReferenceValue=selector;data.FindProperty("selected").objectReferenceValue=selector.GetComponentInChildren<Text>();data.FindProperty("marker").objectReferenceValue=marker;data.FindProperty("drawer").objectReferenceValue=drawer.gameObject;Set(data.FindProperty("choices"),buttons);Set(data.FindProperty("labels"),labels);data.ApplyModifiedProperties();drawer.gameObject.SetActive(false);
        }
        private static Button Button(Transform parent,string name,float top,float width,float height,Sprite sprite)
        {
            var r=Rect(parent,name,top,width,height);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();var colors=b.colors;colors.normalColor=new Color(0.025f,0.47f,0.5f);colors.highlightedColor=new Color(0.06f,0.58f,0.6f);colors.disabledColor=new Color(0.55f,0.64f,0.66f);b.colors=colors;b.targetGraphic=image;var text=Label(r,"Label",0,width-12,height,17);text.color=Color.white;return b;
        }
        private static Text Label(Transform parent,string name,float top,float width,float height,int size){var r=Rect(parent,name,top,width,height);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private static RectTransform Rect(Transform p,string name,float top,float width,float height){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);Top(r,top,width,height);return r;}
        private static void Top(RectTransform r,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0.5f,1);r.pivot=new Vector2(0.5f,1);r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(w,h);}
        private static void Set<T>(SerializedProperty p,T[] values) where T:Object {p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
        private static void Folder(string parent,string name){if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);}
    }
}
