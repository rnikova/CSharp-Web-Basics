﻿using SIS.HTTP.Enums;

namespace SIS.MvcFramework.Attributes.Http
{
    public class HttpPutAttribute : BaseHttpAttribute
    {
        public override HttpRequestMethod Method => HttpRequestMethod.Put;
    }
}
