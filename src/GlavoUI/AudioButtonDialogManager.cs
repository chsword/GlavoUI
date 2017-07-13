using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace GlavoUI
{
    public class AudioButtonDialogManager
    {
        private enum DialogState
        {
            DimissDialog,
            Recording,
            WantToCancel,
            TooShort
        }
        #region control
        private Dialog _mDialog;
        private ImageView _mIcon;
        private ImageView _mVoice;
        private TextView _mLable;
        private readonly Context _mContext;
        #endregion

        public AudioButtonDialogManager(Context context)
        {
            _mContext = context;
        }

        public void ShowRecordingDialog()
        {
            _mDialog = new Dialog(_mContext, Resource.Style.Theme_audioDialog);
            // 用layoutinflater来引用布局
            LayoutInflater inflater = LayoutInflater.From(_mContext);
            View view = inflater.Inflate(Resource.Layout.GlavoAudioButtonDialogManager, null);
            _mDialog.SetContentView(view);
            _mIcon = _mDialog.FindViewById<ImageView>(Resource.Id.GlavoAudioButtonDialogManager_Icon);
            _mVoice = _mDialog.FindViewById<ImageView>(Resource.Id.GlavoAudioButtonDialogManager_Voice);
            _mLable = _mDialog.FindViewById<TextView>(Resource.Id.GlavoAudioButtonDialogManager_Text);
            _mDialog.Show();
        }

        #region dialog 界面变化
        private void DialogStatusChange(DialogState methodName)
        {
            if (_mDialog == null || !_mDialog.IsShowing) return;
            if (methodName == DialogState.DimissDialog)
            {
                _mDialog.Dismiss();
                _mDialog = null;
            }
            else
            {
                _mIcon.Visibility = ViewStates.Visible;
                _mLable.Visibility = ViewStates.Visible;
                switch (methodName)
                {
                    case DialogState.Recording:
                        _mVoice.Visibility = ViewStates.Visible;
                        _mIcon.SetImageResource(Resource.Drawable.glavo_audio_recorder);
                        _mLable.SetText(Resource.String.GlavoAudioRecordButtonUpToCancel);
                        break;
                    case DialogState.WantToCancel:
                        _mVoice.Visibility = ViewStates.Gone;
                        _mIcon.SetImageResource(Resource.Drawable.glavo_audio_cancel);
                        _mLable.SetText(Resource.String.GlavoAudioRecordButtonWantToCancel);
                        break;
                    case DialogState.TooShort:
                        _mVoice.Visibility = ViewStates.Gone;
                        _mIcon.SetImageResource(Resource.Drawable.glavo_audio_voice_too_short);
                        _mLable.SetText(Resource.String.GlavoAudioRecordButtonTooShort);
                        break;
                }
            }
        }
        /// <summary>
        /// 设置正在录音时的dialog界面
        /// </summary>
        public void Recording()
        {
            DialogStatusChange(DialogState.Recording);
        }
       /// <summary>
       /// 取消页面
       /// </summary>
        public void WantToCancel()
        {
            DialogStatusChange(DialogState.WantToCancel);
        }

        /// <summary>
        /// 时间过短
        /// </summary>
        public void TooShort()
        {
            DialogStatusChange(DialogState.TooShort);
        }

        /// <summary>
        /// 隐藏dialog
        /// </summary>
        public void DimissDialog()
        {
            DialogStatusChange(DialogState.DimissDialog);
        }
        #endregion

        public void UpdateVoiceLevel(int level)
        {
            if (_mDialog == null || !_mDialog.IsShowing) return;
            //			mIcon.setVisibility(View.VISIBLE);
            //			mVoice.setVisibility(View.VISIBLE);
            //			mLable.setVisibility(View.VISIBLE);
            var resId = _mContext.Resources.GetIdentifier($"glavo_audio_v{level}",
                "drawable", _mContext.PackageName);
            _mVoice.SetImageResource(resId);
        }
    }
}