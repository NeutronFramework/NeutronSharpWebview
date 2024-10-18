using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using Gtk;
using Action = System.Action;
using Rect = NeutronSharpWebview.API.WinAPI.Rect;
using WinAPI = NeutronSharpWebview.API.WinAPI;
using NeutronSharpWebview.Content;

namespace NeutronSharpWebview.Core;

/// <summary>
/// A cross platform webview.
/// This is a binding class for the native implementation of https://github.com/zserge/webview.
/// </summary>
public class Webview : IDisposable
{
    private bool disposed = false;
    private bool? loopbackEnabled = null;
    private List<CallBackFunction> callbacks = new List<CallBackFunction>();
    private List<DispatchFunction> dispatchFunctions = new List<DispatchFunction>();

    private readonly nint nativeWebview;

    /// <summary>
    /// Creates a new webview object.
    /// </summary>
    /// <param name="debug">
    /// Set to true, to activate a debug view, 
    /// if the current webview implementation supports it.
    /// </param>
    /// <param name="interceptExternalLinks">
    /// Set to true, top open external links in system browser.
    /// </param>
    public Webview(bool debug = false, bool interceptExternalLinks = false)
    {
        nativeWebview = Bindings.webview_create(debug ? 1 : 0, nint.Zero);
        if (interceptExternalLinks)
        {
            InterceptExternalLinks();
        }
    }

    /// <summary>
    /// Set the title of the webview application window.
    /// </summary>
    /// <param name="title">The new title.</param>
    /// <returns>The webview object for a fluent api.</returns>
    public Webview SetTitle(string title)
    {
        Bindings.webview_set_title(nativeWebview, title);
        return this;
    }

    /// <summary>
    /// Set the size information of the webview application window.
    /// </summary>
    /// <param name="width">The width of the webview application window.</param>
    /// <param name="height">The height of the webview application window.</param>
    /// <param name="hint">The type of the size information.</param>
    /// <returns>The webview object for a fluent api.</returns>
    public Webview SetSize(int width, int height, WebviewHint hint)
    {
        Bindings.webview_set_size(nativeWebview, width, height, hint);
        return this;
    }

    /// <summary>
    /// Injects JavaScript code at the initialization of the new page. Every time
    /// the webview will open a new page. this initialization code will be
    /// executed. It is guaranteed that code is executed before window.onload.
    /// </summary>
    /// <remarks>
    /// Execute this method before <see cref="Navigate(IWebviewContent)"/>
    /// </remarks>
    /// <param name="javascript">The javascript code to execute.</param>
    /// <returns>The webview object for a fluent api.</returns>
    public Webview InitScript(string javascript)
    {
        Bindings.webview_init(nativeWebview, javascript);
        return this;
    }

