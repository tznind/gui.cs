using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class TreeViewFluentTests
{

    private readonly TextWriter _out;

    public TreeViewFluentTests (ITestOutputHelper outputHelper) { _out = new TestOutputWriter (outputHelper); }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void TreeView_AllowReOrdering (V2TestDriver d)
    {
        var tv = new TreeView ()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        var root = new TreeNode ("Root")
        {
            Children = [
                           car = new TreeNode("Car"),
                           lorry = new TreeNode("Lorry"),
                           bike = new TreeNode("Bike")
                ]
        };

        tv.AddObject (root);


        using GuiTestContext context =
            With.A<Window> (40, 10, d)
                .Add (tv)
                .Focus(tv)
                .WaitIteration ()
                .ScreenShot ("Before expanding",_out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
                .Then (() => Assert.Null (tv.GetObjectOnRow (1)))
                .Right ()
                .ScreenShot ("After expanding",_out)
                .Then (()=>Assert.Equal (root,tv.GetObjectOnRow (0)))
                .Then (() => Assert.Equal (car, tv.GetObjectOnRow (1)))
                .Then (() => Assert.Equal (lorry, tv.GetObjectOnRow (2)))
                .Then (() => Assert.Equal (bike, tv.GetObjectOnRow (3)))
                .Then (
                       () =>
                       {
                           // Re order
                           root.Children = [bike, car, lorry];
                           tv.RefreshObject (root);
                       })
                .WaitIteration ()
                .ScreenShot ("After re-order",_out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
                .Then (() => Assert.Equal (bike, tv.GetObjectOnRow (1)))
                .Then (() => Assert.Equal (car, tv.GetObjectOnRow (2)))
                .Then (() => Assert.Equal (lorry, tv.GetObjectOnRow (3)))
                .WriteOutLogs (_out);

        context.Stop ();
    }

}
