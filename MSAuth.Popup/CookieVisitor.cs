using System;
using System.Collections.Generic;
using CefSharp;

namespace MSAuth.Popup;

internal class CookieVisitor : ICookieVisitor
{
    public void Dispose() { }

    private readonly List<(string name, string value)> storedCookies = new();
    private readonly Action<IEnumerable<(string name, string value)>> onCookiesReceived;

    public CookieVisitor(Action<IEnumerable<(string name, string value)>> cookiesReceivedCallback)
    {
        onCookiesReceived = cookiesReceivedCallback;
    }

    public bool Visit(Cookie cookie, int count, int total, ref bool deleteCookie)
    {
        storedCookies.Add((cookie.Name, cookie.Value));

        if (count == total - 1)
            onCookiesReceived(storedCookies);

        return true;
    }
}