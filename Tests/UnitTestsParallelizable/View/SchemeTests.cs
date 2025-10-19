﻿#nullable enable
using Xunit;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "View.Scheme")]
public class SchemeTests
{

    [Fact]
    public void GetScheme_Default_ReturnsBaseScheme ()
    {
        var view = new View ();
        var baseScheme = SchemeManager.GetHardCodedSchemes ()? ["Base"];

        Assert.Equal (baseScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void SetScheme_Explicitly_SetsSchemeCorrectly ()
    {
        var view = new View ();
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];

        view.SetScheme (dialogScheme);

        Assert.True (view.HasScheme);
        Assert.Equal (dialogScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void GetScheme_InheritsFromSuperView_WhenNotExplicitlySet ()
    {
        var superView = new View ();
        var subView = new View ();

        superView.Add (subView);

        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        superView.SetScheme (dialogScheme);

        Assert.Equal (dialogScheme, subView.GetScheme ());
        Assert.False (subView.HasScheme);

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void SetSchemeName_OverridesInheritedScheme ()
    {
        var view = new View ();
        view.SchemeName = "Dialog";

        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        Assert.Equal (dialogScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void GetAttribute_ReturnsCorrectAttribute_Via_Mock ()
    {
        var view = new View { SchemeName = "Base" };
        view.Driver = new MockConsoleDriver ();
        view.Driver.SetAttribute (new Attribute (Color.Red, Color.Green));

        // Act
        var attribute = view.GetCurrentAttribute ();

        // Assert
        Assert.Equal (new Attribute (Color.Red, Color.Green), attribute);
    }

    [Fact]
    public void GetAttributeForRole_ReturnsCorrectAttribute ()
    {
        var view = new View { SchemeName = "Base" };

        Assert.Equal (view.GetScheme ().Normal, view.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (view.GetScheme ().HotNormal, view.GetAttributeForRole (VisualRole.HotNormal));
        Assert.Equal (view.GetScheme ().Focus, view.GetAttributeForRole (VisualRole.Focus));
        Assert.Equal (view.GetScheme ().HotFocus, view.GetAttributeForRole (VisualRole.HotFocus));
        Assert.Equal (view.GetScheme ().Disabled, view.GetAttributeForRole (VisualRole.Disabled));

        view.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_DisabledView_ReturnsCorrectAttribute ()
    {
        var view = new View { SchemeName = "Base" };

        view.Enabled = false;
        Assert.Equal (view.GetScheme ().Disabled, view.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (view.GetScheme ().Disabled, view.GetAttributeForRole (VisualRole.HotNormal));

        view.Dispose ();
    }

    [Fact]
    public void SetAttributeForRole_SetsCorrectAttribute ()
    {
        var view = new View { SchemeName = "Base" };
        view.Driver = new MockConsoleDriver ();
        view.Driver.SetAttribute (new Attribute (Color.Red, Color.Green));

        var previousAttribute = view.SetAttributeForRole (VisualRole.Focus);
        Assert.Equal (view.GetScheme ().Focus, view.GetCurrentAttribute ());
        Assert.NotEqual (previousAttribute, view.GetCurrentAttribute ());

        view.Dispose ();
    }

    [Fact]
    public void OnGettingScheme_Override_StopsDefaultBehavior ()
    {
        var view = new CustomView ();
        var customScheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];

        Assert.Equal (customScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void OnSettingScheme_Override_PreventsSettingScheme ()
    {
        var view = new CustomView ();
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];

        view.SetScheme (dialogScheme);

        Assert.NotEqual (dialogScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void GettingScheme_Event_CanOverrideScheme ()
    {
        var view = new View ();
        var customScheme = SchemeManager.GetHardCodedSchemes ()? ["Error"]! with { Normal = Attribute.Default };

        Assert.NotEqual (Attribute.Default, view.GetScheme ().Normal);

        view.GettingScheme += (sender, args) =>
                              {
                                  args.Result = customScheme;
                                  args.Handled = true;
                              };

        Assert.Equal (customScheme, view.GetScheme ());
        Assert.Equal (Attribute.Default, view.GetScheme ().Normal);
        view.Dispose ();
    }

    [Fact]
    public void SettingScheme_Event_CanCancelSchemeChange ()
    {
        var view = new View ();
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];

        view.SchemeChanging += (sender, args) => args.Handled = true;

        view.SetScheme (dialogScheme);

        Assert.NotEqual (dialogScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_Event_CanOverrideAttribute ()
    {
        var view = new View { SchemeName = "Base" };
        var customAttribute = new Attribute (Color.BrightRed, Color.BrightYellow);

        view.GettingAttributeForRole += (sender, args) =>
                                        {
                                            if (args.Role == VisualRole.Focus)
                                            {
                                                args.Result = customAttribute;
                                                args.Handled = true;
                                            }
                                        };

        Assert.Equal (customAttribute, view.GetAttributeForRole (VisualRole.Focus));
        view.Dispose ();
    }

    [Fact]
    public void GetHardCodedSchemes_ReturnsExpectedSchemes ()
    {
        var schemes = Scheme.GetHardCodedSchemes ();

        Assert.NotNull (schemes);
        Assert.Contains ("Base", schemes.Keys);
        Assert.Contains ("Dialog", schemes.Keys);
        Assert.Contains ("Error", schemes.Keys);
        Assert.Contains ("Menu", schemes.Keys);
        Assert.Contains ("Toplevel", schemes.Keys);
    }


    [Fact]
    public void SchemeName_OverridesSuperViewScheme ()
    {
        var superView = new View ();
        var subView = new View ();

        superView.Add (subView);

        subView.SchemeName = "Error";

        var errorScheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];
        Assert.Equal (errorScheme, subView.GetScheme ());

        subView.Dispose ();
        superView.Dispose ();
    }

    [Fact]
    public void Scheme_DefaultsToBase_WhenNotSet ()
    {
        var view = new View ();
        var baseScheme = SchemeManager.GetHardCodedSchemes ()? ["Base"];

        Assert.Equal (baseScheme, view.GetScheme ());
        view.Dispose ();
    }

    [Fact]
    public void Scheme_HandlesNullSuperViewGracefully ()
    {
        var view = new View ();
        view.SchemeName = "Dialog";

        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        Assert.Equal (dialogScheme, view.GetScheme ());

        view.Dispose ();
    }

    private class CustomView : View
    {
        protected override bool OnGettingScheme (out Scheme? scheme)
        {
            scheme = SchemeManager.GetHardCodedSchemes ()? ["Error"];

            return true;
        }

        protected override bool OnSettingScheme (ValueChangingEventArgs<Scheme?> args)
        {
            return true; // Prevent setting the scheme
        }
    }

    [Fact]
    public void View_Resolves_Attributes_From_Scheme ()
    {
        View view = new Label { SchemeName = "Base" };

        foreach (VisualRole role in Enum.GetValues<VisualRole> ())
        {
            Attribute attr = view.GetAttributeForRole (role);
            Assert.NotEqual (default, attr.Foreground); // Defensive: avoid all-defaults
        }

        view.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_SubView_DefersToSuperView_WhenNoExplicitScheme ()
    {
        var parentView = new View { SchemeName = "Base" };
        var childView = new View ();
        parentView.Add (childView);

        // Parent customizes attribute resolution
        var customAttribute = new Attribute (Color.BrightMagenta, Color.BrightGreen);
        parentView.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Normal)
            {
                args.Result = customAttribute;
                args.Handled = true;
            }
        };

        // Child without explicit scheme should get customized attribute from parent
        Assert.Equal (customAttribute, childView.GetAttributeForRole (VisualRole.Normal));

        childView.Dispose ();
        parentView.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_SubView_UsesOwnScheme_WhenExplicitlySet ()
    {
        var parentView = new View { SchemeName = "Base" };
        var childView = new View ();
        parentView.Add (childView);

        // Set explicit scheme on child
        var childScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        childView.SetScheme (childScheme);

        // Parent customizes attribute resolution
        var customAttribute = new Attribute (Color.BrightMagenta, Color.BrightGreen);
        parentView.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Normal)
            {
                args.Result = customAttribute;
                args.Handled = true;
            }
        };

        // Child with explicit scheme should NOT get customized attribute from parent
        Assert.NotEqual (customAttribute, childView.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (childScheme!.Normal, childView.GetAttributeForRole (VisualRole.Normal));

        childView.Dispose ();
        parentView.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_Adornment_UsesParentScheme ()
    {
        // Border (an Adornment) doesn't have a SuperView but should use its Parent's scheme
        var view = new View { SchemeName = "Dialog" };
        var border = view.Border!;
        
        Assert.NotNull (border);
        Assert.Null (border.SuperView); // Adornments don't have SuperView
        Assert.NotNull (border.Parent);
        
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        
        // Border should use its Parent's scheme, not Base
        Assert.Equal (dialogScheme!.Normal, border.GetAttributeForRole (VisualRole.Normal));
        
        view.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_SubView_UsesSchemeName_WhenSet ()
    {
        var parentView = new View { SchemeName = "Base" };
        var childView = new View ();
        parentView.Add (childView);

        // Set SchemeName on child (not explicit scheme)
        childView.SchemeName = "Dialog";

        // Parent customizes attribute resolution
        var customAttribute = new Attribute (Color.BrightMagenta, Color.BrightGreen);
        parentView.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Normal)
            {
                args.Result = customAttribute;
                args.Handled = true;
            }
        };

        // Child with SchemeName should NOT get customized attribute from parent
        // It should use the Dialog scheme instead
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        Assert.NotEqual (customAttribute, childView.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (dialogScheme!.Normal, childView.GetAttributeForRole (VisualRole.Normal));

        childView.Dispose ();
        parentView.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_NestedHierarchy_DefersCorrectly ()
    {
        // Test: grandchild without explicit scheme defers through parent to grandparent
        // Would fail without the SuperView deferral fix (commit 154ac15)
        
        var grandparentView = new View { SchemeName = "Base" };
        var parentView = new View (); // No scheme or SchemeName
        var childView = new View (); // No scheme or SchemeName
        
        grandparentView.Add (parentView);
        parentView.Add (childView);

        // Grandparent customizes attributes
        var customAttribute = new Attribute (Color.BrightYellow, Color.BrightBlue);
        grandparentView.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Normal)
            {
                args.Result = customAttribute;
                args.Handled = true;
            }
        };

        // Child should get attribute from grandparent through parent
        Assert.Equal (customAttribute, childView.GetAttributeForRole (VisualRole.Normal));
        
        // Parent should also get attribute from grandparent
        Assert.Equal (customAttribute, parentView.GetAttributeForRole (VisualRole.Normal));

        childView.Dispose ();
        parentView.Dispose ();
        grandparentView.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_ParentWithSchemeNameBreaksChain ()
    {
        // Test: parent with SchemeName stops deferral chain
        // Would fail without the SchemeName check (commit 866e002)
        
        var grandparentView = new View { SchemeName = "Base" };
        var parentView = new View { SchemeName = "Dialog" }; // Sets SchemeName
        var childView = new View (); // No scheme or SchemeName
        
        grandparentView.Add (parentView);
        parentView.Add (childView);

        // Grandparent customizes attributes
        var customAttribute = new Attribute (Color.BrightYellow, Color.BrightBlue);
        grandparentView.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Normal)
            {
                args.Result = customAttribute;
                args.Handled = true;
            }
        };

        // Parent should NOT get grandparent's customization (it has SchemeName)
        var dialogScheme = SchemeManager.GetHardCodedSchemes ()? ["Dialog"];
        Assert.NotEqual (customAttribute, parentView.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (dialogScheme!.Normal, parentView.GetAttributeForRole (VisualRole.Normal));
        
        // Child should get parent's Dialog scheme (defers to parent, parent uses Dialog scheme)
        Assert.Equal (dialogScheme!.Normal, childView.GetAttributeForRole (VisualRole.Normal));

        childView.Dispose ();
        parentView.Dispose ();
        grandparentView.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_OnGettingAttributeForRole_TakesPrecedence ()
    {
        // Test: view's own OnGettingAttributeForRole takes precedence over parent
        // This should work with or without the fix, but validates precedence
        
        var parentView = new View { SchemeName = "Base" };
        var childView = new TestViewWithAttributeOverride ();
        parentView.Add (childView);

        // Parent customizes attributes
        var parentAttribute = new Attribute (Color.BrightYellow, Color.BrightBlue);
        parentView.GettingAttributeForRole += (sender, args) =>
        {
            if (args.Role == VisualRole.Normal)
            {
                args.Result = parentAttribute;
                args.Handled = true;
            }
        };

        // Child's own override should take precedence
        var childOverrideAttribute = new Attribute (Color.BrightRed, Color.BrightCyan);
        childView.OverrideAttribute = childOverrideAttribute;
        
        Assert.Equal (childOverrideAttribute, childView.GetAttributeForRole (VisualRole.Normal));

        childView.Dispose ();
        parentView.Dispose ();
    }

    [Fact]
    public void GetAttributeForRole_MultipleRoles_DeferCorrectly ()
    {
        // Test: multiple VisualRoles all defer correctly
        // Would fail without the SuperView deferral fix for any role
        
        var parentView = new View { SchemeName = "Base" };
        var childView = new View ();
        parentView.Add (childView);

        var normalAttr = new Attribute (Color.Red, Color.Blue);
        var focusAttr = new Attribute (Color.Green, Color.Yellow);
        var hotNormalAttr = new Attribute (Color.Magenta, Color.Cyan);

        parentView.GettingAttributeForRole += (sender, args) =>
        {
            switch (args.Role)
            {
                case VisualRole.Normal:
                    args.Result = normalAttr;
                    args.Handled = true;
                    break;
                case VisualRole.Focus:
                    args.Result = focusAttr;
                    args.Handled = true;
                    break;
                case VisualRole.HotNormal:
                    args.Result = hotNormalAttr;
                    args.Handled = true;
                    break;
            }
        };

        // All roles should defer to parent
        Assert.Equal (normalAttr, childView.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (focusAttr, childView.GetAttributeForRole (VisualRole.Focus));
        Assert.Equal (hotNormalAttr, childView.GetAttributeForRole (VisualRole.HotNormal));

        childView.Dispose ();
        parentView.Dispose ();
    }

    private class TestViewWithAttributeOverride : View
    {
        public Attribute? OverrideAttribute { get; set; }

        protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
        {
            if (OverrideAttribute.HasValue && role == VisualRole.Normal)
            {
                currentAttribute = OverrideAttribute.Value;
                return true;
            }
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }
    }

}