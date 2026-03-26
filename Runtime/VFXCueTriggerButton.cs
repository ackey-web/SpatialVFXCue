using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    /// <summary>
    /// 空間内に設置するインタラクティブ VFX トリガーボタン。
    /// SpatialInteractable と組み合わせて、来場者がクリック/Fキーで VFX を発動できる。
    ///
    /// 使い方:
    /// 1. 3D オブジェクト（キューブ等）をシーンに配置
    /// 2. SpatialInteractable を追加
    /// 3. VFXCueTriggerButton を追加
    /// 4. Inspector で VFXCueManager と発動するキュー名を設定
    /// </summary>
    public class VFXCueTriggerButton : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("シーン内の VFXCueManager")]
        public VFXCueManager cueManager;

        [Header("発動設定")]
        [Tooltip("発動するキュー名（VFXCueManager で設定した名前）")]
        public string cueName;

        [Tooltip("ボタンのクールダウン（秒）")]
        [Min(0f)]
        public float buttonCooldown = 2f;

        [Header("フィードバック")]
        [Tooltip("押下時にスケールアニメーションする")]
        public bool animateOnPress = true;

        private SpatialInteractable _interactable;
        private float _lastPressTime = -999f;
        private Vector3 _originalScale;
        private float _animTimer;
        private bool _isAnimating;

        private void Start()
        {
            _interactable = GetComponent<SpatialInteractable>();
            _originalScale = transform.localScale;

            if (_interactable != null && _interactable.onInteractEvent != null)
            {
                _interactable.onInteractEvent += OnInteract;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SpatialVFXCue] '{gameObject.name}' に SpatialInteractable がありません");
#endif
            }

            if (cueManager == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[SpatialVFXCue Button] cueManager が未設定です。Inspector で VFXCueManager を設定してください。");
#endif
            }
        }

        private void OnDestroy()
        {
            if (_interactable != null && _interactable.onInteractEvent != null)
            {
                _interactable.onInteractEvent -= OnInteract;
            }
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _animTimer += Time.deltaTime;
            float t = _animTimer / 0.3f;

            if (t >= 1f)
            {
                transform.localScale = _originalScale;
                _isAnimating = false;
            }
            else
            {
                // バウンスアニメーション
                float bounce = 1f - 0.2f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = _originalScale * bounce;
            }
        }

        private void OnInteract()
        {
            if (Time.time - _lastPressTime < buttonCooldown) return;
            if (cueManager == null) return;
            if (string.IsNullOrEmpty(cueName)) return;

            _lastPressTime = Time.time;
            cueManager.TriggerCueByName(cueName);

            if (animateOnPress)
            {
                _animTimer = 0f;
                _isAnimating = true;
            }

#if UNITY_EDITOR
            Debug.Log($"[SpatialVFXCue] ボタン '{gameObject.name}' → '{cueName}' を発動");
#endif
        }
    }
}
