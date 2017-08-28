using System;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace GlavoUI
{
    public class AudioRecordButton : Button
    {
        private enum Message
        {
            AudioPrepared = 0X110,
            VoiceChange = 0X111,
            DialogDimiss = 0X112
        }

        private enum State
        {
            Normal = 1,
            Recording = 2,
            WantToCancel = 3
        }

        private const int DistanceYCancel = 50;
        private State _mCurrentState = State.Normal;

        // �Ѿ���ʼ¼��
        private static bool _isRecording;
        private static AudioButtonDialogManager _mDialogManager;
        private static AudioManager _mAudioManager;
        private static float _mTime;

        // �Ƿ񴥷���onlongclick��׼������
        private bool _mReady;

        protected AudioRecordButton(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public AudioRecordButton(Context context) : base(context)
        {
        }

        public AudioRecordButton(Context context, IAttributeSet attrs) : base(context, attrs)
        {
         
            _mDialogManager = new AudioButtonDialogManager(Context);
          
            // ����û���жϴ��濨�Ƿ���ڣ��п�Ҫ�ж�
            var dir = $"{Android.OS.Environment.ExternalStorageDirectory}/message_audios";
            if (_mAudioManager == null)
            {
                _mAudioManager = AudioManager.GetInstance(dir);
                _mAudioManager.Prepared += AudioManagerPrepared;
            }
            LongClick += AudioButtonLongClick;
        }

        private void AudioManagerPrepared(object sender, EventArgs e)
        {
            Mhandler.SendEmptyMessage((int)Message.AudioPrepared);
        }

        private void AudioButtonLongClick(object sender, LongClickEventArgs e)
        {
            _mReady = true;
            _mAudioManager.PrepareAudio();
        }

        public AudioRecordButton(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public AudioRecordButton(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }
 

        public class FinishedEventArgs : EventArgs
        {
            public FinishedEventArgs(float seconds,string filePath)
            {
                Seconds = seconds;
                FilePath = filePath;
            }
            //¼����ɺ�Ļص����ص���activiy�����Ի��mtime���ļ���·��
            public float Seconds { get; set; }
            public string FilePath { get; set; }
        }

        public event EventHandler<FinishedEventArgs> Finished;
  
    
     
    
        private static readonly Runnable MGetVoiceLevelRunnable = new Runnable(() =>
        {
            while (_isRecording)
            {
                try
                {
                    Thread.Sleep(100);
                    _mTime += 0.1f;
                    Mhandler.SendEmptyMessage((int)Message.VoiceChange);
                }
                catch (InterruptedException e)
                {
                    e.PrintStackTrace();
                }
            }
        });

        // ׼����������

        private static readonly Handler Mhandler = new Handler(msg =>
        {
            switch ((Message)msg.What)
            {
                case Message.AudioPrepared:
                    // ��ʾӦ������audio end prepare֮��ص�
                    _mDialogManager.ShowRecordingDialog();
                    _isRecording = true;
                    new Thread(MGetVoiceLevelRunnable).Start();

                    // ��Ҫ����һ���߳����任����
                    break;
                case Message.VoiceChange:
                    _mDialogManager.UpdateVoiceLevel(_mAudioManager.GetVoiceLevel(7));

                    break;
                case Message.DialogDimiss:
                    break;
            }
        });

        public override bool OnTouchEvent(MotionEvent e)
        {
            var action = e.Action;
            int x = (int)e.GetX();
            int y = (int)e.GetY();

            switch (action)
            {
                case MotionEventActions.Down:
                    ChangeState(State.Recording);
                    break;
                case MotionEventActions.Move:

                    if (_isRecording)
                    {
                        // ����x��y���ж��û��Ƿ���Ҫȡ��
                        ChangeState(WantToCancel(x, y) ? State.WantToCancel : State.Recording);
                    }

                    break;
                case MotionEventActions.Up:
                    // �����ж��Ƿ��д���onlongclick�¼���û�еĻ�ֱ�ӷ���reset
                    if (!_mReady)
                    {
                        Reset();
                        return base.OnTouchEvent(e);
                    }
                    // �������ʱ��̫�̣���û׼���û���ʱ��¼��̫�̣����뿪�ˣ�����ʾ���dialog
                    if (!_isRecording || _mTime < 0.6f)
                    {
                        _mDialogManager.TooShort();
                        _mAudioManager.Cancel();
                        Mhandler.SendEmptyMessageDelayed((int)Message.DialogDimiss, 1300);// ����1.3s
                    }
                    else if (_mCurrentState == State.Recording)
                    {
                        //����¼�ƽ���
                        _mDialogManager.DimissDialog();
                        _mAudioManager.Release();// release�ͷ�һ��mediarecorder
                        // ����callbackActivity������¼��

                        Finished?.Invoke(this,new FinishedEventArgs(_mTime, _mAudioManager.GetCurrentFilePath()));
                    }
                    else if (_mCurrentState == State.WantToCancel)
                    {
                        // cancel
                        _mAudioManager.Cancel();
                        _mDialogManager.DimissDialog();
                    }
                    Reset();// �ָ���־λ
                    break;
            }
            return base.OnTouchEvent(e);
        }
        private void Reset()
        {
            _isRecording = false;
            ChangeState(State.Normal);
            _mReady = false;
            _mTime = 0;
        }
        private bool WantToCancel(int x, int y)
        {
            if (x < 0 || x > Width)
            {// �ж��Ƿ�����ߣ��ұߣ��ϱߣ��±�
                return true;
            }
            return y < -DistanceYCancel || y > Height + DistanceYCancel;
        }

        private void ChangeState(State state)
        {
            if (_mCurrentState == state) return;
            _mCurrentState = state;
            switch (_mCurrentState)
            {
                case State.Normal:
                    SetBackgroundResource(Resource.Drawable.glavo_audio_button_recordnormal);
                    SetText(Resource.String.GlavoAudioRecordButtonNormal);

                    break;
                case State.Recording:
                    SetBackgroundResource(Resource.Drawable.glavo_audio_button_recording);
                    SetText(Resource.String.GlavoAudioRecordButtonRecording);
                    if (_isRecording)
                    {
                        _mDialogManager.Recording();
                    }
                    break;

                case State.WantToCancel:
                    SetBackgroundResource(Resource.Drawable.glavo_audio_button_recording);
                    SetText(Resource.String.GlavoAudioRecordButtonWantToCancel);
                    _mDialogManager.WantToCancel();
                    break;
            }
        }

    }
}