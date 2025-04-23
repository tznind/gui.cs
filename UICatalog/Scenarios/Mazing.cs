using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("A Mazing", "Illustrates how to make a basic maze")]
[ScenarioCategory ("Drawing")]
public class Mazing : Scenario
{
    private Toplevel _top;
    private MazeGenerator _m;

    private List<Point> _potions;
    private List<Point> _goblins;
    private string _message;
    private bool _dead;

    public override void Main ()
    {
        Application.Init ();
        _top = new ();

        _m = new ();

        GenerateNpcs ();

        _top.DrawingContent += (s, e) =>
                              {
                                  // Build maze
                                  var lc = new LineCanvas (_m.BuildWallLinesFromMaze ());

                                  // Print maze
                                  foreach (KeyValuePair<Point, Rune> p in lc.GetMap ())
                                  {
                                      _top.Move (p.Key.X, p.Key.Y);
                                      _top.AddRune (p.Value);
                                  }

                                  // Draw objects
                                  _top.Move (_m.start.X, _m.start.Y);
                                  _top.AddStr ("s");

                                  _top.Move (_m.end.X, _m.end.Y);
                                  _top.AddStr ("e");

                                  _top.Move (_m.player.X, _m.player.Y);
                                  _top.SetAttribute (new (Color.Cyan, _top.GetNormalColor ().Background));
                                  _top.AddStr (_dead ? "x" : "@");

                                  // Draw goblins
                                  foreach (Point goblin in _goblins)
                                  {
                                      _top.Move (goblin.X, goblin.Y);
                                      _top.SetAttribute (new (Color.Red, _top.GetNormalColor ().Background));
                                      _top.AddStr ("G");
                                  }

                                  // Draw potions
                                  foreach (Point potion in _potions)
                                  {
                                      _top.Move (potion.X, potion.Y);
                                      _top.SetAttribute (new (Color.Yellow, _top.GetNormalColor ().Background));
                                      _top.AddStr ("p");
                                  }

                                  // Draw UI
                                  _top.SetAttribute (_top.GetNormalColor ());

                                  var g = new Gradient ([new (Color.Red), new (Color.BrightGreen)], [10]);
                                  _top.Move (_m.MazeWidth + 1, 0);
                                  _top.AddStr ("Name: Sir Flibble");
                                  _top.Move (_m.MazeWidth + 1, 1);
                                  _top.AddStr ("HP:");

                                  for (var i = 0; i < _m.playerHp; i++)
                                  {
                                      _top.Move (_m.MazeWidth + 1 + "HP:".Length + i, 1);
                                      _top.SetAttribute (new (g.GetColorAtFraction (i / 20f)));
                                      _top.AddRune ('█');
                                  }

                                  _top.SetAttribute (_top.GetNormalColor ());

                                  if (!string.IsNullOrWhiteSpace (_message))
                                  {
                                      _top.Move (_m.MazeWidth + 2, 2);
                                      _top.AddStr (_message);
                                  }
                              };

        _top.KeyDown += TopOnKeyDown;

        Application.Run (_top);

        _top.Dispose ();
        Application.Shutdown ();
    }

    private void GenerateNpcs ()
    {
        _goblins = _m.GenerateSpawnLocations (3, new ()); // Generate 3 goblins
        _potions = _m.GenerateSpawnLocations (3, _goblins); // Generate 3 potions
    }

    private void TopOnKeyDown (object sender, Key e)
    {
        if (_dead)
        {
            return;
        }

        Point newPos = _m.player;

        if (e.KeyCode == Key.CursorLeft)
        {
            newPos = new (_m.player.X - 1, _m.player.Y);
        }

        if (e.KeyCode == Key.CursorRight)
        {
            newPos = new (_m.player.X + 1, _m.player.Y);
        }

        if (e.KeyCode == Key.CursorUp)
        {
            newPos = new (_m.player.X, _m.player.Y - 1);
        }

        if (e.KeyCode == Key.CursorDown)
        {
            newPos = new (_m.player.X, _m.player.Y + 1);
        }

        // Only move if in bounds and it's a path
        if (newPos.X >= 0 && newPos.X < _m.maze.GetLength (1) && newPos.Y >= 0 && newPos.Y < _m.maze.GetLength (0) && _m.maze [newPos.Y, newPos.X] == 0)
        {
            _m.player = newPos;

            // Check if player is on a goblin
            if (_goblins.Contains (_m.player))
            {
                _message = "You fight a goblin!";
                _m.playerHp -= 5; // Decrease player's HP when attacked

                // Remove the goblin
                _goblins.Remove (_m.player);


                // Check if player is dead
                if (_m.playerHp <= 0)
                {
                    _message = "You died!";
                    _top.SetNeedsDraw (); // trigger redraw
                    _dead = true;
                    return; // Stop further action if dead
                }
            }
            else if (_potions.Contains (_m.player))
            {
                _message = "You drink a health potion!";
                _m.playerHp = Math.Min (20, _m.playerHp + 5); // increase player's HP when drinking potion

                // Remove the potion
                _potions.Remove (_m.player);
            }
            else
            {
                _message = string.Empty;
            }

            _top.SetNeedsDraw (); // trigger redraw
        }

        // Optional win condition:
        if (_m.player == _m.end)
        {
            _m = new (); // Generate a new maze
            GenerateNpcs ();
            _top.SetNeedsDraw (); // trigger redraw
        }
    }
}

internal class MazeGenerator
{
    private readonly int width = 20;
    private readonly int height = 10;
    public int [,] maze;
    public readonly Random rand = new ();
    public readonly Point start;
    public readonly Point end;
    public Point player;
    public int playerHp = 20;

    // Private accessors for width and height
    public int MazeWidth => width * 2 + 1;
    public int MazeHeight => height * 2 + 1;

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
        Carve (new (startX, startY));

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

    public List<Point> GenerateSpawnLocations (int count, List<Point> exclude)
    {
        // Create a new copy of the list so we can track exclusions
        exclude = exclude.ToList ();

        List<Point> locations = new ();

        for (var i = 0; i < count; i++)
        {
            Point point;

            do
            {
                point = new (rand.Next (1, width * 2), rand.Next (1, height * 2));
            }

            // Ensure the spawn point is not in the exclusion list and it's an open space (not a wall)
            while (exclude.Contains (point) || maze [point.Y, point.X] != 0);

            exclude.Add (point); // Mark this location as occupied
            locations.Add (point); // Add the location to the list
        }

        return locations;
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
