using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// BPM 同期 VFX 自動リピート発動。
    /// キーを押すとその VFX が BPM に合わせて毎拍（または指定間隔）自動発動。
    /// もう一度押すと停止。VJ ツールのようなリズム演出。
    ///
    /// 使い方:
    /// 1. ワンクリックセットアップで自動配置
    /// 2. T キーで曲に合わせてタップ → BPM 自動検出
    /// 3. B キーでビート開始
    /// 4. キー 1〜6 で VFX リピート ON/OFF
    /// </summary>
    public class VFXCueBPMSync : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("シーン内の VFXCueManager")]
        public VFXCueManager cueManager;

        [Header("BPM 設定")]
        [Tooltip("BPM（1分あたりの拍数）")]
        [Min(1f)]
        public float bpm = 128f;

        [Tooltip("発動間隔（拍数単位: 1=毎拍, 2=2拍ごと, 0.5=半拍ごと）")]
        [Min(0.25f)]
        public float beatInterval = 1f;

        [Header("拍合わせ")]
        [Tooltip("タップテンポキー")]
        public KeyCode tapTempoKey = KeyCode.T;

        [Tooltip("ビート開始/停止キー")]
        public KeyCode beatStartKey = KeyCode.B;

        [Tooltip("モード切替キー（リピート ↔ 単発）")]
        public KeyCode modeToggleKey = KeyCode.X;

        [Header("キュー発動")]
        [Tooltip("キーとキュー名のマッピング")]
        public List<BPMCueMapping> cueMappings = new List<BPMCueMapping>();

        [Header("ビジュアルフィードバック")]
        [Tooltip("拍に合わせたフラッシュ表示")]
        public bool showBeatFlash = true;

        /// <summary>
        /// 発動モード: 0=REPEAT, 1=SINGLE, 2=HOLD
        /// </summary>
        [System.NonSerialized]
        public int mode = 0;

        /// <summary>
        /// true の間はキューマッピングのキー入力を無視する（VJController選択モード用）
        /// </summary>
        [System.NonSerialized]
        public bool suppressCueKeys;

        private const int MODE_REPEAT = 0;
        private const int MODE_SINGLE = 1;
        private const int MODE_HOLD = 2;
        private const int MODE_COUNT = 3;
        private static readonly string[] ModeNames = { "REPEAT", "SINGLE", "HOLD" };

        // ビート基準
        private double _beatOrigin;
        private bool _beatStarted;

        // タップテンポ
        private readonly List<float> _tapTimes = new List<float>();
        private const int MaxTaps = 8;
        private const float TapResetTime = 2f;

        // リピート中のキュー（REPEAT モード用）
        private readonly Dictionary<string, RepeatState> _activeRepeats = new Dictionary<string, RepeatState>();

        // 単発モード: 次の拍で発動するキュー
        private readonly List<PendingCue> _pendingCues = new List<PendingCue>();

        // HOLD モード: 現在押されているキューの拍トラッキング
        private readonly Dictionary<string, RepeatState> _holdRepeats = new Dictionary<string, RepeatState>();

        // HOLD 点滅状態
        private bool _holdVisibleState = true;

        // ビートフラッシュ
        private float _flashAlpha;
        private int _lastBeatIndex = -1;

        // カウントダウン統合
        private bool _countdownRunning;
        private float _countdownNextTime;
        private int _countdownCurrent;
        private int _countdownFrom = 3;
        private bool _countdownFiring;
        private string _countdownDisplay = "";  // OnGUI表示用テキスト
        private float _countdownDisplayAlpha;   // フェード用

        // OnGUI スタイルキャッシュ
        private GUIStyle _guiStatusStyle;
        private GUIStyle _guiActiveStyle;
        private GUIStyle _guiCdStyle;
        private GUIStyle _guiCdShadowStyle;
        private GUIStyle _guiVjStyle;
        private GUIStyle _guiHelpStyle;
        private bool _guiStylesReady;

        [System.Serializable]
        public class BPMCueMapping
        {
            [Tooltip("発動キー")]
            public KeyCode key = KeyCode.Alpha1;

            [Tooltip("発動するキュー名")]
            public string cueName;
        }

        private struct PendingCue
        {
            public string cueName;
            public double targetTime;
        }

        private class RepeatState
        {
            public int lastFiredBeat = -1;
        }

        /// <summary>
        /// 1拍の秒数
        /// </summary>
        public float SecondsPerBeat => 60f / bpm;

        /// <summary>
        /// 現在の拍番号（ビート開始からの通算、beatInterval 単位）
        /// </summary>
        public int CurrentBeat => _beatStarted
            ? Mathf.FloorToInt((float)((Time.timeAsDouble - _beatOrigin) / (SecondsPerBeat * beatInterval)))
            : 0;

        /// <summary>
        /// 現在の小節内拍番号（4拍単位）
        /// </summary>
        public int BeatInBar => _beatStarted
            ? Mathf.FloorToInt((float)((Time.timeAsDouble - _beatOrigin) / SecondsPerBeat)) % 4
            : 0;

        private void Start()
        {
            try
            {
                // Spatial で競合しないキーに強制上書き
                modeToggleKey = KeyCode.X;
                tapTempoKey = KeyCode.Y;
                beatStartKey = KeyCode.B;

                if (cueManager != null)
                {
                    cueManager.suppressKeyInput = true;
                    cueManager.bpmSync = this;
                }

                try { SpatialBridge.coreGUIService.DisplayToastMessage("VFXCue BPM: Ready (X=mode, Y=tap, B=beat)"); } catch { }
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[SpatialVFXCue BPM] Start error: {e.Message}");
#endif
            }
        }

        private void Update()
        {
            // ビート開始/停止
            if (Input.GetKeyDown(beatStartKey))
            {
                if (_beatStarted)
                    StopBeat();
                else
                    StartBeat();
            }

            // タップテンポ
            if (Input.GetKeyDown(tapTempoKey))
            {
                Tap();
            }

            // モード切替（M キー: REPEAT → SINGLE → HOLD → ...）
            if (Input.GetKeyDown(modeToggleKey))
            {
                // ホールド中のVFXを全て破棄してからモード切替
                if (cueManager != null)
                {
                    foreach (var kvp in _holdRepeats)
                        cueManager.DestroyHeld(kvp.Key);
                }

                mode = (mode + 1) % MODE_COUNT;
                _activeRepeats.Clear();
                _pendingCues.Clear();
                _holdRepeats.Clear();
                try { SpatialBridge.coreGUIService.DisplayToastMessage("Mode: " + ModeNames[mode]); } catch { }
#if UNITY_EDITOR
                Debug.Log($"[SpatialVFXCue BPM] モード切替 → {ModeNames[mode]}");
#endif
            }

            // キューマッピング（VJController 選択モード中はスキップ）
            if (suppressCueKeys)
            {
                // デバッグ: suppressCueKeys が true のときはキー入力を無視中
            }
            else if (cueMappings == null || cueMappings.Count == 0)
            {
                // cueMappings が空: 何もしない（エラー表示は1回だけ）
            }
            else
            {
                for (int i = 0; i < cueMappings.Count; i++)
                {
                    string cueName = cueMappings[i].cueName;
                    KeyCode key = cueMappings[i].key;

                    switch (mode)
                    {
                        case MODE_REPEAT:
                            if (Input.GetKeyDown(key))
                            {
                                if (_beatStarted)
                                    ToggleRepeat(cueName);
                                else if (cueManager != null)
                                    cueManager.TriggerCueByName(cueName);
                            }
                            break;

                        case MODE_SINGLE:
                            if (Input.GetKeyDown(key))
                            {
                                if (_beatStarted)
                                    TriggerOnNextBeat(cueName);
                                else if (cueManager != null)
                                    cueManager.TriggerCueByName(cueName);
                            }
                            break;

                        case MODE_HOLD:
                            if (Input.GetKeyDown(key))
                            {
                                if (cueManager != null)
                                    cueManager.SpawnHeld(cueName);
                                if (!_holdRepeats.ContainsKey(cueName))
                                    _holdRepeats[cueName] = new RepeatState();
                            }
                            else if (Input.GetKeyUp(key))
                            {
                                _holdRepeats.Remove(cueName);
                                _holdVisibleState = true;
                                if (cueManager != null)
                                    cueManager.DestroyHeld(cueName);
                            }
                            break;
                    }
                }
            }

            // REPEAT: 拍ごとに自動発動
            if (_beatStarted && mode == MODE_REPEAT)
                ProcessRepeats();

            // HOLD + BPM: 押している間だけ拍ごとに発動
            if (_beatStarted && mode == MODE_HOLD)
                ProcessHoldRepeats();

            // SINGLE: 待機中のキューを処理
            if (_beatStarted)
                ProcessPendingCues();

            // カウントダウン（9キー）
            if (Input.GetKeyDown(KeyCode.Alpha9) && !_countdownRunning)
            {
                _countdownRunning = true;
                _countdownCurrent = _countdownFrom;
                _countdownNextTime = Time.time;
                _countdownDisplay = _countdownCurrent.ToString();
                _countdownDisplayAlpha = 1f;
            }
            if (_countdownRunning && !_countdownFiring)
                UpdateCountdown();
            // カウントダウン表示フェード
            if (_countdownDisplayAlpha > 0f)
                _countdownDisplayAlpha = Mathf.Max(0f, _countdownDisplayAlpha - Time.deltaTime * 0.8f);

            // ビートフラッシュ
            if (showBeatFlash)
                UpdateBeatFlash();
        }

        /// <summary>ビート開始済みかどうか</summary>
        public bool IsBeatStarted => _beatStarted;

        /// <summary>モードを次へ切り替え（パネルUI用）</summary>
        public void CycleMode()
        {
            if (cueManager != null)
            {
                foreach (var kvp in _holdRepeats)
                    cueManager.DestroyHeld(kvp.Key);
            }
            mode = (mode + 1) % MODE_COUNT;
            _activeRepeats.Clear();
            _pendingCues.Clear();
            _holdRepeats.Clear();
        }

        /// <summary>HOLDモード: 外部からホールド開始（パネルUI用）</summary>
        public void StartHold(string cueName)
        {
            if (!_holdRepeats.ContainsKey(cueName))
                _holdRepeats[cueName] = new RepeatState();

            // BPM非同期時は即スポーン
            if (!_beatStarted && cueManager != null)
                cueManager.SpawnHeld(cueName);
        }

        /// <summary>HOLDモード: 外部からホールド解除（パネルUI用）</summary>
        public void StopHold(string cueName)
        {
            _holdRepeats.Remove(cueName);
            if (cueManager != null)
                cueManager.DestroyHeld(cueName);
        }

        /// <summary>ビート開始/停止トグル（パネルUI用）</summary>
        public void ToggleBeat()
        {
            if (_beatStarted) StopBeat(); else StartBeat();
        }

        /// <summary>
        /// ビート開始
        /// </summary>
        public void StartBeat()
        {
            _beatOrigin = Time.timeAsDouble;
            _beatStarted = true;
            _lastBeatIndex = -1;

            // VJパネルを自動表示
            if (cueManager != null && cueManager.vjController != null)
                cueManager.vjController.showPanel = true;

            try { SpatialBridge.coreGUIService.DisplayToastMessage($"Beat ON (BPM:{bpm:F0})"); } catch { }
#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue BPM] ビート同期ON (BPM: {bpm})");
#endif
        }

        /// <summary>
        /// ビート停止
        /// </summary>
        public void StopBeat()
        {
            _beatStarted = false;
            _activeRepeats.Clear();
            _pendingCues.Clear();

            // ホールド中のVFXを全部消す
            if (cueManager != null)
            {
                foreach (var kvp in _holdRepeats)
                    cueManager.DestroyHeld(kvp.Key);
            }
            _holdRepeats.Clear();

            // VJパネルを自動非表示
            if (cueManager != null && cueManager.vjController != null)
                cueManager.vjController.showPanel = false;

            try { SpatialBridge.coreGUIService.DisplayToastMessage("Beat OFF"); } catch { }
#if UNITY_EDITOR
            Debug.Log("[SpatialVFXCue BPM] ビート同期OFF");
#endif
        }

        /// <summary>
        /// キュー名のリピートをON/OFF切り替え
        /// </summary>
        public void ToggleRepeat(string cueName)
        {
            if (string.IsNullOrEmpty(cueName)) return;

            if (_activeRepeats.ContainsKey(cueName))
            {
                _activeRepeats.Remove(cueName);
                try { SpatialBridge.coreGUIService.DisplayToastMessage("LOOP OFF: " + cueName); } catch { }
            }
            else
            {
                _activeRepeats[cueName] = new RepeatState();
                // 登録直後に即1回発火
                if (cueManager != null)
                    cueManager.TriggerCueByName(cueName, ignoreCooldown: true);
                try { SpatialBridge.coreGUIService.DisplayToastMessage("LOOP ON: " + cueName); } catch { }
            }
        }

        /// <summary>
        /// 拍ごとにアクティブなキューを発動（クールダウン無視）
        /// </summary>
        private void ProcessRepeats()
        {
            if (cueManager == null || _activeRepeats.Count == 0) return;

            int currentBeat = CurrentBeat;

            // 各アクティブキューを拍タイミングで発動
            foreach (var kvp in _activeRepeats)
            {
                if (kvp.Value.lastFiredBeat != currentBeat)
                {
                    kvp.Value.lastFiredBeat = currentBeat;
                    cueManager.TriggerCueByName(kvp.Key, ignoreCooldown: true);
                }
            }
        }

        /// <summary>
        /// HOLD + BPM: 押している間、拍の前半で表示・後半で非表示（点滅）
        /// ParticleSystem の Play/Stop で切替（Spatial ネットワークオブジェクト対応）
        /// </summary>
        private void ProcessHoldRepeats()
        {
            if (cueManager == null || _holdRepeats.Count == 0) return;

            double elapsed = Time.timeAsDouble - _beatOrigin;
            double interval = SecondsPerBeat * beatInterval;
            double posInBeat = (elapsed % interval) / interval; // 0.0〜1.0

            bool shouldBeVisible = posInBeat < 0.5;

            // 状態変化なし → 何もしない
            if (shouldBeVisible == _holdVisibleState) return;
            _holdVisibleState = shouldBeVisible;

            foreach (var kvp in _holdRepeats)
            {
                GameObject obj;
                if (!cueManager.HeldVFX.TryGetValue(kvp.Key, out obj) || obj == null)
                    continue;

                if (shouldBeVisible)
                {
                    // ON: パーティクル再生 + ライト ON
                    foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
                        ps.Play(true);
                    foreach (var light in obj.GetComponentsInChildren<Light>(true))
                        light.enabled = true;
                }
                else
                {
                    // OFF: パーティクル停止＋クリア + ライト OFF
                    foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>(true))
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    foreach (var light in obj.GetComponentsInChildren<Light>(true))
                        light.enabled = false;
                }
            }
        }

        /// <summary>
        /// 単発モード: 次の拍タイミングで1回だけ発動
        /// </summary>
        public void TriggerOnNextBeat(string cueName)
        {
            if (cueManager == null || string.IsNullOrEmpty(cueName)) return;

            // ビート未開始なら即時発動
            if (!_beatStarted)
            {
                cueManager.TriggerCueByName(cueName);
                return;
            }

            double now = Time.timeAsDouble;
            double interval = SecondsPerBeat * beatInterval;
            double elapsed = now - _beatOrigin;
            double currentPos = elapsed % interval;
            double timeToNext = interval - currentPos;

            // 拍のごく近く（10%以内）なら即発動
            if (timeToNext > interval * 0.9)
                timeToNext = 0;

            _pendingCues.Add(new PendingCue
            {
                cueName = cueName,
                targetTime = now + timeToNext
            });

#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue BPM] '{cueName}' → {timeToNext:F3}秒後（次の拍）に発動");
#endif
        }

        /// <summary>
        /// 単発モード: 待機中のキューを時間が来たら発動
        /// </summary>
        private void ProcessPendingCues()
        {
            if (cueManager == null || _pendingCues.Count == 0) return;

            double now = Time.timeAsDouble;
            for (int i = _pendingCues.Count - 1; i >= 0; i--)
            {
                if (now >= _pendingCues[i].targetTime)
                {
                    cueManager.TriggerCueByName(_pendingCues[i].cueName);
                    _pendingCues.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// タップテンポ
        /// </summary>
        public void Tap()
        {
            float now = Time.time;

            if (_tapTimes.Count > 0 && now - _tapTimes[_tapTimes.Count - 1] > TapResetTime)
            {
                _tapTimes.Clear();
            }

            _tapTimes.Add(now);

            while (_tapTimes.Count > MaxTaps)
                _tapTimes.RemoveAt(0);

            if (_tapTimes.Count >= 2)
            {
                float totalInterval = _tapTimes[_tapTimes.Count - 1] - _tapTimes[0];
                float avgInterval = totalInterval / (_tapTimes.Count - 1);
                float detectedBPM = 60f / avgInterval;

                detectedBPM = Mathf.Clamp(detectedBPM, 20f, 300f);
                bpm = Mathf.Round(detectedBPM);

                _beatOrigin = _tapTimes[_tapTimes.Count - 1];
                _beatStarted = true;

                // VJパネルを自動表示
                if (cueManager != null && cueManager.vjController != null)
                    cueManager.vjController.showPanel = true;

                try { SpatialBridge.coreGUIService.DisplayToastMessage("Tap: " + bpm.ToString("F0") + " BPM"); } catch { }
#if UNITY_EDITOR
                Debug.Log($"[SpatialVFXCue BPM] タップテンポ: {bpm} BPM ({_tapTimes.Count} タップ)");
#endif
            }
        }

        private void UpdateBeatFlash()
        {
            if (!_beatStarted) return;

            int beat = BeatInBar;
            int rawBeat = Mathf.FloorToInt((float)((Time.timeAsDouble - _beatOrigin) / SecondsPerBeat));
            if (rawBeat != _lastBeatIndex)
            {
                _lastBeatIndex = rawBeat;
                _flashAlpha = 1f;
            }

            _flashAlpha = Mathf.Max(0f, _flashAlpha - Time.deltaTime * 4f);
        }

        private void UpdateCountdown()
        {
            if (Time.time < _countdownNextTime + 1f) return; // 1秒待ち

            _countdownCurrent--;
            if (_countdownCurrent >= 1)
            {
                _countdownNextTime = Time.time;
                _countdownDisplay = _countdownCurrent.ToString();
                _countdownDisplayAlpha = 1f;
            }
            else
            {
                // GO! → 全VFX一斉発動
                _countdownDisplay = "GO!";
                _countdownDisplayAlpha = 1.5f; // GO!は長めに表示
                _countdownFiring = true;

                if (cueManager != null && cueManager.cues != null)
                {
                    for (int i = 0; i < cueManager.cues.Count; i++)
                    {
                        cueManager.TriggerCueByName(cueManager.cues[i].cueName, ignoreCooldown: true);
                    }
                }

                _countdownRunning = false;
                _countdownFiring = false;
            }
        }

        private void InitGUIStyles()
        {
            if (_guiStylesReady) return;
            _guiStylesReady = true;

            _guiStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, fontStyle = FontStyle.Bold
            };
            _guiActiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };
            _guiCdStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _guiCdShadowStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _guiVjStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.7f, 0.85f, 1f) }
            };
            _guiHelpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(1f, 1f, 1f, 0.5f) }
            };
        }

        private void OnGUI()
        {
            if (!showBeatFlash) return;
            InitGUIStyles();

            float x = 10f;
            float y = 10f;

            // BPM & 状態表示
            _guiStatusStyle.normal.textColor = _beatStarted ? Color.cyan : Color.gray;
            string status = _beatStarted ? "ON" : "OFF";
            GUI.Label(new Rect(x, y, 190f, 25f), "BPM: " + bpm + "  [" + status + "]  " + ModeNames[mode], _guiStatusStyle);
            y += 25f;

            // 拍インジケーター
            if (_beatStarted)
            {
                int beatInBar = BeatInBar;
                for (int i = 0; i < 4; i++)
                {
                    Color c = i == beatInBar ? Color.cyan : new Color(0.3f, 0.3f, 0.3f);
                    if (i == beatInBar) c.a = Mathf.Lerp(0.3f, 1f, _flashAlpha);
                    GUI.color = c;
                    GUI.Box(new Rect(x + i * 46f, y, 40f, 20f), "");
                }
                GUI.color = Color.white;
                y += 26f;
            }

            // アクティブなリピート / ホールド表示
            if (_activeRepeats.Count > 0)
            {
                _guiActiveStyle.normal.textColor = Color.yellow;
                string names = string.Join(", ", _activeRepeats.Keys);
                GUI.Label(new Rect(x, y, 190f, 20f), "LOOP: " + names, _guiActiveStyle);
                y += 20f;
            }
            if (_holdRepeats.Count > 0)
            {
                _guiActiveStyle.normal.textColor = Color.green;
                string names = string.Join(", ", _holdRepeats.Keys);
                GUI.Label(new Rect(x, y, 190f, 20f), "HOLD: " + names, _guiActiveStyle);
                y += 20f;
            }

            // カウントダウン大画面表示（画面中央）
            if (_countdownDisplayAlpha > 0f && _countdownDisplay.Length > 0)
            {
                float alpha = Mathf.Clamp01(_countdownDisplayAlpha);
                bool isGo = _countdownDisplay == "GO!";
                int cdFontSize = isGo ? 64 : 80;

                _guiCdStyle.fontSize = cdFontSize;
                _guiCdStyle.normal.textColor = isGo
                    ? new Color(1f, 0.8f, 0.2f, alpha)
                    : new Color(1f, 1f, 1f, alpha);

                _guiCdShadowStyle.fontSize = cdFontSize;
                _guiCdShadowStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.6f);

                float cdW = 400f;
                float cdH = 150f;
                float cdX = (Screen.width - cdW) * 0.5f;
                float cdY = (Screen.height - cdH) * 0.5f - 50f;

                GUI.Label(new Rect(cdX + 3f, cdY + 3f, cdW, cdH), _countdownDisplay, _guiCdShadowStyle);
                GUI.Label(new Rect(cdX, cdY, cdW, cdH), _countdownDisplay, _guiCdStyle);
            }

            // カウントダウン状態（左側インジケーター）
            if (_countdownRunning)
            {
                _guiActiveStyle.normal.textColor = new Color(1f, 0.5f, 0.2f);
                GUI.Label(new Rect(x, y, 190f, 20f), "COUNTDOWN: " + _countdownCurrent + "...", _guiActiveStyle);
                y += 20f;
            }

            // VJコントローラー情報（ビート有効時）
            if (_beatStarted && cueManager != null && cueManager.vjController != null)
            {
                var vj = cueManager.vjController;
                GUI.Label(new Rect(x, y, 190f, 16f), "INT:" + vj.masterIntensity.ToString("F1") + " SPD:" + vj.masterSpeed.ToString("F1"), _guiVjStyle);
                y += 16f;

                // レイヤー状態
                string layerInfo = "";
                for (int li = 0; li < vj.layers.Count && li < 4; li++)
                {
                    string m = vj.layers[li].mute ? "M" : "";
                    string s = vj.layers[li].solo ? "S" : "";
                    string flag = (m + s).Length > 0 ? "(" + m + s + ")" : "";
                    if (li > 0) layerInfo += " ";
                    layerInfo += vj.layers[li].layerName + flag;
                }
                if (layerInfo.Length > 0)
                {
                    GUI.Label(new Rect(x, y, 250f, 16f), layerInfo, _guiVjStyle);
                    y += 16f;
                }
            }

            // 操作説明
            GUI.Label(new Rect(x, y, 280f, 20f),
                "[Y]Tap [B]Beat [X]Mode [9]Countdown", _guiHelpStyle);
            y += 16f;
            GUI.Label(new Rect(x, y, 280f, 20f),
                "[L]Layer [K]Mute [J]Solo [C]Color [V]BeatCol", _guiHelpStyle);
            y += 16f;
            GUI.Label(new Rect(x, y, 280f, 20f),
                "[N]Select [P]SavePreset [Z]RecallPreset", _guiHelpStyle);
        }
    }
}
