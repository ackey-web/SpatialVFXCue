using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialVFXCue
{
    /// <summary>
    /// 複数の VFX キューを時間差で自動連鎖発動するチェーン演出。
    /// セットリスト的な演出シーケンスを組める。
    ///
    /// 使い方:
    /// 1. シーンに VFXCueChain を配置
    /// 2. Inspector で chainSteps を設定（キュー名 + 遅延秒数）
    /// 3. キーボードまたは外部から StartChain() を呼ぶ
    /// </summary>
    public class VFXCueChain : MonoBehaviour
    {
        [Serializable]
        public class ChainStep
        {
            [Tooltip("発動するキュー名")]
            public string cueName;

            [Tooltip("前のステップからの遅延秒数")]
            [Min(0f)]
            public float delay = 2f;
        }

        [Header("参照")]
        [Tooltip("シーン内の VFXCueManager")]
        public VFXCueManager cueManager;

        [Header("チェーン設定")]
        [Tooltip("チェーンのステップ一覧（順番に実行）")]
        public List<ChainStep> chainSteps = new List<ChainStep>();

        [Tooltip("チェーン発動キー（None で無効）")]
        public KeyCode triggerKey = KeyCode.Alpha7;

        [Tooltip("ループ再生する")]
        public bool loop = false;

        private Coroutine _chainCoroutine;
        private bool _isRunning;

        /// <summary>
        /// チェーンが実行中かどうか
        /// </summary>
        public bool IsRunning => _isRunning;

        private void Start()
        {
            try
            {
                // デフォルトチェーンが空なら初期設定
                if (chainSteps == null || chainSteps.Count == 0)
                {
                    chainSteps = new List<ChainStep>
                    {
                        new ChainStep { cueName = "Fireworks", delay = 0f },
                        new ChainStep { cueName = "Confetti", delay = 2f },
                        new ChainStep { cueName = "MeteorShower", delay = 2f },
                        new ChainStep { cueName = "SpotlightBeam", delay = 2f },
                        new ChainStep { cueName = "SmokeBurst", delay = 2f },
                        new ChainStep { cueName = "SparkleRain", delay = 2f },
                    };
                }
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SpatialVFXCue Chain] Start error: {e.Message}");
#endif
            }
        }

        private void Update()
        {
            if (triggerKey != KeyCode.None && Input.GetKeyDown(triggerKey))
            {
                if (_isRunning)
                    StopChain();
                else
                    StartChain();
            }
        }

        /// <summary>
        /// チェーン演出を開始する（外部 API）
        /// </summary>
        public void StartChain()
        {
            if (_isRunning) return;
            if (cueManager == null || chainSteps.Count == 0) return;

            _chainCoroutine = StartCoroutine(ChainCoroutine());
        }

        /// <summary>
        /// チェーン演出を停止する
        /// </summary>
        public void StopChain()
        {
            if (_chainCoroutine != null)
            {
                StopCoroutine(_chainCoroutine);
                _chainCoroutine = null;
            }
            _isRunning = false;
        }

        private IEnumerator ChainCoroutine()
        {
            _isRunning = true;
#if UNITY_EDITOR
            Debug.Log("[SpatialVFXCue] チェーン演出を開始");
#endif

            do
            {
                for (int i = 0; i < chainSteps.Count; i++)
                {
                    ChainStep step = chainSteps[i];

                    if (step.delay > 0f)
                        yield return new WaitForSeconds(step.delay);

                    cueManager.TriggerCueByName(step.cueName, ignoreCooldown: true);
#if UNITY_EDITOR
                    Debug.Log($"[SpatialVFXCue] チェーン [{i + 1}/{chainSteps.Count}] '{step.cueName}'");
#endif
                }
            }
            while (loop);

            _isRunning = false;
            _chainCoroutine = null;
#if UNITY_EDITOR
            Debug.Log("[SpatialVFXCue] チェーン演出が完了");
#endif
        }
    }
}
