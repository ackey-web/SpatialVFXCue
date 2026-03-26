#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// 外部 VFX Prefab を SpatialVFXCue 対応に変換するヘルパー。
    /// 選択した Prefab に SpatialNetworkObject + VFXCueEffect を追加し、
    /// PlayOnAwake をオフにする。
    /// </summary>
    public static class VFXCueImportHelper
    {
        [MenuItem("SpatialVFXCue/Convert Selected Prefabs for VFXCue")]
        public static void ConvertSelectedPrefabs()
        {
            Object[] selected = Selection.objects;
            int count = 0;

            foreach (Object obj in selected)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // Prefab を編集モードで開く
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) continue;

                bool modified = false;

                // SpatialNetworkObject を追加
                if (instance.GetComponent<SpatialNetworkObject>() == null)
                {
                    instance.AddComponent<SpatialNetworkObject>();
                    modified = true;
                }

                // VFXCueEffect を追加
                if (instance.GetComponent<VFXCueEffect>() == null)
                {
                    instance.AddComponent<VFXCueEffect>();
                    modified = true;
                }

                // 全 ParticleSystem の PlayOnAwake をオフに
                ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem ps in systems)
                {
                    var main = ps.main;
                    if (main.playOnAwake)
                    {
                        main.playOnAwake = false;
                        modified = true;
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    count++;
                    Debug.Log($"[SpatialVFXCue] '{prefab.name}' を VFXCue 対応に変換しました");
                }

                Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "VFXCue Convert",
                $"{count} 個の Prefab を VFXCue 対応に変換しました。\n\n" +
                "VFXCueManager の Inspector でキューに追加してください。",
                "OK"
            );
        }

        [MenuItem("SpatialVFXCue/Convert Selected Prefabs for VFXCue", true)]
        public static bool ValidateConvertSelectedPrefabs()
        {
            return Selection.objects.Length > 0;
        }
    }
}
#endif
