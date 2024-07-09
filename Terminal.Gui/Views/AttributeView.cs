using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

public class AttributeView : View
{
    /// <summary>
    /// The color pair (foreground and background) to depict.
    /// </summary>
    public Attribute Value { get; set; }

    public override void OnDrawContent(Rectangle viewport)
    {
        base.OnDrawContent(viewport);

        if (viewport.Height >= 3 && viewport.Width >= 5)
        {
            Draw3x5(viewport);
            return;
        }

        if (viewport.Height >= 3 && viewport.Width >= 3)
        {
            Draw3x3(viewport);
            return;
        }

        Draw1x1(viewport);
    }

    private void Draw3x5(Rectangle viewport)
    {
        DrawArray(viewport, new int[,]
        {
            { 1, 1, 1, 0 },
            { 1, 1, 1, 2 },
            { 0, 2, 2, 2 }
        });
    }

    private void DrawArray(Rectangle viewport, int[,] array)
    {
        var x = viewport.X;
        var y = viewport.Y;

        Driver.SetAttribute(Value);

        for (int a = 0; a < array.GetLength(0); a++)
        {
            for (int b = 0; b < array.GetLength(1); b++)
            {
                if (array[a, b] == 0)
                {
                    continue;
                }

                var c = array[a, b] == 1 ? Value.Foreground : Value.Background;
                Driver.SetAttribute(new Attribute(c, c));
                AddRune(x + b, y + a, (Rune)'█');
            }
        }
    }

    private void Draw3x3(Rectangle viewport)
    {
        DrawArray(viewport, new int[,]
        {
            { 1, 1, 0 },
            { 1, 1, 2 },
            { 0, 2, 2 }
        });
    }

    private void Draw1x1(Rectangle viewport)
    {
        var x = viewport.X;
        var y = viewport.Y;

        Driver.SetAttribute(Value);
        AddRune(x, y, (Rune)Glyphs.UpperHalfBlock);
    }
}
