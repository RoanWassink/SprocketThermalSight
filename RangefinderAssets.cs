using System.Globalization;
using HarmonyLib;
using Sprocket.Vehicles;
using Sprocket.PartImporting;
using UnityEngine;

namespace SprocketThermalSight;

internal static class RangefinderAssets
{
    internal const string Guid = "58b0d970-8065-48ea-9af6-6010948b9f46";
    private static readonly Dictionary<RangefinderKind, Mesh> meshes = new();
    private static readonly List<Texture2D> icons = new();
    private static readonly Dictionary<string, Sprite> sprites = new();
    private static bool modelFailed;
    private static readonly HashSet<string> iconFailed = new();
    private static float scanAt;
    private static string PathFor(string name) => Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "assets", name);
    private static Mesh ReadMesh(RangefinderKind kind)
    {
        var positions = new List<Vector3>();
        var vertices = new List<Vector3>();
        foreach (var line in File.ReadLines(PathFor(kind == RangefinderKind.Optical ? "optical-rangefinder.obj" : kind == RangefinderKind.AutomaticLaser ? "automatic-lrf.obj" : "lrf-concept.obj")))
        {
            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 4 && fields[0] == "v")
                positions.Add(new Vector3(float.Parse(fields[1], CultureInfo.InvariantCulture), float.Parse(fields[2], CultureInfo.InvariantCulture), float.Parse(fields[3], CultureInfo.InvariantCulture)));
            else if (fields.Length == 4 && fields[0] == "f")
                for (int i = 1; i <= 3; i++)
                {
                    int index = int.Parse(fields[i].Split('/')[0], CultureInfo.InvariantCulture) - 1;
                    if (index < 0 || index >= positions.Count) throw new InvalidDataException("Invalid LRF vertex index.");
                    vertices.Add(positions[index]);
                }
        }
        if (positions.Count < 3 || positions.Count > 20000 || vertices.Count < 3 || vertices.Count > 60000 || vertices.Count % 3 != 0 || vertices.Any(v => !float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z)))
            throw new InvalidDataException("Unexpected LRF blockout geometry.");
        var created = new Mesh { name = "Laser rangefinder blockout", hideFlags = HideFlags.HideAndDontSave };
        created.vertices = vertices.ToArray();
        created.triangles = Enumerable.Range(0, vertices.Count).ToArray();
        created.RecalculateNormals(); created.RecalculateBounds();
        return created;
    }
    internal static void Tick()
    {
        if (modelFailed || Time.unscaledTime < scanAt) return;
        scanAt = Time.unscaledTime + .5f;
        try
        {
            foreach (var vehicle in UnityEngine.Object.FindObjectsOfType<Vehicle>())
            {
                if (vehicle == null || vehicle.Released) continue;
                var objects = vehicle.ObjectReader.Items;
                int count = objects.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleObject>>().Count;
                for (int i = 0; i < count; i++)
                {
                    var components = objects[i]?.Components;
                    if (components == null) continue;
                    int n = components.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<VehicleComponent>>().Count;
                    for (int j = 0; j < n; j++)
                    {
                        var component = components[j];
                        if (!Rangefinder.Own(component) || component.models == null) continue;
                        foreach (var group in component.models)
                        {
                            if (group?.lods == null) continue;
                            foreach (var lod in group.lods)
                            {
                                var renderer = lod?.Renderer;
                                var filter = renderer?.GetComponent<MeshFilter>();
                                if (filter == null) continue;
                                var kind = RangefinderDevicePolicy.Kind(component.ComponentID);
                                if (!meshes.TryGetValue(kind, out var mesh)) { mesh = ReadMesh(kind); meshes.Add(kind, mesh); }
                                if (filter.sharedMesh != mesh)
                                {
                                    group.SetMesh(mesh);
                                    group.SetInteractionMesh(mesh);
                                    group.SetCollisionMesh(mesh);
                                }
                                if(kind==RangefinderKind.AutomaticLaser)
                                    LensAssets.IntegratedWindow(renderer!.transform,"Integrated rangefinder glass",true,
                                        new Vector3(0,.1228f,.101f),new Vector3(.666667f,.657778f,1),false);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) { modelFailed = true; Plugin.Instance.Log.LogWarning("[Rangefinder] Optional device model disabled: " + ex.Message); }
    }
    private static void Card(PartDefinition __0, PartDisplayCard __result)
    {
        if (__0 == null || __result == null || iconFailed.Contains(__0.guid)) return;
        var path = __0.guid switch
        {
            Guid => "lrf-icon-concept.png",
            "af74a44d-65e0-44ae-8ac3-61198eeb1b30" => "automatic-lrf-icon.png",
            "b93efc74-6702-4d2e-9211-dfe313ffdab3" => "optical-rangefinder-icon.png",
            _ => null
        };
        if (path == null) return;
        try
        {
            if (!sprites.TryGetValue(__0.guid, out var sprite))
            {
                var icon = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                icons.Add(icon);
                if (!ImageConversion.LoadImage(icon, File.ReadAllBytes(PathFor(path)), false)) throw new InvalidDataException("Rangefinder icon decode failed.");
                sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), new Vector2(.5f, .5f), 100);
                sprite.hideFlags = HideFlags.HideAndDontSave; sprites.Add(__0.guid,sprite);
            }
            __result.Icon = sprite;
        }
        catch (Exception ex) { iconFailed.Add(__0.guid); Plugin.Instance.Log.LogWarning("[Rangefinder] Optional icon disabled: " + ex.Message); }
    }
    internal static void Shutdown()
    {
        // Bounded three meshes remain alive while native parts still reference them.
        foreach (var sprite in sprites.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
        foreach (var icon in icons) if (icon != null) UnityEngine.Object.Destroy(icon);
        sprites.Clear(); icons.Clear();
    }
}
