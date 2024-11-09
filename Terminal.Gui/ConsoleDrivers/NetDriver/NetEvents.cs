#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

internal class NetEvents : IDisposable
{
    private CancellationTokenSource? _inputReadyCancellationTokenSource;
    private readonly BlockingCollection<InputResult> _inputQueue = new (new ConcurrentQueue<InputResult> ());
    private readonly ConsoleDriver _consoleDriver;

    /// <summary>
    /// How long to wait before giving up on an <see cref="AnsiEscapeSequenceRequest"/>
    /// </summary>
    private readonly TimeSpan _abandonAfter = TimeSpan.FromMilliseconds (200);

#if PROCESS_REQUEST
    bool _neededProcessRequest;
#endif
    public AnsiEscapeSequenceRequests EscSeqRequests { get; } = new ();

    public AnsiResponseParser<ConsoleKeyInfo> Parser { get; private set; } = new ();

    public NetEvents (ConsoleDriver consoleDriver)
    {
        _consoleDriver = consoleDriver ?? throw new ArgumentNullException (nameof (consoleDriver));
        _inputReadyCancellationTokenSource = new ();

        Parser.UnexpectedResponseHandler = ProcessRequestResponse;

        Task.Run (ProcessInputQueue, _inputReadyCancellationTokenSource.Token);

        Task.Run (CheckWindowSizeChange, _inputReadyCancellationTokenSource.Token);
    }

    public InputResult? DequeueInput ()
    {
        while (_inputReadyCancellationTokenSource is { })
        {
            try
            {
                return _inputQueue.Take (_inputReadyCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

#if PROCESS_REQUEST
            _neededProcessRequest = false;
#endif
        }

        return null;
    }

    private ConsoleKeyInfo ReadConsoleKeyInfo (CancellationToken cancellationToken, bool intercept = true)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // if there is a key available, return it without waiting
            //  (or dispatching work to the thread queue)
            if (Console.KeyAvailable)
            {
                return Console.ReadKey (intercept);
            }

            var oldest = EscSeqRequests.Statuses.MinBy (s => s.Sent);

            if (oldest != null && DateTime.Now.Subtract (oldest.Sent) > _abandonAfter)
            {
                EscSeqRequests.Remove (oldest);
                oldest.AnsiRequest.RaiseResponseFromInput (null);
            }

            if (!_forceRead)
            {
                Task.Delay (100, cancellationToken).Wait (cancellationToken);
            }
        }

        cancellationToken.ThrowIfCancellationRequested ();

        return default (ConsoleKeyInfo);
    }

    internal bool _forceRead;

    private void ProcessInputQueue ()
    {
        while (_inputReadyCancellationTokenSource is { IsCancellationRequested: false })
        {
            if (_inputQueue.Count == 0)
            {
                while (_inputReadyCancellationTokenSource is { IsCancellationRequested: false })
                {
                    ConsoleKeyInfo consoleKeyInfo;

                    consoleKeyInfo = ReadConsoleKeyInfo (_inputReadyCancellationTokenSource.Token);

                    // Parse
                    foreach (var k in Parser.ProcessInput (Tuple.Create (consoleKeyInfo.KeyChar, consoleKeyInfo)))
                    {
                        ProcessMapConsoleKeyInfo (k.Item2);
                    }
                }
            }
        }
    }

