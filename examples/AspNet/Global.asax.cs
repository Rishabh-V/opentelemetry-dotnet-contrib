// <copyright file="Global.asax.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Examples.AspNet;

#pragma warning disable SA1649 // File name should match first type name
public class WebApiApplication : HttpApplication
#pragma warning restore SA1649 // File name should match first type name
{
    private IDisposable? tracerProvider;
    private IDisposable? meterProvider;

    protected void Application_Start()
    {
        var builder = Sdk.CreateTracerProviderBuilder()
             .AddAspNetInstrumentation()
             .AddHttpClientInstrumentation();

        switch (ConfigurationManager.AppSettings["UseExporter"].ToUpperInvariant())
        {
            case "ZIPKIN":
                builder.AddZipkinExporter(zipkinOptions =>
                {
                    zipkinOptions.Endpoint = new Uri(ConfigurationManager.AppSettings["ZipkinEndpoint"]);
                });
                break;
            case "OTLP":
                builder.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(ConfigurationManager.AppSettings["OtlpEndpoint"]);
                    });
                break;
            default:
                builder.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
                break;
        }

        this.tracerProvider = builder.Build();

        // Metrics
        // Note: Tracerprovider is needed for metrics to work
        // https://github.com/open-telemetry/opentelemetry-dotnet/issues/2994

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
             .AddAspNetInstrumentation();

        switch (ConfigurationManager.AppSettings["UseMetricsExporter"].ToUpperInvariant())
        {
            case "OTLP":
                meterBuilder.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(ConfigurationManager.AppSettings["OtlpEndpoint"]);
                });
                break;
            case "PROMETHEUS":
                meterBuilder.AddPrometheusHttpListener();
                break;
            default:
                meterBuilder.AddConsoleExporter((exporterOptions, metricReaderOptions) =>
                {
                    exporterOptions.Targets = ConsoleExporterOutputTargets.Debug;
                });
                break;
        }

        this.meterProvider = meterBuilder.Build();

        GlobalConfiguration.Configure(WebApiConfig.Register);

        AreaRegistration.RegisterAllAreas();
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }

    protected void Application_End()
    {
        this.tracerProvider?.Dispose();
        this.meterProvider?.Dispose();
    }
}
