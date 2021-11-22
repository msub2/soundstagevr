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

using System;
using UnityEngine;

class SoundStageNative
{
	// Filter
	public struct FilterData
	{
		public float f, p, q; //filter coefficients
		public float b0, b1, b2, b3, b4; //filter buffers (beware denormals!)
		public bool LP;
	};

	public static unsafe float ProcessSample(ref FilterData fd, float sample)
	{
		float input = sample - fd.q * fd.b4;//feedback

		float t1 = fd.b1;
		fd.b1 = (input + fd.b0) * fd.p - fd.b1 * fd.f;
		float t2 = fd.b2; fd.b2 = (fd.b1 + t1) * fd.p - fd.b2 * fd.f;
		t1 = fd.b3;
		fd.b3 = (fd.b2 + t2) * fd.p - fd.b3 * fd.f;
		fd.b4 = (fd.b3 + t1) * fd.p - fd.b4 * fd.f;
		fd.b4 = fd.b4 - fd.b4 * fd.b4 * fd.b4 * 0.166667f; //clipping
		fd.b0 = input;

		if (fd.LP) return fd.b4;
		else return input - fd.b4;
	}

	public static unsafe void processStereoFilter(float[] buffer, int length, ref FilterData mfA, ref FilterData mfB)
	{
		for (int i = 0; i < length; i += 2)
		{
			buffer[i] = ProcessSample(ref mfA, buffer[i]);
			buffer[i + 1] = ProcessSample(ref mfB, buffer[i + 1]);
		}
	}

	// Main
	static float PI = 3.14159265f;
	static System.Random rand = new System.Random();

	public static float lerp(float a, float b, float f)
	{
		return (a * (1.0f - f)) + (b * f);
	}

	public static void SetArrayToFixedValue(float[] buf, int length, float value)
	{
		for (int i = 0; i < length; ++i)
			buf[i] = value;
	}

	public static void SetArrayToSingleValue(float[] a, int length, float val)
	{
		for (int i = 0; i < length; ++i)
			a[i] = val;
	}

	public static void MultiplyArrayBySingleValue(float[] buffer, int length, float val)
	{
		for (int i = 0; i < length; ++i) buffer[i] *= val;
	}

	public static void AddArrays(float[] a, float[] b, int length)
	{
		for (int i = 0; i < length; ++i)
		{
			a[i] += b[i];
		}
	}

	public static void CopyArray(float[] from, float[] to, int length)
	{
		for (int i = 0; i < length; ++i)
		{
			to[i] = from[i];
		}
	}

	public static void DuplicateArrayAndReset(float[] from, float[] to, int length, float val)
	{
		for (int i = 0; i < length; ++i)
		{
			to[i] = from[i] * val;
			from[i] = 0;
		}
	}

	public static int CountPulses(float[] buffer, int length, int channels, float[] lastSig)
	{
		int hits = 0;
		for (int i = 0; i < length; i += channels)
		{
			if (buffer[i] > lastSig[1] && lastSig[1] <= lastSig[0])
			{
				hits++;
			}

			lastSig[0] = lastSig[1];
			lastSig[1] = buffer[i];
		}
		return hits;
	}

	public static void MaracaProcessBuffer(float[] buffer, int length, int channels, float amp, ref double _phase, double _sampleDuration)
	{
		for (int i = 0; i < length; i += channels)
		{
			buffer[i] = buffer[i + 1] = ((amp * Mathf.Sin((float)_phase * 2 * PI)) - .5f) * 2; ;
			_phase += amp * 16 * _sampleDuration;
		}
	}

	public static void MaracaProcessAudioBuffer(float[] buffer, float[] controlBuffer, int length, int channels, ref double _phase, double _sampleDuration)
	{
		for (int i = 0; i < length; i += channels)
		{
			float sample = Mathf.Sin((float)_phase * 2 * PI);
			buffer[i] = buffer[i + 1] = ((controlBuffer[i] + 1) / 2.0f) * sample;

			float endFrequency = 200.0f + ((controlBuffer[i] + 1) / 2.0f) * 300.0f;
			_phase += endFrequency * _sampleDuration;
			if (_phase > 1.0) _phase -= 1.0;
		}
	}

