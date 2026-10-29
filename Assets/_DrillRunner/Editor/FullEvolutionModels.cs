using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class FullEvolutionModels
    {
        public static GameObject Create(int stage)
        {
            var path = FoundationSetup.Root + "/Prefabs/Visual" + FullEvolutionSetup.Names[stage - 3].Replace(" ", "") + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            PresentationAssets.RequireNew(path); var root = new GameObject(FullEvolutionSetup.Names[stage - 3] + " Visual");
            try
            {
                var colors = new[] {new Color(0.24f,0.4f,0.52f),new Color(0.63f,0.12f,0.12f),new Color(0.95f,0.65f,0.08f),new Color(0.35f,0.22f,0.59f),new Color(0.84f,0.9f,0.88f),new Color(0.12f,0.2f,0.42f)};
                var paint = PresentationAssets.Material("M8Hull" + stage, colors[stage - 3],0.25f);
                var accent = stage <= 5 ? PresentationAssets.Copper : PresentationAssets.Material("M8Ion",new Color(0.12f,0.8f,0.91f),0.3f);
                if (stage >= 6 && !accent.IsKeywordEnabled("_EMISSION"))
                { accent.EnableKeyword("_EMISSION"); accent.SetColor("_EmissionColor", new Color(0.03f,0.28f,0.34f)); EditorUtility.SetDirty(accent); AssetDatabase.SaveAssetIfDirty(accent); }
                var body = root.transform; var housing = new GameObject("Motor Housing").transform; housing.SetParent(body,false);
                var rotor = new GameObject("Drill Rotor").transform; rotor.SetParent(body,false); rotor.localPosition = new Vector3(0,0.94f,0.02f);
                Part(body,"Lower armored keel",0,0.3f,-0.18f,1.44f,0.18f,1.9f,PresentationAssets.Dark);
                foreach (var side in new[] {-1,1})
                {
                    Part(body,"Continuous tread",side*0.64f,0.25f,-0.17f,0.32f,0.46f,1.94f,PresentationAssets.Rubber);
                    for(var j=0;j<6;j++) Part(body,"Tread cleat",side*0.64f,0.47f,-0.96f+j*0.31f,0.3f,0.055f,0.09f,PresentationAssets.Dark);
                    Part(body,"Lower sill",side*0.66f,0.57f,-0.13f,0.19f,0.18f,1.64f,paint);
                    Part(body,"Nose bracket",side*0.52f,0.69f,0.29f,0.17f,0.28f,0.59f,PresentationAssets.Metal);
                }
                Part(body,"Beveled central hull",0,0.53f,-0.31f,stage==4?1.5f:1.36f,0.42f,1.48f,paint);
                if (stage == 3)
                {
                    Part(housing,"Industrial inline engine",0,0.89f,-0.58f,1.05f,0.48f,0.98f,paint);
                    Part(housing,"Power transmission cover",0,1.15f,-0.57f,0.85f,0.07f,0.76f,PresentationAssets.Copper);
                    for(var j=0;j<4;j++) Part(housing,"Engine cooling rib",0,0.94f,-0.93f+j*0.2f,1.12f,0.06f,0.06f,PresentationAssets.Metal);
                    foreach(var side in new[]{-1,1}) Part(body,"Drive spindle",side*0.52f,0.78f,-0.01f,0.22f,0.18f,0.55f,PresentationAssets.Copper);
                }
                else if(stage == 4)
                {
                    Part(housing,"Heavy flattened engine",0,0.88f,-0.59f,1.34f,0.42f,1.03f,paint);
                    Part(housing,"Broad steel spine",0,1.12f,-0.55f,0.55f,0.11f,1.01f,PresentationAssets.Metal);
                    foreach(var side in new[]{-1,1}) { var p=Part(body,"Borer shoulder",side*0.52f,0.77f,0.03f,0.4f,0.35f,0.69f,paint); p.localRotation=Quaternion.Euler(0,side*8,0); }
                    Part(housing,"Borer index plate",0,1.19f,-0.74f,0.35f,0.04f,0.29f,PresentationAssets.Cream);
                }
                else if(stage == 5)
                {
                    Part(housing,"Hydraulic powerpack",0,0.91f,-0.64f,0.88f,0.49f,0.87f,PresentationAssets.Dark);
                    Part(housing,"Hydraulic top shield",0,1.18f,-0.64f,0.96f,0.09f,0.81f,paint);
                    foreach(var side in new[]{-1,1})
                    {
                        Shaft(body,"Hydraulic pressure barrel",side*0.55f,0.87f,-0.37f,0.23f,0.69f,paint);
                        Shaft(body,"Polished piston rod",side*0.55f,0.87f,0.18f,0.1f,0.56f,PresentationAssets.BitSteel);
                        Part(body,"Piston fork",side*0.55f,0.87f,0.46f,0.23f,0.25f,0.12f,PresentationAssets.Copper);
                    }
                }
                else if(stage == 6)
                {
                    Part(housing,"Crystal cradle",0,0.88f,-0.53f,0.92f,0.31f,0.95f,paint);
                    var crystal=PresentationAssets.Part(housing,"Faceted crystal core",new Vector3(0,1.17f,-0.56f),new Vector3(0.31f,0.32f,0.31f),accent,Crystal());
                    crystal.transform.localRotation=Quaternion.Euler(0,45,0);
                    foreach(var side in new[]{-1,1}) Part(housing,"Ivory containment arch",side*0.39f,1.11f,-0.54f,0.12f,0.45f,0.82f,PresentationAssets.Cream);
                    Part(body,"Crystal conductor",0,0.84f,0.09f,0.26f,0.14f,0.53f,accent);
                }
                else if(stage == 7)
                {
                    Part(housing,"Fusion containment vessel",0,0.94f,-0.59f,1.15f,0.45f,0.96f,paint);
                    Ring(housing,"Fusion core halo",new Vector3(0,1.2f,-0.57f),0.82f,accent,Quaternion.Euler(90,0,0));
                    Part(housing,"Fusion core nucleus",0,1.18f,-0.57f,0.33f,0.15f,0.33f,PresentationAssets.Copper);
                    foreach(var side in new[]{-1,1}) Part(body,"Ceramic shoulder",side*0.49f,0.82f,-0.04f,0.32f,0.29f,0.73f,paint);
                }
                else
                {
                    Part(housing,"Quantum split shell",0,0.91f,-0.65f,0.94f,0.35f,0.83f,paint);
                    foreach(var side in new[]{-1,1})
                    {
                        Part(body,"Swept quantum outrigger",side*0.57f,0.81f,-0.08f,0.23f,0.29f,1.12f,paint);
                        Part(body,"Gold conductive rail",side*0.57f,0.99f,-0.08f,0.09f,0.065f,0.93f,PresentationAssets.Copper);
                    }
                    Ring(housing,"Quantum gimbal outer",new Vector3(0,1.2f,-0.61f),0.7f,PresentationAssets.Copper,Quaternion.Euler(65,0,0));
                    Ring(housing,"Quantum gimbal inner",new Vector3(0,1.2f,-0.61f),0.59f,accent,Quaternion.Euler(115,35,0));
                    PresentationAssets.Part(housing,"Quantum diamond",new Vector3(0,1.2f,-0.61f),new Vector3(0.18f,0.25f,0.18f),accent,Crystal());
                }
                var helix=AssetDatabase.LoadAssetAtPath<Mesh>(FoundationSetup.Root+"/Models/M6HelicalBit.asset");
                var flute=AssetDatabase.LoadAssetAtPath<Mesh>(FoundationSetup.Root+"/Models/M6BFluteStripes.asset");
                var diameter = stage == 4 ? 1.18f : stage == 8 ? 1.1f : 1.04f;
                PresentationAssets.Part(rotor,"Precision fluted cutter",Vector3.zero,new Vector3(diameter,diameter,1.81f),PresentationAssets.BitSteel,helix);
                PresentationAssets.Part(rotor,"Cutting flutes",Vector3.zero,new Vector3(diameter,diameter,1.81f),stage>=6?accent:PresentationAssets.Dark,flute);
                Ring(rotor,"Open cutting drive collar",new Vector3(0,0,0.1f),diameter,stage>=6?accent:PresentationAssets.Copper,Quaternion.identity);
                for(var j=0;j<(stage==4?6:3);j++)
                {
                    var a=j*Mathf.PI*2/(stage==4?6:3); Part(rotor,"Cutting index",Mathf.Cos(a)*0.36f,Mathf.Sin(a)*0.36f,0.37f,0.1f,0.1f,0.28f,stage>=6?accent:PresentationAssets.Copper);
                }
                // Batch separate animation mounts independently; no runtime meshes/material instances.
                PresentationWorld.Combine(housing,"M8Housing"+stage); PresentationWorld.Combine(rotor,"M8Rotor"+stage);
                // Body batching must exclude the two animated subtrees.
                housing.SetParent(null,true); rotor.SetParent(null,true); PresentationWorld.Combine(body,"M8Body"+stage);
                housing.SetParent(body,true); rotor.SetParent(body,true);
                EvolutionSetup.ValidateVisual(root); return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static Transform Part(Transform parent,string name,float x,float y,float z,float sx,float sy,float sz,Material material)
            => PresentationAssets.Part(parent,name,new Vector3(x,y,z),new Vector3(sx,sy,sz),material).transform;
        private static void Shaft(Transform parent,string name,float x,float y,float z,float diameter,float length,Material material)
        {
            var p=GameObject.CreatePrimitive(PrimitiveType.Cylinder); var mesh=p.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(p);
            var part=PresentationAssets.Part(parent,name,new Vector3(x,y,z),new Vector3(diameter,length/2,diameter),material,mesh);
            part.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        private static void Ring(Transform parent,string name,Vector3 position,float diameter,Material material,Quaternion rotation)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(FoundationSetup.Root+"/Models/M6BDriveRing.asset");
            var part=PresentationAssets.Part(parent,name,position,new Vector3(diameter,diameter,0.06f),material,mesh); part.transform.localRotation=rotation;
        }
        private static Mesh Crystal() => PresentationAssets.MeshAsset("M8Crystal",()=>
        {
            var points=new[] {Vector3.up,Vector3.right,Vector3.forward,Vector3.left,Vector3.back,Vector3.down};
            var ids=new[] {0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
            var vertices=new Vector3[ids.Length]; var triangles=new int[ids.Length]; for(var i=0;i<ids.Length;i++){vertices[i]=points[ids[i]];triangles[i]=i;}
            var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        });
    }
}
