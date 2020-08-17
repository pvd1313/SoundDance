using UnityEditor;
using System.Reflection;
using UnityEngine;
using System;

namespace Pingle.EditorTools
{
	public static class AudioUtil
	{
		private static readonly MethodInfo _get_resetAllAudioClipPlayCountsOnPlay;
		private static readonly MethodInfo _set_resetAllAudioClipPlayCountsOnPlay;
		private static readonly MethodInfo _playClip;
		private static readonly MethodInfo _stopClip;
		private static readonly MethodInfo _pauseClip;
		private static readonly MethodInfo _resumeClip;
		private static readonly MethodInfo _loopClip;
		private static readonly MethodInfo _isClipPlaying;
		private static readonly MethodInfo _stopAllClips;
		private static readonly MethodInfo _getClipPosition;
		private static readonly MethodInfo _getClipSamplePosition;
		private static readonly MethodInfo _setClipSamplePosition;
		private static readonly MethodInfo _getSampleCount;
		private static readonly MethodInfo _getChannelCount;
		private static readonly MethodInfo _getBitRate;
		private static readonly MethodInfo _getBitsPerSample;
		private static readonly MethodInfo _getFrequency;
		private static readonly MethodInfo _getSoundSize;
		private static readonly MethodInfo _getSoundCompressionFormat;
		private static readonly MethodInfo _getTargetPlatformSoundCompressionFormat;
		private static readonly MethodInfo _get_canUseSpatializerEffect;
		private static readonly MethodInfo _getAmbisonicDecoderPluginNames;
		private static readonly MethodInfo _hasPreview;
		private static readonly MethodInfo _getImporterFromClip;
		private static readonly MethodInfo _getMinMaxData;
		private static readonly MethodInfo _getDuration;
		private static readonly MethodInfo _getFMODMemoryAllocated;
		private static readonly MethodInfo _getFMODCPUUsage;
		private static readonly MethodInfo _isMovieAudio;
		private static readonly MethodInfo _isTrackerFile;
		private static readonly MethodInfo _getMusicChannelCount;
		private static readonly MethodInfo _getLowpassCurve;
		private static readonly MethodInfo _getListenerPos;
		private static readonly MethodInfo _updateAudio;
		private static readonly MethodInfo _setListenerTransform;
		private static readonly MethodInfo _hasAudioCallback;
		private static readonly MethodInfo _getCustomFilterChannelCount;
		private static readonly MethodInfo _getCustomFilterProcessTime;
		private static readonly MethodInfo _getCustomFilterMaxIn;
		private static readonly MethodInfo _getCustomFilterMaxOut;

