// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Google.Apis.Http;

namespace Generator;

public class LongTimeoutInitializer : IConfigurableHttpClientInitializer
{
    private readonly IConfigurableHttpClientInitializer initialiser;

    public LongTimeoutInitializer(IConfigurableHttpClientInitializer initialiser)
    {
        this.initialiser = initialiser;
    }

    public void Initialize(ConfigurableHttpClient httpClient)
    {
        initialiser.Initialize(httpClient);
        httpClient.Timeout = TimeSpan.FromMinutes(60);
    }
}