using System;
using Android.App;
using Android.Content;
using Android.Bluetooth;
using Android.Views;
using Android.Util;
using Android.Runtime;

namespace Blu
{
    public class BlutoothAdapter
    {
        public readonly static String TAG = typeof(BlutoothAdapter).Name;

        protected BluetoothAdapter adapter;
        protected BluetoothGatt gatt;
        protected System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices = new System.Collections.Generic.Dictionary<string, BluetoothDevice>();
        protected ScanCallBack scancallback;

        public BlutoothAdapter()
        {}

        public string GetName()
        {
            if (adapter != null)
            {
                if (adapter.IsEnabled)
                    return adapter.Name;

                return "disabled";
            }

            return "unknown";
        }

        public bool StartLeScan(Activity activity, Android.Widget.ListView textout)
        {
            if (adapter == null)
            {
                BluetoothManager manager = (BluetoothManager)activity.ApplicationContext.GetSystemService(Context.BluetoothService);
                adapter = manager.Adapter;
            }

            if (scancallback == null)
                scancallback = new ScanCallBack(adapter, activity, mapDevices, textout);
            else
                scancallback.Reset();

            if (scancallback != null)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(o => scancallback.Run());
                return true;
            }

            return false;
        }

        public void StopLeScan()
        {
            if ((scancallback != null) && (adapter != null))
                adapter.StopLeScan(scancallback);
        }

