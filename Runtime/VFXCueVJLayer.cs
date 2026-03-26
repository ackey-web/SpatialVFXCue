using System;
using UnityEngine;

namespace SpatialVFXCue
{
    [Serializable]
    public class VFXCueVJLayer
    {
        [Tooltip("レイヤー名")]
        public string layerName = "Layer";

        [Tooltip("レイヤー識別カラー（GUI用）")]
        public Color layerColor = Color.white;

        [Tooltip("インテンシティ倍率")]
        [Range(0f, 2f)]
        public float intensity = 1f;

        [Tooltip("スピード倍率")]
        [Range(0.1f, 3f)]
        public float speedMultiplier = 1f;

        [Tooltip("ミュート（このレイヤーのVFXを無効化）")]
        public bool mute;

        [Tooltip("ソロ（このレイヤーのみ有効化）")]
        public bool solo;

        [Tooltip("カラーティント")]
        public Color colorTint = Color.white;
    }
}
