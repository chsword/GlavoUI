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

        // 已经开始录音
        private static bool _isRecording;
        private static AudioButtonDialogManager _mDialogManager;
        private static AudioManager _mAudioManager;
        private static float _mTime;

        // 是否触发了onlongclick，准备好了
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
          
            // 这里没有判断储存卡是否存在，有空要判断
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
            //录音完成后的回调，回调给activiy，可以获得mtime和文件的路径
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

        // 准备三个常量

        private static readonly Handler Mhandler = new Handler(msg =>
        {
            switch ((Message)msg.What)
            {
                case Message.AudioPrepared:
                    // 显示应该是在audio end prepare之后回调
                    _mDialogManager.ShowRecordingDialog();
                    _isRecording = true;
                    new Thread(MGetVoiceLevelRunnable).Start();

                    // 需要开启一个线程来变换音量
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
                        // 根据x，y来判断用户是否想要取消
                        ChangeState(WantToCancel(x, y) ? State.WantToCancel : State.Recording);
                    }

                    break;
                case MotionEventActions.Up:
                    // 首先判断是否有触发onlongclick事件，没有的话直接返回reset
                    if (!_mReady)
                    {
                        Reset();
                        return base.OnTouchEvent(e);
                    }
                    // 如果按的时间太短，还没准备好或者时间录制太短，就离开了，则显示这个dialog
                    if (!_isRecording || _mTime < 0.6f)
                    {
                        _mDialogManager.TooShort();
                        _mAudioManager.Cancel();
                        Mhandler.SendEmptyMessageDelayed((int)Message.DialogDimiss, 1300);// 持续1.3s
                    }
                    else if (_mCurrentState == State.Recording)
                    {
                        //正常录制结束
                        _mDialogManager.DimissDialog();
                        _mAudioManager.Release();// release释放一个mediarecorder
                        // 并且callbackActivity，保存录音

                        Finished?.Invoke(this,new FinishedEventArgs(_mTime, _mAudioManager.GetCurrentFilePath()));
                    }
                    else if (_mCurrentState == State.WantToCancel)
                    {
                        // cancel
                        _mAudioManager.Cancel();
                        _mDialogManager.DimissDialog();
                    }
                    Reset();// 恢复标志位
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
            {// 判断是否在左边，右边，上边，下边
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