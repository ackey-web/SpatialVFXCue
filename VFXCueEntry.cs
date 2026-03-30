using System;
using UnityEngine;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    [Serializable]
    public class VFXCueEntry
    {
        [Tooltip("トリガーキー（Alpha1〜Alpha6 など）")]
        public KeyCode triggerKey = KeyCode.Alpha1;

        [Tooltip("スポーンする VFX Prefab（SpatialNetworkObject 必須）")]
        public SpatialNetworkObject vfxPrefab;

        [Tooltip("キュー表示名")]
        public string cueName = "VFX";

        [Tooltip("位置基準 Transform（null なら worldPosition を使用）")]
        public Transform spawnAnchor;

        [Tooltip("spawnAnchor が null の場合のワールド座標")]
        public Vector3 worldPosition = Vector3.zero;

        [Tooltip("spawnAnchor からのオフセット")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("スポーン時の回転（オイラー角）")]
        public Vector3 rotationEuler = Vector3.zero;

        [Tooltip("自動消滅までの秒数")]
        [Min(0.1f)]
        public float autoDestroyTime = 5f;

        [Tooltip("連打防止クールダウン（秒）")]
        [Min(0f)]
        public float cooldown = 0.5f;

        [Tooltip("VJレイヤーインデックス")]
        [Range(0, 7)]
        public int layerIndex = 0;

        // ランタイム用：最後にトリガーした時刻
        [NonSerialized]
        public float lastTriggerTime = -999f;

        /// <summary>
        /// スポーン位置を計算する
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            if (spawnAnchor != null)
                return spawnAnchor.position + spawnAnchor.TransformDirection(positionOffset);
            return worldPosition + positionOffset;
        }

        /// <summary>
        /// スポーン回転を計算する
        /// </summary>
        public Quaternion GetSpawnRotation()
        {
            Quaternion baseRotation = spawnAnchor != null ? spawnAnchor.rotation : Quaternion.identity;
            return baseRotation * Quaternion.Euler(rotationEuler);
        }

        /// <summary>
        /// クールダウン中かチェック
        /// </summary>
        public bool IsOnCooldown()
        {
            return Time.time - lastTriggerTime < cooldown;
        }
    }
}
