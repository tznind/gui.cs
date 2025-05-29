﻿using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class AllViewsTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Tests_All_Constructors (Type viewType)
    {
        Assert.True (TestAllConstructorsOfType (viewType));

        return;

        bool TestAllConstructorsOfType (Type type)
        {
            foreach (ConstructorInfo ctor in type.GetConstructors ())
            {
                View view = CreateViewFromType (type, ctor);

                if (view != null)
                {
                    Assert.True (type.FullName == view.GetType ().FullName);
                }
            }

            return true;
        }
    }

    //[Fact]
    //public void AllViews_HotKey_Works ()
    //{
    //	foreach (var type in GetAllViewClasses ()) {
    //		_output.WriteLine ($"Testing {type.Name}");
    //		var view = GetTypeInitializer (type, type.GetConstructor (Array.Empty<Type> ()));
    //		view.HotKeySpecifier = (Rune)'^';
    //		view.Text = "^text";
    //		Assert.Equal(Key.T, view.HotKey);
    //	}
    //}

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_Select_Raises_Selecting (Type viewType)
    {
        var view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var selectingCount = 0;
        view.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        view.Accepting += (s, e) => { acceptedCount++; };

        if (view.InvokeCommand (Command.Select) == true)
        {
            Assert.Equal (1, selectingCount);
            Assert.Equal (0, acceptedCount);
        }
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_Accept_Raises_Accepted (Type viewType)
    {
        var view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var selectingCount = 0;
        view.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        view.Accepting += (s, e) => { acceptedCount++; };

        if (view.InvokeCommand (Command.Accept) == true)
        {
            Assert.Equal (0, selectingCount);
            Assert.Equal (1, acceptedCount);
        }
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_HotKey_Raises_HandlingHotKey (Type viewType)
    {
        var view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }
        else
        {
            view.HotKey = Key.T;
        }

        var acceptedCount = 0;
        view.Accepting += (s, e) => { acceptedCount++; };

        var handlingHotKeyCount = 0;
        view.HandlingHotKey += (s, e) => { handlingHotKeyCount++; };

        if (view.InvokeCommand (Command.HotKey) == true)
        {
            Assert.Equal (1, handlingHotKeyCount);
            Assert.Equal (0, acceptedCount);
        }
    }
}
