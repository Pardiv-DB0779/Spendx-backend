using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace GPC.Boost.SpendX.Tests.Helpers
{
    public class FakeHttpRequestData : HttpRequestData
    {
        private static readonly IServiceProvider DefaultServiceProvider;

        static FakeHttpRequestData()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<WorkerOptions>(options =>
            {
                options.Serializer = new JsonObjectSerializer();
            });
            DefaultServiceProvider = services.BuildServiceProvider();
        }

        public static FunctionContext CreateDefaultFunctionContext()
        {
            var contextMock = new Mock<FunctionContext>();
            contextMock.Setup(c => c.InstanceServices).Returns(DefaultServiceProvider);
            return contextMock.Object;
        }

        public FakeHttpRequestData(FunctionContext? functionContext, Stream body, Uri? url = null, string method = "POST")
            : base(functionContext ?? CreateDefaultFunctionContext())
        {
            Body = body;
            Url = url ?? new Uri("https://localhost/api/EventOffer");
            Method = method;
            Headers = new HttpHeadersCollection();
        }

        public override Stream Body { get; }
        public override HttpHeadersCollection Headers { get; }
        public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
        public override Uri Url { get; }
        public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
        public override string Method { get; }

        public override HttpResponseData CreateResponse()
        {
            return new FakeHttpResponseData(FunctionContext);
        }
    }

    public class FakeHttpResponseData : HttpResponseData
    {
        public FakeHttpResponseData(FunctionContext functionContext) : base(functionContext)
        {
            Body = new MemoryStream();
            Headers = new HttpHeadersCollection();
        }

        public override HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public override HttpHeadersCollection Headers { get; set; }
        public override Stream Body { get; set; }
        public override HttpCookies Cookies => new FakeHttpCookies();
    }

    public class FakeHttpCookies : HttpCookies
    {
        public override void Append(string name, string value) { }
        public override void Append(IHttpCookie cookie) { }
        public override IHttpCookie CreateNew() => Mock.Of<IHttpCookie>();
    }
}
