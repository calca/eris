using Foundation;
using UIKit;

namespace eris.UI;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        // On Mac Catalyst, MSAL uses ASWebAuthenticationSession which handles
        // auth continuation internally — no AuthenticationContinuationHelper needed.
        return base.OpenUrl(application, url, options);
    }
}
