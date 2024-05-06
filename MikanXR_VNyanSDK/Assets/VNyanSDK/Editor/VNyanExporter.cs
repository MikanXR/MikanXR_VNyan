using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class VNyanExporter : MonoBehaviour
{
    [MenuItem("VNyan SDK/Export Throwable")]
    public static void ExportThrowable()
    {
        GameObject obj = Selection.activeGameObject;

        if(obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        if(obj.GetComponent<ThrowableObject>() == null)
        {
            EditorUtility.DisplayDialog("Export", "ThrowableObject-script needs to be added to the item", "OK");
            return;
        }

        if (obj.GetComponent<Rigidbody>() == null)
        {
            EditorUtility.DisplayDialog("Export", "Throwable item needs a Rigidbody", "OK");
            return;
        }

        var particle = obj.GetComponent<ThrowableObject>().collisionParticlePrefab;

        if(particle != null)
        {
            if (particle.GetComponent<ParticleObject>() == null)
            {
                EditorUtility.DisplayDialog("Export", "Particle prefab needs ParticleObject-component", "OK");
                return;
            }
        }

        string p = EditorUtility.SaveFilePanel("Export Throwable Item", ".", obj.name, "vnthrow");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);

        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanItem" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch(Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch(Exception) 
            { 
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "Throwable item exported!", "OK");
    }

    
    [MenuItem("VNyan SDK/Export Droppable")]
    public static void ExportDroppable()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        if (obj.GetComponent<DroppableObject>() == null)
        {
            EditorUtility.DisplayDialog("Export", "DroppableObject-script needs to be added to the item", "OK");
            return;
        }

        if (obj.GetComponent<Rigidbody>() == null)
        {
            EditorUtility.DisplayDialog("Export", "Droppable item needs a Rigidbody", "OK");
            return;
        }

        var particle = obj.GetComponent<DroppableObject>().collisionParticlePrefab;

        if (particle != null)
        {
            if (particle.GetComponent<ParticleObject>() == null)
            {
                EditorUtility.DisplayDialog("Export", "Particle prefab needs ParticleObject-component", "OK");
                return;
            }
        }

        string p = EditorUtility.SaveFilePanel("Export Droppable Item", ".", obj.name, "vndrop");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);

        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanItem" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch (Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch (Exception)
            {
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "Droppable item exported!", "OK");

    }

    
    [MenuItem("VNyan SDK/Export Edible")]
    public static void ExportEdible()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        if (obj.GetComponent<EdibleItem>() == null)
        {
            EditorUtility.DisplayDialog("Export", "EdibleItem-script needs to be added to the item", "OK");
            return;
        }

        var particle = obj.GetComponent<EdibleItem>().eatParticle;

        if (particle != null)
        {
            if (particle.GetComponent<ParticleObject>() == null)
            {
                EditorUtility.DisplayDialog("Export", "Particle prefab needs ParticleObject-component", "OK");
                return;
            }
        }

        string p = EditorUtility.SaveFilePanel("Export Edible Item", ".", obj.name, "vnnom");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);


        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanItem" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch (Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch (Exception)
            {
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "Edible item exported!", "OK");



    }
    
    [MenuItem("VNyan SDK/Export World")]
    public static void ExportWorld()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        if (obj.GetComponent<World>() == null)
        {
            EditorUtility.DisplayDialog("Export", "World-script needs to be added to the item", "OK");
            return;
        }

        string p = EditorUtility.SaveFilePanel("Export World", ".", obj.name, "vnworld");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);


        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            // we got lightmaps in the scene
            WorldLightmapSettings ls = obj.GetComponent<WorldLightmapSettings>();
            if (ls == null)
            {
                ls = obj.AddComponent<WorldLightmapSettings>();
            }

            ls.ambientMode = RenderSettings.ambientMode;
            ls.ambientColor = RenderSettings.ambientLight;
            ls.ambientGround = RenderSettings.ambientGroundColor;
            ls.ambientEquator = RenderSettings.ambientEquatorColor;
            ls.ambientSkyColor = RenderSettings.ambientSkyColor;
            ls.ambientProbe = RenderSettings.ambientProbe;
            ls.ambientSkybox = RenderSettings.skybox;
            ls.ambientIntensity = RenderSettings.ambientIntensity;

            if (LightmapSettings.lightmaps.Length > 0)
            {
                ls.lightMap = new List<Texture2D>();
                ls.lightMapDir = new List<Texture2D>();
                ls.lightMapShadow = new List<Texture2D>();
                ls.lightmapIndex = new List<MeshLightmapIndex>();
                ls.lightmapsMode = LightmapSettings.lightmapsMode;

                foreach(var l in LightmapSettings.lightmaps)
                {
                    ls.lightMap.Add(l.lightmapColor);
                    ls.lightMapDir.Add(l.lightmapDir);
                    ls.lightMapShadow.Add(l.shadowMask);
                }

                MeshRenderer rootR = obj.GetComponent<MeshRenderer>();
                if(rootR != null)
                {
                    ls.lightmapIndex.Add(new MeshLightmapIndex() { 
                        renderer = rootR, 
                        lightmapIndex = rootR.lightmapIndex,
                        offsetScale = rootR.lightmapScaleOffset,
                        stitchSeams = rootR.stitchLightmapSeams
                    });
                }

                MeshRenderer[] rend = obj.GetComponentsInChildren<MeshRenderer>();
                foreach(var r in rend)
                {
                    
                    ls.lightmapIndex.Add(new MeshLightmapIndex() { 
                        renderer = r, 
                        lightmapIndex = r.lightmapIndex,
                        offsetScale = r.lightmapScaleOffset,
                        stitchSeams = r.stitchLightmapSeams
                    });
                }

            }


            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanWorld" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch (Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch (Exception)
            {
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "World exported!", "OK");


    }


    [MenuItem("VNyan SDK/Export Prop")]
    public static void ExportProp()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        string p = EditorUtility.SaveFilePanel("Export Prop", ".", obj.name, "vnprop");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);


        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanProp" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch (Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch (Exception)
            {
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "Prop exported!", "OK");


    }

    

    [MenuItem("VNyan SDK/Export Custom Object")]
    public static void ExportCustom()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        /*
        if (obj.GetComponent<CustomObject>() == null)
        {
            EditorUtility.DisplayDialog("Export", "CustomObject-script needs to be added to the item", "OK");
            return;
        }*/


        string p = EditorUtility.SaveFilePanel("Export Custom Object", ".", obj.name, "vnobj");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);

        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanItem" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch (Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch (Exception)
            {
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "Custom Object exported!", "OK");

    }

    [MenuItem("VNyan SDK/Export Injected Anim")]
    public static void ExportAnimation()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select the object to export in scene hierarchy", "OK");
            return;
        }

        if (obj.GetComponent<InjectedAnim>() == null &&
            obj.GetComponentsInChildren<InjectedAnim>().Length == 0)
        {
            EditorUtility.DisplayDialog("Export", "At least one InjectedAnim-script needs to be added to the item", "OK");
            return;
        }


        string p = EditorUtility.SaveFilePanel("Export Injected Anim", ".", obj.name, "vnanim");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);

        string prefabPath = "Assets/VNyanTemp.prefab";
        bool done = false;

        try
        {
            // Remove temporary prefab if exists
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(prefabPath))
                File.Delete(prefabPath);

            bool s = false;
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath, out s);
            if (!s)
            {
                Debug.Log("Error creating prefab");
                return;
            }

            AssetBundleBuild bundleBuild = new AssetBundleBuild();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundleBuild.assetBundleName = f;
            bundleBuild.assetNames = new string[] { prefabPath };
            bundleBuild.addressableNames = new string[] { "VNyanItem" };

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ForceRebuildAssetBundle |
                BuildAssetBundleOptions.DeterministicAssetBundle |
                BuildAssetBundleOptions.StrictMode;

            if (obj.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true).Length > 0)
            {
                options = options | BuildAssetBundleOptions.UncompressedAssetBundle;
            }

            BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, new AssetBundleBuild[] { bundleBuild }, options, BuildTarget.StandaloneWindows);


            File.Move(Application.temporaryCachePath + "/" + f, p);
            done = true;
            // Remove temporary prefab
            AssetDatabase.DeleteAsset(prefabPath);
            if (File.Exists(f))
                File.Delete(f);
        }
        catch (Exception)
        {
            try
            {
                // Clean up afterwards
                AssetDatabase.DeleteAsset(prefabPath);
                if (File.Exists(prefabPath))
                    File.Delete(prefabPath);
            }
            catch (Exception)
            {
            }
        }

        if (!done)
            EditorUtility.DisplayDialog("Export", "Export failed!.", "OK");
        else
            EditorUtility.DisplayDialog("Export", "Injected Anim exported!", "OK");

    }

    [MenuItem("VNyan SDK/Export Bone Rotations as JSON")]
    public static void ExportPose()
    {
        GameObject obj = Selection.activeGameObject;
        Animator anim = null;

        //anim.avatar.humanDescription.

        if (obj != null)
            anim = obj.GetComponent<Animator>();

        if (anim == null)
        {
            EditorUtility.DisplayDialog("Export", "Please select a posed humanoid avatar that has Animator", "OK");
            return;
        }

        /*
        if(obj.GetComponent<VRMMeta>() == null)
        {

        }*/



        string p = EditorUtility.SaveFilePanel("Export Pose", ".", obj.name, "json");
        if (string.IsNullOrWhiteSpace(p))
            return;

        string f = Path.GetFileName(p);

        if (File.Exists(p))
            File.Delete(p);


        Dictionary<int, PoseData> bones = new Dictionary<int, PoseData>();

        for(int i=0;i<(int)HumanBodyBones.LastBone;i++)
        {
            var bone = anim.GetBoneTransform((HumanBodyBones)i);
            if (bone != null)
                bones.Add(i, new PoseData()
                {
                    rot = bone.localRotation,
                    pos = bone.localPosition
                });
        }

        // add root with id -1
        var hip = anim.GetBoneTransform(HumanBodyBones.Hips);
        if(hip != null)
        {
            bones.Add(-1, new PoseData()
            {
                rot = hip.parent.localRotation,
                pos = hip.parent.localPosition
            });
        }

        var s = DictionaryToJson(bones);

        File.WriteAllText(p, s);

        EditorUtility.DisplayDialog("Export", "Pose Exported as json", "OK");

    }

    private static string DictionaryToJson(Dictionary<int, PoseData> dict)
    {
        var entries = dict.Select(d =>
            "{ \"bone\": "+ d.Key +", " +          
            "\"x\": " + d.Value.rot.x.ToString(CultureInfo.InvariantCulture) + ", " +
            "\"y\": " + d.Value.rot.y.ToString(CultureInfo.InvariantCulture) + ", " +
            "\"z\": " + d.Value.rot.z.ToString(CultureInfo.InvariantCulture) + ", " +
            "\"w\": " + d.Value.rot.w.ToString(CultureInfo.InvariantCulture) + ", " +
            "\"posx\": " + d.Value.pos.x.ToString(CultureInfo.InvariantCulture) + ", " +
            "\"posy\": " + d.Value.pos.y.ToString(CultureInfo.InvariantCulture) + ", " +
            "\"posz\": " + d.Value.pos.z.ToString(CultureInfo.InvariantCulture) + " " +
            " }");
        return "{ \"data\": [" + string.Join(",", entries) + "] }";
    }
}

public class PoseData
{
    public Quaternion rot { get; set; }
    public Vector3 pos { get; set; }
}