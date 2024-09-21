using System.Runtime.InteropServices;

namespace SharpWebview.Scripts.Core;

public enum WebviewHint
{
    /// <summary>
    /// Width and height are default size
    /// </summary>
    None = 0,
    /// <summary>
    /// Width and height are minimum bounds
    /// </summary>
    Min = 1,
    /// <summary>
    ///  Width and height are maximum bounds
    /// </summary>
    Max = 2,
    /// <summary>
    /// Window size can not be changed by a user
    /// </summary>
    Fixed = 3,
}

public enum WebviewNativeHandleKind {
    // Top-level window. @c GtkWindow pointer (GTK), @c NSWindow pointer (Cocoa) or @c HWND (Win32).
    UIWindow = 0,
    // Browser widget. @c GtkWidget pointer (GTK), @c NSView pointer (Cocoa) or @c HWND (Win32).
    UIWidget = 1,
    // Browser controller. @c WebKitWebView pointer (WebKitGTK), @c WKWebView pointer (Cocoa/WebKit) or @c ICoreWebView2Controller pointer (Win32/WebView2).
    BrowserController = 2
}

public enum RPCResult
{
    Success = 0,
    Error = 1,
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void DispatchFunction(nint webview, nint arguments);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void CallBackFunction(nint idPtr, nint parametersPtr, nint argument);

internal static partial class Bindings
{
    private const string libraryFile = "webview";

    /// <summary>
    /// <para>Creates a new webview instance.</para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API webview_t webview_create(int debug, void ///window);
    /// </para>
    /// </summary>
    /// <param name="debug">
    /// If debug is non-zero - developer tools will
    /// be enabled (if the platform supports them).
    /// </param>
    /// <param name="window">
    /// Window parameter can be a
    /// pointer to the native window handle. If it's non-null - then child WebView
    /// is embedded into the given parent window. Otherwise a new window is created.
    /// Depending on the platform, a GtkWindow, NSWindow or HWND pointer can be
    /// passed here.
    /// </param>
    /// <returns></returns>
    [LibraryImport(libraryFile, EntryPoint = "webview_create")]
    internal static partial nint webview_create(int debug, nint window);

    /// <summary>
    /// <para>Destroys a webview and closes the native window.</para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_destroy(webview_t w);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview pointer to destroy.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_destroy")]
    internal static partial void webview_destroy(nint webview);

    /// <summary>
    /// <para>
    /// Runs the main loop until it's terminated. After this function exits - you
    /// must destroy the webview.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_run(webview_t w);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview pointer to run.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_run")]
    internal static partial void webview_run(nint webview);

    /// <summary>
    /// <para>
    /// Stops the main loop. It is safe to call this function from another other
    /// background thread.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_terminate(webview_t w);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview pointer to terminate.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_terminate")]
    internal static partial void webview_terminate(nint webview);

    /// <summary>
    /// <para>Updates the title of the native window. Must be called from the UI thread.</para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_set_title(webview_t w, const char/// title);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to update.</param>
    /// <param name="title">New webview title.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_set_title", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webview_set_title(nint webview, string title);

    /// <summary>
    /// <para>Updates native window size.</para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_set_size(webview_t w, int width, int height, int hints);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to update</param>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    /// <param name="hint">Size behaviour</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_set_size")]
    internal static partial void webview_set_size(nint webview, int width, int height, WebviewHint hint);

    /// <summary>
    /// <para>
    /// Navigates webview to the given URL. URL may be a data URI, i.e.
    /// "data:text/text,<html>...</html>". It is often ok not to url-encode it
    /// properly, webview will re-encode it for you.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_navigate(webview_t w, const char/// url);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to update</param>
    /// <param name="url">The url to navigate to</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_navigate", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webview_navigate(nint webview, string url);

    /// <summary>
    /// <para>
    /// Injects JavaScript code at the initialization of the new page. Every time
    /// the webview will open a the new page - this initialization code will be
    /// executed. It is guaranteed that code is executed before window.onload.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_init(webview_t w, const char/// js);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to execute the javascript.</param>
    /// <param name="js">The javascript to execute.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_init", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webview_init(nint webview, string js);

    /// <summary>
    /// <para>
    /// Evaluates arbitrary JavaScript code. Evaluation happens asynchronously, also
    /// the result of the expression is ignored. Use RPC bindings if you want to
    /// receive notifications about the results of the evaluation.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_eval(webview_t w, const char ///js);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to execute the javascript in.</param>
    /// <param name="js">The javascript to evaluate.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_eval", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webview_eval(nint webview, string js);

    /// <summary>
    /// <para>
    /// Posts a function to be executed on the main thread. You normally do not need
    /// to call this function, unless you want to tweak the native window.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_dispatch(webview_t w, void (///fn)(webview_t w, void ///arg), void ///arg);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to dispatch the function to</param>
    /// <param name="dispatchFunction">The function to execute on the webview thread</param>
    /// <param name="args">Paramters to pass to the dispatched function</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_dispatch")]
    internal static partial void webview_dispatch(nint webview, DispatchFunction dispatchFunction, nint args);

    /// <summary>
    /// <para>
    /// Binds a native C callback so that it will appear under the given name as a
    /// global JavaScript function. Internally it uses webview_init(). Callback
    /// receives a request string and a user-provided argument pointer. Request
    /// string is a JSON array of all the arguments passed to the JavaScript
    /// function.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_bind(webview_t w, const char/// name, 
    ///     void (/// fn) (const char/// seq, const char/// req, void/// arg), void/// arg);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to return the result to.</param>
    /// <param name="name">Name of the JS function.</param>
    /// <param name="callback">Callback function.</param>
    /// <param name="arg">User argument.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_bind", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webview_bind(nint webview, string name, CallBackFunction callback, nint arg);

    /// <summary>
    /// <para>
    /// Allows to return a value from the native binding. Original request pointer
    /// must be provided to help internal RPC engine match requests with responses.
    /// If status is zero - result is expected to be a valid JSON result value.
    /// If status is not zero - result is an error JSON object.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_return(webview_t w, const char/// seq, int status, const char/// result);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to return the result to.</param>
    /// <param name="id">The id of the call.</param>
    /// <param name="result">The result of the call.</param>
    /// <param name="resultJson">The json data to return to the webview.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_return", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webview_return(nint webview, string id, RPCResult result, string resultJson);

    /// <summary>
    /// <para>
    /// Returns a native window handle pointer. When using GTK backend the pointer
    /// is GtkWindow pointer, when using Cocoa backend the pointer is NSWindow
    /// pointer, when using Win32 backend the pointer is HWND pointer.
    /// </para>
    /// <para>
    /// Binding for:
    /// WEBVIEW_API void webview_get_window(webview_t w);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to return the result to.</param>
    [LibraryImport(libraryFile, EntryPoint = "webview_get_window")]
    internal static partial nint webview_get_window(nint webview);

    /// <summary>
    /// <para>
    /// Get a native handle of choice.
    /// </para>
    /// <para>
    /// Binding for:
    /// *webview_get_native_handle(webview_t w, webview_native_handle_kind_t kind);
    /// </para>
    /// </summary>
    /// <param name="webview">The webview to return the result to.</param>
    /// <param name="webviewNativeHandleKind">kind The kind of handle to retrieve.</param>
    /// <returns>The native handle or @c NULL.</returns>
    [LibraryImport(libraryFile, EntryPoint = "webview_get_native_handle")]
    internal static partial nint webview_get_native_handle(nint webview, WebviewNativeHandleKind webviewNativeHandleKind);
}