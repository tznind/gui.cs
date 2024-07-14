﻿using Xunit.Abstractions;
using Color = Terminal.Gui.Color;

namespace UnitTests.Views;

public class ColorPicker2Tests
{
    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_Construct_DefaultValue ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };

        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        // Should be only a single text field (Hex) because ShowTextFields is false
        Assert.Single (cp.Subviews.OfType<TextField> ());

        cp.Draw ();

        // All bars should be at 0 with the triangle at 0 (+2 because of "H:", "S:" etc)
        var h = GetColorBar (cp, ColorPickerPart.Bar1);
        Assert.Equal ("H:", h.Text);
        Assert.Equal (2, h.TrianglePosition);
        Assert.IsType<HueBar> (h);

        var s = GetColorBar (cp, ColorPickerPart.Bar2);
        Assert.Equal ("S:", s.Text);
        Assert.Equal (2, s.TrianglePosition);
        Assert.IsType<SaturationBar> (s);

        var v = GetColorBar (cp, ColorPickerPart.Bar3);
        Assert.Equal ("V:", v.Text);
        Assert.Equal (2, v.TrianglePosition);
        Assert.IsType<ValueBar> (v);

        var hex = GetTextField (cp, ColorPickerPart.Hex);
        Assert.Equal ("#000000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_RGB_KeyboardNavigation ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.IsType<RBar> (r);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.IsType<GBar> (g);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.IsType<BBar> (b);
        Assert.Equal ("#000000", hex.Text);

        Assert.IsAssignableFrom<IColorBar> (cp.Focused);
        cp.NewKeyDownEvent (Key.CursorRight);

        cp.Draw ();

        Assert.Equal (3, r.TrianglePosition);
        Assert.Equal ("#0F0000", hex.Text);

        cp.NewKeyDownEvent (Key.CursorRight);

        cp.Draw ();

        Assert.Equal (4, r.TrianglePosition);
        Assert.Equal ("#1E0000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_RGB_MouseNavigation ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.IsType<RBar> (r);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.IsType<GBar> (g);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.IsType<BBar> (b);
        Assert.Equal ("#000000", hex.Text);

        Assert.IsAssignableFrom<IColorBar> (cp.Focused);

        cp.Focused.OnMouseEvent (
                                 new ()
                                 {
                                     Flags = MouseFlags.Button1Pressed,
                                     Position = new (3, 0)
                                 });

        cp.Draw ();

        Assert.Equal (3, r.TrianglePosition);
        Assert.Equal ("#0F0000", hex.Text);

        cp.Focused.OnMouseEvent (
                                  new ()
                                  {
                                      Flags = MouseFlags.Button1Pressed,
                                      Position = new (4, 0)
                                  });

        cp.Draw ();

        Assert.Equal (4, r.TrianglePosition);
        Assert.Equal ("#1E0000", hex.Text);

        top.Dispose ();
    }

    public static IEnumerable<object []> ColorPickerTestData ()
    {
        yield return new object []
        {
            new Color(255, 0),
            "R:", 19, "G:", 2, "B:", 2, "#FF0000"
        };

        yield return new object []
        {
            new Color(0, 255),
            "R:", 2, "G:", 19, "B:", 2, "#00FF00"
        };

        yield return new object []
        {
            new Color(0, 0, 255),
            "R:", 2, "G:", 2, "B:", 19, "#0000FF"
        };

        yield return new object []
        {
            new Color(125, 125, 125),
            "R:", 11, "G:", 11, "B:", 11, "#7D7D7D"
        };
    }

    [Theory]
    [AutoInitShutdown]
    [MemberData (nameof (ColorPickerTestData))]
    public void ColorPicker_RGB_NoText (Color c, string expectedR, int expectedRTriangle, string expectedG, int expectedGTriangle, string expectedB, int expectedBTriangle, string expectedHex)
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = c };

        cp.Style.ShowTextFields = false;
        cp.Style.ColorModel = ColorModel.RGB;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal (expectedR, r.Text);
        Assert.Equal (expectedRTriangle, r.TrianglePosition);
        Assert.Equal (expectedG, g.Text);
        Assert.Equal (expectedGTriangle, g.TrianglePosition);
        Assert.Equal (expectedB, b.Text);
        Assert.Equal (expectedBTriangle, b.TrianglePosition);
        Assert.Equal (expectedHex, hex.Text);

        top.Dispose ();
    }

    public static IEnumerable<object []> ColorPickerTestData_WithTextFields ()
    {
        yield return new object []
        {
            new Color(255, 0),
            "R:", 15, 255, "G:", 2, 0, "B:", 2, 0, "#FF0000"
        };

        yield return new object []
        {
            new Color(0, 255),
            "R:", 2, 0, "G:", 15, 255, "B:", 2, 0, "#00FF00"
        };

        yield return new object []
        {
            new Color(0, 0, 255),
            "R:", 2, 0, "G:", 2, 0, "B:", 15, 255, "#0000FF"
        };

        yield return new object []
        {
            new Color(125, 125, 125),
            "R:", 9, 125, "G:", 9, 125, "B:", 9, 125, "#7D7D7D"
        };
    }

    [Theory]
    [AutoInitShutdown]
    [MemberData (nameof (ColorPickerTestData_WithTextFields))]
    public void ColorPicker_RGB_NoText_WithTextFields (Color c, string expectedR, int expectedRTriangle, int expectedRValue, string expectedG, int expectedGTriangle, int expectedGValue, string expectedB, int expectedBTriangle, int expectedBValue, string expectedHex)
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = c };

        cp.Style.ShowTextFields = true;
        cp.Style.ColorModel = ColorModel.RGB;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);
        var rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        var gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        var bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal (expectedR, r.Text);
        Assert.Equal (expectedRTriangle, r.TrianglePosition);
        Assert.Equal (expectedRValue.ToString (), rTextField.Text);
        Assert.Equal (expectedG, g.Text);
        Assert.Equal (expectedGTriangle, g.TrianglePosition);
        Assert.Equal (expectedGValue.ToString (), gTextField.Text);
        Assert.Equal (expectedB, b.Text);
        Assert.Equal (expectedBTriangle, b.TrianglePosition);
        Assert.Equal (expectedBValue.ToString (), bTextField.Text);
        Assert.Equal (expectedHex, hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_ClickingAtEndOfBar_SetsMaxValue ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        // Click at the end of the Red bar
        cp.Focused.OnMouseEvent (
                                 new ()
                                 {
                                     Flags = MouseFlags.Button1Pressed,
                                     Position = new (19, 0) // Assuming 0-based indexing
                                 });

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_ClickingBeyondBar_ChangesToMaxValue ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        // Click beyond the bar
        cp.Focused.OnMouseEvent (
                                 new ()
                                 {
                                     Flags = MouseFlags.Button1Pressed,
                                     Position = new (21, 0) // Beyond the bar
                                 });

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_ChangeValueOnUI_UpdatesAllUIElements ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = true;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        // Change value using text field
        TextField rBarTextField = cp.Subviews.OfType<TextField> ().First (tf => tf.Text == "0");

        rBarTextField.Text = "128";
        rBarTextField.OnLeave (cp);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);
        var rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        var gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        var bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (9, r.TrianglePosition);
        Assert.Equal ("128", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#800000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_InvalidHexInput_DoesNotChangeColor ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = true;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        // Enter invalid hex value
        TextField hexField = cp.Subviews.OfType<TextField> ().First (tf => tf.Text == "#000000");
        hexField.Text = "#ZZZZZZ";
        hexField.OnLeave (cp);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (2, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#000000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_ClickingDifferentBars_ChangesFocus ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        // Click on Green bar
        cp.Subviews.OfType<GBar> ()
          .Single ()
          .OnMouseEvent (
                         new ()
                         {
                             Flags = MouseFlags.Button1Pressed,
                             Position = new (0, 1)
                         });

        cp.Draw ();

        Assert.IsAssignableFrom<GBar> (cp.Focused);

        // Click on Blue bar
        cp.Subviews.OfType<BBar> ()
          .Single ()
          .OnMouseEvent (
                         new ()
                         {
                             Flags = MouseFlags.Button1Pressed,
                             Position = new (0, 2)
                         });

        cp.Draw ();

        Assert.IsAssignableFrom<BBar> (cp.Focused);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_SwitchingColorModels_ResetsBars ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (255, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (19, r.TrianglePosition);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        // Switch to HSV
        cp.Style.ColorModel = ColorModel.HSV;
        cp.ApplyStyleChanges ();
        cp.Draw ();

        var h = GetColorBar (cp, ColorPickerPart.Bar1);
        var s = GetColorBar (cp, ColorPickerPart.Bar2);
        var v = GetColorBar (cp, ColorPickerPart.Bar3);

        Assert.Equal ("H:", h.Text);
        Assert.Equal (2, h.TrianglePosition);
        Assert.Equal ("S:", s.Text);
        Assert.Equal (19, s.TrianglePosition);
        Assert.Equal ("V:", v.Text);
        Assert.Equal (19, v.TrianglePosition);
        Assert.Equal ("#FF0000", hex.Text);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_SyncBetweenTextFieldAndBars ()
    {
        var cp = new ColorPicker2 { Width = 20, Height = 4, Value = new (0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = true;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        // Change value using the bar
        RBar rBar = cp.Subviews.OfType<RBar> ().First ();
        rBar.Value = 128;

        cp.Draw ();

        var r = GetColorBar (cp, ColorPickerPart.Bar1);
        var g = GetColorBar (cp, ColorPickerPart.Bar2);
        var b = GetColorBar (cp, ColorPickerPart.Bar3);
        var hex = GetTextField (cp, ColorPickerPart.Hex);
        var rTextField = GetTextField (cp, ColorPickerPart.Bar1);
        var gTextField = GetTextField (cp, ColorPickerPart.Bar2);
        var bTextField = GetTextField (cp, ColorPickerPart.Bar3);

        Assert.Equal ("R:", r.Text);
        Assert.Equal (9, r.TrianglePosition);
        Assert.Equal ("128", rTextField.Text);
        Assert.Equal ("G:", g.Text);
        Assert.Equal (2, g.TrianglePosition);
        Assert.Equal ("0", gTextField.Text);
        Assert.Equal ("B:", b.Text);
        Assert.Equal (2, b.TrianglePosition);
        Assert.Equal ("0", bTextField.Text);
        Assert.Equal ("#800000", hex.Text);

        top.Dispose ();
    }

    enum ColorPickerPart
    {
        Bar1 = 0,
        Bar2 = 1,
        Bar3 = 2,
        Hex = 3,
    }

    private TextField GetTextField (ColorPicker2 cp, ColorPickerPart toGet)
    {
        if (!cp.Style.ShowTextFields)
        {
            if (toGet <= ColorPickerPart.Bar3)
            {
                throw new NotSupportedException ("There are no bar text fields for ColorPicker because ShowTextFields is false");
            }

            return cp.Subviews.OfType<TextField> ().Single ();
        }

        return cp.Subviews.OfType<TextField> ().ElementAt ((int)toGet);
    }

    private ColorBar GetColorBar (ColorPicker2 cp, ColorPickerPart toGet)
    {
        if (toGet <= ColorPickerPart.Bar3)
        {
            return cp.Subviews.OfType<ColorBar> ().ElementAt ((int)toGet);
        }
        throw new NotSupportedException ("ColorPickerPart must be a bar");
    }

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_ChangedEvent_Fires ()
    {
        Color oldColor = default;
        Color newColor = default;
        var count = 0;

        var cp = new ColorPicker2 ();

        cp.ColorChanged += (s, e) =>
        {
            count++;
            oldColor = e.PreviousColor;
            newColor = e.Color;

            Assert.Equal (cp.Value, e.Color);
        };

        cp.Value = new (1, 2, 3);
        Assert.Equal (1, count);
        Assert.Equal (new (1, 2, 3), newColor);

        cp.Value = new (2, 3, 4);

        Assert.Equal (2, count);
        Assert.Equal (new (1, 2, 3), oldColor);
        Assert.Equal (new (2, 3, 4), newColor);

        // Set to same value
        cp.Value = new (2, 3, 4);

        // Should have no effect
        Assert.Equal (2, count);
    }
}