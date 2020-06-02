using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using WorldMazes;
using Rnd = UnityEngine.Random;

abstract class WorldMazeBase : MonoBehaviour
{
#pragma warning disable 649    // Stop compiler warning: “Field is not assigned to” (they are assigned in Unity)
    public KMBombModule Module;
    public WorldMazeScaffold ScaffoldPrefab;
#pragma warning restore 649

    public Mode Mode;
    public bool AutoReset;

    protected static readonly Shape[] _shapes = { Shape.Circle, Shape.Square, Shape.Diamond, Shape.Trapezoid, Shape.Parallelogram, Shape.Triangle, Shape.Heart, Shape.Star };

    protected abstract string MazeID { get; }
    protected abstract void GenerateRules(MonoRandom rnd);
    protected abstract List<string> GetAllStates();
    protected abstract string GetStateFullName(string code);
    protected abstract string GetStateDisplayName(string code);

    protected WorldMazeScaffold _scaffold;
    private static readonly Dictionary<string, int> _moduleIDCounters = new Dictionary<string, int>();
    protected int _moduleID;
    private bool _isActive = false;
    private string _originState;
    private string _destinationState;
    protected string _currentState { get; private set; }
    private WorldSettings Settings = new WorldSettings();

    void Awake()
    {
        _scaffold = Instantiate(ScaffoldPrefab, transform);
        var moduleSelectable = GetComponent<KMSelectable>();
        moduleSelectable.Children = new KMSelectable[12];
        for (var i = 0; i < _scaffold.Shapes.Length; i++)
        {
            _scaffold.Shapes[i].Parent = moduleSelectable;
            moduleSelectable.Children[i + 4] = _scaffold.Shapes[i];
        }
        var modConfig = new ModConfig<WorldSettings>("WorldSettings");
        Settings = modConfig.Settings;
        modConfig.Settings = Settings;
        Mode = Settings.Mode;
        AutoReset = Settings.AutoReset;

        _scaffold.Reset.Parent = moduleSelectable;
        _scaffold.Reset.gameObject.SetActive(Mode == Mode.Memory);
        moduleSelectable.Children[3] = Mode == Mode.Memory ? _scaffold.Reset : null;
        moduleSelectable.ChildRowLength = 4;
        moduleSelectable.UpdateChildren();

        _moduleIDCounters.IncSafe(Module.ModuleType);
        _moduleID = _moduleIDCounters[Module.ModuleType];
        _scaffold.VisCurrent.color = Color.black;
        _scaffold.VisMaze.color = Color.black;
        _scaffold.VisDestination.color = Color.black;
        _scaffold.VisReset.color = Color.black;

        if (_scaffold.RuleSeedable != null)
        {
            var rnd = _scaffold.RuleSeedable.GetRNG();
            Log("Using rule seed: {0}.", rnd.Seed);
            GenerateRules(rnd);
        }

        var allStates = GetAllStates();
        var originIx = Rnd.Range(0, allStates.Count);
        _originState = allStates[originIx];
        allStates.RemoveAt(originIx);
        _destinationState = allStates[Rnd.Range(0, allStates.Count)];
        _currentState = _originState;

        for (int i = 0; i < _scaffold.Shapes.Length; i++)
            _scaffold.Shapes[i].OnInteract = ButtonHandler(i);

        _scaffold.Reset.OnInteract = delegate
        {
            _scaffold.Reset.AddInteractionPunch(.3f);
            _scaffold.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _scaffold.Reset.transform);
            if (!_isActive || Mode != Mode.Memory)
                return false;
            setCurrentState(_originState, alwaysShow: true);
            Log("Returned to {1} ({2}).", _moduleID, GetStateFullName(_originState), GetStateDisplayName(_originState));
            return false;
        };

        Module.OnActivate += delegate () { Activate(); };
    }

    private void setCurrentState(string code, bool alwaysShow = false)
    {
        _currentState = code;
        _scaffold.VisCurrent.text = alwaysShow || Mode == Mode.Standard ? GetStateDisplayName(code) : "";
    }

    void Activate()
    {
        _scaffold.VisMaze.text = MazeID;
        _scaffold.VisCurrent.text = GetStateDisplayName(_currentState);
        _scaffold.VisDestination.text = _destinationState;

        _scaffold.VisCurrent.color = Color.white;
        _scaffold.VisMaze.color = Color.white;
        _scaffold.VisDestination.color = Color.white;
        _scaffold.VisReset.color = Color.white;

        _isActive = true;
        Log("Departing {0} ({1}) to {2} ({3}).", GetStateFullName(_originState), GetStateDisplayName(_originState), GetStateFullName(_destinationState), GetStateDisplayName(_destinationState));
    }

    protected abstract MoveResult TryMove(string curState, Shape shape, object externalInfo = null);

    KMSelectable.OnInteractHandler ButtonHandler(int i)
    {
        return delegate
        {
            _scaffold.Shapes[i].AddInteractionPunch(.3f);
            _scaffold.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _scaffold.Shapes[i].transform);
            if (!_isActive)
                return false;

            var result = TryMove(_currentState, _shapes[i]);

            if (result.NewState == null)
            {
                Log(result.StrikeMessage);
                Module.HandleStrike();
                if (Mode == Mode.Memory && AutoReset)
                {
                    setCurrentState(_originState, alwaysShow: true);
                    Log("Returned to {0} ({1}).", GetStateFullName(_originState), GetStateDisplayName(_originState));
                }
            }
            else
            {
                Log("{0} - traveled from {1} ({2}) to {3} ({4}).", _shapes[i], GetStateFullName(_currentState), GetStateDisplayName(_currentState), GetStateFullName(result.NewState), GetStateDisplayName(result.NewState));
                setCurrentState(result.NewState, result.RequireView);
                if (_currentState == _destinationState)
                {
                    Log("Arrived at destination!", _moduleID);
                    Module.HandlePass();
                    _isActive = false;
                }
            }
            return false;
        };
    }

