﻿using System.IO;
using SIS.HTTP.Enums;
using SIS.WebServer.Result;
using SIS.HTTP.Requests;
using SIS.HTTP.Responses;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Demo.App.Controllers
{
    public abstract class BaseController
    {
        protected IHttpRequest HttpRequest { get; set; }

        protected Dictionary<string, object> ViewData = new Dictionary<string, object>();

        protected bool IsLoggedIn()
        {
            return this.HttpRequest.Session.ContainsParameter("username");
        }

        private string ParseTemplate(string viewContent)
        {
            foreach (var param in this.ViewData)
            {
                viewContent = viewContent.Replace($"@Model.{param.Key.ToLower()}", param.Value.ToString());
            }

            return viewContent;
        }

        public IHttpResponse View([CallerMemberName] string view = null)
        {
            string controllerName = this.GetType().Name.Replace("Controller", string.Empty);
            string viewName = view;

            string viewContent = File.ReadAllText("Views/" + controllerName + "/" + viewName + ".html");

            viewContent = this.ParseTemplate(viewContent);

            HtmlResult htmlResult = new HtmlResult(viewContent, HttpResponseStatusCode.Ok);

            return htmlResult;
        }

        public IHttpResponse Redirect(string url)
        {
            return new RedirectResult(url);
        }
    }
}
