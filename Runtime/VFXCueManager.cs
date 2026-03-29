using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// VFX 演出キューマネージャー。
    /// シーンに 1 つ配置し、キーボード入力で VFX Prefab をネットワークスポーンする。
    /// </summary>
    public class VFXCueManager : MonoBehaviour
    {
        [Header("キュー設定")]
        [Tooltip("VFX キュー定義リスト")]
        public List<VFXCueEntry> cues = new List<VFXCueEntry>();

        [Header("権限")]
        [Tooltip("スペースオーナー/管理者のみ操作可能にする")]
        public bool adminOnly = true;

        [Header("制限")]
        [Tooltip("同時スポーン上限")]
        [Min(1)]
        public int maxConcurrentVFX = 10;

        /// <summary>
        /// true にするとキーボード直接入力を無視する（BPM同期モード用）
        /// 毎セッションで false から始まり、BPMSync が Start() で true に設定する
        /// </summary>
        [System.NonSerialized]
        public bool suppressKeyInput;

        [System.NonSerialized]
        public VFXCueVJController vjController;

        [System.NonSerialized]
        public VFXCueBPMSync bpmSync;

        /// <summary>キュー発動時に通知するイベント（AudioVisualizer連携用）</summary>
        public event System.Action<string> OnCueTriggered;

        private int _activeVFXCount;
        private VFXCueLog _log;

        // ホールド中の VFX インスタンス（キュー名 → GameObject）
        private readonly Dictionary<string, GameObject> _heldVFX = new Dictionary<string, GameObject>();

        /// <summary>ホールド中VFXへの読み取りアクセス</summary>
        public IReadOnlyDictionary<string, GameObject> HeldVFX => _heldVFX;

        /// <summary>現在のアクティブVFX数</summary>
        public int ActiveVFXCount => _activeVFXCount;

        private void Start()
        {
            _log = GetComponent<VFXCueLog>();

            // Spatial のエモートキーバインド（1〜5）を無効化して VFX キーに使う
#if !UNITY_EDITOR
            try
            {
                SpatialBridge.inputService.SetEmoteBindingsEnabled(false);
            }
            catch (System.Exception e)
            {
                // エモート無効化エラーは無視（Spatial環境依存）
            }
#endif

        }

        private void Update()
        {
            // BPM同期モード中はキー入力を無視（BPMSync側でクォンタイズ発動する）
            if (suppressKeyInput) return;

            // 権限チェック（早期リターン）
            if (adminOnly && !IsAuthorized())
                return;

            // キュー走査（最小限のループ）
            for (int i = 0; i < cues.Count; i++)
            {
                VFXCueEntry cue = cues[i];
                if (cue.vfxPrefab == null) continue;
                if (!Input.GetKeyDown(cue.triggerKey)) continue;
                if (cue.IsOnCooldown()) continue;
                if (_activeVFXCount >= maxConcurrentVFX) continue;

                TriggerCue(cue);
            }
        }

        /// <summary>
        /// キュー名で外部から発動する（API 連携用）
        /// </summary>
        public void TriggerCueByName(string cueName, bool ignoreCooldown = false)
        {
            if (adminOnly && !IsAuthorized()) return;

            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].cueName == cueName)
                {
                    TriggerCue(cues[i], ignoreCooldown);
                    return;
                }
            }
#if UNITY_EDITOR
            Debug.LogWarning($"[SpatialVFXCue] キュー '{cueName}' が見つかりません");
