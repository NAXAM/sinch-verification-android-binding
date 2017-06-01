using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using Android;
using Com.Sinch.Verification;
using Android.Util;
using Android.Support.V4.Content;
using Android.Content.PM;

namespace SinchVerificationQs
{
    [Activity(Label = "VerificationActivity", ScreenOrientation = ScreenOrientation.Portrait)]
    public class VerificationActivity : Activity, ActivityCompat.IOnRequestPermissionsResultCallback
    {
        private static readonly string TAG = "VerificationActivity";

        private static readonly string APPLICATION_KEY = "enter-app-key";

        private IVerification mVerification;
        private bool mIsSmsVerification;
        private bool mShouldFallback = true;
        private string mPhoneNumber;
        private static readonly String[] SMS_PERMISSIONS = {
            Manifest.Permission.Internet,
            Manifest.Permission.ReadSms,
            Manifest.Permission.ReceiveSms,
            Manifest.Permission.AccessNetworkState
        };
        private static readonly String[] FLASHCALL_PERMISSIONS = {
            Manifest.Permission.Internet,
            Manifest.Permission.ReadPhoneState,
            Manifest.Permission.ReadCallLog,
            Manifest.Permission.CallPhone,
            Manifest.Permission.AccessNetworkState
        };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_verification);

            var btn = FindViewById<Button>(Resource.Id.codeInputButton);
            btn.Click += delegate {
                OnSubmitClicked(btn);
            };

