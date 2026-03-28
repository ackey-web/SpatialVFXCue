#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// シーン内の GameObject にスクリプトコンポーネントを再アタッチするワンクリック修復ツール。
    /// メニュー: SpatialVFXCue → Repair Scene Components
    /// </summary>
    public static class VFXCueSceneRepair
    {
        [MenuItem("SpatialVFXCue/Repair Scene Components")]
        public static void RepairScene()
        {
            // 現在のシーンで作業
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Error", "有効なシーンが開かれていません。", "OK");
                return;
            }

            int addedCount = 0;

            // --- VFXCueManager ---
            GameObject managerObj = GameObject.Find("VFXCueManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("VFXCueManager");
                Debug.Log("[Repair] VFXCueManager オブジェクトを新規作成");
            }

            VFXCueManager manager = managerObj.GetComponent<VFXCueManager>();
            if (manager == null)
            {
                manager = managerObj.AddComponent<VFXCueManager>();
                manager.adminOnly = false;
                manager.maxConcurrentVFX = 10;
                addedCount++;
                Debug.Log("[Repair] VFXCueManager コンポーネントを追加");
            }
            else
            {
                manager.adminOnly = false; // 確実に OFF にする
            }

            // VFXCueLog
            if (managerObj.GetComponent<VFXCueLog>() == null)
            {
                managerObj.AddComponent<VFXCueLog>();
                addedCount++;
                Debug.Log("[Repair] VFXCueLog コンポーネントを追加");
            }

            // --- キュー設定 ---
            if (manager.cues == null || manager.cues.Count == 0)
            {
                SetupCues(manager);
                Debug.Log("[Repair] VFX キュー 6 個を設定");
            }

            // --- VFXCueBPMSync ---
            GameObject bpmObj = GameObject.Find("VFXCueBPMSync");
            if (bpmObj == null)
            {
                bpmObj = new GameObject("VFXCueBPMSync");
                Debug.Log("[Repair] VFXCueBPMSync オブジェクトを新規作成");
            }
            VFXCueBPMSync bpmSync = bpmObj.GetComponent<VFXCueBPMSync>();
            if (bpmSync == null)
            {
                bpmSync = bpmObj.AddComponent<VFXCueBPMSync>();
                addedCount++;
                Debug.Log("[Repair] VFXCueBPMSync コンポーネントを追加");
            }
            bpmSync.cueManager = manager;
            bpmSync.modeToggleKey = KeyCode.X;  // M はSpatialマイクと競合
            SetupBPMCueMappings(bpmSync);

            // --- VFXCueVJController ---
            GameObject vjObj = GameObject.Find("VFXCueVJController");
            if (vjObj == null)
            {
                vjObj = new GameObject("VFXCueVJController");
                Debug.Log("[Repair] VFXCueVJController オブジェクトを新規作成");
            }
            VFXCueVJController vjController = vjObj.GetComponent<VFXCueVJController>();
            if (vjController == null)
            {
                vjController = vjObj.AddComponent<VFXCueVJController>();
                addedCount++;
                Debug.Log("[Repair] VFXCueVJController コンポーネントを追加");
            }
            vjController.cueManager = manager;

            // VFXCueVJPanel
            VFXCueVJPanel vjPanel = vjObj.GetComponent<VFXCueVJPanel>();
            if (vjPanel == null)
            {
                vjPanel = vjObj.AddComponent<VFXCueVJPanel>();
                addedCount++;
                Debug.Log("[Repair] VFXCueVJPanel コンポーネントを追加");
            }
            vjPanel.vjController = vjController;
            vjPanel.bpmSync = bpmSync;

            // --- VFXCueRandom ---
            GameObject randomObj = GameObject.Find("VFXCueRandom");
            if (randomObj == null)
            {
                randomObj = new GameObject("VFXCueRandom");
                Debug.Log("[Repair] VFXCueRandom オブジェクトを新規作成");
            }
            VFXCueRandom random = randomObj.GetComponent<VFXCueRandom>();
            if (random == null)
            {
                random = randomObj.AddComponent<VFXCueRandom>();
                addedCount++;
                Debug.Log("[Repair] VFXCueRandom コンポーネントを追加");
            }
            random.cueManager = manager;
            random.cueNames = new System.Collections.Generic.List<string>
            {
                "Fireworks", "Confetti", "MeteorShower", "SpotlightBeam", "SmokeBurst", "SparkleRain"
            };

            // --- VFXCueChain ---
            GameObject chainObj = GameObject.Find("VFXCueChain");
            if (chainObj == null)
            {
                chainObj = new GameObject("VFXCueChain");
                Debug.Log("[Repair] VFXCueChain オブジェクトを新規作成");
            }
            VFXCueChain chain = chainObj.GetComponent<VFXCueChain>();
            if (chain == null)
            {
                chain = chainObj.AddComponent<VFXCueChain>();
                addedCount++;
                Debug.Log("[Repair] VFXCueChain コンポーネントを追加");
            }
            chain.cueManager = manager;

            // --- VFXCueCountdown ---
            GameObject countdownObj = GameObject.Find("VFXCueCountdown");
            if (countdownObj == null)
            {
                countdownObj = new GameObject("VFXCueCountdown");
                Debug.Log("[Repair] VFXCueCountdown オブジェクトを新規作成");
            }
            VFXCueCountdown countdown = countdownObj.GetComponent<VFXCueCountdown>();
            if (countdown == null)
            {
                countdown = countdownObj.AddComponent<VFXCueCountdown>();
                addedCount++;
                Debug.Log("[Repair] VFXCueCountdown コンポーネントを追加");
            }
            countdown.cueManager = manager;
            countdown.climaxCueNames = new System.Collections.Generic.List<string>
            {
                "Fireworks", "Confetti", "MeteorShower", "SpotlightBeam", "SmokeBurst", "SparkleRain"
            };

            // シーンを保存
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string message = $"修復完了！\n\n" +
                $"追加したコンポーネント: {addedCount} 個\n" +
                $"VFX キュー: {manager.cues.Count} 個\n\n" +
                "シーンを保存しました。Publish してテストしてください。";

            EditorUtility.DisplayDialog("SpatialVFXCue Scene Repair", message, "OK");
            Debug.Log($"[Repair] 完了: {addedCount} 個のコンポーネントを追加しました");
        }

        private static void SetupCues(VFXCueManager manager)
        {
            manager.cues = new System.Collections.Generic.List<VFXCueEntry>();

            string[] prefabNames = { "VFX_Fireworks", "VFX_Confetti", "VFX_MeteorShower", "VFX_SpotlightBeam", "VFX_SmokeBurst", "VFX_SparkleRain" };
            string[] cueNames = { "Fireworks", "Confetti", "MeteorShower", "SpotlightBeam", "SmokeBurst", "SparkleRain" };
            KeyCode[] keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };

            for (int i = 0; i < prefabNames.Length; i++)
            {
                string[] guids = AssetDatabase.FindAssets($"{prefabNames[i]} t:Prefab");
                string path = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : string.Empty;
                GameObject prefabGo = !string.IsNullOrEmpty(path) ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
                SpatialNetworkObject netObj = prefabGo != null ? prefabGo.GetComponent<SpatialNetworkObject>() : null;

                if (netObj == null)
                {
                    Debug.LogWarning($"[Repair] Prefab '{path}' が見つからないか SpatialNetworkObject がありません");
                    continue;
                }

                VFXCueEntry entry = new VFXCueEntry
                {
                    cueName = cueNames[i],
                    triggerKey = keys[i],
                    vfxPrefab = netObj,
                    autoDestroyTime = 5f,
                    cooldown = 0.5f,
                    layerIndex = i < 3 ? 0 : 1 // 前半: Layer 0, 後半: Layer 1
                };

                manager.cues.Add(entry);
                Debug.Log($"[Repair] キュー追加: [{keys[i]}] {cueNames[i]} → {prefabNames[i]}");
            }
        }

        private static void SetupBPMCueMappings(VFXCueBPMSync bpmSync)
        {
            if (bpmSync.cueMappings != null && bpmSync.cueMappings.Count > 0)
                return;

            bpmSync.cueMappings = new System.Collections.Generic.List<VFXCueBPMSync.BPMCueMapping>
            {
                new VFXCueBPMSync.BPMCueMapping { key = KeyCode.Alpha1, cueName = "Fireworks" },
                new VFXCueBPMSync.BPMCueMapping { key = KeyCode.Alpha2, cueName = "Confetti" },
                new VFXCueBPMSync.BPMCueMapping { key = KeyCode.Alpha3, cueName = "MeteorShower" },
                new VFXCueBPMSync.BPMCueMapping { key = KeyCode.Alpha4, cueName = "SpotlightBeam" },
                new VFXCueBPMSync.BPMCueMapping { key = KeyCode.Alpha5, cueName = "SmokeBurst" },
                new VFXCueBPMSync.BPMCueMapping { key = KeyCode.Alpha6, cueName = "SparkleRain" },
            };
        }
    }
}
#endif
