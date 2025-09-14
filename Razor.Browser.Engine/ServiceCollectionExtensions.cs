// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