	public static void processFader(float[] buffer, int length, int channels, float[] bufferB, int lengthB, bool aSig, bool bSig, bool samePercent, float lastpercent, float sliderPercent)
	{
		float p = sliderPercent;
		if (aSig && bSig)
		{
			//if (bufferB.Length != buffer.Length)
			//System.Array.Resize(ref bufferB, buffer.Length);

			//SetArrayToSingleValue(bufferB, bufferB.Length, 0.0f);

			for (int i = 0; i < length; i += channels)
			{
				if (!samePercent) p = lerp(lastpercent, sliderPercent, (float)i / length);// Mathf.Lerp(lastpercent, sliderPercent, (float)i / buffer.Length);

				buffer[i] = lerp(buffer[i], bufferB[i], p);
				buffer[i + 1] = lerp(buffer[i + 1], bufferB[i + 1], p);
			}
		}
		else
		{
			float modA = 0;
			float modB = 1;

			if (aSig)
			{

				p = 1 - p;
				modA = 1;
				modB = -1;
				//incomingA.processBuffer(buffer, dspTime, channels);
			}
			//else
			//{
			//	incomingB.processBuffer(buffer, dspTime, channels);
			//}
			for (int i = 0; i < length; i += channels)
			{
				if (!samePercent) p = lerp(lastpercent, sliderPercent, (float)i / length) * modB + modA;
				buffer[i] *= p;
				buffer[i + 1] *= p;
			}
		}
	}

