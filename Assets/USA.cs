using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UsaMaze;
using Rnd = UnityEngine.Random;

class USA : MonoBehaviour
{
    public KMBombInfo Info;
    public KMBombModule Module;
    public KMSelectable ModuleSelectable;
    public KMAudio Audio;
    public KMSelectable[] Shapes;
    public KMSelectable Reset;
    public TextMesh visCurrent, visMaze, visDestination, visReset;
    public KMRuleSeedable RuleSeedable;

    public Mode Mode;
    public bool AutoReset;

    private WorldSettings Settings = new WorldSettings();
    internal int _moduleID;
    private static int _moduleIDCounter = 1;
    private bool _isActive = false;
    private string _origin, _destination, _current;

    // Rules
    private static readonly Shape[] _shapes = { Shape.Circle, Shape.Square, Shape.Diamond, Shape.Trapezoid, Shape.Parallelogram, Shape.Triangle, Shape.Heart, Shape.Star };
    private static readonly string[] _states = "AL,AR,AZ,CA,CO,CT,DE,FL,GA,IA,ID,IL,IN,KS,KY,LA,MA,MD,ME,MI,MN,MO,MS,MT,NC,ND,NE,NH,NJ,NM,NV,NY,OH,OK,OR,PA,RI,SC,SD,TN,TX,UT,VA,VT,WA,WI,WV,WY".Split(',');
    private static readonly string[] _stateBorders = "ID-NV,NV-OR,CO-KS,CO-OK,CO-NM,AZ-UT,NM-TX,NM-OK,IA-NE,MO-NE,KS-MO,MO-OK,AR-OK,MN-WI,IA-WI,IL-WI,IA-IL,IL-MO,AR-TN,AR-MS,LA-MS,IL-KY,IN-KY,KY-OH,KY-WV,VA-WV,KY-MO,KY-TN,TN-VA,NC-VA,FL-GA,AL-FL,NC-SC,GA-SC,DE-MD,DE-PA,NJ-PA,DE-NJ,NJ-NY,NY-PA,MA-NH,MA-VT,MA-NY,CT-MA,MA-RI,AL-GA,MS-TN,AL-TN,GA-TN,NC-TN,VT-NY,MD-WV,MD-VA,OH-WV,IN-OH,IL-IN,IA-MO,MI-WI,AZ-CA,CA-NV,CA-OR,ID-MT,MT-SD,SD-WY,NE-SD,IA-SD,MN-ND,MN-SD,IA-MN,ID-OR,OR-WA,KS-OK,AL-MS,AR-LA,MT-WY,ID-WY,UT-WY,CO-WY,CO-NE,KS-NE,ID-WA,KY-VA,ME-NH,NH-VT,CT-NY,AR-MO,IN-MI,MI-OH,GA-NC,MO-TN,AZ-NM,CO-UT,MD-PA,PA-WV,OH-PA,CT-RI,ID-UT,NV-UT,AZ-NV,NE-WY,LA-TX,AR-TX,OK-TX,ND-SD,MT-ND".Split(',');
    private static readonly string[] _outStates = "AL,AZ,CA,CT,DE,FL,GA,ID,LA,MA,MD,ME,MI,MN,MS,MT,NC,ND,NH,NJ,NM,NY,OH,OR,PA,RI,SC,TX,VA,VT,WA,WI".Split(',');
    private static readonly string[] _outlying = "AK,HI".Split(',');

    private static readonly Dictionary<string, string> _stateNames = new Dictionary<string, string>
    {
        { "AK", "Alaska" },
        { "AL", "Alabama" },
        { "AR", "Arkansas" },
        { "AZ", "Arizona" },
        { "CA", "California" },
        { "CO", "Colorado" },
        { "CT", "Connecticut" },
        { "DE", "Delaware" },
        { "FL", "Florida" },
        { "GA", "Georgia" },
        { "HI", "Hawaii" },
        { "IA", "Iowa" },
        { "ID", "Idaho" },
        { "IL", "Illinois" },
        { "IN", "Indiana" },
        { "KS", "Kansas" },
        { "KY", "Kentucky" },
        { "LA", "Louisiana" },
        { "MA", "Massachusetts" },
        { "MD", "Maryland" },
        { "ME", "Maine" },
        { "MI", "Michigan" },
        { "MN", "Minnesota" },
        { "MO", "Missouri" },
        { "MS", "Mississippi" },
        { "MT", "Montana" },
        { "NC", "North Carolina" },
        { "ND", "North Dakota" },
        { "NE", "Nebraska" },
        { "NH", "New Hampshire" },
        { "NJ", "New Jersey" },
        { "NM", "New Mexico" },
        { "NV", "Nevada" },
        { "NY", "New York" },
        { "OH", "Ohio" },
        { "OK", "Oklahoma" },
        { "OR", "Oregon" },
        { "PA", "Pennsylvania" },
        { "RI", "Rhode Island" },
        { "SC", "South Carolina" },
        { "SD", "South Dakota" },
        { "TN", "Tennessee" },
        { "TX", "Texas" },
        { "UT", "Utah" },
        { "VA", "Virginia" },
        { "VT", "Vermont" },
        { "WA", "Washington" },
        { "WI", "Wisconsin" },
        { "WV", "West Virginia" },
        { "WY", "Wyoming" }
    };

