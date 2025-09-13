// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Razor.Browser.Abstractions;

namespace Razor.Browser.Engine;

[PublicAPI]
public class BrowserEngineFactory(Func<IBrowserEngine> engineFactory) : IBrowserEngineFactory
{
    public IBrowserEngine CreateEngine()
    {
        return engineFactory();
    }
}
