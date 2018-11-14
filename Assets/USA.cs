using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;

class USA : MonoBehaviour
{
    private WorldSettings Settings = new WorldSettings();
    private static int _moduleIDCounter = 1;
    private static List<int> chosenMazes = new List<int>();
    internal int _moduleID;
    public KMBombInfo Info;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable[] Shapes;
    public TextMesh visCurrent, visMaze, visDestination, visReset;
    internal bool isActive = false;
    private int origin, destination, current, maze;
    internal List<string> circle, square, diamond, trap, parallel, triangle, heart, star,
        initials, fullNames, flown;
    internal List<string[]> assignments = new List<string[]>();
    internal static List<string> Mazes, MazeNames;
    internal static bool Read;
    public TextAsset Selections;
    private readonly string[] shapeNames = new[] { "Circle", "Square", "Diamond", "Trapezoid", "Parallelogram", "Triangle", "Heart", "Star" };
    internal static DayOfWeek day = DateTime.Now.DayOfWeek;
    public Mode Mode;
    public bool auto;

    internal void Inits()
    {
        circle = new List<string>();
        square = new List<string>();
        diamond = new List<string>();
        trap = new List<string>();
        parallel = new List<string>();
        triangle = new List<string>();
        heart = new List<string>();
        star = new List<string>();
    }

    void Start()
    {
        _moduleID = _moduleIDCounter++;
        Inits();
        ModConfig<WorldSettings> modConfig = new ModConfig<WorldSettings>("WorldSettings");
        Settings = modConfig.Settings;
        visCurrent.color = Color.black;
        visMaze.color = Color.black;
        visDestination.color = Color.black;
        Shapes.Last().gameObject.SetActive(false);
        if (!Read)
        {
            //Read what mazes are available [this should only need to be done once, as it is static]
            Mazes = TextReader.MazesOriginal(this);
            //Veto mazes based on settings, which should also only need to be done once.
            TextReader.Lose(Settings);
            //To allow each maze to be generated at least once, keep track of how many instances of each maze there are.
            foreach (string value in Mazes)
            {
                chosenMazes.Add(0);
            }
        }
        //For testing, since we can't access settings in Unity
#if (UNITY_EDITOR)
        Settings.Mode = Mode;
        Settings.AutoReset = auto;
#else
        //Mode and auto are used for TP compatibility, rather than using Settings.Mode and Settings.AutoReset throughout the code.
        //This is so they can be set during the instance of TP without changing the setting.
        Mode = Settings.Mode;
        auto = Settings.AutoReset;
#endif
        //See if there are more instances of one maze over the others
        var h = chosenMazes.Max();
        var c = chosenMazes.Min();
        //Add the indices of the remaining values to a new list, which will go into random.range
        //Then the value of chosenMazes at the value of ints will be passed into the rest of the code.
        var ints = new List<int>();
        if (h == c) ints.AddRange(chosenMazes.Select(x => chosenMazes.IndexOf(x)));
        else
        {
            var e = chosenMazes.Where(x => x != h).Select(x => chosenMazes.IndexOf(x));
            ints.AddRange(e);
        }
        //Randomize the values in ints, return the value that will be the maze used
        maze = ints[UnityEngine.Random.Range(0, ints.Count)];
        //Update the list to let it know a maze has been selected
        chosenMazes[maze]++;
        Debug.LogFormat("[USA Maze #{0}] Chosen maze: {1}", _moduleID, MazeNames[maze]);
        //Grab the information from the maze chosen.
        TextReader.Run(this, Selections, maze);
        origin = UnityEngine.Random.Range(0, initials.Count);
        destination = origin;
        while (destination == origin) destination = UnityEngine.Random.Range(0, initials.Count);
        current = origin;
        Module.OnActivate += delegate () { Activate(); };
    }

    void Activate()
    {
        visCurrent.text = initials[current];
        visMaze.text = Mazes[maze];
        visDestination.text = initials[destination];
        for (int i = 0; i < Shapes.Count(); i++)
        {
            int j = i;
            Shapes[i].OnInteract = ButtonHandler(j);
        }
        visCurrent.color = Color.white;
        visMaze.color = Color.white;
        visDestination.color = Color.white;
        if (Mode == Mode.Memory) Shapes.Last().gameObject.SetActive(true);
        visReset.color = Color.white;
        isActive = true;
        Debug.LogFormat("[USA Maze #{0}] {3}: Departing {1} to {2}.", _moduleID, fullNames[origin], fullNames[destination], day.ToString());
    }

