﻿using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace UnitTests;

/// <summary>
///     Provides xUnit-style assertions for <see cref="IConsoleDriver"/> contents.
/// </summary>
internal partial class DriverAssert
{
    private const char SpaceChar = ' ';
    private static readonly Rune SpaceRune = (Rune)SpaceChar;
#pragma warning disable xUnit1013 // Public method should be marked as test
    /// <summary>
    ///     Verifies <paramref name="expectedAttributes"/> are found at the locations specified by
    ///     <paramref name="expectedLook"/>. <paramref name="expectedLook"/> is a bitmap of indexes into
    ///     <paramref name="expectedAttributes"/> (e.g. "00110" means the attribute at <c>expectedAttributes[1]</c> is expected
    ///     at the 3rd and 4th columns of the 1st row of driver.Contents).
    /// </summary>
    /// <param name="expectedLook">
    ///     Numbers between 0 and 9 for each row/col of the console.  Must be valid indexes into
    ///     <paramref name="expectedAttributes"/>.
    /// </param>
    /// <param name="output"></param>
    /// <param name="driver">The IConsoleDriver to use. If null <see cref="Application.Driver"/> will be used.</param>
    /// <param name="expectedAttributes"></param>
    public static void AssertDriverAttributesAre (
        string expectedLook,
        ITestOutputHelper output,
        IConsoleDriver driver = null,
        params Attribute [] expectedAttributes
    )
    {
#pragma warning restore xUnit1013 // Public method should be marked as test

        if (expectedAttributes.Length > 10)
        {
            throw new ArgumentException ("This method only works for UIs that use at most 10 colors");
        }

        expectedLook = expectedLook.Trim ();
        driver ??= Application.Driver;

        Cell [,] contents = driver!.Contents;

        var line = 0;

        foreach (string lineString in expectedLook.Split ('\n').Select (l => l.Trim ()))
        {
            for (var c = 0; c < lineString.Length; c++)
            {
                Attribute? val = contents! [line, c].Attribute;

                List<Attribute> match = expectedAttributes.Where (e => e == val).ToList ();

                switch (match.Count)
                {
                    case 0:
                        output.WriteLine (
                                          $"{Application.ToString (driver)}\n"
                                          + $"Expected Attribute {val} (PlatformColor = {val!.Value.PlatformColor}) at Contents[{line},{c}] {contents [line, c]} ((PlatformColor = {contents [line, c].Attribute.Value.PlatformColor}) was not found.\n"
                                          + $" Expected: {string.Join (",", expectedAttributes.Select (attr => attr))}\n"
                                          + $" But Was: <not found>"
                                         );
                        Assert.Empty (match);

                        return;
                    case > 1:
                        throw new ArgumentException (
                                                     $"Bad value for expectedColors, {match.Count} Attributes had the same Value"
                                                    );
                }

                char colorUsed = Array.IndexOf (expectedAttributes, match [0]).ToString () [0];
                char userExpected = lineString [c];

                if (colorUsed != userExpected)
                {
                    output.WriteLine ($"{Application.ToString (driver)}");
                    output.WriteLine ($"Unexpected Attribute at Contents[{line},{c}] = {contents [line, c]}.");
                    output.WriteLine ($" Expected: {userExpected} ({expectedAttributes [int.Parse (userExpected.ToString ())]})");
                    output.WriteLine ($"  But Was: {colorUsed} ({val})");

                    // Print `contents` as the expected and actual attribute indexes in a grid where each cell is of the form "e:a" (e = expected, a = actual)
                    // e.g:
                    // 0:1 0:0 1:1
                    // 0:0 1:1 0:0
                    // 0:0 1:1 0:0

                    //// Use StringBuilder since output only has .WriteLine
                    //var sb = new StringBuilder ();
                    //// for each line in `contents`
                    //for (var r = 0; r < driver.Rows; r++)
                    //{
                    //    // for each column in `contents`
                    //    for (var cc = 0; cc < driver.Cols; cc++)
                    //    {
                    //        // get the attribute at the current location
                    //        Attribute? val2 = contents [r, cc].Attribute;
                    //        // if the attribute is not null
                    //        if (val2.HasValue)
                    //        {
                    //            // get the index of the attribute in `expectedAttributes`
                    //            int index = Array.IndexOf (expectedAttributes, val2.Value);
                    //            // if the index is -1, it means the attribute was not found in `expectedAttributes`

                    //            // get the index of the actual attribute in `expectedAttributes`


                    //            if (index == -1)
                    //            {
                    //                sb.Append ("x:x ");
                    //            }
                    //            else
                    //            {
                    //                sb.Append ($"{index}:{val2.Value} ");
                    //            }
                    //        }
                    //        else
                    //        {
                    //            sb.Append ("x:x ");
                    //        }
                    //    }
                    //    sb.AppendLine ();
                    //}

                    //output.WriteLine ($"Contents:\n{sb}");

                    Assert.Equal (userExpected, colorUsed);

                    return;
                }
            }

            line++;
        }
    }

#pragma warning disable xUnit1013 // Public method should be marked as test
    /// <summary>Asserts that the driver contents match the expected contents, optionally ignoring any trailing whitespace.</summary>
    /// <param name="expectedLook"></param>
    /// <param name="output"></param>
    /// <param name="driver">The IConsoleDriver to use. If null <see cref="Application.Driver"/> will be used.</param>
    /// <param name="ignoreLeadingWhitespace"></param>
    public static void AssertDriverContentsAre (
        string expectedLook,
        ITestOutputHelper output,
        IConsoleDriver driver = null,
        bool ignoreLeadingWhitespace = false
    )
    {
#pragma warning restore xUnit1013 // Public method should be marked as test
        var actualLook = Application.ToString (driver ?? Application.Driver);

        if (string.Equals (expectedLook, actualLook))
        {
            return;
        }

        // get rid of trailing whitespace on each line (and leading/trailing whitespace of start/end of full string)
        expectedLook = TrailingWhiteSpaceRegEx ().Replace (expectedLook, "").Trim ();
        actualLook = TrailingWhiteSpaceRegEx ().Replace (actualLook, "").Trim ();

        if (ignoreLeadingWhitespace)
        {
            expectedLook = LeadingWhitespaceRegEx ().Replace (expectedLook, "").Trim ();
            actualLook = LeadingWhitespaceRegEx ().Replace (actualLook, "").Trim ();
        }

        // standardize line endings for the comparison
        expectedLook = expectedLook.Replace ("\r\n", "\n");
        actualLook = actualLook.Replace ("\r\n", "\n");

        // If test is about to fail show user what things looked like
        if (!string.Equals (expectedLook, actualLook))
        {
            output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
            output?.WriteLine (" But Was:" + Environment.NewLine + actualLook);
        }

        Assert.Equal (expectedLook, actualLook);
    }