            Intent intent = Intent;
            if (intent != null)
            {
                mPhoneNumber = intent.GetStringExtra(MainActivity.INTENT_PHONENUMBER);
                string method = intent.GetStringExtra(MainActivity.INTENT_METHOD);
                mIsSmsVerification = method.Equals(MainActivity.SMS);
                TextView phoneText = FindViewById<TextView>(Resource.Id.numberText);
                phoneText.Text = (mPhoneNumber);

                RequestPermissions();
            }
            else
            {
                Log.Error(TAG, "The provided intent is null.");
            }
        }

        private void RequestPermissions()
        {
            List<String> missingPermissions;
            string methodText;

            if (mIsSmsVerification)
            {
                missingPermissions = GetMissingPermissions(SMS_PERMISSIONS);
                methodText = "SMS";
            }
            else
            {
                missingPermissions = GetMissingPermissions(FLASHCALL_PERMISSIONS);
                methodText = "calls";
            }

            if (missingPermissions.Count == 0)
            {
                CreateVerification();
            }
            else
            {
                if (NeedPermissionsRationale(missingPermissions))
                {
                    Toast.MakeText(this, "This application needs permissions to read your " + methodText + " to automatically verify your "
                            + "phone, you may disable the permissions once you have been verified.", ToastLength.Long)
                            .Show();
                }
                ActivityCompat.RequestPermissions(this,
                                                  missingPermissions.ToArray(),
                                                  0);
            }
        }

        private void CreateVerification()
        {
            // It is important to pass ApplicationContext to the Verification config builder as the
            // verification process might outlive the activity.
            IConfig config = SinchVerification.Config()
                                             .ApplicationKey(APPLICATION_KEY)
                                             .Context(ApplicationContext)
                                             .Build();
            TextView messageText = FindViewById<TextView>(Resource.Id.textView);

            IVerificationListener listener = new MyVerificationListener(this);

            if (mIsSmsVerification)
            {
                messageText.SetText(Resource.String.sending_sms);
                mVerification = SinchVerification.CreateSmsVerification(config, mPhoneNumber, listener);
                mVerification.Initiate();
            }
            else
            {
                messageText.SetText(Resource.String.flashcalling);
                mVerification = SinchVerification.CreateFlashCallVerification(config, mPhoneNumber, listener);
                mVerification.Initiate();
            }

            ShowProgress();
        }

        private bool NeedPermissionsRationale(List<String> permissions)
        {
            foreach (string permission in permissions)
            {
                if (ActivityCompat.ShouldShowRequestPermissionRationale(this, permission))
                {
                    return true;
                }
            }
            return false;
        }
        
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            // Proceed with verification after requesting permissions.
            // If the verification SDK fails to intercept the code automatically due to missing permissions,
            // the VerificationListener.onVerificationFailed(1) method will be executed with an instance of
            // CodeInterceptionException. In this case it is still possible to proceed with verification
            // by asking the user to enter the code manually.
            CreateVerification();
        }

        private List<String> GetMissingPermissions(String[] requiredPermissions)
        {
            List<String> missingPermissions = new List<string>();
            foreach (string permission in requiredPermissions)
            {
                if (ContextCompat.CheckSelfPermission(ApplicationContext, permission)
                        != Android.Content.PM.Permission.Granted)
                {
                    missingPermissions.Add(permission);
                }
            }
            return missingPermissions;
        }

        public void OnSubmitClicked(View view)
        {
            string code = (FindViewById<EditText>(Resource.Id.inputCode)).Text;
            if (!string.IsNullOrEmpty(code))
            {
                if (mVerification != null)
                {
                    mVerification.Verify(code);
                    ShowProgress();
                    TextView messageText = (TextView)FindViewById(Resource.Id.textView);
                    messageText.Text = ("Verification in progress");
                    EnableInputField(false);
                }
            }
        }

        private void EnableInputField(bool enable)
        {
            View container = FindViewById(Resource.Id.inputContainer);
            if (enable)
            {
                TextView hintText = (TextView)FindViewById(Resource.Id.enterToken);
                hintText.SetText(mIsSmsVerification ? Resource.String.sms_enter_code : Resource.String.flashcall_enter_cli);
                container.Visibility = (ViewStates.Visible);
                EditText input = (EditText)FindViewById(Resource.Id.inputCode);
                input.RequestFocus();
            }
            else
            {
                container.Visibility = (ViewStates.Gone);
            }
        }

        private void HideProgressAndShowMessage(int message)
        {
            HideProgress();
            TextView messageText = (TextView)FindViewById(Resource.Id.textView);
            messageText.SetText(message);
        }

        private void HideProgress()
        {
            ProgressBar progressBar = (ProgressBar)FindViewById(Resource.Id.progressIndicator);
            progressBar.Visibility = (ViewStates.Invisible);
            TextView progressText = (TextView)FindViewById(Resource.Id.progressText);
            progressText.Visibility = (ViewStates.Invisible);
        }

        private void ShowProgress()
        {
            ProgressBar progressBar = (ProgressBar)FindViewById(Resource.Id.progressIndicator);
            progressBar.Visibility = (ViewStates.Visible);
        }

        private void ShowCompleted()
        {
            ImageView checkMark = (ImageView)FindViewById(Resource.Id.checkmarkImage);
            checkMark.Visibility = (ViewStates.Visible);
        }

        class MyVerificationListener : Java.Lang.Object, IVerificationListener
        {
            readonly VerificationActivity outer;
            public MyVerificationListener(VerificationActivity outer)
            {
                this.outer = outer;
            }
            public void OnInitiated(IInitiationResult result)
            {
                Log.Debug(TAG, "Initialized!");
                outer.ShowProgress();
            }

            public void OnInitiationFailed(Java.Lang.Exception exception)
            {
                Log.Error(TAG, "Verification initialization failed: " + exception.Message);
                outer.HideProgressAndShowMessage(Resource.String.failed);

                if (exception is InvalidInputException)
                {
                    // Incorrect number provided
                }
                else if (exception is ServiceErrorException)
                {
                    // Verification initiation aborted due to early reject feature,
                    // client callback denial, or some other Sinch service error.
                    // Fallback to other verification method here.

                    if (outer.mShouldFallback)
                    {
                        outer.mIsSmsVerification = !outer.mIsSmsVerification;
                        if (outer.mIsSmsVerification)
                        {
                            Log.Info(TAG, "Falling back to sms verification.");
                        }
                        else
                        {
                            Log.Info(TAG, "Falling back to flashcall verification.");
                        }
                        outer.mShouldFallback = false;
                        // Initiate verification with the alternative method.
                        outer.RequestPermissions();
                    }
                }
                else
                {
                    // Other system error, such as UnknownHostException in case of network error
                }
            }

            public void OnVerified()
            {
                Log.Debug(TAG, "Verified!");
                outer.HideProgressAndShowMessage(Resource.String.verified);
                outer.ShowCompleted();
            }

            public void OnVerificationFailed(Java.Lang.Exception exception)
            {
                Log.Error(TAG, "Verification failed: " + exception.Message);
                if (exception is CodeInterceptionException)
                {
                    // Automatic code interception failed, probably due to missing permissions.
                    // Let the user try and enter the code manually.
                    outer.HideProgress();
                }
                else
                {
                    outer.HideProgressAndShowMessage(Resource.String.failed);
                }
                outer.EnableInputField(true);
            }
        }
    }
}