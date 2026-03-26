using System.Collections.Generic;
using UnityEngine;

namespace SpatialVFXCue
{
    /// <summary>
    /// ランダム VFX ルーレット。
    /// ボタンを押す or API を呼ぶと、プールからランダムに 1 つの VFX を発動する。
    /// ガチャ感覚で来場者が楽しめる。
    ///
    /// 使い方:
    /// 1. シーンに VFXCueRandom を配置
    /// 2. Inspector で cueNames にキュー名を追加
    /// 3. キーまたは VFXCueTriggerButton 経由で発動
    /// </summary>
    public class VFXCueRandom : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("シーン内の VFXCueManager")]
        public VFXCueManager cueManager;

        [Header("ランダム設定")]
        [Tooltip("ランダム候補のキュー名リスト")]
        public List<string> cueNames = new List<string>();

        [Tooltip("発動キー（None で無効）")]
        public KeyCode triggerKey = KeyCode.Alpha8;

        [Tooltip("クールダウン（秒）")]
        [Min(0f)]
        public float cooldown = 1f;

        [Tooltip("同じ VFX が連続しないようにする")]
        public bool avoidRepeat = true;

        private float _lastTriggerTime = -999f;
        private int _lastIndex = -1;

        private void Start()
        {
#if UNITY_EDITOR
            if (cueManager == null)
                Debug.LogWarning("[SpatialVFXCue Random] cueManager が未設定です。Inspector で VFXCueManager を設定してください。");
#endif
        }

        private void Update()
        {
            if (triggerKey != KeyCode.None && Input.GetKeyDown(triggerKey))
            {
                TriggerRandom();
            }
        }

        /// <summary>
        /// ランダムに VFX を 1 つ発動する（外部 API）
        /// </summary>
        public void TriggerRandom()
        {
            if (cueManager == null || cueNames.Count == 0) return;
            if (Time.time - _lastTriggerTime < cooldown) return;

            _lastTriggerTime = Time.time;

            int index;
            if (cueNames.Count == 1)
            {
                index = 0;
            }
            else if (avoidRepeat)
            {
                // 前回と違うインデックスを選ぶ
                do { index = Random.Range(0, cueNames.Count); }
                while (index == _lastIndex);
            }
            else
            {
                index = Random.Range(0, cueNames.Count);
            }

            _lastIndex = index;
            string selected = cueNames[index];
            cueManager.TriggerCueByName(selected);
#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue] ランダム発動 → '{selected}'");
#endif
        }
    }
}