    KMSelectable.OnInteractHandler ButtonHandler(int j)
    {
        return delegate
        {
            if (j == 8)
            {
                current = origin;
                Debug.LogFormat("[USA Maze #{0}] Returned to {1} - {2}", _moduleID, initials[origin], fullNames[origin]);
                return false;
            }
            var d = current;
            Shapes[j].AddInteractionPunch(.3f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if (!isActive) return false;
            var lists = new[] { circle, square, diamond, trap, parallel, triangle, heart, star };
            if (lists[j].Contains(initials[current]))
            {
                var i = lists[j].IndexOf(initials[current]);
                var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                current = initials.IndexOf(lists[j][ni]);
                if (Mode == Mode.Standard) visCurrent.text = lists[j][ni];
            }
            if (current != d && (flown.Contains(initials[d]) || flown.Contains(initials[current]))) Debug.LogFormat("[USA Maze #{0}] Taking flight {1} from {2} - {3} to {4} - {5}!", _moduleID, shapeNames[j], initials[d], fullNames[d], initials[current], fullNames[current]);
            else if (current != d) Debug.LogFormat("[USA Maze #{0}] Border {1} passed, next stop: {2} - {3}!", _moduleID, shapeNames[j], initials[current], fullNames[current]);
            else
            {
                var text = String.Format("[USA Maze #{0}] Halted at border {1}, Strike!", _moduleID, shapeNames[j]);
                if (Mode.Equals(Mode.Memory) && auto)
                {
                    current = origin;
                    text = String.Format("[USA Maze #{0}] Halted at border {1}, Strike! Returned to {2} - {3}", _moduleID, shapeNames[j], initials[current], fullNames[current]);
                }
                Debug.LogFormat(text);
                Module.HandleStrike();
            }
            if (current == destination)
            {
                Debug.LogFormat("[USA Maze #{0}] Arrived at destination!", _moduleID);
                visCurrent.text = initials[current];
                Module.HandlePass();
                isActive = false;
            }
            return false;
        };
    }

    private void Update()
    {
        if (day != DateTime.Now.DayOfWeek)
        {
            day = DateTime.Now.DayOfWeek;
            TextReader.Assign(this);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Submit path using “!{0} press 01234567” or “!{0} press c q d z p t h r” [Circle, sQuare, Diamond, trapeZoid, Parallelogram, Triangle, Heart, staR]. Toggle modes with !{0} [toggle/switch] [standard/memory]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        List<KMSelectable> s = new List<KMSelectable>();
        var match = "^(full|)(toggle|switch)( standard| memory|)";
        if (Regex.Match(command, match).Success) yield return "sendtochat " + ToggleMode(command);
        if (!command.StartsWith("press ")) yield break;
        yield return null;
        if (command.Contains("reset") && Mode == Mode.Memory) yield return new[] { Shapes.Last() };
        command = command.Replace("press ", "");
        var chars = new[] { 'c', 'q', 'd', 'z', 'p', 't', 'h', 'r' };
        auto = true;
        foreach (char c in command)
        {
            yield return null;
            if (c == ' ') continue;
            int index;
            var b = int.TryParse(c.ToString(), out index);
            if (chars.Contains(c)) index = Array.IndexOf(chars, c);
            if (index < 0 || index > 7) yield break;
            s.Add(Shapes[index]);
        }
        auto = Settings.AutoReset;
        yield return s.ToArray();
    }

    private string ToggleMode(string command)
    {
        var c = command.Contains("full");
        var m = command.Contains("memory");
        var s = command.Contains("standard");
        if (c)
        {
            if (command.Contains(Settings.Mode.ToString())) return "Setting not changed, already set.";
            else if (m) Settings.Mode = Mode.Memory;
            else if (s) Settings.Mode = Mode.Standard;
            else Settings.Mode = Settings.Mode.Equals(Mode.Standard) ? Mode.Memory : Mode.Standard;
        }
        if (command.Contains(Mode.ToString())) return "Mode already enabled!";
        else if (m) Mode = Mode.Memory;
        else if (s) Mode = Mode.Standard;
        else Mode = Mode.Equals(Mode.Standard) ? Mode.Memory : Mode.Standard;

        switch (Mode)
        {
            case Mode.Standard:
                Shapes.Last().gameObject.SetActive(false);
                visCurrent.text = initials[current];
                return "Mode for current module changed to Standard mode";
            case Mode.Memory:
                Shapes.Last().gameObject.SetActive(true);
                visReset.color = Color.white;
                visCurrent.text = initials[origin];
                current = origin;
                return "Module reset! Mode for current module changed to Memory mode";
        }
        return "";
    }
}

public enum Mode
{
    Standard,
    Memory
}

class WorldSettings
{
#pragma warning disable 414
    private string HowToUseMode = "To use standard mode, use Standard or 0. For memory mode, use Memory or 1.";
    public Mode Mode = Mode.Standard;
    private string AutoResetOption = "Have the module automatically reset on strikes in Memory Mode.";
    public bool AutoReset = true;
    private string VetoInfo = "Mazes will typically take the full name featured in the manual.";
    public List<string> Veto = new List<string> { "If you would like to keep certain mazes from spawning, enter them here." };
#pragma warning restore 414
}