using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("A Mazing", "Illustrates how to make a basic maze")]
[ScenarioCategory ("Drawing")]
public class Mazing : Scenario
{
    public override void Main ()
    {
        Application.Init ();
        var top = new Toplevel ();

        var m = new MazeGenerator ();

        top.DrawingContent += (s, e) =>
                              {
                                  var lc = new LineCanvas (m.BuildWallLinesFromMaze ());

                                  // Print maze
                                  foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
                                  {
                                      top.Move (p.Key.X, p.Key.Y);
                                      top.AddRune (p.Value);
                                  }

                                  top.Move (m.start.x, m.start.y);
                                  top.AddStr ("s");

                                  top.Move (m.end.x, m.end.y);
                                  top.AddStr ("e");
                              };

        Application.Run (top);

        top.Dispose ();
        Application.Shutdown ();
    }
}

internal class MazeGenerator
{
    public readonly int width = 20;
    public readonly int height = 10;
    public int [,] maze;
    public readonly Random rand = new ();
    public readonly (int x, int y) start;
    public readonly (int x, int y) end;

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
        Carve (startX, startY);

        // Set random entrance
        start = GetRandomEdgePoint (w, h, true);
        maze [start.y, start.x] = 0;

        // Set random exit (ensure it's not same as entrance)
        end = GetRandomEdgePoint (w, h, false, start.x, start.y);
        maze [end.y, end.x] = 0;
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

    private void Carve (int x, int y)
    {
        maze [y, x] = 0;

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
            int nx = x + dir [0], ny = y + dir [1];

            if (nx > 0 && ny > 0 && nx < width * 2 && ny < height * 2 && maze [ny, nx] == 1)
            {
                maze [y + dir [1] / 2, x + dir [0] / 2] = 0;
                Carve (nx, ny);
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

    private (int x, int y) GetRandomEdgePoint (int w, int h, bool isEntrance, int avoidX = -1, int avoidY = -1)
    {
        List<(int x, int y)> candidates = new ();

        for (var i = 1; i < h - 1; i += 2)
        {
            candidates.Add ((0, i)); // Left edge
            candidates.Add ((w - 1, i)); // Right edge
        }

        for (var i = 1; i < w - 1; i += 2)
        {
            candidates.Add ((i, 0)); // Top edge
            candidates.Add ((i, h - 1)); // Bottom edge
        }

        // Remove one if same as entrance
        if (!isEntrance)
        {
            candidates.RemoveAll (p => p.x == avoidX && p.y == avoidY);
        }

        return candidates [rand.Next (candidates.Count)];
    }
}
