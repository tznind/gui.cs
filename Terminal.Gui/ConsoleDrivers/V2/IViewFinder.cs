namespace Terminal.Gui;

public interface IViewFinder
{
    View GetViewAt (Point screenPosition, out Point viewportPoint);
}

internal class StaticViewFinder : IViewFinder
{
    /// <inheritdoc/>
    public View GetViewAt (Point screenPosition, out Point viewportPoint)
    {
        View hit = View.GetViewsUnderMouse (screenPosition).LastOrDefault ();

        viewportPoint = hit?.ScreenToViewport (screenPosition) ?? Point.Empty;

        return hit;
    }
}
