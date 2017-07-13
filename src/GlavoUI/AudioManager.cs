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
                // һ��ʼӦ����false��
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
                    // ��������ļ�
                    _mRecorder.SetOutputFile(file.AbsolutePath);
                }
                //����meidaRecorder����ƵԴ����˷�
                _mRecorder.SetAudioSource(AudioSource.Mic);
                // �����ļ���Ƶ�������ʽΪamr
                _mRecorder.SetOutputFormat(OutputFormat.RawAmr);
                // ������Ƶ�ı����ʽΪamr
                _mRecorder.SetAudioEncoder(AudioEncoder.AmrNb);
                //����¼������
                _mRecorder.SetAudioChannels(1);
                //���ñ��������
                _mRecorder.SetAudioEncodingBitRate(1);
                //���ñ��������
                _mRecorder.SetAudioSamplingRate(8000);
                // �ϸ�����google�ٷ�api������mediaRecorder��״̬����ͼ
                try
                {
                    _mRecorder.Prepare();
                }
                catch (IOException e)
                {
                    Debug.WriteLine("¼��δ�ɹ���������" + e.Message);
                }
                _mRecorder.Start();
                // ׼������
                _isPrepared = true;

                // �Ѿ�׼�����ˣ�����¼����
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

        // ���������level
        public int GetVoiceLevel(int maxLevel)
        {
            // mRecorder.getMaxAmplitude()�������Ƶ�������Χ��ֵ����1-32767
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

        // �ͷ���Դ
        public void Release()
        {
            _mRecorder.Stop();
            _mRecorder.Release();
            _mRecorder.Dispose();
            _mRecorder = null;
        }
        // ȡ��,��Ϊprepareʱ������һ���ļ�������cancel����Ӧ��Ҫɾ������ļ���
        // ������release�ķ���������
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