    /// <summary>
    ///     Asserts that the driver contents are equal to the provided string.
    /// </summary>
    /// <param name="expectedLook"></param>
    /// <param name="output"></param>
    /// <param name="driver">The IConsoleDriver to use. If null <see cref="Application.Driver"/> will be used.</param>
    /// <returns></returns>
    public static Rectangle AssertDriverContentsWithFrameAre (
        string expectedLook,
        ITestOutputHelper output,
        IConsoleDriver driver = null
    )
    {
        List<List<Rune>> lines = new ();
        var sb = new StringBuilder ();
        driver ??= Application.Driver;
        int x = -1;
        int y = -1;
        int w = -1;
        int h = -1;

        Cell [,] contents = driver.Contents;

        for (var rowIndex = 0; rowIndex < driver.Rows; rowIndex++)
        {
            List<Rune> runes = [];

            for (var colIndex = 0; colIndex < driver.Cols; colIndex++)
            {
                Rune runeAtCurrentLocation = contents [rowIndex, colIndex].Rune;

                if (runeAtCurrentLocation != SpaceRune)
                {
                    if (x == -1)
                    {
                        x = colIndex;
                        y = rowIndex;

                        for (var i = 0; i < colIndex; i++)
                        {
                            runes.InsertRange (i, [SpaceRune]);
                        }
                    }

                    if (runeAtCurrentLocation.GetColumns () > 1)
                    {
                        colIndex++;
                    }

                    if (colIndex + 1 > w)
                    {
                        w = colIndex + 1;
                    }

                    h = rowIndex - y + 1;
                }

                if (x > -1)
                {
                    runes.Add (runeAtCurrentLocation);
                }

                // See Issue #2616
                //foreach (var combMark in contents [r, c].CombiningMarks) {
                //	runes.Add (combMark);
                //}
            }

            if (runes.Count > 0)
            {
                lines.Add (runes);
            }
        }

        // Remove unnecessary empty lines
        if (lines.Count > 0)
        {
            for (int r = lines.Count - 1; r > h - 1; r--)
            {
                lines.RemoveAt (r);
            }
        }

        // Remove trailing whitespace on each line
        foreach (List<Rune> row in lines)
        {
            for (int c = row.Count - 1; c >= 0; c--)
            {
                Rune rune = row [c];

                if (rune != (Rune)' ' || row.Sum (x => x.GetColumns ()) == w)
                {
                    break;
                }

                row.RemoveAt (c);
            }
        }

        // Convert Rune list to string
        for (var r = 0; r < lines.Count; r++)
        {
            var line = StringExtensions.ToString (lines [r]);

            if (r == lines.Count - 1)
            {
                sb.Append (line);
            }
            else
            {
                sb.AppendLine (line);
            }
        }

        var actualLook = sb.ToString ();

        if (string.Equals (expectedLook, actualLook))
        {
            return new (x > -1 ? x : 0, y > -1 ? y : 0, w > -1 ? w : 0, h > -1 ? h : 0);
        }

        // standardize line endings for the comparison
        expectedLook = expectedLook.ReplaceLineEndings ();
        actualLook = actualLook.ReplaceLineEndings ();

        // Remove the first and the last line ending from the expectedLook
        if (expectedLook.StartsWith (Environment.NewLine))
        {
            expectedLook = expectedLook [Environment.NewLine.Length..];
        }

        if (expectedLook.EndsWith (Environment.NewLine))
        {
            expectedLook = expectedLook [..^Environment.NewLine.Length];
        }

        // If test is about to fail show user what things looked like
        if (!string.Equals (expectedLook, actualLook))
        {
            output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
            output?.WriteLine (" But Was:" + Environment.NewLine + actualLook);
        }

        Assert.Equal (expectedLook, actualLook);

        return new (x > -1 ? x : 0, y > -1 ? y : 0, w > -1 ? w : 0, h > -1 ? h : 0);
    }


