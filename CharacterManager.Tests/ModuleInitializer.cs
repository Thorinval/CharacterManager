using System.Runtime.CompilerServices;
using System.Text;

namespace CharacterManager.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }
}
