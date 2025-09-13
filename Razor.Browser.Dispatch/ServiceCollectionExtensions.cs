// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Dispatch;

/// <summary>Provides extension methods for configuring browser dispatch services in an <see cref="IServiceCollection"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds browser dispatch services to the provided <see cref="IServiceCollection"/>.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> with browser dispatch services registered.</returns>
    public static IServiceCollection AddBrowserDispatch(this IServiceCollection services)
    {
        _ = services.AddSingleton<IBrowserDispatchFactory, BrowserDispatchFactory>();
        _ = services.AddTransient<IBrowserDispatch>(provider =>
            provider.GetRequiredService<IBrowserDispatchFactory>().CreateDispatch()
        );

        return services;
    }
}