#endif
        }

        /// <summary>
        /// キーコードで外部から発動する（API 連携用）
        /// </summary>
        public void TriggerCueByKey(KeyCode key)
        {
            if (adminOnly && !IsAuthorized()) return;

            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].triggerKey == key)
                {
                    TriggerCue(cues[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// ホールド用: VFX を生成して保持する。既に存在する場合は何もしない。
        /// </summary>
        public void SpawnHeld(string cueName)
        {
            if (_heldVFX.ContainsKey(cueName) && _heldVFX[cueName] != null) return;

            VFXCueEntry cue = FindCue(cueName);
            if (cue == null || cue.vfxPrefab == null) return;

            Vector3 pos = cue.GetSpawnPosition();
            Quaternion rot = cue.GetSpawnRotation();

#if UNITY_EDITOR
            GameObject obj = Instantiate(cue.vfxPrefab.gameObject, pos, rot);
            VFXCueEffect heldEffect = obj.GetComponent<VFXCueEffect>();
            if (heldEffect != null)
            {
                heldEffect.autoDestroyTime = 9999f; // ホールドは手動破棄
            }
            _heldVFX[cueName] = obj;

            // VJコントローラーのパラメータを適用
            try
            {
                if (vjController != null && heldEffect != null)
                    vjController.ApplyParametersToEffect(heldEffect, cue);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SpatialVFXCue] VJパラメータ適用エラー(Held): {e.Message}");
            }
#else
            // Spatial 本番: コルーチンでスポーン
            StartCoroutine(SpawnHeldCoroutine(cueName, cue, pos, rot));
#endif
        }

#if !UNITY_EDITOR
        private IEnumerator SpawnHeldCoroutine(string cueName, VFXCueEntry cue, Vector3 pos, Quaternion rot)
        {
            var request = SpatialBridge.spaceContentService.SpawnNetworkObject(cue.vfxPrefab, pos, rot);
            yield return request;
            if (request.succeeded && request.networkObject != null)
            {
                GameObject obj = request.networkObject.gameObject;
                _heldVFX[cueName] = obj;

                // パーティクルを再生（PlayOnAwake=false 対策）
                foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Play(true);
            }
        }
#endif

        /// <summary>
        /// ホールド用: 保持中の VFX を破棄する。
        /// </summary>
        public void DestroyHeld(string cueName)
        {
            if (_heldVFX.ContainsKey(cueName))
            {
                if (_heldVFX[cueName] != null)
                    Destroy(_heldVFX[cueName]);
                _heldVFX.Remove(cueName);
            }
        }

        /// <summary>
        /// ホールド中の VFX があるか
        /// </summary>
        public bool IsHeld(string cueName)
        {
            return _heldVFX.ContainsKey(cueName) && _heldVFX[cueName] != null;
        }

        private VFXCueEntry FindCue(string cueName)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].cueName == cueName)
                    return cues[i];
            }
            return null;
        }

        private void TriggerCue(VFXCueEntry cue, bool ignoreCooldown = false)
        {
            if (!ignoreCooldown && cue.IsOnCooldown()) return;
            if (_activeVFXCount >= maxConcurrentVFX) return;

            cue.lastTriggerTime = Time.time;
            OnCueTriggered?.Invoke(cue.cueName);
            StartCoroutine(SpawnVFXCoroutine(cue));
        }

        private IEnumerator SpawnVFXCoroutine(VFXCueEntry cue)
        {
            _activeVFXCount++;

            Vector3 pos = cue.GetSpawnPosition();
            Quaternion rot = cue.GetSpawnRotation();

            GameObject spawnedObj = null;

#if UNITY_EDITOR
            // エディタテスト用: 直接 Instantiate でスポーン
            spawnedObj = Instantiate(cue.vfxPrefab.gameObject, pos, rot);
#else
            // Spatial 本番環境: ネットワークスポーン
            SpawnNetworkObjectRequest request = SpatialBridge.spaceContentService.SpawnNetworkObject(
                cue.vfxPrefab,
                pos,
                rot
            );
            yield return request;

            if (request.succeeded)
            {
                spawnedObj = request.networkObject?.gameObject;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SpatialVFXCue] '{cue.cueName}' のスポーンに失敗しました");
#endif
                _activeVFXCount = Mathf.Max(0, _activeVFXCount - 1);
                yield break;
            }
#endif

            if (spawnedObj != null)
            {
                // パーティクルを直接再生（PlayOnAwake=false 対策）
                foreach (var ps in spawnedObj.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Play(true);

                // Prefab に事前アタッチ済みの VFXCueEffect を取得
                VFXCueEffect effect = spawnedObj.GetComponent<VFXCueEffect>();
                if (effect != null)
                {
                    effect.autoDestroyTime = cue.autoDestroyTime;

                    // VJコントローラーのパラメータを適用
                    if (vjController != null)
                        vjController.ApplyParametersToEffect(effect, cue);
                }
                else
                {
                    // VFXCueEffect が無い場合はタイマーで直接消滅
                    Destroy(spawnedObj, cue.autoDestroyTime);
                }
            }

            if (_log != null)
            {
                _log.AddLog(cue.cueName, cue.triggerKey);
            }

#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue] '{cue.cueName}' をスポーンしました");
#endif

            // 自動消滅後にカウント減算
            yield return new WaitForSeconds(cue.autoDestroyTime + 0.5f);
            _activeVFXCount = Mathf.Max(0, _activeVFXCount - 1);
        }

        private bool IsAuthorized()
        {
#if UNITY_EDITOR
            return true;  // エディタでは常に許可
#else
            try
            {
                var localActor = SpatialBridge.actorService.localActor;
                return localActor.isSpaceOwner || localActor.isSpaceAdministrator;
            }
            catch
            {
                return true;
            }
#endif
        }
    }
}
