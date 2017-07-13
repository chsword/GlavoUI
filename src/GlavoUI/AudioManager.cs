using System;
using System.Diagnostics;
using Android.Media;
using Java.IO;
using Java.Lang;
using Java.Util;
using Exception = Java.Lang.Exception;
using Object = System.Object;

namespace GlavoUI
{
    public class AudioManager
    {
        private MediaRecorder _mRecorder;
        private readonly string _mDirString;
        private string _mCurrentFilePathString;

        private bool _isPrepared;
        private static AudioManager _mInstance;
        private static readonly Object Obj = new Object();
        private AudioManager(string dir)
        {
            _mDirString = dir;
        }

        public static AudioManager GetInstance(string dir)
        {
            if (_mInstance == null)
            {
                lock (Obj)
                {
                    if (_mInstance == null)
                    {
                        _mInstance = new AudioManager(dir);
                    }
                }
            }
            return _mInstance;
        }

        public event EventHandler Prepared;

        public void PrepareAudio()
        {
            try
            {
                // 一开始应该是false的
                _isPrepared = false;

                File dir = new File(_mDirString);
                if (!dir.Exists())
                {
                    dir.Mkdirs();
                }
                string fileNameString = GeneralFileName();
                using (var file = new File(dir, fileNameString))
                {

                    _mCurrentFilePathString = file.AbsolutePath;

                    _mRecorder = new MediaRecorder();
                    // 设置输出文件
                    _mRecorder.SetOutputFile(file.AbsolutePath);
                }
                //设置meidaRecorder的音频源是麦克风
                _mRecorder.SetAudioSource(AudioSource.Mic);
                // 设置文件音频的输出格式为amr
                _mRecorder.SetOutputFormat(OutputFormat.RawAmr);
                // 设置音频的编码格式为amr
                _mRecorder.SetAudioEncoder(AudioEncoder.AmrNb);
                //设置录音声道
                _mRecorder.SetAudioChannels(1);
                //设置编码比特率
                _mRecorder.SetAudioEncodingBitRate(1);
                //设置编码采样率
                _mRecorder.SetAudioSamplingRate(8000);
                // 严格遵守google官方api给出的mediaRecorder的状态流程图
                try
                {
                    _mRecorder.Prepare();
                }
                catch (IOException e)
                {
                    Debug.WriteLine("录音未成功，请重试" + e.Message);
                }
                _mRecorder.Start();
                // 准备结束
                _isPrepared = true;

                // 已经准备好了，可以录制了
                Prepared?.Invoke(this, new EventArgs());
            }
            catch (IllegalStateException e)
            {
                e.PrintStackTrace();
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
            }

        }


        private string GeneralFileName()
        {
            return $"{UUID.RandomUUID()}.amr";
        }

        // 获得声音的level
        public int GetVoiceLevel(int maxLevel)
        {
            // mRecorder.getMaxAmplitude()这个是音频的振幅范围，值域是1-32767
            if (!_isPrepared) return 1;
            try
            {
                if (_mRecorder == null)
                {
                    return 1;
                }
                return maxLevel * _mRecorder.MaxAmplitude / 32768 + 1;
            }
            catch (Exception)
            {
            }

            return 1;
        }

        // 释放资源
        public void Release()
        {
            _mRecorder.Stop();
            _mRecorder.Release();
            _mRecorder.Dispose();
            _mRecorder = null;
        }
        // 取消,因为prepare时产生了一个文件，所以cancel方法应该要删除这个文件，
        // 这是与release的方法的区别
        public void Cancel()
        {
            Release();
            if (_mCurrentFilePathString == null) return;
            using (var file = new File(_mCurrentFilePathString))
            {
                file.Delete();
            }
            _mCurrentFilePathString = null;
        }

        public string GetCurrentFilePath()
        {
            // TODO Auto-generated method stub
            return _mCurrentFilePathString;
        }
    }
}