using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;
public class AttributeView : View
{

    /// <summary>
    /// The color pair (foreground and background) to depict.
    /// </summary>
    public Attribute Value { get; set; }


    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        Draw1x1 (viewport);
    }

    private void Draw1x1 (Rectangle viewport)
    {
        var x = viewport.X;
        var y = viewport.Y;

        Driver.SetAttribute (Value);
        AddRune (x,y,Glyphs.UpperHalfBlock);
    }
}
