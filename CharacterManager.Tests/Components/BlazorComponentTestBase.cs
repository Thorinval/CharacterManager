using Bunit;
using CharacterManager.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CharacterManager.Tests.Components;

/// <summary>
/// Base class for bUnit component tests with common services configured
/// </summary>
public abstract class BlazorComponentTestBase : TestContext
{
    protected BlazorComponentTestBase()
    {
        // Register common mock services
        Services.AddSingleton<LanguageContextService>();
        
        // Add JSInterop mock (required for many Blazor components)
        JSInterop.SetupVoid("Blazor._internal.navigationManager.enableNavigationInterception", _ => true);
        JSInterop.SetupVoid("Blazor._internal.navigationManager.navigateTo", _ => true);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
