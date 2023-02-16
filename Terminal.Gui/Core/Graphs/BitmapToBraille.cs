// This is a C# port of https://github.com/andraaspar/bitmap-to-braille by Andraaspar
using System;
using System.Text;

namespace Terminal.Gui.Graphs
{
    /// <summary>
    /// Renders an image as unicode Braille.
    /// </summary>
    public class BitmapToBraille
    {

        public const int CHAR_WIDTH = 2;
        public const int CHAR_HEIGHT = 4;

        const string CHARS = " ⠁⠂⠃⠄⠅⠆⠇⡀⡁⡂⡃⡄⡅⡆⡇⠈⠉⠊⠋⠌⠍⠎⠏⡈⡉⡊⡋⡌⡍⡎⡏⠐⠑⠒⠓⠔⠕⠖⠗⡐⡑⡒⡓⡔⡕⡖⡗⠘⠙⠚⠛⠜⠝⠞⠟⡘⡙⡚⡛⡜⡝⡞⡟⠠⠡⠢⠣⠤⠥⠦⠧⡠⡡⡢⡣⡤⡥⡦⡧⠨⠩⠪⠫⠬⠭⠮⠯⡨⡩⡪⡫⡬⡭⡮⡯⠰⠱⠲⠳⠴⠵⠶⠷⡰⡱⡲⡳⡴⡵⡶⡷⠸⠹⠺⠻⠼⠽⠾⠿⡸⡹⡺⡻⡼⡽⡾⡿⢀⢁⢂⢃⢄⢅⢆⢇⣀⣁⣂⣃⣄⣅⣆⣇⢈⢉⢊⢋⢌⢍⢎⢏⣈⣉⣊⣋⣌⣍⣎⣏⢐⢑⢒⢓⢔⢕⢖⢗⣐⣑⣒⣓⣔⣕⣖⣗⢘⢙⢚⢛⢜⢝⢞⢟⣘⣙⣚⣛⣜⣝⣞⣟⢠⢡⢢⢣⢤⢥⢦⢧⣠⣡⣢⣣⣤⣥⣦⣧⢨⢩⢪⢫⢬⢭⢮⢯⣨⣩⣪⣫⣬⣭⣮⣯⢰⢱⢲⢳⢴⢵⢶⢷⣰⣱⣲⣳⣴⣵⣶⣷⢸⢹⢺⢻⢼⢽⢾⢿⣸⣹⣺⣻⣼⣽⣾⣿";

        public int WidthPixels {get; }
        public int HeightPixels { get; }

        public Func<int,int,bool> PixelIsLit {get;}

        public BitmapToBraille (int widthPixels, int heightPixels, Func<int, int, bool> pixelIsLit)
        {
            WidthPixels = widthPixels;
            HeightPixels = heightPixels;
            PixelIsLit = pixelIsLit;
        }

        public string GenerateImage() {
            int imageHeightChars = (int) Math.Ceiling((double)HeightPixels / CHAR_HEIGHT);
            int imageWidthChars = (int) Math.Ceiling((double)WidthPixels / CHAR_WIDTH);

            var result = new StringBuilder();

            for (int y = 0; y < imageHeightChars; y++) {
                
                for (int x = 0; x < imageWidthChars; x++) {
                    int baseX = x * CHAR_WIDTH;
                    int baseY = y * CHAR_HEIGHT;

                    int charIndex = 0;
                    int value = 1;

                    for (int charX = 0; charX < CHAR_WIDTH; charX++) {
                        for (int charY = 0; charY < CHAR_HEIGHT; charY++) {
                            int bitmapX = baseX + charX;
                            int bitmapY = baseY + charY;
                            bool pixelExists = bitmapX < WidthPixels && bitmapY < HeightPixels;

                            if (pixelExists && PixelIsLit(bitmapX, bitmapY)) {
                                charIndex += value;
                            }
                            value *= 2;
                        }
                    }

                    result.Append(CHARS[charIndex]);
                }
                result.Append('\n');
            }
            return result.ToString().TrimEnd();
        }  
    }
}

