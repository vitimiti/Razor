// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Dispatch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserDispatch(this IServiceCollection services)
    {
        services.AddSingleton<IBrowserDispatchFactory, BrowserDispatchFactory>();
        services.AddTransient<IBrowserDispatch>(provider =>
            provider.GetRequiredService<IBrowserDispatchFactory>().CreateDispatch()
        );
        return services;
    }
}
