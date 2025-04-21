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
                      // Print maze
                      for (var y = 0; y < m.height; y++)
                      {
                          for (var x = 0; x < m.width; x++)
                          {
                              top.Move (x, y);
                              var c = m.maze [y, x] == 1 ? "#" : " ";
                              top.AddStr (c);
                          }
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
   public readonly int width = 40;
   public readonly int height = 20;
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