    /// <summary>
    ///     Verifies the console used all the <paramref name="expectedColors"/> when rendering. If one or more of the
    ///     expected colors are not used then the failure will output both the colors that were found to be used and which of
    ///     your expectations was not met.
    /// </summary>
    /// <param name="driver">if null uses <see cref="Application.Driver"/></param>
    /// <param name="expectedColors"></param>
    internal static void AssertDriverUsedColors (IConsoleDriver driver = null, params Attribute [] expectedColors)
    {
        driver ??= Application.Driver;
        Cell [,] contents = driver.Contents;

        List<Attribute> toFind = expectedColors.ToList ();

        // Contents 3rd column is an Attribute
        HashSet<Attribute> colorsUsed = new ();

        for (var r = 0; r < driver.Rows; r++)
        {
            for (var c = 0; c < driver.Cols; c++)
            {
                Attribute? val = contents [r, c].Attribute;

                if (val.HasValue)
                {
                    colorsUsed.Add (val.Value);

                    Attribute match = toFind.FirstOrDefault (e => e == val);

                    // need to check twice because Attribute is a struct and therefore cannot be null
                    if (toFind.Any (e => e == val))
                    {
                        toFind.Remove (match);
                    }
                }
            }
        }

        if (!toFind.Any ())
        {
            return;
        }

        var sb = new StringBuilder ();
        sb.AppendLine ("The following colors were not used:" + string.Join ("; ", toFind.Select (a => a.ToString ())));
        sb.AppendLine ("Colors used were:" + string.Join ("; ", colorsUsed.Select (a => a.ToString ())));

        throw new (sb.ToString ());
    }


    [GeneratedRegex ("^\\s+", RegexOptions.Multiline)]
    private static partial Regex LeadingWhitespaceRegEx ();


    [GeneratedRegex ("\\s+$", RegexOptions.Multiline)]
    private static partial Regex TrailingWhiteSpaceRegEx ();
}
