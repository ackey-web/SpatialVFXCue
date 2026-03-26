using System;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// VJメインコントローラー。
    /// インテンシティ/スピード/カラー制御、レイヤー管理、プリセット機能を提供。
    /// パラメータ階層: 最終値 = キュー個別 × レイヤー × マスター
    /// </summary>
    public class VFXCueVJController : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("VFXCueManager")]
        public VFXCueManager cueManager;

        [Header("マスターパラメータ")]
        [Range(0f, 2f)]
        public float masterIntensity = 1f;

        [Range(0.1f, 3f)]
        public float masterSpeed = 1f;

        public Color masterColorTint = Color.white;

        [Header("レイヤー")]
        public List<VFXCueVJLayer> layers = new List<VFXCueVJLayer>();

        [Header("プリセット")]
        public List<VFXCueVJPreset> presets = new List<VFXCueVJPreset>();

        [Header("カラーパレット")]
        public Color[] colorPalette = new Color[]
        {
            Color.white,
            Color.red,
            new Color(1f, 0.5f, 0f),   // Orange
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            new Color(0.5f, 0f, 1f)    // Purple
        };

        [Header("ビートカラーサイクル")]
        public bool beatColorCycleEnabled;
        [Range(1, 16)]
        public int beatColorDivision = 4;

        [Header("キー設定")]
        public KeyCode selectModeKey = KeyCode.N;
        public KeyCode colorNextKey = KeyCode.C;
        public KeyCode beatColorToggleKey = KeyCode.V;
        public KeyCode layerSelectKey = KeyCode.L;
        public KeyCode layerMuteKey = KeyCode.K;
        public KeyCode layerSoloKey = KeyCode.J;
        public KeyCode presetSaveKey = KeyCode.P;
        public KeyCode panelToggleKey = KeyCode.None;
        public KeyCode helpKey = KeyCode.H;

        // プリセットリコールキー
        private readonly KeyCode[] _presetRecallKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

        // 選択状態（ランタイム専用、シーンに保存しない）
        [System.NonSerialized] public bool selectMode;
        [System.NonSerialized] public int selectedCueIndex = -1;
        [System.NonSerialized] public int selectedLayerIndex;
        [System.NonSerialized] public int selectedColorIndex;
        [System.NonSerialized] public bool showPanel = false;
        [System.NonSerialized] public bool showHelp;

        // キュー個別パラメータ
        [Serializable]
        public class CueParameter
        {
            public float intensity = 1f;
            public float speed = 1f;
            public Color colorTint = Color.white;
        }

        private readonly Dictionary<string, CueParameter> _cueParams = new Dictionary<string, CueParameter>();

        // パラメータ調整
        private const float IntensityStep = 0.05f;
        private const float SpeedStep = 0.05f;
        private const float KeyRepeatDelay = 0.4f;
        private const float KeyRepeatRate = 0.05f;

        private float _keyHoldTimeUp, _keyHoldTimeDown, _keyHoldTimeLeft, _keyHoldTimeRight;
        private float _nextRepeatUp, _nextRepeatDown, _nextRepeatLeft, _nextRepeatRight;

        // ビートカラーサイクル用
        private VFXCueBPMSync _bpmSync;
        private int _lastBeatForColor = -1;

        private void Start()
        {
            // 最優先: VFXCueManager に自身を登録
            try
            {
                if (cueManager != null)
                {
                    cueManager.vjController = this;
                    if (cueManager.bpmSync != null)
                        _bpmSync = cueManager.bpmSync;
                }
            }
            catch { }

            try
            {
                panelToggleKey = KeyCode.None;

                if (layers.Count == 0)
                {
                    layers.Add(new VFXCueVJLayer { layerName = "All", layerColor = Color.white });
                    layers.Add(new VFXCueVJLayer { layerName = "BG", layerColor = new Color(0.3f, 0.5f, 1f) });
                    layers.Add(new VFXCueVJLayer { layerName = "FG", layerColor = new Color(1f, 0.4f, 0.3f) });
                    layers.Add(new VFXCueVJLayer { layerName = "Accent", layerColor = new Color(1f, 0.8f, 0.2f) });
                }

                while (presets.Count < 8)
                    presets.Add(new VFXCueVJPreset());
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[VFXCueVJController] Start error: {e.Message}");
#endif
            }
        }

        // プリセットサイクル用
        private int _presetCycleIndex;

        private void Update()
        {
            // BPMSync 遅延取得（Start順序問題対策）
            if (_bpmSync == null && cueManager != null && cueManager.bpmSync != null)
                _bpmSync = cueManager.bpmSync;

            HandlePanelToggle();
            HandleHelpToggle();
            HandleSelectMode();
            HandleColorInput();
            HandleLayerInput();
            HandlePresetCycle();
#if UNITY_EDITOR
            // エディタ専用: 矢印キー（Spatial移動キーと競合）
            HandleParameterAdjustment();
            // Q/W/E/R 個別プリセット（Spatial移動キーと競合）
            HandlePresetInput();
#endif
            HandleBeatColorCycle();
        }

        /// <summary>
        /// VFXCueEffect にレイヤー×マスターのパラメータを適用する
        /// </summary>
        public void ApplyParametersToEffect(VFXCueEffect effect, VFXCueEntry cue)
        {
            if (effect == null || cue == null) return;
            if (layers.Count == 0) return;

            int layerIdx = Mathf.Clamp(cue.layerIndex, 0, layers.Count - 1);
            VFXCueVJLayer layer = layers[layerIdx];

            // レイヤーミュート／ソロチェック
            if (IsLayerMuted(layerIdx))
            {
                effect.SetIntensity(0f);
                return;
            }

            CueParameter cueParam = GetCueParameter(cue.cueName);

            float finalIntensity = cueParam.intensity * layer.intensity * masterIntensity;
            float finalSpeed = cueParam.speed * layer.speedMultiplier * masterSpeed;
            Color finalColor = MultiplyColors(cueParam.colorTint, layer.colorTint, masterColorTint);

            effect.SetIntensity(finalIntensity);
            effect.SetSpeedMultiplier(finalSpeed);
            effect.SetColorTint(finalColor);
        }

        /// <summary>
        /// 指定レイヤーが実質ミュート状態か判定
        /// </summary>
        public bool IsLayerMuted(int layerIdx)
        {
            if (layerIdx < 0 || layerIdx >= layers.Count) return false;

            // ソロがどこかでONなら、ソロでないレイヤーはミュート
            bool anySolo = false;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].solo) { anySolo = true; break; }
            }

            if (anySolo && !layers[layerIdx].solo)
                return true;

            return layers[layerIdx].mute;
        }

        /// <summary>
        /// キュー個別パラメータを取得（なければ作成）
        /// </summary>
        public CueParameter GetCueParameter(string cueName)
        {
            if (!_cueParams.TryGetValue(cueName, out CueParameter param))
            {
                param = new CueParameter();
                _cueParams[cueName] = param;
            }
            return param;
        }

        // --- パネル表示 ---

        private void HandlePanelToggle()
        {
            if (Input.GetKeyDown(panelToggleKey))
            {
                showPanel = !showPanel;
            }
        }

        private void HandleHelpToggle()
        {
            if (Input.GetKeyDown(helpKey))
                showHelp = !showHelp;
        }

        // --- 選択モード ---

        private void HandleSelectMode()
        {
            if (Input.GetKeyDown(selectModeKey))
            {
                selectMode = !selectMode;
                // BPMSync のキュー発動を抑制/解除
                if (_bpmSync != null)
                    _bpmSync.suppressCueKeys = selectMode;

                string msg = selectMode ? "Select ON (1-6=cue)" : "Select OFF";
                try { SpatialBridge.coreGUIService.DisplayToastMessage(msg); } catch { }
#if UNITY_EDITOR
                Debug.Log("[SpatialVFXCue VJ] " + msg);
#endif
            }

            if (!selectMode || cueManager == null) return;

            // 数字キーでキュー選択（発動はしない）
            for (int i = 0; i < cueManager.cues.Count && i < 9; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key))
                {
                    selectedCueIndex = i;
                    try { SpatialBridge.coreGUIService.DisplayToastMessage("Cue: " + cueManager.cues[i].cueName); } catch { }
#if UNITY_EDITOR
                    Debug.Log("[SpatialVFXCue VJ] キュー選択: " + cueManager.cues[i].cueName);
#endif
                }
            }
        }

        // --- パラメータ調整（長押し対応） ---

        private void HandleParameterAdjustment()
        {
            if (selectedCueIndex < 0 || cueManager == null) return;
            if (selectedCueIndex >= cueManager.cues.Count) return;

            string cueName = cueManager.cues[selectedCueIndex].cueName;
            CueParameter param = GetCueParameter(cueName);

            // Up: インテンシティ増加
            if (ProcessKeyRepeat(KeyCode.UpArrow, ref _keyHoldTimeUp, ref _nextRepeatUp))
                param.intensity = Mathf.Clamp(param.intensity + IntensityStep, 0f, 2f);

            // Down: インテンシティ減少
            if (ProcessKeyRepeat(KeyCode.DownArrow, ref _keyHoldTimeDown, ref _nextRepeatDown))
                param.intensity = Mathf.Clamp(param.intensity - IntensityStep, 0f, 2f);

            // Right: スピード増加
            if (ProcessKeyRepeat(KeyCode.RightArrow, ref _keyHoldTimeRight, ref _nextRepeatRight))
                param.speed = Mathf.Clamp(param.speed + SpeedStep, 0.1f, 3f);

            // Left: スピード減少
            if (ProcessKeyRepeat(KeyCode.LeftArrow, ref _keyHoldTimeLeft, ref _nextRepeatLeft))
                param.speed = Mathf.Clamp(param.speed - SpeedStep, 0.1f, 3f);

            // ホールド中VFXにリアルタイム反映
            ApplyToHeldVFX();
        }

        private bool ProcessKeyRepeat(KeyCode key, ref float holdTime, ref float nextRepeat)
        {
            if (Input.GetKeyDown(key))
            {
                holdTime = Time.time;
                nextRepeat = Time.time + KeyRepeatDelay;
                return true;
            }
            if (Input.GetKey(key))
            {
                if (Time.time >= nextRepeat)
                {
                    nextRepeat = Time.time + KeyRepeatRate;
                    return true;
                }
            }
            else
            {
                holdTime = 0f;
            }
            return false;
        }

        // --- カラー ---

        private void HandleColorInput()
        {
            if (colorPalette == null || colorPalette.Length == 0) return;

            if (Input.GetKeyDown(colorNextKey))
            {
                selectedColorIndex = (selectedColorIndex + 1) % colorPalette.Length;
                Color newColor = colorPalette[selectedColorIndex];

                if (selectedCueIndex >= 0 && cueManager != null && selectedCueIndex < cueManager.cues.Count)
                {
                    string cueName = cueManager.cues[selectedCueIndex].cueName;
                    GetCueParameter(cueName).colorTint = newColor;
                    ApplyToHeldVFX();
                }
                else
                {
                    masterColorTint = newColor;
                    ApplyToHeldVFX();
                }
                try { SpatialBridge.coreGUIService.DisplayToastMessage("Color: " + (selectedColorIndex + 1)); } catch { }
            }

            if (Input.GetKeyDown(beatColorToggleKey))
            {
                beatColorCycleEnabled = !beatColorCycleEnabled;
                string state = beatColorCycleEnabled ? "ON" : "OFF";
                try { SpatialBridge.coreGUIService.DisplayToastMessage("BeatColor: " + state); } catch { }
            }
        }

        // --- レイヤー ---

        private void HandleLayerInput()
        {
            if (layers.Count == 0) return;

            if (Input.GetKeyDown(layerSelectKey))
            {
                selectedLayerIndex = (selectedLayerIndex + 1) % layers.Count;
                try { SpatialBridge.coreGUIService.DisplayToastMessage("Layer: " + layers[selectedLayerIndex].layerName); } catch { }
            }

            if (Input.GetKeyDown(layerMuteKey))
            {
                layers[selectedLayerIndex].mute = !layers[selectedLayerIndex].mute;
                string state = layers[selectedLayerIndex].mute ? "ON" : "OFF";
                try { SpatialBridge.coreGUIService.DisplayToastMessage(layers[selectedLayerIndex].layerName + " Mute:" + state); } catch { }
                ApplyToHeldVFX();
            }

            if (Input.GetKeyDown(layerSoloKey))
            {
                layers[selectedLayerIndex].solo = !layers[selectedLayerIndex].solo;
                string state = layers[selectedLayerIndex].solo ? "ON" : "OFF";
                try { SpatialBridge.coreGUIService.DisplayToastMessage(layers[selectedLayerIndex].layerName + " Solo:" + state); } catch { }
                ApplyToHeldVFX();
            }
        }

        // --- プリセットサイクル（Spatial対応: Zキー） ---

        private void HandlePresetCycle()
        {
            // Z: 次のプリセットをリコール（Spatialでも動作）
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (presets.Count == 0) return;
                // 保存済みプリセットを探して次のを呼ぶ
                for (int i = 0; i < presets.Count; i++)
                {
                    _presetCycleIndex = (_presetCycleIndex + 1) % presets.Count;
                    if (!string.IsNullOrEmpty(presets[_presetCycleIndex].presetName))
                    {
                        RecallPreset(_presetCycleIndex);
                        try { SpatialBridge.coreGUIService.DisplayToastMessage("Preset " + (_presetCycleIndex + 1)); } catch { }
                        break;
                    }
                }
            }

            // P: プリセット保存（Spatialでも動作）
            if (Input.GetKeyDown(presetSaveKey))
            {
                SavePresetDialog();
                try { SpatialBridge.coreGUIService.DisplayToastMessage("Preset Saved"); } catch { }
            }
        }

        // --- プリセット（エディタ専用: Q/W/E/R） ---

        private void HandlePresetInput()
        {
            // エディタ専用: Q/W/E/R プリセットリコール
            // （P保存は HandlePresetCycle で処理）
            for (int i = 0; i < _presetRecallKeys.Length && i < presets.Count; i++)
            {
                if (Input.GetKeyDown(_presetRecallKeys[i]))
                {
                    RecallPreset(i);
                }
            }
        }

        private void SavePresetDialog()
        {
            // 最初の空きスロットに保存
            int slot = -1;
            for (int i = 0; i < presets.Count; i++)
            {
                if (string.IsNullOrEmpty(presets[i].presetName))
                {
                    slot = i;
                    break;
                }
            }

            // 空きがなければ最後のスロットに上書き
            if (slot < 0) slot = presets.Count - 1;

            SavePreset(slot);
#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue VJ] プリセット保存: スロット {slot + 1}");
#endif
        }

        public void SavePreset(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= presets.Count) return;

            var preset = presets[slotIndex];
            preset.presetName = $"Preset{slotIndex + 1}";
            preset.masterIntensity = masterIntensity;
            preset.masterSpeed = masterSpeed;
            preset.masterColorTint = masterColorTint;

            // キュー状態を保存
            preset.cueStates.Clear();
            if (cueManager != null)
            {
                for (int i = 0; i < cueManager.cues.Count; i++)
                {
                    var cue = cueManager.cues[i];
                    var param = GetCueParameter(cue.cueName);
                    preset.cueStates.Add(new VFXCueVJPreset.CueState
                    {
                        cueName = cue.cueName,
                        active = cueManager.IsHeld(cue.cueName),
                        intensity = param.intensity,
                        speed = param.speed,
                        colorTint = param.colorTint
                    });
                }
            }

            // レイヤー状態を保存
            preset.layerStates.Clear();
            for (int i = 0; i < layers.Count; i++)
            {
                preset.layerStates.Add(new VFXCueVJPreset.LayerState
                {
                    layerIndex = i,
                    intensity = layers[i].intensity,
                    speedMultiplier = layers[i].speedMultiplier,
                    mute = layers[i].mute,
                    solo = layers[i].solo,
                    colorTint = layers[i].colorTint
                });
            }
        }

        public void RecallPreset(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= presets.Count) return;

            var preset = presets[slotIndex];
            if (string.IsNullOrEmpty(preset.presetName))
            {
#if UNITY_EDITOR
                Debug.Log($"[SpatialVFXCue VJ] プリセット {slotIndex + 1}: 空");
#endif
                return;
            }

            masterIntensity = preset.masterIntensity;
            masterSpeed = preset.masterSpeed;
            masterColorTint = preset.masterColorTint;

            // キュー状態を復元
            foreach (var cueState in preset.cueStates)
            {
                var param = GetCueParameter(cueState.cueName);
                param.intensity = cueState.intensity;
                param.speed = cueState.speed;
                param.colorTint = cueState.colorTint;
            }

            // レイヤー状態を復元
            foreach (var layerState in preset.layerStates)
            {
                if (layerState.layerIndex < layers.Count)
                {
                    var layer = layers[layerState.layerIndex];
                    layer.intensity = layerState.intensity;
                    layer.speedMultiplier = layerState.speedMultiplier;
                    layer.mute = layerState.mute;
                    layer.solo = layerState.solo;
                    layer.colorTint = layerState.colorTint;
                }
            }

            ApplyToHeldVFX();
