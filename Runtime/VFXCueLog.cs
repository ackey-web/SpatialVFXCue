using System.Collections.Generic;
using UnityEngine;

namespace SpatialVFXCue
{
    /// <summary>
    /// VFX キュー発動のランタイムログを画面に表示する（オプション）。
    /// VFXCueManager と同じ GameObject にアタッチして使用。
    /// </summary>
    public class VFXCueLog : MonoBehaviour
    {
        [Tooltip("ログの最大表示件数")]
        [SerializeField] private int maxLogCount = 8;

        [Tooltip("各ログの表示秒数")]
        [SerializeField] private float logDisplayTime = 4f;

        [Tooltip("ログ表示を有効にする")]
        [SerializeField] private bool showLog = true;

        private struct LogEntry
        {
            public string message;
            public float timestamp;
        }

        private readonly List<LogEntry> _logs = new List<LogEntry>();

        /// <summary>
        /// ログを追加する（VFXCueManager から呼ばれる）
        /// </summary>
        public void AddLog(string cueName, KeyCode key)
        {
            if (!showLog) return;

            string msg = $"[{key}] {cueName}";
            _logs.Add(new LogEntry { message = msg, timestamp = Time.time });

            // 上限超過分を削除
            while (_logs.Count > maxLogCount)
            {
                _logs.RemoveAt(0);
            }
        }

        private void OnGUI()
        {
#if UNITY_EDITOR
            if (!showLog || _logs.Count == 0) return;

            float now = Time.time;

            // 期限切れログを除去
            _logs.RemoveAll(e => now - e.timestamp > logDisplayTime);

            // 左下に表示
            float y = Screen.height - 40f;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            for (int i = _logs.Count - 1; i >= 0; i--)
            {
                float alpha = Mathf.Clamp01(1f - (now - _logs[i].timestamp) / logDisplayTime);
                Color c = style.normal.textColor;
                c.a = alpha;
                style.normal.textColor = c;

                GUI.Label(new Rect(20f, y, 400f, 24f), _logs[i].message, style);
                y -= 24f;
            }
#endif
        }
    }
}
