using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("A Mazing", "Illustrates how to make a basic maze")]
[ScenarioCategory ("Drawing")]
public class Mazing : Scenario
{
    private Toplevel top;
    private MazeGenerator m;

    public override void Main ()
    {
        Application.Init ();
        top = new Toplevel();

        m = new MazeGenerator ();

        top.DrawingContent += (s, e) =>
                              {
                                  var lc = new LineCanvas (m.BuildWallLinesFromMaze ());

                                  // Print maze
                                  foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
                                  {
                                      top.Move (p.Key.X, p.Key.Y);
                                      top.AddRune (p.Value);
                                  }

                                  top.Move (m.start.X, m.start.Y);
                                  top.AddStr ("s");

                                  top.Move (m.end.X, m.end.Y);
                                  top.AddStr ("e");

                                  top.Move (m.player.X, m.player.Y);
                                  top.SetAttribute (new Attribute (Color.BrightGreen, top.GetNormalColor ().Background));
                                  top.AddStr ("@");
                              };

        top.KeyDown += TopOnKeyDown;


        Application.Run (top);

        top.Dispose ();
        Application.Shutdown ();
    }

    private void TopOnKeyDown (object sender, Key e)
    {
       Point newPos = m.player;

           if(e.KeyCode ==  Key.CursorLeft)
               newPos = new Point (m.player.X - 1, m.player.Y);
           if (e.KeyCode == Key.CursorRight)
                newPos = new Point (m.player.X + 1, m.player.Y);
           if (e.KeyCode == Key.CursorUp)
                newPos = new Point (m.player.X, m.player.Y - 1);
           if (e.KeyCode == Key.CursorDown)
                newPos = new Point (m.player.X, m.player.Y + 1);

       // Only move if in bounds and it's a path
       if (newPos.X >= 0 && newPos.X < m.maze.GetLength (1) &&
           newPos.Y >= 0 && newPos.Y < m.maze.GetLength (0) &&
           m.maze [newPos.Y, newPos.X] == 0)
       {
           m.player = newPos;
           top.SetNeedsDraw(); // trigger redraw
       }

       // Optional win condition:
       if (m.player == m.end)
       {
           MessageBox.Query (30, 7, "Maze", "You made it!", "Ok");
       }
    }
}

internal class MazeGenerator
{
    public readonly int width = 20;
    public readonly int height = 10;
    public int [,] maze;
    public readonly Random rand = new ();
    public readonly Point start;
    public readonly Point end;
    public Point player;

    public MazeGenerator ()
    {
        int w = width * 2 + 1;
        int h = height * 2 + 1;
        maze = new int [h, w];

        // Fill with walls
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            maze [y, x] = 1;
        }

        // Start carving from a random odd cell
        int startX = rand.Next (width) * 2 + 1;
        int startY = rand.Next (height) * 2 + 1;
        Carve (new(startX, startY));

        // Set random entrance
        start = GetRandomEdgePoint (w, h, true);
        maze [start.Y, start.X] = 0;
        player = start;

        // Set random exit (ensure it's not same as entrance)
        end = GetRandomEdgePoint (w, h, false, start.X, start.Y);
        maze [end.Y, end.X] = 0;
    }

    public List<StraightLine> BuildWallLinesFromMaze ()
    {
        List<StraightLine> lines = new ();

        int h = maze.GetLength (0);
        int w = maze.GetLength (1);

        // Horizontal lines
        for (var y = 0; y < h; y++)
        {
            var x = 0;

            while (x < w)
            {
                if (maze [y, x] == 1)
                {
                    int startX = x;

                    while (x < w && maze [y, x] == 1)
                    {
                        x++;
                    }

                    int length = x - startX;

                    if (length > 1)
                    {
                        lines.Add (new (new (startX, y), length, Orientation.Horizontal, LineStyle.Single));
                    }
                }
                else
                {
                    x++;
                }
            }
        }

        // Vertical lines
        for (var x = 0; x < w; x++)
        {
            var y = 0;

            while (y < h)
            {
                if (maze [y, x] == 1)
                {
                    int startY = y;

                    while (y < h && maze [y, x] == 1)
                    {
                        y++;
                    }

                    int length = y - startY;
                    lines.Add (new (new (x, startY), length, Orientation.Vertical, LineStyle.Single));
                }
                else
                {
                    y++;
                }
            }
        }

        return lines;
    }

    private void Carve (Point p)
    {
        maze [p.Y, p.X] = 0;

        int [] [] dirs =
        {
            new [] { 0, -2 },
            new [] { 0, 2 },
            new [] { -2, 0 },
            new [] { 2, 0 }
        };

        Shuffle (dirs);

        foreach (int [] dir in dirs)
        {
            int nx = p.X + dir [0], ny = p.Y + dir [1];

            if (nx > 0 && ny > 0 && nx < width * 2 && ny < height * 2 && maze [ny, nx] == 1)
            {
                maze [p.Y + dir [1] / 2, p.X + dir [0] / 2] = 0;
                Carve (new (nx, ny));
            }
        }
    }

    private void Shuffle (int [] [] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rand.Next (i + 1);
            int [] temp = array [i];
            array [i] = array [j];
            array [j] = temp;
        }
    }

    private Point GetRandomEdgePoint (int w, int h, bool isEntrance, int avoidX = -1, int avoidY = -1)
    {
        List<Point> candidates = new ();

        for (var i = 1; i < h - 1; i += 2)
        {
            candidates.Add (new (0, i)); // Left edge
            candidates.Add (new (w - 1, i)); // Right edge
        }

        for (var i = 1; i < w - 1; i += 2)
        {
            candidates.Add (new (i, 0)); // Top edge
            candidates.Add (new (i, h - 1)); // Bottom edge
        }

        // Remove one if same as entrance
        if (!isEntrance)
        {
            candidates.RemoveAll (p => p.X == avoidX && p.Y == avoidY);
        }

        return candidates [rand.Next (candidates.Count)];
    }
}