    void ProcessMapConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        _inputQueue.Add (
                             new ()
                         {
                             EventType = EventType.Key, ConsoleKeyInfo = AnsiEscapeSequenceRequestUtils.MapConsoleKeyInfo (consoleKeyInfo)
                         }
                        );
    }

    private void CheckWindowSizeChange ()
    {
        void RequestWindowSize (CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a while then check if screen has changed sizes
                Task.Delay (500, cancellationToken).Wait (cancellationToken);

                int buffHeight, buffWidth;

                if (((NetDriver)_consoleDriver).IsWinPlatform)
                {
                    buffHeight = Math.Max (Console.BufferHeight, 0);
                    buffWidth = Math.Max (Console.BufferWidth, 0);
                }
                else
                {
                    buffHeight = _consoleDriver.Rows;
                    buffWidth = _consoleDriver.Cols;
                }

                if (EnqueueWindowSizeEvent (
                                            Math.Max (Console.WindowHeight, 0),
                                            Math.Max (Console.WindowWidth, 0),
                                            buffHeight,
                                            buffWidth
                                           ))
                {
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested ();
        }

        while (_inputReadyCancellationTokenSource is { IsCancellationRequested: false })
        {
            try
            {
                RequestWindowSize (_inputReadyCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Enqueue a window size event if the window size has changed.</summary>
    /// <param name="winHeight"></param>
    /// <param name="winWidth"></param>
    /// <param name="buffHeight"></param>
    /// <param name="buffWidth"></param>
    /// <returns></returns>
    private bool EnqueueWindowSizeEvent (int winHeight, int winWidth, int buffHeight, int buffWidth)
    {
        if (winWidth == _consoleDriver.Cols && winHeight == _consoleDriver.Rows)
        {
            return false;
        }

        int w = Math.Max (winWidth, 0);
        int h = Math.Max (winHeight, 0);

        _inputQueue.Add (
                         new ()
                         {
                             EventType = EventType.WindowSize, WindowSizeEvent = new () { Size = new (w, h) }
                         }
                        );

        return true;
    }

    private bool ProcessRequestResponse (IEnumerable<Tuple<char, ConsoleKeyInfo>> obj)
    {
        // Added for signature compatibility with existing method, not sure what they are even for.
        ConsoleKeyInfo newConsoleKeyInfo = default;
        ConsoleKey key = default;
        ConsoleModifiers mod = default;

        ProcessRequestResponse (ref newConsoleKeyInfo, ref key, obj.Select (v => v.Item2).ToArray (), ref mod);

        // Handled
        return true;
    }

    // Process a CSI sequence received by the driver (key pressed, mouse event, or request/response event)
    private void ProcessRequestResponse (
        ref ConsoleKeyInfo newConsoleKeyInfo,
        ref ConsoleKey key,
        ConsoleKeyInfo [] cki,
        ref ConsoleModifiers mod
    )
    {
        // isMouse is true if it's CSI<, false otherwise
        AnsiEscapeSequenceRequestUtils.DecodeEscSeq (
                                  EscSeqRequests,
                                  ref newConsoleKeyInfo,
                                  ref key,
                                  cki,
                                  ref mod,
                                  out string c1Control,
                                  out string code,
                                  out string [] values,
                                  out string terminating,
                                  out bool isMouse,
                                  out List<MouseFlags> mouseFlags,
                                  out Point pos,
                                  out AnsiEscapeSequenceRequestStatus? seqReqStatus,
                                  (f, p) => HandleMouseEvent (MapMouseFlags (f), p)
                                 );

        if (isMouse)
        {
            foreach (MouseFlags mf in mouseFlags)
            {
                HandleMouseEvent (MapMouseFlags (mf), pos);
            }

            return;
        }

        if (seqReqStatus is { })
        {
            //HandleRequestResponseEvent (c1Control, code, values, terminating);

            var ckiString = AnsiEscapeSequenceRequestUtils.ToString (cki);

            lock (seqReqStatus.AnsiRequest._responseLock)
            {
                seqReqStatus.AnsiRequest.RaiseResponseFromInput (ckiString);
            }

            return;
        }

        if (!string.IsNullOrEmpty (AnsiEscapeSequenceRequestUtils.InvalidRequestTerminator))
        {
            if (EscSeqRequests.Statuses.TryDequeue (out AnsiEscapeSequenceRequestStatus? result))
            {
                lock (result.AnsiRequest._responseLock)
                {
                    result.AnsiRequest.RaiseResponseFromInput (AnsiEscapeSequenceRequestUtils.InvalidRequestTerminator);

                    AnsiEscapeSequenceRequestUtils.InvalidRequestTerminator = null;
                }
            }

            return;
        }

        if (newConsoleKeyInfo != default)
        {
            HandleKeyboardEvent (newConsoleKeyInfo);
        }
    }

    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private MouseButtonState MapMouseFlags (MouseFlags mouseFlags)
    {
        MouseButtonState mbs = default;

        foreach (object flag in Enum.GetValues (mouseFlags.GetType ()))
        {
            if (mouseFlags.HasFlag ((MouseFlags)flag))
            {
                switch (flag)
                {
                    case MouseFlags.Button1Pressed:
                        mbs |= MouseButtonState.Button1Pressed;

                        break;
                    case MouseFlags.Button1Released:
                        mbs |= MouseButtonState.Button1Released;

                        break;
                    case MouseFlags.Button1Clicked:
                        mbs |= MouseButtonState.Button1Clicked;

                        break;
                    case MouseFlags.Button1DoubleClicked:
                        mbs |= MouseButtonState.Button1DoubleClicked;

                        break;
                    case MouseFlags.Button1TripleClicked:
                        mbs |= MouseButtonState.Button1TripleClicked;

                        break;
                    case MouseFlags.Button2Pressed:
                        mbs |= MouseButtonState.Button2Pressed;

                        break;
                    case MouseFlags.Button2Released:
                        mbs |= MouseButtonState.Button2Released;

                        break;
                    case MouseFlags.Button2Clicked:
                        mbs |= MouseButtonState.Button2Clicked;

                        break;
                    case MouseFlags.Button2DoubleClicked:
                        mbs |= MouseButtonState.Button2DoubleClicked;

                        break;
                    case MouseFlags.Button2TripleClicked:
                        mbs |= MouseButtonState.Button2TripleClicked;

                        break;
                    case MouseFlags.Button3Pressed:
                        mbs |= MouseButtonState.Button3Pressed;

                        break;
                    case MouseFlags.Button3Released:
                        mbs |= MouseButtonState.Button3Released;

                        break;
                    case MouseFlags.Button3Clicked:
                        mbs |= MouseButtonState.Button3Clicked;

                        break;
                    case MouseFlags.Button3DoubleClicked:
                        mbs |= MouseButtonState.Button3DoubleClicked;

                        break;
                    case MouseFlags.Button3TripleClicked:
                        mbs |= MouseButtonState.Button3TripleClicked;

                        break;
                    case MouseFlags.WheeledUp:
                        mbs |= MouseButtonState.ButtonWheeledUp;

                        break;
                    case MouseFlags.WheeledDown:
                        mbs |= MouseButtonState.ButtonWheeledDown;

                        break;
                    case MouseFlags.WheeledLeft:
                        mbs |= MouseButtonState.ButtonWheeledLeft;

                        break;
                    case MouseFlags.WheeledRight:
                        mbs |= MouseButtonState.ButtonWheeledRight;

                        break;
                    case MouseFlags.Button4Pressed:
                        mbs |= MouseButtonState.Button4Pressed;

                        break;
                    case MouseFlags.Button4Released:
                        mbs |= MouseButtonState.Button4Released;

                        break;
                    case MouseFlags.Button4Clicked:
                        mbs |= MouseButtonState.Button4Clicked;

                        break;
                    case MouseFlags.Button4DoubleClicked:
                        mbs |= MouseButtonState.Button4DoubleClicked;

                        break;
                    case MouseFlags.Button4TripleClicked:
                        mbs |= MouseButtonState.Button4TripleClicked;

                        break;
                    case MouseFlags.ButtonShift:
                        mbs |= MouseButtonState.ButtonShift;

                        break;
                    case MouseFlags.ButtonCtrl:
                        mbs |= MouseButtonState.ButtonCtrl;

                        break;
                    case MouseFlags.ButtonAlt:
                        mbs |= MouseButtonState.ButtonAlt;

                        break;
                    case MouseFlags.ReportMousePosition:
                        mbs |= MouseButtonState.ReportMousePosition;

                        break;
                    case MouseFlags.AllEvents:
                        mbs |= MouseButtonState.AllEvents;

                        break;
                }
            }
        }

        return mbs;
    }

    //private Point _lastCursorPosition;

    //private void HandleRequestResponseEvent (string c1Control, string code, string [] values, string terminating)
    //{
    //    if (terminating ==

    //        // BUGBUG: I can't find where we send a request for cursor position (ESC[?6n), so I'm not sure if this is needed.
    //        // The observation is correct because the response isn't immediate and this is useless
    //        EscSeqUtils.CSI_RequestCursorPositionReport.Terminator)
    //    {
    //        var point = new Point { X = int.Parse (values [1]) - 1, Y = int.Parse (values [0]) - 1 };

    //        if (_lastCursorPosition.Y != point.Y)
    //        {
    //            _lastCursorPosition = point;
    //            var eventType = EventType.WindowPosition;
    //            var winPositionEv = new WindowPositionEvent { CursorPosition = point };

    //            _inputQueue.Enqueue (
    //                                 new InputResult { EventType = eventType, WindowPositionEvent = winPositionEv }
    //                                );
    //        }
    //        else
    //        {
    //            return;
    //        }
    //    }
    //    else if (terminating == EscSeqUtils.CSI_ReportTerminalSizeInChars.Terminator)
    //    {
    //        if (values [0] == EscSeqUtils.CSI_ReportTerminalSizeInChars.Value)
    //        {
    //            EnqueueWindowSizeEvent (
    //                                    Math.Max (int.Parse (values [1]), 0),
    //                                    Math.Max (int.Parse (values [2]), 0),
    //                                    Math.Max (int.Parse (values [1]), 0),
    //                                    Math.Max (int.Parse (values [2]), 0)
    //                                   );
    //        }
    //        else
    //        {
    //            EnqueueRequestResponseEvent (c1Control, code, values, terminating);
    //        }
    //    }
    //    else
    //    {
    //        EnqueueRequestResponseEvent (c1Control, code, values, terminating);
    //    }

    //    _inputReady.Set ();
    //}

    //private void EnqueueRequestResponseEvent (string c1Control, string code, string [] values, string terminating)
    //{
    //    var eventType = EventType.RequestResponse;
    //    var requestRespEv = new RequestResponseEvent { ResultTuple = (c1Control, code, values, terminating) };

    //    _inputQueue.Enqueue (
    //                         new InputResult { EventType = eventType, RequestResponseEvent = requestRespEv }
    //                        );
    //}

    private void HandleMouseEvent (MouseButtonState buttonState, Point pos)
    {
        var mouseEvent = new MouseEvent { Position = pos, ButtonState = buttonState };

        _inputQueue.Add (
                         new () { EventType = EventType.Mouse, MouseEvent = mouseEvent }
                        );
    }

    public enum EventType
    {
        Key = 1,
        Mouse = 2,
        WindowSize = 3,
        WindowPosition = 4,
        RequestResponse = 5
    }

    [Flags]
    public enum MouseButtonState
    {
        Button1Pressed = 0x1,
        Button1Released = 0x2,
        Button1Clicked = 0x4,
        Button1DoubleClicked = 0x8,
        Button1TripleClicked = 0x10,
        Button2Pressed = 0x20,
        Button2Released = 0x40,
        Button2Clicked = 0x80,
        Button2DoubleClicked = 0x100,
        Button2TripleClicked = 0x200,
        Button3Pressed = 0x400,
        Button3Released = 0x800,
        Button3Clicked = 0x1000,
        Button3DoubleClicked = 0x2000,
        Button3TripleClicked = 0x4000,
        ButtonWheeledUp = 0x8000,
        ButtonWheeledDown = 0x10000,
        ButtonWheeledLeft = 0x20000,
        ButtonWheeledRight = 0x40000,
        Button4Pressed = 0x80000,
        Button4Released = 0x100000,
        Button4Clicked = 0x200000,
        Button4DoubleClicked = 0x400000,
        Button4TripleClicked = 0x800000,
        ButtonShift = 0x1000000,
        ButtonCtrl = 0x2000000,
        ButtonAlt = 0x4000000,
        ReportMousePosition = 0x8000000,
        AllEvents = -1
    }

    public struct MouseEvent
    {
        public Point Position;
        public MouseButtonState ButtonState;
    }

    public struct WindowSizeEvent
    {
        public Size Size;
    }

    public struct WindowPositionEvent
    {
        public int Top;
        public int Left;
        public Point CursorPosition;
    }

    public struct RequestResponseEvent
    {
        public (string c1Control, string code, string [] values, string terminating) ResultTuple;
    }

    public struct InputResult
    {
        public EventType EventType;
        public ConsoleKeyInfo ConsoleKeyInfo;
        public MouseEvent MouseEvent;
        public WindowSizeEvent WindowSizeEvent;
        public WindowPositionEvent WindowPositionEvent;
        public RequestResponseEvent RequestResponseEvent;

        public readonly override string ToString ()
        {
            return (EventType switch
                    {
                        EventType.Key => ToString (ConsoleKeyInfo),
                        EventType.Mouse => MouseEvent.ToString (),

                        //EventType.WindowSize => WindowSize.ToString (),
                        //EventType.RequestResponse => RequestResponse.ToString (),
                        _ => "Unknown event type: " + EventType
                    })!;
        }

        /// <summary>Prints a ConsoleKeyInfoEx structure</summary>
        /// <param name="cki"></param>
        /// <returns></returns>
        public readonly string ToString (ConsoleKeyInfo cki)
        {
            var ke = new Key ((KeyCode)cki.KeyChar);
            var sb = new StringBuilder ();
            sb.Append ($"Key: {(KeyCode)cki.Key} ({cki.Key})");
            sb.Append ((cki.Modifiers & ConsoleModifiers.Shift) != 0 ? " | Shift" : string.Empty);
            sb.Append ((cki.Modifiers & ConsoleModifiers.Control) != 0 ? " | Control" : string.Empty);
            sb.Append ((cki.Modifiers & ConsoleModifiers.Alt) != 0 ? " | Alt" : string.Empty);
            sb.Append ($", KeyChar: {ke.AsRune.MakePrintable ()} ({(uint)cki.KeyChar}) ");
            string s = sb.ToString ().TrimEnd (',').TrimEnd (' ');

            return $"[ConsoleKeyInfo({s})]";
        }
    }

    private void HandleKeyboardEvent (ConsoleKeyInfo cki)
    {
        var inputResult = new InputResult { EventType = EventType.Key, ConsoleKeyInfo = cki };

        _inputQueue.Add (inputResult);
    }

    public void Dispose ()
    {
        _inputReadyCancellationTokenSource?.Cancel ();
        _inputReadyCancellationTokenSource?.Dispose ();
        _inputReadyCancellationTokenSource = null;

        try
        {
            // throws away any typeahead that has been typed by
            // the user and has not yet been read by the program.
            while (Console.KeyAvailable)
            {
                Console.ReadKey (true);
            }
        }
        catch (InvalidOperationException)
        {
            // Ignore - Console input has already been closed
        }
    }
}