		static AudioUtil()
		{
			var assembly = typeof(AudioImporter).Assembly;
			var audioUtil = assembly.GetType("UnityEditor.AudioUtil");
			var flags = BindingFlags.Static | BindingFlags.Public;

			_get_resetAllAudioClipPlayCountsOnPlay = audioUtil.GetMethod("get_resetAllAudioClipPlayCountsOnPlay", flags, null, new Type[] { }, null);
			_set_resetAllAudioClipPlayCountsOnPlay = audioUtil.GetMethod("set_resetAllAudioClipPlayCountsOnPlay", flags, null, new Type[] { typeof(bool) }, null);
			_playClip = audioUtil.GetMethod("PlayClip", flags, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
			_stopClip = audioUtil.GetMethod("StopClip", flags, null, new Type[] { typeof(AudioClip) }, null);
			_pauseClip = audioUtil.GetMethod("PauseClip", flags, null, new Type[] { typeof(AudioClip) }, null);
			_resumeClip = audioUtil.GetMethod("ResumeClip", flags, null, new Type[] { typeof(AudioClip) }, null);
			_loopClip = audioUtil.GetMethod("LoopClip", flags, null, new Type[] { typeof(AudioClip), typeof(bool) }, null);
			_isClipPlaying = audioUtil.GetMethod("IsClipPlaying", flags, null, new Type[] { typeof(AudioClip) }, null);
			_stopAllClips = audioUtil.GetMethod("StopAllClips", flags, null, new Type[] { }, null);
			_getClipPosition = audioUtil.GetMethod("GetClipPosition", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getClipSamplePosition = audioUtil.GetMethod("GetClipSamplePosition", flags, null, new Type[] { typeof(AudioClip) }, null);
			_setClipSamplePosition = audioUtil.GetMethod("SetClipSamplePosition", flags, null, new Type[] { typeof(AudioClip), typeof(int) }, null);
			_getSampleCount = audioUtil.GetMethod("GetSampleCount", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getChannelCount = audioUtil.GetMethod("GetChannelCount", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getBitRate = audioUtil.GetMethod("GetBitRate", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getBitsPerSample = audioUtil.GetMethod("GetBitsPerSample", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getFrequency = audioUtil.GetMethod("GetFrequency", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getSoundSize = audioUtil.GetMethod("GetSoundSize", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getSoundCompressionFormat = audioUtil.GetMethod("GetSoundCompressionFormat", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getTargetPlatformSoundCompressionFormat = audioUtil.GetMethod("GetTargetPlatformSoundCompressionFormat", flags, null, new Type[] { typeof(AudioClip) }, null);
			_get_canUseSpatializerEffect = audioUtil.GetMethod("get_canUseSpatializerEffect", flags, null, new Type[] { }, null);
			_getAmbisonicDecoderPluginNames = audioUtil.GetMethod("GetAmbisonicDecoderPluginNames", flags, null, new Type[] { }, null);
			_hasPreview = audioUtil.GetMethod("HasPreview", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getImporterFromClip = audioUtil.GetMethod("GetImporterFromClip", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getMinMaxData = audioUtil.GetMethod("GetMinMaxData", flags, null, new Type[] { typeof(AudioImporter) }, null);
			_getDuration = audioUtil.GetMethod("GetDuration", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getFMODMemoryAllocated = audioUtil.GetMethod("GetFMODMemoryAllocated", flags, null, new Type[] { }, null);
			_getFMODCPUUsage = audioUtil.GetMethod("GetFMODCPUUsage", flags, null, new Type[] { }, null);
			_isMovieAudio = audioUtil.GetMethod("IsMovieAudio", flags, null, new Type[] { typeof(AudioClip) }, null);
			_isTrackerFile = audioUtil.GetMethod("IsTrackerFile", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getMusicChannelCount = audioUtil.GetMethod("GetMusicChannelCount", flags, null, new Type[] { typeof(AudioClip) }, null);
			_getLowpassCurve = audioUtil.GetMethod("GetLowpassCurve", flags, null, new Type[] { typeof(AudioLowPassFilter) }, null);
			_getListenerPos = audioUtil.GetMethod("GetListenerPos", flags, null, new Type[] { }, null);
			_updateAudio = audioUtil.GetMethod("UpdateAudio", flags, null, new Type[] { }, null);
			_setListenerTransform = audioUtil.GetMethod("SetListenerTransform", flags, null, new Type[] { typeof(Transform) }, null);
			_hasAudioCallback = audioUtil.GetMethod("HasAudioCallback", flags, null, new Type[] { typeof(MonoBehaviour) }, null);
			_getCustomFilterChannelCount = audioUtil.GetMethod("GetCustomFilterChannelCount", flags, null, new Type[] { typeof(MonoBehaviour) }, null);
			_getCustomFilterProcessTime = audioUtil.GetMethod("GetCustomFilterProcessTime", flags, null, new Type[] { typeof(MonoBehaviour) }, null);
			_getCustomFilterMaxIn = audioUtil.GetMethod("GetCustomFilterMaxIn", flags, null, new Type[] { typeof(MonoBehaviour), typeof(int) }, null);
			_getCustomFilterMaxOut = audioUtil.GetMethod("GetCustomFilterMaxOut", flags, null, new Type[] { typeof(MonoBehaviour), typeof(int) }, null);
		}
		public static bool get_resetAllAudioClipPlayCountsOnPlay()
		{
			return (bool)_get_resetAllAudioClipPlayCountsOnPlay.Invoke(null, new object[] { });
		}
		public static void set_resetAllAudioClipPlayCountsOnPlay(bool value)
		{
			_set_resetAllAudioClipPlayCountsOnPlay.Invoke(null, new object[] { value });
		}
		public static void PlayClip(AudioClip clip, int startSample, bool loop)
		{
			_playClip.Invoke(null, new object[] { clip, startSample, loop });
		}
		public static void StopClip(AudioClip clip)
		{
			_stopClip.Invoke(null, new object[] { clip });
		}
		public static void PauseClip(AudioClip clip)
		{
			_pauseClip.Invoke(null, new object[] { clip });
		}
		public static void ResumeClip(AudioClip clip)
		{
			_resumeClip.Invoke(null, new object[] { clip });
		}
		public static void LoopClip(AudioClip clip, bool on)
		{
			_loopClip.Invoke(null, new object[] { clip, on });
		}
		public static bool IsClipPlaying(AudioClip clip)
		{
			return (bool)_isClipPlaying.Invoke(null, new object[] { clip });
		}
		public static void StopAllClips()
		{
			_stopAllClips.Invoke(null, new object[] { });
		}
		public static float GetClipPosition(AudioClip clip)
		{
			return (float)_getClipPosition.Invoke(null, new object[] { clip });
		}
		public static int GetClipSamplePosition(AudioClip clip)
		{
			return (int)_getClipSamplePosition.Invoke(null, new object[] { clip });
		}
		public static void SetClipSamplePosition(AudioClip clip, int iSamplePosition)
		{
			_setClipSamplePosition.Invoke(null, new object[] { clip, iSamplePosition });
		}
		public static int GetSampleCount(AudioClip clip)
		{
			return (int)_getSampleCount.Invoke(null, new object[] { clip });
		}
		public static int GetChannelCount(AudioClip clip)
		{
			return (int)_getChannelCount.Invoke(null, new object[] { clip });
		}
		public static int GetBitRate(AudioClip clip)
		{
			return (int)_getBitRate.Invoke(null, new object[] { clip });
		}
		public static int GetBitsPerSample(AudioClip clip)
		{
			return (int)_getBitsPerSample.Invoke(null, new object[] { clip });
		}
		public static int GetFrequency(AudioClip clip)
		{
			return (int)_getFrequency.Invoke(null, new object[] { clip });
		}
		public static int GetSoundSize(AudioClip clip)
		{
			return (int)_getSoundSize.Invoke(null, new object[] { clip });
		}
		public static AudioCompressionFormat GetSoundCompressionFormat(AudioClip clip)
		{
			return (AudioCompressionFormat)_getSoundCompressionFormat.Invoke(null, new object[] { clip });
		}
		public static AudioCompressionFormat GetTargetPlatformSoundCompressionFormat(AudioClip clip)
		{
			return (AudioCompressionFormat)_getTargetPlatformSoundCompressionFormat.Invoke(null, new object[] { clip });
		}
		public static bool get_canUseSpatializerEffect()
		{
			return (bool)_get_canUseSpatializerEffect.Invoke(null, new object[] { });
		}
		public static string[] GetAmbisonicDecoderPluginNames()
		{
			return (string[])_getAmbisonicDecoderPluginNames.Invoke(null, new object[] { });
		}
		public static bool HasPreview(AudioClip clip)
		{
			return (bool)_hasPreview.Invoke(null, new object[] { clip });
		}
		public static AudioImporter GetImporterFromClip(AudioClip clip)
		{
			return (AudioImporter)_getImporterFromClip.Invoke(null, new object[] { clip });
		}
		public static float[] GetMinMaxData(AudioImporter importer)
		{
			return (float[])_getMinMaxData.Invoke(null, new object[] { importer });
		}
		public static double GetDuration(AudioClip clip)
		{
			return (double)_getDuration.Invoke(null, new object[] { clip });
		}
		public static int GetFMODMemoryAllocated()
		{
			return (int)_getFMODMemoryAllocated.Invoke(null, new object[] { });
		}
		public static float GetFMODCPUUsage()
		{
			return (float)_getFMODCPUUsage.Invoke(null, new object[] { });
		}
		public static bool IsMovieAudio(AudioClip clip)
		{
			return (bool)_isMovieAudio.Invoke(null, new object[] { clip });
		}
		public static bool IsTrackerFile(AudioClip clip)
		{
			return (bool)_isTrackerFile.Invoke(null, new object[] { clip });
		}
		public static int GetMusicChannelCount(AudioClip clip)
		{
			return (int)_getMusicChannelCount.Invoke(null, new object[] { clip });
		}
		public static AnimationCurve GetLowpassCurve(AudioLowPassFilter lowPassFilter)
		{
			return (AnimationCurve)_getLowpassCurve.Invoke(null, new object[] { lowPassFilter });
		}
		public static Vector3 GetListenerPos()
		{
			return (Vector3)_getListenerPos.Invoke(null, new object[] { });
		}
		public static void UpdateAudio()
		{
			_updateAudio.Invoke(null, new object[] { });
		}
		public static void SetListenerTransform(Transform t)
		{
			_setListenerTransform.Invoke(null, new object[] { t });
		}
		public static bool HasAudioCallback(MonoBehaviour behaviour)
		{
			return (bool)_hasAudioCallback.Invoke(null, new object[] { behaviour });
		}
		public static int GetCustomFilterChannelCount(MonoBehaviour behaviour)
		{
			return (int)_getCustomFilterChannelCount.Invoke(null, new object[] { behaviour });
		}
		public static int GetCustomFilterProcessTime(MonoBehaviour behaviour)
		{
			return (int)_getCustomFilterProcessTime.Invoke(null, new object[] { behaviour });
		}
		public static float GetCustomFilterMaxIn(MonoBehaviour behaviour, int channel)
		{
			return (float)_getCustomFilterMaxIn.Invoke(null, new object[] { behaviour, channel });
		}
		public static float GetCustomFilterMaxOut(MonoBehaviour behaviour, int channel)
		{
			return (float)_getCustomFilterMaxOut.Invoke(null, new object[] { behaviour, channel });
		}
	}
}
