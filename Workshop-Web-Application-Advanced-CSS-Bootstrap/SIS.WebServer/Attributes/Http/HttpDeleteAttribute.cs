﻿using SIS.HTTP.Enums;

namespace SIS.MvcFramework.Attributes.Http
{
    public class HttpDeleteAttribute : BaseHttpAttribute
    {
        public override HttpRequestMethod Method => HttpRequestMethod.Delete;
    }
}
