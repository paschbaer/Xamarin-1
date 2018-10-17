using Android.Bluetooth;

namespace Core
{
    public class BlutoothAdapter
    {
        protected BluetoothAdapter btAdtp;

        public BlutoothAdapter()
        {
            btAdtp = BluetoothAdapter.DefaultAdapter;
        }

        public string GetName()
        {
            if (btAdtp != null)
            {
                if (btAdtp.IsEnabled)
                    return btAdtp.Name;

                return "disabled";
            }

            return "unknown";
        }
    }
}