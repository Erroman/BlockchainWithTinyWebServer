﻿using System;
using System.Net;
using System.Text;
using System.Threading;

namespace TinyWebServer
{
    /// <summary> 
    /// Listens for the specified request, and executes the given handler.
    /// 
    /// Example: 
    /// <code>
    /// var server = new WebServer(request => { return "<h1>Hello world!</h1>"; }, "http://localhost:8080/hello/");
    /// server.Run();
    /// ....
    /// server.Stop();
    /// </code>
    /// 
    /// Note - this code is adapted from http://codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server.aspx
    /// The purpose of this library is more-or-less just to provide a Nuget package for this class.
    /// </summary>
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _handler;

        public WebServer(Func<HttpListenerRequest, string> handler, params string[] urls)
        {
            if (urls == null || urls.Length == 0)
                throw new ArgumentException("prefixes");
            if (handler == null)
                throw new ArgumentException("method");

            foreach (string s in urls)
                _listener.Prefixes.Add(s);

            _handler = handler;
            _listener.Start();
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem(c =>
                    {
                        var ctx = c as HttpListenerContext;
                        if (ctx != null)
                        {
                            try
                            {
                                var responseStr = _handler(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(responseStr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            finally
                            {
                                ctx.Response.OutputStream.Close();
                            }
                        }
                    }, _listener.GetContext());
                }
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

        ~WebServer()
        {
            Stop();
        }
    }
}
