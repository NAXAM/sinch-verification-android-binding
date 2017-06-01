using Android.App;
using Android.Widget;
using Android.OS;
using Android.Text;
using Java.Util;
using Android;
using Android.Telephony;
using Android.Content;
using Android.Support.V4.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using PhoneNumberUtils = Com.Sinch.Verification.PhoneNumberUtils;
using Android.Graphics;
using Com.Sinch.Verification;
using System;
using Android.Util;

namespace SinchVerificationQs
{
    [Activity(Label = "SinchVerificationQs", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        public static readonly string SMS = "sms";
        public static readonly string FLASHCALL = "flashcall";
        public static readonly string INTENT_PHONENUMBER = "phonenumber";
        public static readonly string INTENT_METHOD = "method";

        private EditText mPhoneNumber;
        private Button mSmsButton;
        private Button mFlashCallButton;
        private string mCountryIso;

        class XLogger : Java.Lang.Object, ILogger
        {
            public void Println(int priority, string tag, string message)
            {
                Log.WriteLine((LogPriority)priority, tag, message);
            }
        }

        static MainActivity()
        {
            // Provide an external logger
            //TODO
            SinchVerification.SetLogger(new XLogger());
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            mPhoneNumber = FindViewById<EditText>(Resource.Id.phoneNumber);
            mSmsButton = FindViewById<Button>(Resource.Id.smsVerificationButton);
            mFlashCallButton = FindViewById<Button>(Resource.Id.callVerificationButton);

            mCountryIso = PhoneNumberUtils.GetDefaultCountryIso(this);
            string defaultCountryName = new Locale("", mCountryIso).DisplayName;
            CountrySpinner spinner = FindViewById<CountrySpinner>(Resource.Id.spinner);
            spinner.Init(defaultCountryName);
            spinner.CountryIsoSelected += (sender, selectedIso) =>
            {
                if (selectedIso != null)
                {
                    mCountryIso = selectedIso;

                    mPhoneNumber.Text = mCountryIso;
                }
            };
            mPhoneNumber.AfterTextChanged += delegate {
                if (IsPossiblePhoneNumber())
                {
                    SetButtonsEnabled(true);
                    mPhoneNumber.SetTextColor(Color.Black);
                }
                else
                {
                    SetButtonsEnabled(false);
                    mPhoneNumber.SetTextColor(Color.Red);
                }
            };

            TryAndPrefillPhoneNumber();

            mSmsButton.Click += delegate {
                OnButtonClicked(mSmsButton);
            };
            mFlashCallButton.Click += delegate {
                OnButtonClicked(mFlashCallButton);
            };
        }

        private void TryAndPrefillPhoneNumber()
        {
            if (CheckCallingOrSelfPermission(Manifest.Permission.ReadPhoneState) == Permission.Granted)
            {
                TelephonyManager manager = (TelephonyManager)GetSystemService(Context.TelephonyService);
                mPhoneNumber.Text = (manager.Line1Number);
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadPhoneState }, 0);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                TryAndPrefillPhoneNumber();
            }
            else
            {
                if (ActivityCompat.ShouldShowRequestPermissionRationale(this, permissions[0]))
                {
                    Toast.MakeText(this, "This application needs permission to read your phone number to automatically pre-fill it", ToastLength.Long).Show();
                }
            }
        }

        private void OpenActivity(string phoneNumber, string method)
        {
            Intent verification = new Intent(this, typeof(VerificationActivity));
            verification.PutExtra(INTENT_PHONENUMBER, phoneNumber);
            verification.PutExtra(INTENT_METHOD, method);
            StartActivity(verification);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            mSmsButton.Enabled = (enabled);
            mFlashCallButton.Enabled = (enabled);
        }

        public void OnButtonClicked(View view)
        {
            if (view == mSmsButton)
            {
                OpenActivity(GetE164Number(), SMS);
            }
            else if (view == mFlashCallButton)
            {
                OpenActivity(GetE164Number(), FLASHCALL);
            }
        }

        private bool IsPossiblePhoneNumber()
        {
            return PhoneNumberUtils.IsPossibleNumber(mPhoneNumber.Text, mCountryIso);
        }

        private string GetE164Number()
        {
            return PhoneNumberUtils.FormatNumberToE164(mPhoneNumber.Text, mCountryIso);
        }
    }
}

