using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;

namespace Xamarin_1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // Get our UI controls from the loaded layout
            Button btBlutoothAdapter = FindViewById<Button>(Resource.Id.BlutoothAdapter);
            TextView txtAdptName = FindViewById<TextView>(Resource.Id.BlutoothAdapterName);

            // Add code to handle button clicks
            btBlutoothAdapter.Click += (sender, e) =>
            {
                Core.BlutoothAdapter blu = new Core.BlutoothAdapter();
                string strBtAdptName = blu.GetName();
                txtAdptName.Text = strBtAdptName;
            };
            
        }
    }
}