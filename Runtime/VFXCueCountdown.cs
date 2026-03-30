using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// カウントダウン演出。
    /// 3... 2... 1... の後に全 VFX を一斉発動するクライマックス演出。
    /// Spatialトースト通知でカウントダウンを表示し、VFX を一斉発動。
    ///
    /// 使い方:
    /// 1. シーンに VFXCueCountdown を配置
    /// 2. Inspector で countdownFrom, climaxCueNames を設定
    /// 3. キー9 または API で StartCountdown() を呼ぶ
    /// </summary>
    public class VFXCueCountdown : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("シーン内の VFXCueManager")]
        public VFXCueManager cueManager;

        [Header("カウントダウン設定")]
        [Tooltip("カウントダウン開始数字")]
        [Min(1)]
        public int countdownFrom = 3;

        [Tooltip("各カウント間の秒数")]
        [Min(0.5f)]
        public float intervalSeconds = 1f;

        [Tooltip("カウントダウン後に発動するキュー名リスト（全て同時発動）")]
        public List<string> climaxCueNames = new List<string>();

        [Tooltip("クライマックスで各キューの発動間隔（0で同時）")]
        [Min(0f)]
        public float climaxStagger = 0.15f;

        [Header("発動")]
        [Tooltip("発動キー（None で無効）")]
        public KeyCode triggerKey = KeyCode.Alpha9;

        private Coroutine _countdownCoroutine;
        private bool _isRunning;

        /// <summary>
        /// カウントダウンが実行中か
        /// </summary>
        public bool IsRunning => _isRunning;

        private void Start()
        {
            // Start() では何もしない（Spatialサンドボックスでのクラッシュ防止）
            // テキストオブジェクトはカウントダウン開始時に遅延生成
        }

        private void Update()
        {
            if (triggerKey != KeyCode.None && Input.GetKeyDown(triggerKey))
            {
                if (!_isRunning)
                    StartCountdown();
            }
        }

        /// <summary>
        /// カウントダウンを開始する（外部 API）
        /// </summary>
        public void StartCountdown()
        {
            if (_isRunning) return;
            if (cueManager == null) return;

            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }

        /// <summary>
        /// カウントダウンを中止する
        /// </summary>
        public void CancelCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
            _isRunning = false;
        }

        private IEnumerator CountdownCoroutine()
        {
            _isRunning = true;

            // カウントダウン（トースト表示のみ、3Dテキストなし）
            for (int i = countdownFrom; i >= 1; i--)
            {
                try { SpatialBridge.coreGUIService.DisplayToastMessage($"{i}..."); } catch { }
                yield return new WaitForSeconds(intervalSeconds);
            }

            // GO!
            try { SpatialBridge.coreGUIService.DisplayToastMessage("GO!"); } catch { }

            // クライマックス VFX 一斉発動
            int fired = 0;
            for (int i = 0; i < climaxCueNames.Count; i++)
            {
                cueManager.TriggerCueByName(climaxCueNames[i], ignoreCooldown: true);
                fired++;

                if (climaxStagger > 0f && i < climaxCueNames.Count - 1)
                    yield return new WaitForSeconds(climaxStagger);
            }

            try { SpatialBridge.coreGUIService.DisplayToastMessage($"Fired {fired} VFX!"); } catch { }

            yield return new WaitForSeconds(2f);
            _isRunning = false;
            _countdownCoroutine = null;
        }
    }
}
