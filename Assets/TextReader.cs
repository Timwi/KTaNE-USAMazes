using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using UnityEngine;

static class TextReader {
    private static readonly char[] comparesD = new char[] { 'N', 'M', 'E', 'W', 'U', 'F', 'S' };
    private static readonly char[] comparesS = new char[] { 'C', 'Q', 'D', 'Z', 'P', 'T', 'H', 'R' };

    internal static List<string> MazesOriginal(USA USA)
    {
        var i = 0;
        USA.MazeNames = new List<string>();
        var maps = new List<string>();
        while (USA.Selections.text.IndexOf(string.Format("[{0}]", i)) != -1)
        {
            var index = USA.Selections.text.IndexOf(']', USA.Selections.text.IndexOf(string.Format("[{0}]", i))) + 1;
            var index2 = USA.Selections.text.IndexOf(' ', index);
            var index3 = USA.Selections.text.IndexOf('\n', index2);
            var word = USA.Selections.text.Substring(index, index2 - index).ToUpperInvariant();
            var word2 = USA.Selections.text.Substring(index2 + 1, index3 - (index2 + 1));
            if (word.Length > 4) word = word.Substring(0, 4);
            maps.Add(word);
            USA.MazeNames.Add(word2);
            i++;
        }
        return maps;
    }

    internal static void Run(USA USA, TextAsset selections, int Maze)
    {
        //Where to start reading
        var index = selections.text.IndexOf(string.Format("[{0}]", Maze));
        var indexS = selections.text.IndexOf('\n', index) + 1;
        //Where to stop reading
        var count = selections.text.IndexOf(string.Format("[{0}]", Maze + 1));
        //If there are no more mazes, assume end of file
        if (count == -1) count = selections.text.Length;
        //Select the text to read
        var text = selections.text.Substring(indexS, count - indexS);
        //Split at every newline
        var Selections = text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        //Initialize some lists, not including in inits as I don't need these to change per day
        USA.initials = new List<string>();
        USA.fullNames = new List<string>();
        USA.flown = new List<string>();
        foreach (string s in Selections)
        {
            var split = s.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            //"States"
            USA.initials.Add(split[0]);
            //"State Names"
            //Add in a replace because I forgot I'm splitting with spaces
            USA.fullNames.Add(split[1].Replace("_", " "));
            Assign(USA, split[2].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries), Array.IndexOf(Selections, s), true);
        }
        //RandomizeBorders(USA, 0);
    }

    internal static void Assign(USA USA)
    {
        USA.isActive = false;
        Debug.LogFormat("[USA Maze {0}] {1}: Day has passed, updating flights. Please wait.", USA._moduleID, DateTime.Now.DayOfWeek.ToString());
        //Empty all of the lists
        USA.Inits();
        //Reassign to them
        foreach (string[] state in USA.assignments)
        {
            Assign(USA, state, USA.assignments.IndexOf(state), false);
        }
        USA.isActive = true;
        Debug.LogFormat("[USA Maze {0}] {1}: Thank you for your patience, flights have been updated.", USA._moduleID, DateTime.Now.DayOfWeek.ToString());
    }

    //This is the method that matches each value together.
    private static void Assign(USA USA, string[] selections, int curIndex, bool write)
    {
        if (write) USA.assignments.Add(selections);
        var lists = new List<List<string>> { USA.circle, USA.square, USA.diamond, USA.trap, USA.parallel, USA.triangle, USA.heart, USA.star };
        foreach (string s in selections)
        {
            var i = 0;
            var compares = comparesD.Concat(comparesS).ToArray();
            var index = Array.IndexOf(comparesD, s[i]);
            if (comparesD.Contains(s[i]) && comparesS.Contains(s[i + 1]))
            {
                //Use these for special debug lines [Used for Hawaii and Alaska]
                if (!USA.flown.Contains(USA.initials[curIndex])) USA.flown.Add(USA.initials[curIndex]);
                //Ideally GetHashCode should be the enum value for USA.day.
                var day = USA.day.GetHashCode();
                if (index == day) i++;
                else continue;
            }
            if (!compares.Contains(s[i])) continue;
            index = Array.IndexOf(comparesS, s[i]);
            var option = s.Substring(i + 1);
            if (!lists[index].Contains(option))
            {
                lists[index].Add(option);
                lists[index].Add(USA.initials[curIndex]);
            }
        }
    }

    internal static void Lose(WorldSettings Settings)
    {
        //Veto mazes [Manual Names preferred]
        foreach (string maze in USA.MazeNames)
        {
            if (Settings.Veto.Contains(maze))
            {
                USA.Mazes.RemoveAt(USA.MazeNames.IndexOf(maze));
                USA.MazeNames.Remove(maze);
            }
        }
        //If you try to veto all of the mazes, you will be set to USA by default.
        if (USA.Mazes.Count == 0)
        {
            USA.Mazes.Add("USA");
            USA.MazeNames.Add("USA");
        }
        USA.Read = true;
    }

    //Possible function for RuleSeedModifier
    internal static void RandomizeBorders(USA USA, int shapeCount)
    {
        //make a new list of border canditates
        var count = new List<string>[USA.initials.Count];
        var borders = new List<char>[USA.initials.Count];
        foreach (string[] state in USA.assignments)
        {
            //Since states are only processed once, some states don't have any information
            //To avoid indexing errors, ignore these values when processing the original information
            if (state[0] == "Y") continue;
            int i = USA.assignments.IndexOf(state);
            count[i] = state.ToList();
            borders[i] = new List<char>();
            foreach (string s in state)
            {
                //Use the compare function from Assign to remove extra characters for the time being.
                var index = 1;
                if (comparesD.Contains(s[0])) index++;
                var o = s.Substring(index);
                //The new list will use the original number system rather than comparing strings like the new system.
                //This is why count is an array - we'll be assigning those numbers to array indicies.
                if (USA.initials.Contains(o))
                {
                    count[i][Array.IndexOf(state, s)] = USA.initials.IndexOf(o).ToString();
                    borders[i].Add(s[index - 1]);
                }
            }
        }
        Debug.LogFormat("[USA Maze #{0}] " + string.Join(", ", count.Select(x => x != null ? x.Count.ToString() : "0").ToArray()), USA._moduleID);
        Debug.LogFormat("[USA Maze #{0}] " + string.Join("\n", count.Select(x => string.Join(", ", x != null ? x.ToArray() : new[] { "" })).ToArray()), USA._moduleID);
        Debug.LogFormat("[USA Maze #{0}] " + string.Join(", ", borders.Select(x => string.Join("", x != null ? x.Select(y => y.ToString()).ToArray() : new[] { "" })).ToArray()), USA._moduleID);

        var value = count.Count();
        var uses = new int[shapeCount];
        var pool = new List<List<int>[]>();
        var array = new int[value];
        var pass = false;
        for (int i = 0; i < count.Count(); i++)
        {
            if (count[i] == null) continue;
            for (int j = 0; j < count[i].Count; j++)
            {
                //while (array.All(x => x != count[Array.IndexOf(array, x)].Count()))
                //{

                //}
            }
        }
    }

    //Determine at least one safe state per state that can access all other states
    //If there is more than one number, then that means one or the other can be considered required.
    //if the number provided is higher than the available number of lists, that means more than one state is required
    private static bool SecureStates()
    {
        return true;
    }
}
