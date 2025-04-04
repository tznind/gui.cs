using TerminalGuiFluentTesting;
using Xunit;

namespace TerminalGuiFluentTestingXunit;

public static partial class XunitContextExtensions
{
    // Placeholder

    public static GuiTestContext AssertTrue (this GuiTestContext context, bool? condition)
    {
        context.Then (
                      () =>
                      {
                          Assert.True (condition);
                      });
        return context;
    }
}
