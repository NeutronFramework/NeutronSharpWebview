using NeutronSharpWebview.Content;
using NeutronSharpWebview.Core;

namespace Test;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using (var webview = new Webview(true, true))
        {
            webview.SetTitle("Hello Webview");
            webview.SetSize(960, 540, WebviewHint.None);
            webview.Center();
            webview.Navigate(new UrlContent("https://annasvirtual.vercel.app"));
            webview.Run();
        }

    }
}
