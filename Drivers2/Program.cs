using System.Collections.Concurrent;
using Terminal.Gui;
using static Terminal.Gui.WindowsConsole;

namespace Drivers2;

class Program
{
    static void Main (string [] args)
    {
        bool win = false;

        if (args.Length > 0)
        {
            if (args [0] == "net")
            {
                // default
            }
            else if(args [0] == "win")
            {
                win = true;
            }
            else
            {
                Console.WriteLine("Arg must be 'win' or 'net' or blank to use default");
            }
        }

        if (win)
        {
            using var input = new WindowsInput ();
            using var output = new WindowsOutput ();
            var parser = new AnsiResponseParser<InputRecord> ();
            var buffer = new ConcurrentQueue<InputRecord> ();

            var loop = new MainLoop<InputRecord> ();
            loop.Initialize (buffer,parser,output);

            var coordinator = new MainLoopCoordinator<InputRecord> (input,loop);

            coordinator.Start ();

            output.Write ("Hello World");

            coordinator.Stop ();
        }
        else
        {
            using var input = new NetInput ();
            using var output = new NetOutput () ;
            var parser = new AnsiResponseParser<ConsoleKeyInfo> ();
            var buffer = new ConcurrentQueue<ConsoleKeyInfo> ();

            var loop = new MainLoop<ConsoleKeyInfo> ();
            loop.Initialize (buffer, parser, output);

            var coordinator = new MainLoopCoordinator<ConsoleKeyInfo> (input, loop);

            coordinator.Start ();

            output.Write ("Hello World");


            coordinator.Stop ();
        }

    }
}