        public void EnumServices(Context ctx, string identifer)
        {
            BluetoothDevice device = mapDevices[identifer];
            if (device != null)
            {
                GattDevice gattdevice = new GattDevice(ctx, identifer);
                if (gatt != null)
                {
                    //gatt.Disconnect();
                    gatt.Close();   //or gatt.Connect() to re-connect to device
                    gatt.Dispose();
                }

                gatt = device.ConnectGatt(ctx, false, gattdevice);  //-> OnConnectionStateChange() -> DiscoverServices() -> OnServicesDiscovered() -> GattDevice.Initialize()
                if (gatt == null)
                {
                    Log.Error(TAG, string.Format("failed to connect to GATT server '{0}'", identifer));
                }
            }
        }
    }

    public class ScanCallBack : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
    {
        private BluetoothAdapter adapter;
        private System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices;
        private Android.Widget.ListView textout;
        private StringListAdapter listAdapter; 

        public ScanCallBack(BluetoothAdapter adapter, Activity activity, System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices, Android.Widget.ListView textout)
        {
            this.adapter = adapter;
            this.mapDevices = mapDevices;
            this.textout = textout;

            listAdapter = new StringListAdapter(activity);
            this.textout.Adapter = listAdapter;
        }
            
        public void Reset()
        {
            //textout.Text = string.Empty;
            mapDevices.Clear();
            listAdapter.Reset();
        }

        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
        {
            string deviceName = device.Name;
            if (string.IsNullOrEmpty(device.Name))
                deviceName = device.Address;

            if (!mapDevices.ContainsKey(deviceName))
            {
                mapDevices.Add(deviceName, device);
                listAdapter.Add(deviceName);

                UpdateTextOut(textout);
            }
        }

        public void UpdateTextOut(Android.Widget.ListView textout)
        {
            textout.Adapter = null;
            textout.Adapter = listAdapter;
        }

        public void Run()
        {
            adapter.StartLeScan(this);
        }
    }

    public class GattDevice : BluetoothGattCallback
    {
        public readonly static String TAG = typeof(GattDevice).Name;

        //public GenericAccessService genericAccess;
        protected SampleService sampleService;

        protected Context ctx;
        protected string gattServerName;

        protected static Java.Util.UUID SERVICE_GENERIC_ACCESS = Java.Util.UUID.FromString("00001800-0000-1000-8000-00805f9b34fb");    //org.bluetooth.service.generic_access

        public GattDevice(Context ctx, string identifier)
        {
            this.ctx = ctx;
            gattServerName = identifier;
        }

        public virtual void Initialize(BluetoothGatt gatt)
        {
            DumpServices(gatt);

            BluetoothGattService service = gatt.Services[0];
            sampleService = new SampleService(gatt, service);

            /*foreach (BluetoothGattService service in gatt.Services)
            {
                BlutoothService.Dump(service, gattServerName);

                if (sampleService != null)
                {
                    sampleService.Dispose();
                    sampleService = null;
                }

                sampleService = new SampleService(gatt, service);
            }*/

            //BluetoothGattService service = gatt.GetService(SERVICE_GENERIC_ACCESS);
            //if (service != null)
            //{
            //    genericAccess = new GenericAccessService(gatt, service);
            //}
        }

        protected static void DumpServices(BluetoothGatt gatt)
        {
            foreach (BluetoothGattService service in gatt.Services)
            {
                Log.Debug(TAG, string.Format("UUID: '0x{0:X}'", BlutoothService.GetAssignedNumber(service.Uuid)));
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            if (BlutoothService.GetAssignedNumber(characteristic.Service.Uuid) == 0x1800)   //org.bluetooth.service.generic_access
            {
                //if (genericAccess != null)
                //    genericAccess.ReadCharacteristic(characteristic);

                if (sampleService != null)
                {
                    sampleService.ReadCharacteristic(characteristic);
                    sampleService.ReadNextCharacteristic();
                }

            }
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicWrite(gatt, characteristic, status);
        }
        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState state)
        {
            if (state == ProfileState.Connected)
            {
                Log.Debug(TAG, string.Format("connected to GATT server '{0}'", gattServerName));
                if (gatt != null)
                {
                    Log.Debug(TAG, string.Format("discover services of '{0}'", gattServerName));
                    gatt.DiscoverServices();
                }
            }
            else if (state == ProfileState.Disconnected)
                Log.Debug(TAG, string.Format("disconnected from GATT server '{0}'", gattServerName));

            //base.OnConnectionStateChange(gatt, status, state);
        }

        public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorRead(gatt, descriptor, status);
        }

        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorWrite(gatt, descriptor, status);
        }

        public override void OnMtuChanged(BluetoothGatt gatt, Int32 mtu, GattStatus status)
        {
            base.OnMtuChanged(gatt, mtu, status);
        }

        public override void OnReadRemoteRssi(BluetoothGatt gatt, Int32 rssi, GattStatus status)
        {
            base.OnReadRemoteRssi(gatt, rssi, status);
        }

        public override void OnReliableWriteCompleted(BluetoothGatt gatt, GattStatus status)
        {
            base.OnReliableWriteCompleted(gatt, status);
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            if (status == GattStatus.Success)
            {
                Log.Debug(TAG, string.Format("services of '{0}' successfully retrieved", gattServerName));
                
                System.Threading.ThreadPool.QueueUserWorkItem(o => Initialize(gatt)); //Initialize(gatt);
            }
            else if (status == GattStatus.Success)
                Log.Error(TAG, string.Format("failed to retrieve services of '{0}'", gattServerName));

            //base.OnServicesDiscovered(gatt, status);
        }

        protected void UpdateUi(string key, string value)
        {// If desired, pass some values to the broadcast receiver.
            Intent message = new Intent("com.xamarin.example.BLU");
 
            message.PutExtra(key, value);
            Android.Support.V4.Content.LocalBroadcastManager.GetInstance(ctx).SendBroadcast(message);
        }
    }

    public class StringListAdapter : Android.Widget.BaseAdapter<string>
    {
        private System.Collections.Generic.List<string> listValues = new System.Collections.Generic.List<string>();
        private Activity activity;

        public StringListAdapter(Activity activity) : base()
        {
            this.activity = activity;
        }

        public void Add(string value)
        {
            listValues.Add(value);
        }

        public void Reset()
        {
            listValues.Clear();
            NotifyDataSetChanged();

        }

        public override string this[int position]
        {
            get { return listValues[position]; }
        }

        public override int Count
        {
            get { return listValues.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = activity.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);

            view.FindViewById<Android.Widget.TextView>(Android.Resource.Id.Text1).Text = listValues[position];

            return view;
        }
    }

    public abstract class BlutoothService
    {
        protected BluetoothGatt gatt = null;
        protected BluetoothGattService service = null;

        public BlutoothService(BluetoothGatt gatt, BluetoothGattService service)
        {
            this.gatt = gatt;
            this.service = service;
        }

        public static int GetAssignedNumber(Java.Util.UUID uuid)
        {
            // Keep only the significant bits of the UUID
            return (int)((uuid.MostSignificantBits & 0x0000FFFF00000000L) >> 32);
        }

        public static void Dump(BluetoothGattService service, string serviceName)
        {
            if (service != null)
            {
                foreach (BluetoothGattCharacteristic characteristic in service.Characteristics)
                {
                    Log.Debug(serviceName, string.Format("'0x{0:X}: characteristic's UUID : '0x{1:X}'", GetAssignedNumber(service.Uuid), GetAssignedNumber(characteristic.Uuid)));
                }
            }
        }

        public abstract string GetName();

    }

    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { "com.xamarin.example.BLU.SampleService" })]
    public class SampleService : BlutoothService
    {
        public readonly static String TAG = typeof(SampleService).Name;

        protected int index = 0;

        public SampleService(BluetoothGatt gatt, BluetoothGattService service) : base(gatt, service)
        {
            index = 0;

            if (service != null)
            {
                if (service.Characteristics.Count > 0)
                {
                    BluetoothGattCharacteristic characteristic = service.Characteristics[0];
                    gatt.ReadCharacteristic(characteristic);
                }
            }
        }

        public void ReadCharacteristic(BluetoothGattCharacteristic characteristic)
        {
            byte[] value = characteristic.GetValue();
            if (value != null)
                Log.Debug(TAG, "characteristic '0x{0:X}' - '{1}'", BlutoothService.GetAssignedNumber(characteristic.Uuid), BitConverter.ToString(value));
        }

        public void ReadNextCharacteristic()
        {
            index++;
            if (index < service.Characteristics.Count)
            {
                BluetoothGattCharacteristic characteristic = service.Characteristics[index];
                gatt.ReadCharacteristic(characteristic);
            }
        }

        public override string GetName()
        {
            return TAG;
        }
    }


    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { "com.xamarin.example.BLU.GenericAccessService" })]
    public class GenericAccessService : BlutoothService
    {
        public readonly static String TAG = typeof(GenericAccessService).Name;

        public string deviceName { get; private set; }
        public Int16 appearance { get; private set; }

        protected static Java.Util.UUID CHAR_DEVICE_NAME = Java.Util.UUID.FromString("00002A00-0000-1000-8000-00805f9b34fb");    //org.bluetooth.characteristic.gap.device_name
        protected static Java.Util.UUID CHAR_APPEARANCE = Java.Util.UUID.FromString("00002A01-0000-1000-8000-00805f9b34fb");    //org.bluetooth.characteristic.gap.appearance

        public GenericAccessService(BluetoothGatt gatt, BluetoothGattService service) : base(gatt, service)
        {
            if (service != null)
            {
                BluetoothGattCharacteristic characteristic = service.GetCharacteristic(CHAR_DEVICE_NAME);
                if (characteristic != null)
                    gatt.ReadCharacteristic(characteristic);
            }
        }

        public void ReadCharacteristic(BluetoothGattCharacteristic characteristic)
        {
            if (BlutoothService.GetAssignedNumber(characteristic.Uuid) == 0x2A00)   //org.bluetooth.characteristic.gap.device_name
            {
                byte[] value = characteristic.GetValue();
                if (value != null)
                    deviceName = BitConverter.ToString(value);
            }

            if (BlutoothService.GetAssignedNumber(characteristic.Uuid) == 0x2A01)   //org.bluetooth.characteristic.gap.appearance
            {
                byte[] value = characteristic.GetValue();
                if (value != null)
                    appearance = BitConverter.ToInt16(value, 0);
            }

        }

        public override string GetName()
        {
            return TAG;
        }
    }
}