﻿using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.DirectSound;

namespace JocysCom.TextToSpeech.Monitor
{

	/// <summary>
	/// Summary description for Sound Effects Player.
	/// </summary>
	public partial class AudioPlayer : IDisposable
	{

		public event EventHandler<EventArgs> BeforePlay;

		public AudioPlayer(IntPtr handle)
		{
			_Handle = handle;
		}

		IntPtr _Handle;
		public SecondarySoundBuffer ApplicationBuffer = null;
		DirectSound ApplicationDevice = null;

		public byte[] GetBytes(Stream stream)
		{
			// Play.
			stream.Position = 0;
			// Make copy of the stream.
			var ms = new MemoryStream();
			int bufSize = 4096;
			byte[] buf = new byte[bufSize];
			int bytesRead = 0;
			while ((bytesRead = stream.Read(buf, 0, bufSize)) > 0)
				ms.Write(buf, 0, bytesRead);
			return ms.ToArray();
		}

		public void GetInfo(byte[] bytes, out int sampleRate, out int bitsPerSample, out int channelCount)
		{
			channelCount = BitConverter.ToInt16(bytes, 22);
			sampleRate = BitConverter.ToInt32(bytes, 24);
			bitsPerSample = BitConverter.ToInt16(bytes, 34);
		}

		/// <summary>
		/// Load sound data.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns>Returns duration.</returns>
		public decimal Load(Stream stream)
		{
			int sampleRate;
			int bitsPerSample;
			int channelCount;
			var bytes = GetBytes(stream);
			GetInfo(bytes, out sampleRate, out bitsPerSample, out channelCount);
			return Load(bytes, sampleRate, bitsPerSample, channelCount);
		}

		/// <summary>
		/// Load sound data.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns>Returns duration.</returns>
		public decimal Load(byte[] bytes, int sampleRate, int bitsPerSample, int channelCount)
		{
			// Create and set the buffer description.
			var buffer_desc = new SoundBufferDescription();
			var format = new SharpDX.Multimedia.WaveFormat(sampleRate, bitsPerSample, channelCount);
			buffer_desc.Format = format;
			buffer_desc.Flags =
				// Play sound even if application loses focus.
				BufferFlags.GlobalFocus |
				// This has to be true to use effects.
				BufferFlags.ControlEffects;
			buffer_desc.BufferBytes = bytes.Length;
			// Create and set the buffer for playing the sound.
			ApplicationBuffer = new SecondarySoundBuffer(ApplicationDevice, buffer_desc);
			ApplicationBuffer.Write(bytes, 0, LockFlags.None);
			var dataLength = (int)bytes.Length - 44;
			var duration = ((decimal)dataLength * 8m) / (decimal)channelCount / (decimal)sampleRate / (decimal)bitsPerSample * 1000m;
			return duration;
		}

		public void Play()
		{
			var ab = ApplicationBuffer;
			if (ab == null)
				return;
			// Used to apply effects.
			var ev = BeforePlay;
			if (ev != null)
				ev(this, new EventArgs());
			// If there is no sound then go to "Playback Devices", select your device,
			// Press [Configure] button, press [Test] button to see if all speaker are producing sound.
			ab.Play(0, PlayFlags.None);
		}

		public void Stop()
		{
			// Build the effects array
			//ApplicationBuffer.Volume = -10000;
			var ab = ApplicationBuffer;
			if (ab != null)
			{
				ab.Stop();
			}
			//ApplicationBuffer.Volume = 0;
		}

		string CurrentDeviceName;

		public void ChangeAudioDevice(string deviceName = null)
		{
			if (CurrentDeviceName == deviceName && ApplicationDevice != null)
				return;
			var playbackDevices = DirectSound.GetDevices();
			// Use default device.
			Guid driverGuid = Guid.Empty;
			foreach (var device in playbackDevices)
			{
				// Pick specific device for the plaback.
				if (string.Compare(device.Description, deviceName, true) == 0)
					driverGuid = device.DriverGuid;
			}
			if (ApplicationDevice != null)
			{
				ApplicationDevice.Dispose();
				ApplicationDevice = null;
			}
			// Create and set the sound device.
			ApplicationDevice = new DirectSound(driverGuid);
			SpeakerConfiguration speakerSet;
			SpeakerGeometry geometry;
			ApplicationDevice.GetSpeakerConfiguration(out speakerSet, out geometry);
			ApplicationDevice.SetCooperativeLevel(_Handle, CooperativeLevel.Normal);
			CurrentDeviceName = deviceName;
		}

		public static string s_DefaultDevice = "Default Device";

		public static string[] GetDeviceNames()
		{
			var list = new List<string>();
			list.Add(s_DefaultDevice);
			var devices = SharpDX.DirectSound.DirectSound.GetDevices();
			foreach (var device in devices)
			{
				if (device.DriverGuid == Guid.Empty)
					continue;
				list.Add(device.Description);
			}
			return list.ToArray();
		}


		#region IDisposable

		public virtual void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool IsDisposing;

		void Dispose(bool disposing)
		{
			if (disposing)
			{

				// Don't dispose twice.
				if (IsDisposing)
					return;
				IsDisposing = true;
				if (ApplicationDevice != null) ApplicationDevice.Dispose();
				if (ApplicationBuffer != null) ApplicationBuffer.Dispose();
				_Handle = IntPtr.Zero;
			}
		}

		#endregion

	}
}