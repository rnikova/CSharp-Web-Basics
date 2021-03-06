﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SIS.HTTP.Common;
using SIS.HTTP.Cookies;
using SIS.HTTP.Enums;
using SIS.HTTP.Exceptions;
using SIS.HTTP.Requests;
using SIS.HTTP.Responses;
using SIS.WebServer.Result;
using SIS.WebServer.Routing;
using SIS.MvcFramework.Sessions;
using SIS.HTTP.Sessions;
using SIS.Common;

namespace SIS.WebServer
{
    public class ConnectionHandler
    {
        private readonly Socket client;

        private readonly IServerRoutingTable serverRoutingTable;

        private readonly IHttpSessionStorage httpSessionStorage;

        public ConnectionHandler(Socket client, IServerRoutingTable serverRoutingTable, IHttpSessionStorage httpSessionStorage)
        {
            client.ThrowIfNull(nameof(client));
            serverRoutingTable.ThrowIfNull(nameof(serverRoutingTable));
            httpSessionStorage.ThrowIfNull(nameof(httpSessionStorage));

            this.client = client;
            this.serverRoutingTable = serverRoutingTable;
            this.httpSessionStorage = httpSessionStorage;
        }

        private async Task<IHttpRequest> ReadRequestAsync()
        {
            var result = new StringBuilder();
            var data = new ArraySegment<byte>(new byte[1024]);

            while (true)
            {
                int numberOfBytesToRead = await this.client.ReceiveAsync(data, SocketFlags.None);

                if (numberOfBytesToRead == 0)
                {
                    break;
                }

                var bytesAsString = Encoding.UTF8.GetString(data.Array, 0, numberOfBytesToRead);
                result.Append(bytesAsString);

                if (numberOfBytesToRead < 1023)
                {
                    break;
                }
            }

            if (result.Length == 0)
            {
                return null;
            }

            return new HttpRequest(result.ToString());
        }

        private IHttpResponse ReturnIfResource(IHttpRequest httpRequest)
        {
            string folderPrefix = "../";
            string resourcesFolderPath = "Resources/";
            string requestedResource = httpRequest.Path;
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string fullPathToResource = assemblyLocation + folderPrefix + resourcesFolderPath + requestedResource;

            if (File.Exists(fullPathToResource))
            {
                byte[] context = File.ReadAllBytes(fullPathToResource);

                return new InlineResourceResult(context, HttpResponseStatusCode.Found);
            }
            else
            {
                return new TextResult($"Route with method {httpRequest.RequestMethod} and path \"{httpRequest.Path}\" not found.", HttpResponseStatusCode.NotFound);
            }

        }

        private IHttpResponse HandleRequest(IHttpRequest httpRequest)
        {
            if (!this.serverRoutingTable.Contains(httpRequest.RequestMethod, httpRequest.Path))
            {
                return this.ReturnIfResource(httpRequest);
            }

            return this.serverRoutingTable.Get(httpRequest.RequestMethod, httpRequest.Path).Invoke(httpRequest);
        }

        private string SetRequestSession(IHttpRequest httpRequest)
        {
            if (httpRequest.Cookies.ContainsCookie(HttpSessionStorage.SessionCookieKey))
            {
                var cookie = httpRequest.Cookies.GetCookie(HttpSessionStorage.SessionCookieKey);
                var sessionId = cookie.Value;

                sessionId = Guid.NewGuid().ToString();

                if (this.httpSessionStorage.ContainsSession(sessionId))
                {
                    httpRequest.Session = this.httpSessionStorage.GetSession(sessionId);
                }
            }

            if (httpRequest.Session == null)
            {
                var sessionId = Guid.NewGuid().ToString();

                httpRequest.Session = this.httpSessionStorage.GetSession(sessionId);
            }

            return httpRequest.Session?.Id;
        }

        private void SetResponseSession(IHttpResponse httpResponse, string sessionId)
        {
            IHttpSession responceSession = this.httpSessionStorage.GetSession(sessionId);

            if (responceSession.IsNew)
            {
                responceSession.IsNew = false;
                httpResponse.Cookies
                    .AddCookie(new HttpCookie(HttpSessionStorage.SessionCookieKey, sessionId));
            }
        }

        private void PrepareResponse(IHttpResponse httpResponse)
        {
            byte[] byteSegments = httpResponse.GetBytes();

            this.client.Send(byteSegments, SocketFlags.None);
        }

        public async Task ProcessRequestAsync()
        {
            IHttpResponse httpResponse = null;
            try
            {
                IHttpRequest httpRequest = await this.ReadRequestAsync();

                if (httpRequest != null)
                {
                    Console.WriteLine($"Processing: {httpRequest.RequestMethod} {httpRequest.Path}...");

                    string sessionId = this.SetRequestSession(httpRequest);

                    httpResponse = this.HandleRequest(httpRequest);

                    this.SetResponseSession(httpResponse, sessionId);
                }
            }
            catch (BadRequestException e)
            {
                httpResponse = new TextResult(e.ToString(), HttpResponseStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                httpResponse = new TextResult(e.ToString(), HttpResponseStatusCode.InternalServerError);
            }
            this.PrepareResponse(httpResponse);

            this.client.Shutdown(SocketShutdown.Both);
        }
    }
}
