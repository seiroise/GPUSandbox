using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Seiro.GPUSandbox
{

    public sealed class FPSIndicator : MonoBehaviour
    {
        public Text fps;

        private static readonly int MAX_SAMPLE_COUNT = 32;
        private float[] _samples = new float[MAX_SAMPLE_COUNT];
        private int _sampleIndex = 0;
        private int _sampleCount = 0;

        // private float _sum;
        // private float _min;
        // private float _max;

        private void Update()
        {
            if (fps == null) return;

            _samples[_sampleIndex++] = Time.deltaTime;
            if (_sampleCount < MAX_SAMPLE_COUNT) _sampleCount++;
            if (_sampleIndex >= MAX_SAMPLE_COUNT) _sampleIndex = 0;

            float avg = 0f;
            for (int i = 0; i < _sampleCount; ++i)
            {
                avg += _samples[i];
            }
            avg /= _sampleCount;
            float avgFps = 1f / avg;
            fps.text = avgFps.ToString("00.0");
        }
    }
}
