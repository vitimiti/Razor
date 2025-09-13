// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

[PublicAPI]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserEngine<T>(this IServiceCollection services)
        where T : class, IBrowserEngine
    {
        services.AddSingleton<IBrowserEngineFactory>(provider => new BrowserEngineFactory(() =>
            ActivatorUtilities.CreateInstance<T>(provider)
        ));

        services.AddSingleton<IBrowserEngine>(provider =>
            provider.GetRequiredService<IBrowserEngineFactory>().CreateEngine()
        );

        return services;
    }
}