    private Dictionary<string, Shape> _openBorders;
    private Dictionary<string, Shape> _outShapes;
    private Shape[][] _outOpen; // First index is same as in ‘outlying’; second is day of week

    static readonly Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "WorldSettings.json" },
            { "Name", "USA Maze Settings" },
            { "Listings", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Key", "Mode" },
                    { "Description", "“Memory mode” = user has to remember their starting location and keep track of their movements. “Standard mode” = location is shown at all times." },
                    { "Type", "Dropdown" },
                    { "DropdownItems", new List<object> { "Standard", "Memory" } }
                },
                new Dictionary<string, object>
                {
                    { "Key", "AutoReset" },
                    { "Text", "Auto Reset" },
                    { "Description", "Have the module reset on strikes in Memory Mode." }
                }
            }}
        }
    };

    void Start()
    {
        _moduleID = _moduleIDCounter++;
        var modConfig = new ModConfig<WorldSettings>("WorldSettings");
        Settings = modConfig.Settings;
        visCurrent.color = Color.black;
        visMaze.color = Color.black;
        visDestination.color = Color.black;
        Reset.gameObject.SetActive(false);

        //For testing, since we can't access settings in Unity
#if (UNITY_EDITOR)
        Settings.Mode = Mode;
        Settings.AutoReset = AutoReset;
#else
        //Mode and AutoReset are used for TP compatibility, rather than using Settings.Mode and Settings.AutoReset throughout the code.
        //This is so they can be set during the instance of TP without changing the setting.
        Mode = Settings.Mode;
        AutoReset = Settings.AutoReset;
#endif


        // RULE SEED STARTS HERE
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat(@"[USA Maze #{0}] Using rule seed: {1}.", _moduleID, rnd.Seed);
        if (rnd.Seed == 1)
        {
            _outShapes = new Dictionary<string, Shape>
            {
                { "WA", Shape.Circle },
                { "CA", Shape.Square },
                { "SC", Shape.Trapezoid },
                { "DE", Shape.Parallelogram },
                { "RI", Shape.Diamond },
                { "ME", Shape.Triangle },
                { "ND", Shape.Heart },
                { "TX", Shape.Star }
            };

            _openBorders = new Dictionary<string, Shape>
            {
                { "AL-FL", Shape.Circle },
                { "AL-MS", Shape.Trapezoid },
                { "AR-LA", Shape.Circle },
                { "AR-MS", Shape.Square },
                { "AR-TN", Shape.Trapezoid },
                { "AR-TX", Shape.Triangle },
                { "AZ-NV", Shape.Parallelogram },
                { "CA-OR", Shape.Circle },
                { "CO-KS", Shape.Diamond },
                { "CO-OK", Shape.Parallelogram },
                { "CT-MA", Shape.Heart },
                { "DE-MD", Shape.Diamond },
                { "FL-GA", Shape.Square },
                { "GA-SC", Shape.Diamond },
                { "IA-IL", Shape.Star },
                { "IA-MN", Shape.Parallelogram },
                { "IA-SD", Shape.Circle },
                { "ID-OR", Shape.Diamond },
                { "ID-UT", Shape.Heart },
                { "ID-WA", Shape.Square },
                { "ID-WY", Shape.Triangle },
                { "IL-IN", Shape.Diamond },
                { "IL-MO", Shape.Square },
                { "IL-WI", Shape.Heart },
                { "IN-MI", Shape.Trapezoid },
                { "KS-OK", Shape.Trapezoid },
                { "KY-OH", Shape.Triangle },
                { "KY-VA", Shape.Trapezoid },
                { "MA-NY", Shape.Trapezoid },
                { "MA-RI", Shape.Star },
                { "MA-VT", Shape.Triangle },
                { "MD-PA", Shape.Square },
                { "ME-NH", Shape.Diamond },
                { "MI-OH", Shape.Parallelogram },
                { "MI-WI", Shape.Star },
                { "MN-WI", Shape.Triangle },
                { "MO-NE", Shape.Star },
                { "MO-OK", Shape.Heart },
                { "MO-TN", Shape.Parallelogram },
                { "MS-TN", Shape.Diamond },
                { "MT-SD", Shape.Square },
                { "MT-WY", Shape.Circle },
                { "NC-VA", Shape.Circle },
                { "ND-SD", Shape.Diamond },
                { "NE-SD", Shape.Trapezoid },
                { "NH-VT", Shape.Square },
                { "NJ-PA", Shape.Trapezoid },
                { "NM-OK", Shape.Square },
                { "NM-TX", Shape.Circle },
                { "NV-OR", Shape.Trapezoid },
                { "NY-PA", Shape.Circle },
                { "OH-WV", Shape.Heart },
                { "PA-WV", Shape.Star },
                { "TN-VA", Shape.Star },
                { "UT-WY", Shape.Star }
            };

            _outOpen = new Shape[][]
            {
                new Shape[] { Shape.Circle, Shape.Square, Shape.Trapezoid, Shape.Parallelogram, Shape.Diamond, Shape.Triangle, Shape.Heart },
                new Shape[] { Shape.Square, Shape.Circle, Shape.Triangle, Shape.Diamond, Shape.Parallelogram, Shape.Trapezoid, Shape.Star }
            };
        }
        else
        {
            _outShapes = new Dictionary<string, Shape>();
            _openBorders = new Dictionary<string, Shape>();

            tryAgain:
            var usedShapesPerState = new Dictionary<string, List<Shape>>();

            // STEP 1: Decide which states have an “out” (connection to Alaska/Hawaii) and assign them shapes
            var availableOutStates = rnd.ShuffleFisherYates(_outStates.ToArray());
            for (var i = 0; i < 8; i++)
            {
                _outShapes[availableOutStates[i]] = _shapes[i];
                addSafe(usedShapesPerState, availableOutStates[i], _shapes[i]);
            }

            // STEP 2: Generate the main maze and assign a shape to each opened border
            _openBorders = new Dictionary<string, Shape>();
            var openBorder = new Func<string, bool>(borderId =>
            {
                var m = Regex.Match(borderId, "^(..)-(..)$");
                if (!m.Success)
                {
                    Debug.LogFormat(@"[USA Maze #{0}] There is a bug in the rule seed generator. Please contact Timwi about this.", _moduleID);
                    throw new InvalidOperationException(string.Format(@"There is a bug in the rule seed generator: “{0}” is not a valid border.", borderId));
                }
                var state = m.Groups[1].Value;
                var otherState = m.Groups[2].Value;
                var availableShapes = _shapes.ToList();
                foreach (var st in new[] { state, otherState })
                    if (usedShapesPerState.ContainsKey(st))
                        foreach (var shape in usedShapesPerState[st])
                            availableShapes.Remove(shape);
                if (availableShapes.Count == 0)
                    return false;
                var chosenShape = availableShapes[rnd.Next(0, availableShapes.Count)];
                _openBorders[borderId] = chosenShape;
                foreach (var st in new[] { state, otherState })
                    addSafe(usedShapesPerState, st, chosenShape);
                return true;
            });

            var startState = _states[rnd.Next(0, _states.Length)];
            var visited = new HashSet<string> { startState };
            var queue = new Queue<string>();
            queue.Enqueue(startState);
            while (queue.Count > 0)
            {
                var state = queue.Dequeue();
                foreach (var otherState in rnd.ShuffleFisherYates(_states.ToArray()))
                    if (!visited.Contains(otherState))
                        foreach (var borderId in new[] { otherState + "-" + state, state + "-" + otherState })
                            if (_stateBorders.Contains(borderId))
                            {
                                queue.Enqueue(otherState);
                                visited.Add(otherState);
                                if (!openBorder(borderId))
                                    goto tryAgain;
                            }
            }

            // STEP 3: Open three more random borders
            var allBorders = rnd.ShuffleFisherYates(_stateBorders.ToList());
            var ix = 0;
            var extraBorders = 0;
            while (extraBorders < 3)
            {
                if (ix >= allBorders.Count)
                {
                    Debug.LogFormat(@"<USA Maze #{0}> Rule seed generator: ran into the case with no extra borders. Retrying");
                    goto tryAgain;
                }
                if (!_openBorders.ContainsKey(allBorders[ix]) && openBorder(allBorders[ix]))
                    extraBorders++;
                ix++;
            }

            // Day-of-week table
            // To ensure that every row and every column has unique symbols,
            // let’s use the same trick that I used in Elder Futhark and just use the top-left corner
            var rowShuffle = rnd.ShuffleFisherYates(Enumerable.Range(0, 8).ToArray());
            var columnShuffle = rnd.ShuffleFisherYates(Enumerable.Range(0, 8).ToArray());
            _outOpen = new Shape[_outlying.Length][];
            for (var outIx = 0; outIx < _outlying.Length; outIx++)
            {
                _outOpen[outIx] = new Shape[7];
                for (var dow = 0; dow < 7; dow++)
                    _outOpen[outIx][dow] = _shapes[(columnShuffle[dow] + rowShuffle[outIx]) % 8];
            }

            Debug.LogFormat("<USA Maze #{0}> Open borders: {1}", _moduleID, _openBorders.Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value)).Join("; "));
            Debug.LogFormat("<USA Maze #{0}> Out-connections: {1}", _moduleID, _outShapes.Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value)).Join("; "));
            for (var outIx = 0; outIx < _outlying.Length; outIx++)
                Debug.LogFormat("<USA Maze #{0}> Out times for {1}: {2}", _moduleID, _outlying[outIx], _outOpen[outIx].Join(", "));
        }
        // END RULE SEED

        var allStates = _states.Concat(_outlying).ToList();
        var originIx = Rnd.Range(0, allStates.Count);
        _origin = allStates[originIx];
        allStates.RemoveAt(originIx);
        _destination = allStates[Rnd.Range(0, allStates.Count)];
        _current = _origin;

        for (int i = 0; i < Shapes.Length; i++)
            Shapes[i].OnInteract = ButtonHandler(i);

        Reset.OnInteract = delegate
        {
            Reset.AddInteractionPunch(.3f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Reset.transform);
            if (!_isActive || Mode != Mode.Memory)
                return false;
            setCurrentState(_origin, alwaysShow: true);
            Debug.LogFormat("[USA Maze #{0}] Returned to {1} ({2}).", _moduleID, _origin, _stateNames[_origin]);
            return false;
        };

        Module.OnActivate += delegate () { Activate(); };
    }

    private void addSafe<TKey, TValue>(Dictionary<TKey, List<TValue>> dic, TKey key, TValue value)
    {
        List<TValue> list;
        if (!dic.TryGetValue(key, out list))
            dic[key] = list = new List<TValue>();
        list.Add(value);
    }

    private void setCurrentState(string state, bool alwaysShow = false)
    {
        _current = state;
        visCurrent.text = alwaysShow || Mode == Mode.Standard ? state : "";
    }

    DayOfWeek getDow()
    {
        if (Info.QueryWidgets("day", null).Count == 1)
            return (DayOfWeek) Enum.Parse(typeof(DayOfWeek), Info.QueryWidgets("day", null).Select(x => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(x)).First()["day"]);
        return DateTime.Now.DayOfWeek;
    }

    void Activate()
    {
        visMaze.text = "USA";
        visCurrent.text = _current;
        visDestination.text = _destination;

        visCurrent.color = Color.white;
        visMaze.color = Color.white;
        visDestination.color = Color.white;

        if (Mode == Mode.Memory)
            Reset.gameObject.SetActive(true);
        else
        {
            ModuleSelectable.Children[3] = null;
            ModuleSelectable.UpdateChildren();
        }
        visReset.color = Color.white;
        _isActive = true;
        Debug.LogFormat("[USA Maze #{0}] Departing {1} to {2}.", _moduleID, _stateNames[_origin], _stateNames[_destination]);
    }

    KMSelectable.OnInteractHandler ButtonHandler(int i)
    {
        var shape = _shapes[i];
        return delegate
        {
            Shapes[i].AddInteractionPunch(.3f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Shapes[i].transform);
            if (!_isActive)
                return false;

            Shape s;

            // Are we in an outlying territory? Then we need to fly back on an open flight
            var dow = getDow();
            var outIx = Array.IndexOf(_outlying, _current);
            if (outIx != -1)
            {
                var stateIx = _states.IndexOf(st => _outShapes.TryGetValue(st, out s) && s == shape);
                if (stateIx == -1)
                {
                    // This should never happen because the rule generator generates an out-connection for every shape
                    Debug.LogFormat("[USA Maze #{0}] {1} - there is no state with an out-connection with that shape. Strike!", _moduleID, shape);
                    goto strike;
                }
                if (_outOpen[outIx][(int) dow] != shape)
                {
                    Debug.LogFormat("[USA Maze #{0}] {1} - this connection is not open on this day ({2}). Strike!", _moduleID, shape, dow);
                    goto strike;
                }
                Debug.LogFormat("[USA Maze #{0}] {1} - flew to {2} ({3}).", _moduleID, shape, _states[stateIx], _stateNames[_states[stateIx]]);
                setCurrentState(_states[stateIx]);
                goto traveled;
            }

            // Does the current state have this shape as an out-connection?
            if (_outShapes.TryGetValue(_current, out s) && s == shape)
            {
                // Is this out-connection open on the current day?
                for (var outlIx = 0; outlIx < _outlying.Length; outlIx++)
                    if (_outOpen[outlIx][(int) dow] == shape)
                    {
                        Debug.LogFormat("[USA Maze #{0}] {1} - flew to {2} ({3}).", _moduleID, shape, _outlying[outlIx], _stateNames[_outlying[outlIx]]);
                        setCurrentState(_outlying[outlIx]);
                        goto traveled;
                    }

                Debug.LogFormat("[USA Maze #{0}] {1} - tried to fly out but this connection is not open on this day ({2}). Strike!", _moduleID, shape, dow);
                goto strike;
            }

            // Does this shape connect the current state to another state?
            foreach (var otherState in _states)
                if ((_openBorders.TryGetValue(_current + "-" + otherState, out s) && s == shape) ||
                    (_openBorders.TryGetValue(otherState + "-" + _current, out s) && s == shape))
                {
                    Debug.LogFormat("[USA Maze #{0}] {1} - traveled to {2} ({3}).", _moduleID, shape, otherState, _stateNames[otherState]);
                    setCurrentState(otherState);
                    goto traveled;
                }

            Debug.LogFormat("[USA Maze #{0}] {1} - the current state has no connection with that symbol. Strike!", _moduleID, shape);
            goto strike;

            strike:
            Module.HandleStrike();
            if (Mode == Mode.Memory && AutoReset)
            {
                setCurrentState(_origin, alwaysShow: true);
                Debug.LogFormat("[USA Maze #{0}] Returned to {1} ({2}).", _moduleID, _origin, _stateNames[_origin]);
            }
            return false;

            traveled:
            if (_current == _destination)
            {
                Debug.LogFormat("[USA Maze #{0}] Arrived at destination!", _moduleID);
                Module.HandlePass();
                _isActive = false;
            }
            return false;
        };
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press 01234567 | !{0} press c q d z p t h r [Circle, sQuare, Diamond, trapeZoid, Parallelogram, Triangle, Heart, staR] | !{0} mode (standard/memory/memoryreset) | !{0} reset [memory mode only]";
#pragma warning restore 414

    private IEnumerable<KMSelectable> ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*(?:press\s+)?([0-7cqdzpthr ,;]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var btns = new List<KMSelectable>();
            foreach (var ch in m.Groups[1].Value)
                switch (ch)
                {
                    case '0': case 'c': case 'C': btns.Add(Shapes[0]); break;
                    case '1': case 'q': case 'Q': btns.Add(Shapes[1]); break;
                    case '2': case 'd': case 'D': btns.Add(Shapes[2]); break;
                    case '3': case 'z': case 'Z': btns.Add(Shapes[3]); break;
                    case '4': case 'p': case 'P': btns.Add(Shapes[4]); break;
                    case '5': case 't': case 'T': btns.Add(Shapes[5]); break;
                    case '6': case 'h': case 'H': btns.Add(Shapes[6]); break;
                    case '7': case 'r': case 'R': btns.Add(Shapes[7]); break;
                }
            return btns;
        }
        else if (Mode == Mode.Memory && Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new[] { Reset };
        else if ((m = Regex.Match(command, @"^\s*(?:mode|set *mode|switch|toggle)\s+(standard|memory|memory *reset)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            if (m.Groups[1].Value.Equals("standard", StringComparison.InvariantCultureIgnoreCase))
                Mode = Mode.Standard;
            else
            {
                Mode = Mode.Memory;
                AutoReset = !m.Groups[1].Value.Equals("memory", StringComparison.InvariantCultureIgnoreCase);
            }
            setCurrentState(_origin);
            return new KMSelectable[0];
        }
        return null;
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
    private string HowToUseAutoReset = "Have the module automatically reset on strikes in Memory Mode.";
    public bool AutoReset = true;
    private string HowToVeto = "Mazes will typically take the full name featured in the manual.";
    public List<string> Veto = new List<string> { "If you would like to keep certain mazes from spawning, enter them here." };
#pragma warning restore 414
}