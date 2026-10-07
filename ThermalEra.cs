using Il2CppInterop.Runtime;
using Sprocket.PartImporting;
using Sprocket.TechTrees;
using Sprocket.Vehicles;
using Sprocket.Vehicles.PartImporting;

namespace SprocketThermalSight;

// Use the active native database, so JSON edits follow the same load/restart lifecycle
// as vanilla selection. Placed-object IDs are never treated as asset GUIDs.
internal static class ThermalEraAccess
{
    private static IntPtr database;
    private static readonly Dictionary<string,PartDefinition?> definitions=new(StringComparer.Ordinal);
    private static readonly HashSet<string> reported=new(StringComparer.Ordinal);
    private static PartDefinition? Definition(VehicleComponent component)
    {
        var source=IVehiclePartInfoSource.Instance?.TryCast<PartInfoSource>();
        var db=source?.partDatabase;if(db==null)return null;
        if(database!=db.Pointer){database=db.Pointer;definitions.Clear();}
        var id=component.ComponentID;
        if(definitions.TryGetValue(id,out var known))return known;
        PartDefinition? found=null;
        foreach(var part in db.partDefinitions)
        {
            if(part?.components==null)continue;
            if(!part.components.Any(c=>c!=null && c.fileID==id))continue;
            if(found!=null){Runtime.Warn("Ambiguous native Part definition for "+id+"; availability disabled.");definitions[id]=null;return null;}
            found=part;
        }
        // Do not cache absence while the native database is still loading.
        if(found!=null)
        {
            definitions[id]=found;
            if(reported.Count<12 && reported.Add(id))
                Plugin.Instance.Log.LogInfo($"[Thermal native date] component={id}; part={found.guid}; invention={found.creationDate}");
        }
        return found;
    }
    internal static bool Allowed(VehicleComponent? component)
    {
        try
        {
            if(component==null)return false;
            var design=component.Vehicle?.DesignInfo;var eras=VehicleClassifications.eras;
            if(design==null || eras==null || eras.Length==0)return false;
            var part=Definition(component);if(part==null || !part.creationDate.Valid || !design.Date.Valid)return false;
            // Let vanilla (plus its exact final-boundary repair) classify context.
            int index=VehicleClassifications.GetEraIndex(design.Date);
            if(index<0 || index>=eras.Length)return false;
            var previous=TechDate.MinValue;
            for(int i=0;i<eras.Length;i++)
            {
                if(eras[i]==null)return false;
                var d=eras[i].StartDate;
                if(!d.Valid || d==TechDate.MaxValue || (i>0 && TechDate.Compare(d,previous)<=0))return false;
                previous=d;
            }
            var date=design.Date;var invention=part.creationDate;
            // Native comparison accepts year-zero/default Part dates and retains
            // the open-ended final-era sentinel without inventing a finite horizon.
            return TechDate.Compare(date,new TechDate(invention.Year,invention.Month,invention.Day))>=0;
        }
        catch(Exception ex){Runtime.Warn("Native Part availability unavailable; normal sight retained: "+ex.Message);return false;}
    }
}