    /// <summary>
    /// Navigates webview to the given content.
    /// </summary>
    /// <param name="webviewContent">The content to navigate to.</param>
    /// <remarks>Content can be a UrlContent, HtmlContent or WebhostContent</remarks>
    /// <returns>The webview object for a fluent api.</returns>
    public Webview Navigate(IWebviewContent webviewContent)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        // Only check the loopback exceptions once, if the url routes to the localhsot
        // We want to avoid the check on each navigation!
        // If the current url is not routing to the localhost, the check will return 'null'
        if (isWindows && loopbackEnabled == null) loopbackEnabled = CheckLoopbackException(webviewContent.ToWebviewUrl());
        if (isWindows && loopbackEnabled != null && !loopbackEnabled.Value)
        {
            Bindings.webview_navigate(nativeWebview, new HtmlContent("Loopback not enabled!").ToWebviewUrl());
        }
        else
        {
            Bindings.webview_navigate(nativeWebview, webviewContent.ToWebviewUrl());
        }
        return this;
    }

    /// <summary>
    /// Binds a callback so that it will appear under the given name as a global JavaScript function. 
    /// </summary>
    /// <param name="name">Global name of the javascript function.</param>
    /// <param name="callback">Callback with two parameters. id -> The id of the call, req -> The parameters of the call as json</param>
    /// <returns>The webview object for a fluent api.</returns>
    public Webview Bind(string name, Action<string, string> callback)
    {
        var callbackInstance = new CallBackFunction((idPtr, parametersPtr, _) =>
        {
            var id = Marshal.PtrToStringUTF8(idPtr);
            var parameters = Marshal.PtrToStringUTF8(parametersPtr);

            callback(id, parameters);
        });

        callbacks.Add(callbackInstance); // Pin the callback for the GC

        Bindings.webview_bind(nativeWebview, name, callbackInstance, nint.Zero);
        return this;
    }

    /// <summary>
    /// Center the webview application window
    /// </summary>
    /// <returns>The webview object for a fluent api.</returns>
    /// <exception cref="NotSupportedException">Thrown when the operating system is not supported</exception>
    public Webview Center()
    {
        nint windowPtr = Bindings.webview_get_window(nativeWebview);

        if (OperatingSystem.IsWindows())
        {
            Rect rect;

            WinAPI.GetWindowRect(windowPtr, out rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            int screenWidth = WinAPI.GetSystemMetrics(WinAPI.SM_CXSCREEN);
            int screenHeight = WinAPI.GetSystemMetrics(WinAPI.SM_CYSCREEN);

            int x = (screenWidth - width) / 2;
            int y = (screenHeight - height) / 2;

            WinAPI.SetWindowPos(windowPtr, nint.Zero, x, y, 0, 0, WinAPI.SWP_NOSIZE | WinAPI.SWP_NOZORDER);
        }
        else if (OperatingSystem.IsLinux())
        {
            var window = new Window(windowPtr);

            window.SetPosition(WindowPosition.Center);
        }
        else
        {
            throw new NotSupportedException("Operating system not supported.");
        }

        return this;
    }

    /// <summary>
    /// Maximize the webview application window
    /// </summary>
    /// <returns>The webview object for a fluent api.</returns>
    /// <exception cref="NotSupportedException">Thrown when the operating system is not supported</exception>
    public Webview Maximize()
    {
        nint windowPtr = Bindings.webview_get_window(nativeWebview);

        if (OperatingSystem.IsWindows())
        {
            WinAPI.ShowWindow(windowPtr, WinAPI.SW_MAXIMIZE);
        }
        else if (OperatingSystem.IsLinux())
        {
            var window = new Window(windowPtr);
            window.Maximize();
        }
        else
        {
            throw new NotSupportedException("Operating system not supported.");
        }

        return this;
    }

    /// <summary>
    /// Minimize the webview application window
    /// </summary>
    /// <returns>The webview object for a fluent api.</returns>
    /// <exception cref="NotSupportedException">Thrown when the operating system is not supported</exception>
    public Webview Minimize()
    {
        nint windowPtr = Bindings.webview_get_window(nativeWebview);

        if (OperatingSystem.IsWindows())
        {
            WinAPI.ShowWindow(windowPtr, WinAPI.SW_MINIMIZE);
        }
        else if (OperatingSystem.IsLinux())
        {
            var window = new Window(windowPtr);
            window.Iconify();
        }
        else
        {
            throw new NotSupportedException("Operating system not supported.");
        }

        return this;
    }

    /// <summary>
    /// Set the size of webview application window
    /// </summary>
    /// <param name="width">The width of the window</param>
    /// <param name="height">The height of the window</param>
    /// <returns>The webview object for a fluent api.</returns>
    /// <exception cref="NotSupportedException">Thrown when the operating system is not supported</exception>
    public Webview SetSize(int width, int height)
    {
        nint windowPtr = Bindings.webview_get_window(nativeWebview);

        if (OperatingSystem.IsWindows())
        {
            WinAPI.SetWindowPos(windowPtr, 0, 0, 0, width, height, WinAPI.SWP_NOZORDER | WinAPI.SWP_NOMOVE);
        }
        else if (OperatingSystem.IsLinux())
        {
            var window = new Window(windowPtr);
            window.SetDefaultSize(width, height);
        }
        else
        {
            throw new NotSupportedException("Operating system not supported.");
        }

        return this;
    }

    /// <summary>
    /// Runs the main loop of the webview. Should be used as the last statement.
    /// </summary>
    /// <returns>The webview object.</returns>
    public Webview Run()
    {
        Bindings.webview_run(nativeWebview);
        return this;
    }

    /// <summary>
    /// Allows to return a value to the caller of a bound callback <see cref="Bind(string, Action{string, string})"/>.
    /// </summary>
    /// <param name="id">The id of the call.</param>
    /// <param name="result">The result of the call.</param>
    /// <param name="resultJson">The result data as json.</param>
    public void Return(string id, RPCResult result, string resultJson)
    {
        Bindings.webview_return(nativeWebview, id, result, resultJson);
    }

    /// <summary>
    /// Evaluates arbitrary JavaScript code. Evaluation happens asynchronously, also
    /// the result of the expression is ignored. Use bindings if you want to
    /// receive notifications about the results of the evaluation.
    /// </summary>
    /// <param name="javascript">The javascript to execute.</param>
    public void Evaluate(string javascript)
    {
        Bindings.webview_eval(nativeWebview, javascript);
    }

    /// <summary>
    /// Posts a function to be executed on the main thread of the webview.
    /// </summary>
    /// <param name="dispatchFunc">The function to call on the main thread</param>
    public void Dispatch(Action dispatchFunc)
    {
        DispatchFunction dispatchFuncInstance = null!;

        dispatchFuncInstance = new DispatchFunction((_, __) =>
        {
            lock (dispatchFunctions)
            {
                dispatchFunctions.Remove(dispatchFuncInstance);
            }
            dispatchFunc();
        });

        lock (dispatchFunctions)
        {
            dispatchFunctions.Add(dispatchFuncInstance); // Pin the callback for the GC
        }

        Bindings.webview_dispatch(nativeWebview, dispatchFuncInstance, nint.Zero);
    }

    /// <summary>
    /// Disposes the current webview.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            Bindings.webview_terminate(nativeWebview);
            Bindings.webview_destroy(nativeWebview);
            callbacks.Clear();

            lock (dispatchFunctions)
            {
                dispatchFunctions.Clear();
            }

            disposed = true;
        }
    }

    private void InterceptExternalLinks()
    {
        // Bind a native method as JavaScript
        Bind("openExternalLink", (id, request) =>
        {
            // Deserialize the request into a JsonElement
            var arguments = JsonSerializer.Deserialize<JsonElement>(request);

            // Assuming 'req' contains a JSON array, get the first element (the URL)
            if (arguments.ValueKind == JsonValueKind.Array && arguments.GetArrayLength() > 0)
            {
                string url = arguments[0].GetString();

                // Open the URL in the system browser
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }

            // Return a success result
            Return(id, RPCResult.Success, "{}");
        });

        // Inject JavaScript to intercept external links
        InitScript(@"
        function interceptClickEvent(event) {
            var href = '';
            var target = event.target || event.srcElement;

            if (target.tagName === 'A') {
                href = target.getAttribute('href');
            } else if (target.tagName === 'IMG') {
                href = target.parentElement.getAttribute('href');
            }

            if (href.startsWith('http') && !href.startsWith('http://localhost') && !href.startsWith('http://127.0.0.1')) {
                openExternalLink(href);
                event.preventDefault();
            }
        }

        if (document.addEventListener) {
            document.addEventListener('click', interceptClickEvent);
        } else if (document.attachEvent) {
            document.attachEvent('onclick', interceptClickEvent);
        }
    ");
    }

    private bool? CheckLoopbackException(string url)
    {
        // https://docs.microsoft.com/de-de/windows/win32/sysinfo/operating-system-version
        if (Environment.OSVersion.Version.Major < 6 || Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor < 2)
        {
            return true;
        }
        else if (url.Contains("localhost") && !url.Contains("127.0.0.1"))
        {
            return null;
        }

        var loopBack = new Loopback();

        return loopBack.IsWebViewLoopbackEnabled();
    }

    ~Webview()
    {
        Dispose(false);
    }
}
