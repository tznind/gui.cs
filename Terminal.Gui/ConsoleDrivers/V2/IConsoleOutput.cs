using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;
public interface IConsoleOutput : IDisposable
{
    void Write(string text);
    void Write (IOutputBuffer buffer);
}
