using Android;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;

namespace Xamarin_1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        enum ScanState { start, stop };
        enum PermissionRequestCode { REQUEST_BLUETOOTH = 1, REQUEST_BLUETOOTH_ADMIN, REQUEST_ACCESS_COARSE_LOCATION, REQUEST_ACCESS_FINE_LOCATION };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // Get our UI controls from the loaded layout
            Button btBlutoothAdapter = FindViewById<Button>(Resource.Id.BlutoothAdapter);
            TextView txtAdptName = FindViewById<TextView>(Resource.Id.BlutoothAdapterName);
            Button btScan = FindViewById<Button>(Resource.Id.StartScan);
            TextView txtDeviceAddr = FindViewById<TextView>(Resource.Id.BluetoothDeviceAddr);

            // Get App context
            Context ctxApp = Android.App.Application.Context;

            // Check permissions
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(ctxApp, Manifest.Permission.Bluetooth) == Android.Content.PM.Permission.Denied)
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] {Manifest.Permission.Bluetooth}, (int)PermissionRequestCode.REQUEST_BLUETOOTH);

            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(ctxApp, Manifest.Permission.BluetoothAdmin) == Android.Content.PM.Permission.Denied)
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.BluetoothAdmin }, (int)PermissionRequestCode.REQUEST_BLUETOOTH_ADMIN);

            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(ctxApp, Manifest.Permission.AccessCoarseLocation) == Android.Content.PM.Permission.Denied)
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessCoarseLocation }, (int)PermissionRequestCode.REQUEST_ACCESS_COARSE_LOCATION);

            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(ctxApp, Manifest.Permission.AccessFineLocation) == Android.Content.PM.Permission.Denied)
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, (int)PermissionRequestCode.REQUEST_ACCESS_FINE_LOCATION);

            // Create BluetoothAdapter
            Core.BlutoothAdapter blu = new Core.BlutoothAdapter(ctxApp);

            // Add code to handle button clicks
            btBlutoothAdapter.Click += (sender, e) =>
            {
                if (blu != null)
                {
                    string strBtAdptName = blu.GetName();
                    txtAdptName.Text = strBtAdptName;
                }
            };

            ScanState scanState = ScanState.start;

            btScan.Click += (sender, e) =>
            {
                if (blu != null)
                {
                    if (scanState == ScanState.start)
                    {
                        if (blu.StartLeScan(txtAdptName))
                        {
                            scanState = ScanState.stop;
                            btScan.Text = "Stop Scan";
                        }
                    }
                    else
                    {
                        blu.StopLeScan();
                        scanState = ScanState.start;
                        btScan.Text = "Start Scan";
                    }
                }
            };
        }
    }
}