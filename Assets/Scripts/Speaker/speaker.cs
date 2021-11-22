// Copyright 2017 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class speaker : MonoBehaviour {

  public float volume = 1;
  public signalGenerator incoming;
  private AudioSource audioSource;
  private float[] buffer = new float[1024];

  //[DllImport("__Internal")]
  //public static extern void MultiplyArrayBySingleValue(float[] buffer, int length, float val);
  [DllImport("__Internal")] public static extern void CreateBuffer();
  [DllImport("__Internal")] public static extern void UpdateBuffer(float[] buffer, int bufferLength);

  private void Awake() {
    CreateBuffer();
    InvokeRepeating("AudioUpdate", 0, 0.023f); // 1024 / 44100
  }

  private void AudioUpdate() {
    if (incoming == null) return;
    double dspTime = AudioSettings.dspTime;
    incoming.processBuffer(buffer, dspTime, 2);
    if (volume != 1) SoundStageNative.MultiplyArrayBySingleValue(buffer, buffer.Length, volume);
    UpdateBuffer(buffer, buffer.Length);
  }

  // private void OnAudioFilterRead(float[] buffer, int channels) {
  //   Debug.Log(buffer.Length);
  //   if (incoming == null) return;
  //   double dspTime = AudioSettings.dspTime;
  //   incoming.processBuffer(buffer, dspTime, channels);
  //   if (volume != 1) SoundStageNative.MultiplyArrayBySingleValue(buffer, buffer.Length, volume);
  // }
}
