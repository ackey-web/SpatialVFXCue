using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// VJコントローラーパネル — 画面下部にグラフィカル表示。
    /// GUI.Label / GUI.Box / GUI.color のみ使用（Texture2D / GUI.Button 不使用）。
    /// BPMシンク有効時に自動表示。
    /// </summary>
    public class VFXCueVJPanel : MonoBehaviour
    {
        [Tooltip("VJコントローラー参照")]
        public VFXCueVJController vjController;

        [Tooltip("BPMSync 参照")]
        public VFXCueBPMSync bpmSync;

        private VFXCueBPMSync _bpm;
        private GUIStyle _sTitle;
        private GUIStyle _sInfo;
        private GUIStyle _sVal;
        private GUIStyle _sPad;
        private GUIStyle _sPadOn;
        private bool _stylesReady;

        private const float PanelH = 150f;
        private const float PadSize = 42f;
        private const float PadGap = 3f;
        private const float FaderW = 22f;
        private const float FaderH = 80f;

        private void Start()
        {
            try { if (bpmSync != null) _bpm = bpmSync; } catch { }
        }

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _sTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.7f, 1f) }
            };
            _sInfo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
            };
            _sVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _sPad = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.7f) }, wordWrap = true
            };
            _sPadOn = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }, wordWrap = true
            };
        }

        private void OnGUI()
        {
            try
            {
                if (vjController == null) return;
                if (_bpm == null && bpmSync != null) _bpm = bpmSync;
                if (_bpm == null && vjController.cueManager != null)
                    _bpm = vjController.cueManager.bpmSync;
                if (_bpm == null || !_bpm.IsBeatStarted) return;

                InitStyles();

                float pw = Screen.width;
                float panelY = Screen.height - PanelH;

                // 背景
                Color prev = GUI.color;
                GUI.color = new Color(0.06f, 0.06f, 0.1f, 0.88f);
                GUI.Box(new Rect(0, panelY, pw, PanelH), "");
                GUI.color = prev;

                float x = 8f;
                float topY = panelY + 4f;

                // === CUE PADS ===
                try { x = DrawCuePads(x, topY); } catch { x += 160f; }
                x += 10f;

                // === MASTER フェーダー ===
                try { x = DrawMasterFaders(x, topY); } catch { x += 80f; }
                x += 10f;

                // === LAYER フェーダー ===
                try { x = DrawLayerFaders(x, topY); } catch { x += 200f; }

            }
            catch { }
        }

        // ================================================================
        // CUE PADS — 3x2 グリッド。クリックで発動。戻り値: 次の x 位置
        // ================================================================
        private float DrawCuePads(float ox, float oy)
        {
            GUI.Label(new Rect(ox, oy, 100f, 14f), "CUE PADS", _sTitle);
            oy += 15f;

            var mgr = vjController.cueManager;
            if (mgr == null || mgr.cues == null) return ox + 140f;

            int count = Mathf.Min(mgr.cues.Count, 6);
            for (int i = 0; i < count; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float bx = ox + col * (PadSize + PadGap);
                float by = oy + row * (PadSize + PadGap);
                Rect r = new Rect(bx, by, PadSize, PadSize);

                var cue = mgr.cues[i];
                if (cue == null) continue;

                bool held = false;
                try { held = mgr.IsHeld(cue.cueName); } catch { }
                bool sel = (i == vjController.selectedCueIndex);

                // パッド背景
                Color prev = GUI.color;
                if (sel)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.7f);
                    GUI.Box(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), "");
                }
                GUI.color = held ? new Color(0.2f, 0.5f, 1f, 0.8f) : new Color(0.15f, 0.15f, 0.22f, 1f);
                GUI.Box(r, "");
                GUI.color = prev;

                string label = (i + 1) + "\n" + Trunc(cue.cueName, 6);
                GUI.Label(r, label, held ? _sPadOn : _sPad);

                // クリック
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    vjController.TriggerCueByIndex(i);
                    Event.current.Use();
                }
            }

            return ox + 3 * (PadSize + PadGap);
        }

        // ================================================================
        // MASTER フェーダー (INT / SPD)
        // ================================================================
        private float DrawMasterFaders(float ox, float oy)
        {
            GUI.Label(new Rect(ox, oy, 80f, 14f), "MASTER", _sTitle);
            oy += 15f;

            var vj = vjController;
            float x = ox;

            // INT
            DrawFader(x, oy, "INT", vj.masterIntensity, 0f, 2f, new Color(0.3f, 0.7f, 1f), false,
                delegate(float v) { vj.SetMasterIntensity(v); });
            x += FaderW + 12f;

            // SPD
            DrawFader(x, oy, "SPD", vj.masterSpeed, 0.1f, 3f, new Color(1f, 0.6f, 0.3f), false,
                delegate(float v) { vj.SetMasterSpeed(v); });
            x += FaderW + 6f;

            return x;
        }

        // ================================================================
        // LAYER フェーダー (各レイヤーの intensity + M/S 表示)
        // ================================================================
        private float DrawLayerFaders(float ox, float oy)
        {
            GUI.Label(new Rect(ox, oy, 100f, 14f), "LAYER", _sTitle);
            oy += 15f;

            var vj = vjController;
            if (vj == null) return ox;
            if (vj.layers == null) return ox;
            int count = vj.layers.Count;
            if (count == 0) return ox;
            if (count > 4) count = 4;

            float x = ox;
            int selIdx = vj.selectedLayerIndex;

            for (int i = 0; i < count; i++)
            {
                var layer = vj.layers[i];
                if (layer == null) { x += FaderW + 18f; continue; }

                bool muted = false;
                try { muted = vj.IsLayerMuted(i); } catch { }
                bool isSel = (i == selIdx);

                Color col = muted ? new Color(0.3f, 0.3f, 0.3f) : layer.layerColor;
                string name = (layer.layerName != null) ? Trunc(layer.layerName, 6) : "L" + i;

                // 選択マーカー
                if (isSel)
                {
                    Color prev = GUI.color;
                    GUI.color = new Color(1f, 1f, 0.5f, 0.3f);
                    GUI.Box(new Rect(x - 2, oy - 2, FaderW + 4f, FaderH + 34f), "");
                    GUI.color = prev;
                }

                // フェーダー描画（クリック対応）
                int captureI = i;
                DrawFader(x, oy, name, layer.intensity, 0f, 2f, col, muted,
                    delegate(float v) { vj.SetLayerIntensity(captureI, v); });

                // M/S 表示
                float flagY = oy + FaderH + 18f;
                string flags = "";
                if (layer.mute) flags += "M";
                if (layer.solo) flags += "S";
                if (flags.Length > 0)
                    GUI.Label(new Rect(x, flagY, FaderW, 12f), flags, _sInfo);

                x += FaderW + 18f;
            }

            return x;
        }

        // ================================================================
        // 汎用フェーダー描画（GUI.Box のみ、GUI.Button 不使用）
        // ================================================================
        private delegate void FloatSetter(float v);

        private void DrawFader(float fx, float fy, string label, float value,
            float min, float max, Color fillColor, bool dimmed, FloatSetter setter)
        {
            // ラベル（幅を広げて中央揃え）
            float labelW = 56f;
            GUI.Label(new Rect(fx - (labelW - FaderW) * 0.5f, fy, labelW, 12f), label, _sInfo);
            fy += 13f;

            // トラック背景
            Rect track = new Rect(fx, fy, FaderW, FaderH);
            Color prev = GUI.color;
            GUI.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            GUI.Box(track, "");

            // フィル
            float norm = Mathf.InverseLerp(min, max, value);
            float fillH = FaderH * norm;
            if (fillH > 1f)
            {
                GUI.color = dimmed ? new Color(0.25f, 0.25f, 0.25f) : fillColor;
                GUI.Box(new Rect(track.x, track.yMax - fillH, FaderW, fillH), "");
            }

            // ノブ
            GUI.color = Color.white;
            float knobY = Mathf.Lerp(track.yMax, track.y, norm);
            GUI.Box(new Rect(track.x - 2f, knobY - 2f, FaderW + 4f, 4f), "");
            GUI.color = prev;

            // 値表示
            GUI.Label(new Rect(fx - (labelW - FaderW) * 0.5f, fy + FaderH + 1f, labelW, 14f),
                value.ToString("F1"), _sVal);

            // クリックで値変更
            if (Event.current.type == EventType.MouseDown && track.Contains(Event.current.mousePosition))
            {
                float n = 1f - (Event.current.mousePosition.y - track.y) / track.height;
                setter(Mathf.Lerp(min, max, Mathf.Clamp01(n)));
                Event.current.Use();
            }
        }

        // ================================================================
        // ヘルパー
        // ================================================================
        private string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