	public static bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf)
	{
		bool on = false;
		for (int i = 0; i < length; i += channels)
		{
			if (lastBuf == buffer[i] && buffer[i] < 0) on = false;
			else on = true;
			lastBuf = buffer[i];
		}

		return on;
	}

	public static bool IsPulse(float[] buffer, int length)
	{
		if (buffer[0] == -1 && buffer[1] == -1)
		{
			for (int i = 2; i < length; ++i)
			{
				if (buffer[i] == -1) return false;
			}
			return true;
		}
		else return false;
	}

	public static void CompressClip(float[] buffer, int length)
	{
		float maxVal = 0;
		for (int i = 0; i < length; i += 2)
		{
			if (buffer[i] > maxVal) maxVal = buffer[i];
		}

		if (maxVal <= 1) return;

		float mod = 1.0f / maxVal;
		for (int i = 0; i < length; ++i)
		{
			buffer[i] *= mod;
		}
	}

	public static void MicFunction(float[] a, float[] b, int length, float val)
	{
		for (int i = 0; i < length; i += 2)
		{
			a[i] = a[i + 1] = b[i] * val;
		}
	}

	public static float LoganTest()
	{
		return 1.31f;
	}

	public static void ColorTest(char[] a)
	{
		a[5] = (char)128;
	}

	public static void GateProcessBuffer(float[] buffer, int length, int channels, bool incoming, float[] controlBuffer, bool bControlSig, float amp)
	{
		if (!incoming)
		{
			//float endAmp = 4 * amp - 2;//(amp - .5f) * 2;
			if (!bControlSig)
			{
				float endAmp = 4 * amp - 2;
				for (int i = 0; i < length; i++) buffer[i] = endAmp;// endAmp;
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					buffer[i] = amp * 2 * (controlBuffer[i] + 1) - 1.0f;
				}
			}
		}
		else
		{
			float endAmp = amp * 2;
			if (!bControlSig)
			{

				for (int i = 0; i < length; i++)
				{
					buffer[i] *= endAmp;
				}
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					//buffer[i] = ((controlBuffer[i] + 1) / 2.0f) * ((buffer[i] + 1) / 2.0f) * endAmp;
					//buffer[i] = .25f * (controlBuffer[i] + 1) * (buffer[i] + 1) * endAmp;
					buffer[i] = .5f * (controlBuffer[i] + 1) * (buffer[i] + 1) * endAmp - 1.0f;

				}

			}
		}
		/*
		if (incoming)
		{
			if (!bControlSig)
			{
				float endAmp = amp * 2;
				for (int i = 0; i < length; i += channels)
				{
					buffer[i] *= endAmp;
					buffer[i + 1] *= endAmp;
				}
			}
			else
			{
				float endAmp;
				for (int i = 0; i < length; i += channels)
				{
					endAmp = amp * 2 * ((controlBuffer[i] + 1) / 2.0f);
					buffer[i] *= endAmp;
					buffer[i + 1] *= endAmp;
				}

			}
		}
		else
		{
			if (!bControlSig)
			{
				float endAmp = (amp - .5f) * 2;
				for (int i = 0; i < length; i++)buffer[i] = endAmp;
			}
			else
			{
				float endAmp = (amp - .5f) * 2;
				for (int i = 0; i < length; i++)
				{
					buffer[i] = endAmp + controlBuffer[i];
					buffer[i] = (buffer[i] > 1.0f) ? 1.0f : ((buffer[i] < -1.0f) ? -1.0f : buffer[i]); //clamp function made of ternary operators
				}
			}
		}
		*/
	}

	public static int NoiseProcessBuffer(float[] buffer, ref float sample, int length, int channels, float frequency, int counter, int speedFrames, ref bool updated)
	{

		if (frequency > .95f)
		{
			updated = true;
			for (int i = 0; i < length; i += channels)
			{
				sample = buffer[i] = buffer[i + 1] = -1 + 2 * ((float)rand.Next()) / int.MaxValue;
			}
		}

		else
		{
			for (int i = 0; i < length; i += channels)
			{
				counter++;
				if (counter > speedFrames)
				{
					updated = true;
					counter = 0;
					sample = -1 + 2 * ((float)rand.Next()) / int.MaxValue;
				}
				buffer[i] = buffer[i + 1] = sample;
			}
		}
		return counter;
	}

	public static int DrumSignalGenerator(float[] buffer, int length, int channels, bool signalOn, int counter)
	{
		float val = signalOn ? 1f : -1f;

		int endSignal = length;

		if (signalOn)
			endSignal = counter;

		for (int i = 0; i < length; i += channels)
		{
			val = (signalOn && (i < endSignal)) ? 1f : -1f;
			buffer[i] = buffer[i + 1] = val;
		}
		counter -= length;

		return counter;

	}

	public static float getADSR(int curFrame, float startVal, int frameCount, int[] frames, float[] volumes)
	{
		switch (curFrame)
		{
			case 0:
				return startVal + (volumes[0] - startVal) * frameCount / frames[0];

			case 1:
				return volumes[0] + (volumes[1] - volumes[0]) * (float)frameCount / (float)frames[1];

			case 2:
				return volumes[1];
			case 3:
				return volumes[1] * (1f - (float)frameCount / (float)frames[3]);
			case 4:
				return 0;
			default:
				break;
		}
		return 0;
	}



	public static void ADSRSignalGenerator(float[] buffer, int length, int channels, int[] frames, ref int frameCount, bool active, ref float ADSRvolume,
		float[] volumes, float startVal, ref int curFrame, bool sustaining)
	{
		if (!active)
		{
			for (int i = 0; i < length; i += channels)
			{
				buffer[i] = buffer[i + 1] = -1f;
			}
			return;
		}

		for (int i = 0; i < length; i += channels)
		{
			buffer[i + 1] = buffer[i] = ADSRvolume = lerp((getADSR(curFrame, startVal, frameCount, frames, volumes) + -.5f) * 2f, ADSRvolume, .98f);

			if (curFrame != 2) frameCount++;
			else if (curFrame == 2 && !sustaining) frameCount++;

			if (curFrame < 4)
			{
				if (frameCount >= frames[curFrame])
				{
					curFrame++;
					frameCount = 0;
				}
			}
		}
	}

	public static void KeyFrequencySignalGenerator(float[] buffer, int length, int channels, int semitone, float keyMultConst, ref float filteredVal)
	{
		float val = Mathf.Pow(keyMultConst, semitone) - 1;
		for (int i = 0; i < length; i += channels)
		{
			buffer[i] = buffer[i + 1] = filteredVal = lerp(val, filteredVal, .9f);
		}
	}

	public static unsafe float ClipSignalGenerator(float[] buffer, float[] speedBuffer, float[] ampBuffer, float[] seqBuffer, int length, float[] lastSeqGen, int channels, bool speedGen, bool ampGen, bool seqGen, float floatingBufferCount
		, int[] sampleBounds, float playbackSpeed, void* clip, int clipChannels, float amplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, ref bool active)
	{

		float* clipdata = (float*)(clip);

		for (int i = 0; i < length; i += channels)
		{
			if (seqGen)
			{
				if (seqBuffer[i] > lastSeqGen[1] && lastSeqGen[1] <= lastSeqGen[0])
				{
					if (playbackSpeed >= 0) floatingBufferCount = bufferCount = sampleBounds[0];
					else floatingBufferCount = bufferCount = sampleBounds[1];
					active = true;
				}

				lastSeqGen[0] = lastSeqGen[1];
				lastSeqGen[1] = seqBuffer[i];
			}

			bufferCount = (int)(floatingBufferCount + 0.5f); // round to int
			bool endOfSample = false;
			if (bufferCount > sampleBounds[1])
			{
				endOfSample = true;
				floatingBufferCount = bufferCount = playdirection ? sampleBounds[0] : sampleBounds[1];
			}
			else if (bufferCount < sampleBounds[0])
			{
				endOfSample = true;
				floatingBufferCount = bufferCount = playdirection ? sampleBounds[0] : sampleBounds[1];
			}

			if (endOfSample)
			{
				if (!looping) active = false;
				else if (seqGen && lastSeqGen[1] != 1) active = false;
			}

			float endAmplitude = amplitude;
			if (active)
			{
				if (speedGen) floatingBufferCount += speedBuffer[i] * (playbackSpeed > 0.0f ? 1.0f : -1.0f) + playbackSpeed;
				else floatingBufferCount += playbackSpeed;

				if (ampGen) endAmplitude = endAmplitude * ((ampBuffer[i] + 1) / 2.0f);


				buffer[i] = clipdata[bufferCount * clipChannels] * endAmplitude;
				if (clipChannels == 2) buffer[i + 1] = clipdata[bufferCount * clipChannels + 1] * amplitude;
				else buffer[i + 1] = buffer[i];
			}
		}
		return floatingBufferCount;
	}

	public static void XylorollMergeSignalsWithOsc(float[] buf, int length, float[] buf1, float[] buf2)
	{
		for (int i = 0; i < length; ++i)
		{
			buf[i] += (buf1[i] + buf2[i]) * .3f;
		}
	}

	public static void XylorollMergeSignalsWithoutOsc(float[] buf, int length, float[] buf1, float[] buf2)
	{
		for (int i = 0; i < length; ++i)
		{
			buf[i] += buf1[i] * ((buf2[i] + 1) / 2f);
		}
	}

	public static void OscillatorSignalGenerator(float[] buffer, int length, int channels, ref double _phase, float analogWave, float frequency, float amplitude, float prevAmplitude
		, float[] frequencyBuffer, float[] amplitudeBuffer, bool bFreqGen, bool bAmpGen, double _sampleDuration, ref double dspTime)
	{
		for (int i = 0; i < length; i += channels)
		{
			//create signal based on wave form
			double sample = Mathf.Sin((float)_phase * 2 * PI); //assuming sine wave
			if (analogWave > 0.95f)
			{
				sample = _phase * 2.0 - 1.0;
			}
			else if (analogWave > 0.05f)
			{
				float sign = sample >= 0 ? 1f : -1f;
				if (analogWave <= 0.5f)
				{
					sample = (sample * (0.5f - analogWave) + sign * analogWave) * 2;
				}
				else
				{

					sample = (sign * (1 - analogWave) + (_phase * 2.0 - 1.0) * (analogWave - .5f)) * 2;
				}
			}

			//frequency compute
			float endFrequency = frequency;

			//amp compute
			float endAmplitude = amplitude;
			if (prevAmplitude != amplitude) endAmplitude = lerp(prevAmplitude, amplitude, (float)i / length);

			//calc side chain effect
			if (bFreqGen)
			{
				endFrequency = frequency * (frequencyBuffer[i] + 1);
			}
			if (bAmpGen)
			{
				endAmplitude = endAmplitude * ((amplitudeBuffer[i] + 1) / 2f);
			}

			//update phase for next frame
			_phase += endFrequency * _sampleDuration;
			if (_phase > 1.0) _phase -= 1.0;

			//final buffer
			buffer[i] = buffer[i + 1] = (float)sample * endAmplitude;

			//dsptime update
			dspTime += _sampleDuration;
		}
	}

	public static void addCombFilterSignal(float[] inputbuffer, float[] addbuffer, int length, float[] delayBufferL, float[] delayBufferR, int delaylength, float gain, ref int inPoint, ref int outPoint)
	{
		float inputL = 0;
		float inputR = 0;

		for (int i = 0; i < length; i += 2)
		{
			inputL = inputbuffer[i];
			inputR = inputbuffer[i + 1];

			delayBufferL[inPoint] = inputbuffer[i] + delayBufferL[outPoint] * gain;
			delayBufferR[inPoint] = inputbuffer[i + 1] + delayBufferR[outPoint] * gain;

			inPoint++;
			if (inPoint == delaylength) inPoint = 0;

			addbuffer[i] += delayBufferL[outPoint];
			addbuffer[i + 1] += delayBufferR[outPoint];

			outPoint++;
			if (outPoint == delaylength) outPoint = 0;
		}
	}

	public static void processCombFilterSignal(float[] buffer, int length, float[] delayBufferL, float[] delayBufferR, int delaylength, float gain, ref int inPoint, ref int outPoint)
	{
		float inputL = 0;
		float inputR = 0;

		for (int i = 0; i < length; i += 2)
		{
			delayBufferL[inPoint] = inputL = buffer[i] + delayBufferL[outPoint] * gain;
			delayBufferR[inPoint] = inputR = buffer[i + 1] + delayBufferR[outPoint] * gain;

			inPoint++;
			if (inPoint == delaylength) inPoint = 0;

			buffer[i] = delayBufferL[outPoint] - gain * inputL;
			buffer[i + 1] = delayBufferR[outPoint] - gain * inputR;

			outPoint++;
			if (outPoint == delaylength) outPoint = 0;
		}
	}

	public static void lowpassSignal(float[] buffer, int length, ref float lowpassL, ref float lowpassR)
	{
		for (int i = 0; i < length; i += 2)
		{
			buffer[i] = lowpassL = 0.7f * lowpassL + 0.3f * buffer[i];
			buffer[i + 1] = lowpassR = 0.7f * lowpassR + 0.3f * buffer[i + 1];
		}
	}

	public static void combineArrays(float[] buffer, float[] bufferB, int length, float levelA, float levelB)
	{
		for (int i = 0; i < length; ++i)
		{
			buffer[i] = buffer[i] * levelA + bufferB[i] * levelB;
		}
	}


	public static unsafe void ProcessWaveTexture(float[] buffer, int length, void* pixels, byte Ra, byte Ga, byte Ba, byte Rb, byte Gb, byte Bb,
		int period, int waveheight, int wavewidth, ref int lastWaveH, ref int curWaveW)
	{
		byte* data = (byte*)(pixels);

		for (int i = 0; i < length / period; ++i)
		{
			float temp = (buffer[i * period] + 1.0f) * .5f;
			temp = (temp > 1.0f) ? 1.0f : ((temp < 0.0f) ? 0.0f : temp);
			int curH = (int)((waveheight - 1) * temp);
			for (int i2 = 0; i2 < waveheight; i2++)
			{
				byte* pixel = data + 4 * (i2 * wavewidth + curWaveW);
				if (lastWaveH >= i2 && i2 >= curH)
				{
					pixel[0] = Ra;
					pixel[1] = Ga;
					pixel[2] = Ba;
					pixel[3] = 255;
				}
				else if (lastWaveH <= i2 && i2 <= curH)
				{
					pixel[0] = Ra;
					pixel[1] = Ga;
					pixel[2] = Ba;
					pixel[3] = 255;
				}
				else
				{
					pixel[0] = Rb;
					pixel[1] = Gb;
					pixel[2] = Bb;
					pixel[3] = 255;
				}
			}

			lastWaveH = curH;
			curWaveW = (curWaveW + 1) % wavewidth;
		}
	}
}