#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue VJ] プリセット {slotIndex + 1} リコール: {preset.presetName}");
#endif
        }

        // --- ビートカラーサイクル ---

        private void HandleBeatColorCycle()
        {
            if (!beatColorCycleEnabled || _bpmSync == null) return;

            int currentBeat = _bpmSync.CurrentBeat;
            if (currentBeat != _lastBeatForColor)
            {
                _lastBeatForColor = currentBeat;

                if (currentBeat % beatColorDivision == 0 && colorPalette.Length > 0)
                {
                    selectedColorIndex = (selectedColorIndex + 1) % colorPalette.Length;
                    masterColorTint = colorPalette[selectedColorIndex];
                    ApplyToHeldVFX();
                }
            }
        }

        // --- ホールド中VFXへのリアルタイム反映 ---

        private void ApplyToHeldVFX()
        {
            if (cueManager == null) return;

            // 辞書の反復中変更を防ぐためキーをコピー
            var heldKeys = new List<string>(cueManager.HeldVFX.Keys);
            for (int k = 0; k < heldKeys.Count; k++)
            {
                string key = heldKeys[k];
                if (!cueManager.HeldVFX.TryGetValue(key, out GameObject obj) || obj == null)
                    continue;

                VFXCueEffect effect = obj.GetComponent<VFXCueEffect>();
                if (effect == null) continue;

                // キュー名からVFXCueEntryを探す
                VFXCueEntry matchedCue = null;
                for (int i = 0; i < cueManager.cues.Count; i++)
                {
                    if (cueManager.cues[i].cueName == key)
                    {
                        matchedCue = cueManager.cues[i];
                        break;
                    }
                }

                if (matchedCue != null)
                    ApplyParametersToEffect(effect, matchedCue);
            }
        }

        // --- パネルUI操作用 publicメソッド ---

        /// <summary>レイヤーのインテンシティを設定</summary>
        public void SetLayerIntensity(int index, float value)
        {
            if (index < 0 || index >= layers.Count) return;
            layers[index].intensity = Mathf.Clamp(value, 0f, 2f);
            ApplyToHeldVFX();
        }

        /// <summary>レイヤーのミュートを設定</summary>
        public void SetLayerMute(int index, bool mute)
        {
            if (index < 0 || index >= layers.Count) return;
            layers[index].mute = mute;
            ApplyToHeldVFX();
        }

        /// <summary>レイヤーのソロを設定</summary>
        public void SetLayerSolo(int index, bool solo)
        {
            if (index < 0 || index >= layers.Count) return;
            layers[index].solo = solo;
            ApplyToHeldVFX();
        }

        /// <summary>マスターインテンシティを設定</summary>
        public void SetMasterIntensity(float value)
        {
            masterIntensity = Mathf.Clamp(value, 0f, 2f);
            ApplyToHeldVFX();
        }

        /// <summary>マスタースピードを設定</summary>
        public void SetMasterSpeed(float value)
        {
            masterSpeed = Mathf.Clamp(value, 0.1f, 3f);
            ApplyToHeldVFX();
        }

        /// <summary>インデックスでキューを発動（パッドクリック用）</summary>
        public void TriggerCueByIndex(int index)
        {
            if (cueManager == null) return;
            if (index < 0 || index >= cueManager.cues.Count) return;

            var bpmSync = _bpmSync;
            if (bpmSync != null)
            {
                string cueName = cueManager.cues[index].cueName;
                switch (bpmSync.mode)
                {
                    case 0: // REPEAT
                        bpmSync.ToggleRepeat(cueName);
                        break;
                    case 1: // SINGLE
                        bpmSync.TriggerOnNextBeat(cueName);
                        break;
                    case 2: // HOLD
                        bpmSync.StartHold(cueName);
                        break;
                }
            }
            else
            {
                cueManager.TriggerCueByName(cueManager.cues[index].cueName);
            }
        }

        /// <summary>インデックスでキューを解放（HOLDモード マウスリリース用）</summary>
        public void ReleaseCueByIndex(int index)
        {
            if (cueManager == null) return;
            if (index < 0 || index >= cueManager.cues.Count) return;

            string cueName = cueManager.cues[index].cueName;
            if (_bpmSync != null)
                _bpmSync.StopHold(cueName);
            else
                cueManager.DestroyHeld(cueName);
        }

        /// <summary>キューを選択</summary>
        public void SelectCue(int index)
        {
            if (cueManager == null) return;
            if (index < 0 || index >= cueManager.cues.Count) return;
            selectedCueIndex = index;
        }

        /// <summary>カラーパレットから色を選択</summary>
        public void SelectColor(int index)
        {
            if (index < 0 || index >= colorPalette.Length) return;
            selectedColorIndex = index;
            Color newColor = colorPalette[index];

            if (selectedCueIndex >= 0 && cueManager != null && selectedCueIndex < cueManager.cues.Count)
            {
                string cueName = cueManager.cues[selectedCueIndex].cueName;
                GetCueParameter(cueName).colorTint = newColor;
            }
            else
            {
                masterColorTint = newColor;
            }
            ApplyToHeldVFX();
        }

        // --- ユーティリティ ---

        private static Color MultiplyColors(Color a, Color b, Color c)
        {
            return new Color(a.r * b.r * c.r, a.g * b.g * c.g, a.b * b.b * c.b, 1f);
        }

        /// <summary>
        /// 指定インデックスのレイヤー名を取得（範囲外なら"?"）
        /// </summary>
        public string GetLayerName(int index)
        {
            if (index >= 0 && index < layers.Count)
                return layers[index].layerName;
            return "?";
        }
    }
}
