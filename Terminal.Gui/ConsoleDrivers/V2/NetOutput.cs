using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;
public class NetOutput : IConsoleOutput
{
    /// <inheritdoc />
    public void Write (string text)
    {
        Console.WriteLine (text);
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        Console.Clear ();
    }

}
