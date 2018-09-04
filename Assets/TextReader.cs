using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using UnityEngine;

public class TextReader {
    internal static void Run(USA USA, TextAsset selections, int Maze)
    {
        //Where to start reading
        var index = selections.text.IndexOf(string.Format("[{0}]", Maze));
        var indexS = selections.text.IndexOf(' ', index) + 1;
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
        foreach (string s in selections)
        {
            //This represents the substring we're checking. Since we can't edit s, we increase the substring index instead.
            //This is because the same code looks at the day values after the day value is found.
            var i = 0;
            //The first letter represents either a shape or a day
            var option = s.Substring(i + 1);
            //Check days first, as these values feature a letter for both a day and a shape.
            var compares = new[] { 'N', 'M', 'E', 'W', 'U', 'F', 'S' };
            if (compares.Contains(s[i]))
            {
                //Use these for special debug lines [Used for Hawaii and Alaska]
                if (!USA.flown.Contains(USA.initials[curIndex])) USA.flown.Add(USA.initials[curIndex]);
                var index = Array.IndexOf(compares, s[i]);
                //Ideally GetHashCode should be the enum value for USA.day.
                var day = USA.day.GetHashCode();
                Debug.LogFormat(day + " " + index);
                if (index == day)
                {
                    i++;
                    option = s.Substring(i + 1);
                }
            }
            compares = new[] { 'C', 'Q', 'D', 'Z', 'P', 'T', 'H', 'R' };
            var lists = new List<List<string>> { USA.circle, USA.square, USA.diamond, USA.trap, USA.parallel, USA.triangle, USA.heart, USA.star };
            if (compares.Contains(s[i]))
            {
                var index = Array.IndexOf(compares, s[i]);
                if (!lists[index].Contains(option))
                {
                    lists[index].Add(option);
                    lists[index].Add(USA.initials[curIndex]);
                }
            }
        }
    }

    internal static void Lose(WorldSettings Settings)
    {
        //Veto mazes [Manual Names preferred]
        foreach (string maze in USA.Mazes)
        {
            if (Settings.Veto.Contains(maze)) USA.Mazes.Remove(maze);
        }
        //If you try to veto all of the mazes, you will be set to USA by default.
        if (USA.Mazes.Count == 0) USA.Mazes.Add("USA");
        USA.Read = true;
    }

    //Possible function for RuleSeedModifier
    internal static void RandomizeBorders(USA USA)
    {
        //make a new list of border canditates
        var count = new List<string>[USA.initials.Count];
        foreach (string[] state in USA.assignments)
        {
            int i = USA.assignments.IndexOf(state);
            count[i] = state.ToList();
            foreach (string s in state)
            {
                //Use the compare function from Assign to remove extra characters for the time being.
                var compares = new[] { 'N', 'M', 'E', 'W', 'U', 'F', 'S' };
                var o = s;
                if (compares.Contains(s[0])) o = s.Substring(1);
                o = s.Substring(1);
                //The new list will use the original number system rather than comparing strings like the new system.
                //This is why count is an array - we'll be assigning those numbers to array indicies.
                if (USA.initials.Contains(o)) count[i][Array.IndexOf(state, s)] = USA.initials.IndexOf(o).ToString();
            }
        }
    }
}
