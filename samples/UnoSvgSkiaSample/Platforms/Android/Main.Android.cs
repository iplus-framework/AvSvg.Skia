using Android.App;
using Android.Runtime;

namespace UnoSvgSkiaSample.Droid;

[Application(
    Label = "UnoSvgSkiaSample",
    LargeHeap = true,
    HardwareAccelerated = true)]
public sealed class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }
}