#pragma warning disable 414     // Stop compiler warning: “Field is assigned but its value never used” (they are used by Tweaks and TP)
    private static readonly Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "WorldSettings.json" },
            { "Name", "World Maze Settings" },
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
                    { "Description", "Have the modules reset on strikes in Memory Mode." }
                }
            }}
        }
    };

    private readonly string TwitchHelpMessage = "!{0} press 01234567 | !{0} press c q d z p t h r [Circle, sQuare, Diamond, trapeZoid, Parallelogram, Triangle, Heart, staR] | !{0} mode (standard/memory/memoryreset) | !{0} reset [memory mode only]";
#pragma warning restore 414

    protected IEnumerable<KMSelectable> ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*(?:press\s+)?([0-7cqdzpthr ,;]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var btns = new List<KMSelectable>();
            foreach (var ch in m.Groups[1].Value)
                switch (ch)
                {
                    case '0': case 'c': case 'C': btns.Add(_scaffold.Shapes[0]); break;
                    case '1': case 'q': case 'Q': btns.Add(_scaffold.Shapes[1]); break;
                    case '2': case 'd': case 'D': btns.Add(_scaffold.Shapes[2]); break;
                    case '3': case 'z': case 'Z': btns.Add(_scaffold.Shapes[3]); break;
                    case '4': case 'p': case 'P': btns.Add(_scaffold.Shapes[4]); break;
                    case '5': case 't': case 'T': btns.Add(_scaffold.Shapes[5]); break;
                    case '6': case 'h': case 'H': btns.Add(_scaffold.Shapes[6]); break;
                    case '7': case 'r': case 'R': btns.Add(_scaffold.Shapes[7]); break;
                }
            return btns;
        }
        else if (Mode == Mode.Memory && Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return new[] { _scaffold.Reset };
        else if ((m = Regex.Match(command, @"^\s*(?:mode|set *mode|switch|toggle)\s+(standard|memory|memory *reset)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            if (m.Groups[1].Value.Equals("standard", StringComparison.InvariantCultureIgnoreCase))
                Mode = Mode.Standard;
            else
            {
                Mode = Mode.Memory;
                AutoReset = !m.Groups[1].Value.Equals("memory", StringComparison.InvariantCultureIgnoreCase);
            }
            setCurrentState(_originState);
            return new KMSelectable[0];
        }
        return null;
    }

    protected void Log(string formatString, params object[] formatArguments)
    {
        Debug.LogFormat("[{0} #{1}] {2}", Module.ModuleDisplayName, _moduleID, string.Format(formatString, formatArguments));
    }

    protected void LogDebug(string formatString, params object[] formatArguments)
    {
        Debug.LogFormat("<{0} #{1}> {2}", Module.ModuleDisplayName, _moduleID, string.Format(formatString, formatArguments));
    }

    struct Connection
    {
        public string FromState;
        public string ToState;
        public Shape Shape;
    }

    // Retrieves some external information at the start of the autosolve algorithm.
    // For example: USA Maze uses this to get the Day-of-Week at the start of the algorithm.
    protected virtual object TwitchSolver_GetExternalInfo() { return null; }

    // Ensures that the external information is still valid before a button is pressed.
    // For example: USA Maze uses this to check that the Day-of-Week is still the same as it was at the start.
    protected virtual bool TwitchSolver_ExternalInfoStillValid(object externalInfo) { return true; }

    protected IEnumerator TwitchHandleForcedSolve()
    {
        // If we are travelling to an outlying territory (Alaska/Hawaii), and the day changes while we are executing the button presses, we need to start all over to calculate a new route
        recalculateRoute:
        var ext = TwitchSolver_GetExternalInfo();

        // Breadth-first search
        var already = new HashSet<string>();
        var parents = new Dictionary<string, Connection>();
        var q = new Queue<string>();
        q.Enqueue(_currentState);

        while (q.Count > 0)
        {
            var state = q.Dequeue();
            if (!already.Add(state))
                continue;
            if (state == _destinationState)
                goto found;

            var traversible = new List<Connection>();
            foreach (var shape in _shapes)
            {
                var result = TryMove(state, shape, ext);
                if (result.NewState != null)
                    traversible.Add(new Connection { FromState = state, ToState = result.NewState, Shape = shape });
            }

            foreach (var connection in traversible)
            {
                if (already.Contains(connection.ToState))
                    continue;
                q.Enqueue(connection.ToState);
                parents[connection.ToState] = connection;
            }
        }

        throw new Exception("The auto-solve handler found no path to the destination. This could be a bug in the module’s maze generator or the auto-solver. Please contact the developer about this.");

        found:
        var path = new List<Shape>();
        var curState = _destinationState;
        while (curState != _currentState)
        {
            var connection = parents[curState];
            path.Add(connection.Shape);
            curState = connection.FromState;
        }

        for (var i = path.Count - 1; i >= 0; i--)
        {
            if (!TwitchSolver_ExternalInfoStillValid(ext))
                goto recalculateRoute;
            _scaffold.Shapes[(int) path[i]].OnInteract();
            yield return new WaitForSeconds(.2f);
        }
    }
}