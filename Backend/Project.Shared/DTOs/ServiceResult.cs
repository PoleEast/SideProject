using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Project.Shared.DTOs
{
    public class ServiceResult<T>
    {
        public HttpStatusCode Code { get; init; }
        public T? Result { get; init; }
    }
    public class ServiceResult
    {
        public HttpStatusCode Code { get; init; }
    }
}
