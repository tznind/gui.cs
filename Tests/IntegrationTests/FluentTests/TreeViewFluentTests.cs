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
        var tv = new TreeView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        var root = new TreeNode ("Root")
        {
            Children =
            [
                car = new ("Car"),
                lorry = new ("Lorry"),
                bike = new ("Bike")
            ]
        };

        tv.AddObject (root);

        using GuiTestContext context =
            With.A<Window> (40, 10, d)
                .Add (tv)
                .Focus (tv)
                .WaitIteration ()
                .ScreenShot ("Before expanding", _out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
                .Then (() => Assert.Null (tv.GetObjectOnRow (1)))
                .Right ()
                .ScreenShot ("After expanding", _out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
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
                .ScreenShot ("After re-order", _out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
                .Then (() => Assert.Equal (bike, tv.GetObjectOnRow (1)))
                .Then (() => Assert.Equal (car, tv.GetObjectOnRow (2)))
                .Then (() => Assert.Equal (lorry, tv.GetObjectOnRow (3)))
                .WriteOutLogs (_out);

        context.Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void TreeViewReOrder_PreservesExpansion (V2TestDriver d)
    {
        var tv = new TreeView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        TreeNode mrA;
        TreeNode mrB;

        TreeNode mrC;

        TreeNode mrD;
        TreeNode mrE;

        var root = new TreeNode ("Root")
        {
            Children =
            [
                car = new ("Car")
                {
                    Children =
                    [
                        mrA = new ("Mr A"),
                        mrB = new ("Mr B")
                    ]
                },
                lorry = new ("Lorry")
                {
                    Children =
                    [
                        mrC = new ("Mr C")
                    ]
                },
                bike = new ("Bike")
                {
                    Children =
                    [
                        mrD = new ("Mr D"),
                        mrE = new ("Mr E")
                    ]
                }
            ]
        };

        tv.AddObject (root);
        tv.ExpandAll ();

        using GuiTestContext context =
            With.A<Window> (40, 13, d)
                .Add (tv)
                .WaitIteration ()
                .ScreenShot ("Initial State", _out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
                .Then (() => Assert.Equal (car, tv.GetObjectOnRow (1)))
                .Then (() => Assert.Equal (mrA, tv.GetObjectOnRow (2)))
                .Then (() => Assert.Equal (mrB, tv.GetObjectOnRow (3)))
                .Then (() => Assert.Equal (lorry, tv.GetObjectOnRow (4)))
                .Then (() => Assert.Equal (mrC, tv.GetObjectOnRow (5)))
                .Then (() => Assert.Equal (bike, tv.GetObjectOnRow (6)))
                .Then (() => Assert.Equal (mrD, tv.GetObjectOnRow (7)))
                .Then (() => Assert.Equal (mrE, tv.GetObjectOnRow (8)))
                .Then (
                       () =>
                       {
                           // Re order
                           root.Children = [bike, car, lorry];
                           tv.RefreshObject (root);
                       })
                .WaitIteration ()
                .ScreenShot ("After re-order", _out)
                .Then (() => Assert.Equal (root, tv.GetObjectOnRow (0)))
                .Then (() => Assert.Equal (bike, tv.GetObjectOnRow (1)))
                .Then (() => Assert.Equal (mrD, tv.GetObjectOnRow (2)))
                .Then (() => Assert.Equal (mrE, tv.GetObjectOnRow (3)))
                .Then (() => Assert.Equal (car, tv.GetObjectOnRow (4)))
                .Then (() => Assert.Equal (mrA, tv.GetObjectOnRow (5)))
                .Then (() => Assert.Equal (mrB, tv.GetObjectOnRow (6)))
                .Then (() => Assert.Equal (lorry, tv.GetObjectOnRow (7)))
                .Then (() => Assert.Equal (mrC, tv.GetObjectOnRow (8)))
                .WriteOutLogs (_out);

        context.Stop ();
    }
}
