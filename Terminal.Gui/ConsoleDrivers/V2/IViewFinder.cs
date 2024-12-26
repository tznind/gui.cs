using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;

public interface IViewFinder
{
    View GetViewAt (Point screenPosition, out Point viewportPoint);
}

class StaticViewFinder : IViewFinder
{
    /// <inheritdoc />
    public View GetViewAt (Point screenPosition, out Point viewportPoint)
    {
        var hit = View.GetViewsUnderMouse (screenPosition).LastOrDefault ();

        viewportPoint = hit?.ScreenToViewport (screenPosition) ?? Point.Empty;

        return hit;
    }
}