#nullable enable
namespace Terminal.Gui;

internal class AlreadyResolvedException : Exception
{
    public AlreadyResolvedException ():base("MouseButtonSequence already resolved")
    {
    }
}
