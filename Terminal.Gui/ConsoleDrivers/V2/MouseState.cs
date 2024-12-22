#nullable enable

namespace Terminal.Gui;

internal class MouseState
{
    public MouseButtonStateEx [] ButtonStates = new MouseButtonStateEx? [4];

    public Point Position;
}