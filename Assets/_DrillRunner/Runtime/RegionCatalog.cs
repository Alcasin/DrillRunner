using System;
using UnityEngine;
namespace DrillRunner
{
    [CreateAssetMenu(menuName="DrillRunner/Regions")]
    public sealed class RegionCatalog : ScriptableObject
    {
        public RegionDefinition[] Regions = Array.Empty<RegionDefinition>();
        public RegionDefinition Find(string id) { foreach(var r in Regions) if(r != null && r.Id == id) return r; throw new ArgumentException("Unknown region reference."); }
        public void Validate()
        {
            if(Regions == null || Regions.Length != 4) throw new ArgumentException("Four region definitions required.");
            var ids=new System.Collections.Generic.HashSet<string>(); foreach(var r in Regions){if(r==null || !ids.Add(r.Id))throw new ArgumentException("Missing/duplicate region.");r.Validate();}
        }
    }
}
