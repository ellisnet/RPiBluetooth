using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using AndroidBtServer.Droid.Services;
using AndroidBtServer.Services;
using Prism;
using Prism.Ioc;

namespace AndroidBtServer.Droid
{
    [Activity(
        Label = "AndroidBtServer",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            //Initialization step required for CodeBrix
            CodeBrix.Prism.Platform.Init(this, savedInstanceState);

            LoadApplication(new App(new AndroidInitializer(this)));
        }

        //Override of OnRequestPermissionsResult required by CodeBrix and Xamarin.Essentials
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            CodeBrix.Prism.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    //Platform initializer for Prism (and CodeBrix)
    public class AndroidInitializer : IPlatformInitializer
    {
        private readonly Context _context;

        public void RegisterTypes(IContainerRegistry container)
        {
            // Register CodeBrix pages and services
            CodeBrix.Prism.Platform.RegisterTypes(container);

            // Register any platform-specific services created for this application
            container.RegisterInstance<IBluetoothHostService>(new AndroidBluetoothHostService(_context));
        }

        public AndroidInitializer(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
    }
}
