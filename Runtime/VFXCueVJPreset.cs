using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialVFXCue
{
    [Serializable]
    public class VFXCueVJPreset
    {
        public string presetName = "";

        [Serializable]
        public class CueState
        {
            public string cueName;
            public bool active;
            public float intensity = 1f;
            public float speed = 1f;
            public Color colorTint = Color.white;
        }

        [Serializable]
        public class LayerState
        {
            public int layerIndex;
            public float intensity = 1f;
            public float speedMultiplier = 1f;
            public bool mute;
            public bool solo;
            public Color colorTint = Color.white;
        }

        public List<CueState> cueStates = new List<CueState>();
        public List<LayerState> layerStates = new List<LayerState>();
        public float masterIntensity = 1f;
        public float masterSpeed = 1f;
        public Color masterColorTint = Color.white;
    }
}
