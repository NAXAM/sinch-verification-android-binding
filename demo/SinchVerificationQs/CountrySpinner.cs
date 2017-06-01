using System;
using System.Collections.Generic;
using Android.Content;
using Android.Widget;
using Android.Util;
using Java.Util;

namespace SinchVerificationQs
{
    class CountrySpinner : Spinner
    {
        public event EventHandler<string> CountryIsoSelected;

        private Dictionary<String, String> mCountries = new Dictionary<String, String>();

        public CountrySpinner(Context context) : base(context)
        {
        }

        public CountrySpinner(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public void Init(string defaultCountry)
        {
            InitCountries();
            List<String> countryList = new List<String>();

            countryList.AddRange(mCountries.Keys);
            countryList.Remove(defaultCountry);
            countryList.Insert(0, defaultCountry);

            ArrayAdapter adapter = new ArrayAdapter<String>(Context, Android.Resource.Layout.SimpleSpinnerItem, countryList);

            Adapter = (adapter);

            ItemSelected += (sender, e) =>
            {
                AdapterView adapterView = e.Parent;
                string selectedCountry = (String)adapterView.GetItemAtPosition(e.Position);
                NotifyListeners(selectedCountry);
            };
        }

        private void InitCountries()
        {
            String[] isoCountryCodes = Locale.GetISOCountries();
            foreach (string iso in isoCountryCodes)
            {
                string country = new Locale("", iso).DisplayCountry;
                mCountries.Add(country, iso);
            }
        }

        private void NotifyListeners(string selectedCountry)
        {
            string selectedIso = mCountries[selectedCountry];
            CountryIsoSelected?.Invoke(this, selectedIso);
        }
    }
}