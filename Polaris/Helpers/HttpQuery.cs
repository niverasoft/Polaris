using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nivera;

namespace Polaris.Helpers
{
    public class HttpQuery
    {
        private ProcessDelegate processDelegate;
        private HttpListener httpListener;
        private string key;

        public delegate byte[] ProcessDelegate(string data);

        public HttpQuery(string url)
        {
            Log.JoinCategory("http");

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(url);

            key = Nivera.Utils.RandomGen.RandomBytesString();

            Log.Info($"Listening to {url}");
        }

        public void StartListening()
        {
            httpListener.Start();
            httpListener.GetContextAsync().ContinueWith(ProcessRequestHandler);
        }

        public void StopListening()
        {
            httpListener.Stop();
        }

        private void ProcessRequestHandler(Task<HttpListenerContext> result)
        {
            var ctx = result.Result;

            httpListener.GetContextAsync().ContinueWith(ProcessRequestHandler);

            string req = new StreamReader(ctx.Request.InputStream).ReadToEnd();

            byte[] response = processDelegate(req);

            ctx.Response.ContentLength64 = response.Length;

            var outStr = ctx.Response.OutputStream;

            outStr.WriteAsync(response, 0, response.Length);
            outStr.Close();
        }
    }
}