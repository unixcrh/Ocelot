﻿using Ocelot.Middleware;

namespace Ocelot.UnitTests.Headers
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Headers;
    using Ocelot.Headers.Middleware;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class HttpRequestHeadersBuilderMiddlewareTests
    {
        private readonly Mock<IAddHeadersToRequest> _addHeaders;
        private Response<DownstreamRoute> _downstreamRoute;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private HttpRequestHeadersBuilderMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public HttpRequestHeadersBuilderMiddlewareTests()
        {
            _addHeaders = new Mock<IAddHeadersToRequest>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<HttpRequestHeadersBuilderMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _middleware = new HttpRequestHeadersBuilderMiddleware(_next, _loggerFactory.Object, _addHeaders.Object);
            _downstreamContext.DownstreamRequest = new HttpRequestMessage();
        }

        [Fact]
        public void should_call_add_headers_to_request_correctly()
        {
            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(new DownstreamReRouteBuilder()
                            .WithDownstreamPathTemplate("any old string")
                            .WithClaimsToHeaders(new List<ClaimToThing>
                            {
                                new ClaimToThing("UserId", "Subject", "", 0)
                            })
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build());

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheAddHeadersToDownstreamRequestReturnsOk())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAddHeadersToRequestIsCalledCorrectly())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void GivenTheAddHeadersToDownstreamRequestReturnsOk()
        {
            _addHeaders
                .Setup(x => x.SetHeadersOnDownstreamRequest(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<System.Security.Claims.Claim>>(),
                    It.IsAny<HttpRequestMessage>()))
                .Returns(new OkResponse());
        }

        private void ThenTheAddHeadersToRequestIsCalledCorrectly()
        {
            _addHeaders
                .Verify(x => x.SetHeadersOnDownstreamRequest(
                    It.IsAny<List<ClaimToThing>>(),
                    It.IsAny<IEnumerable<System.Security.Claims.Claim>>(), 
                    _downstreamContext.DownstreamRequest), Times.Once);
        }
    }
}
