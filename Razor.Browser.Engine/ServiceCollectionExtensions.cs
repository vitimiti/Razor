// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

/// <summary>Provides extension methods for configuring browser engine services in an <see cref="IServiceCollection"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers services for a custom browser engine of type <typeparamref name="T"/> into the provided <see cref="IServiceCollection"/>.</summary>
    /// <typeparam name="T">The type of the browser engine, must implement <see cref="IBrowserEngine"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the browser engine services to.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddBrowserEngine<T>(this IServiceCollection services)
        where T : class, IBrowserEngine
    {
        _ = services.AddSingleton<IBrowserEngineFactory>(provider => new BrowserEngineFactory(() =>
            ActivatorUtilities.CreateInstance<T>(provider)
        ));

        _ = services.AddSingleton<IBrowserEngine>(provider =>
            provider.GetRequiredService<IBrowserEngineFactory>().CreateEngine()
        );

        return services;
    }
}
