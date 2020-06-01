using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WorldMazes;

class USAMaze : WorldMazeBase
{
    protected override string MazeID { get { return "USA"; } }

    private static readonly string[] _states = "AL,AR,AZ,CA,CO,CT,DE,FL,GA,IA,ID,IL,IN,KS,KY,LA,MA,MD,ME,MI,MN,MO,MS,MT,NC,ND,NE,NH,NJ,NM,NV,NY,OH,OK,OR,PA,RI,SC,SD,TN,TX,UT,VA,VT,WA,WI,WV,WY".Split(',');
    private static readonly string[] _stateBorders = "ID-NV,NV-OR,CO-KS,CO-OK,CO-NM,AZ-UT,NM-TX,NM-OK,IA-NE,MO-NE,KS-MO,MO-OK,AR-OK,MN-WI,IA-WI,IL-WI,IA-IL,IL-MO,AR-TN,AR-MS,LA-MS,IL-KY,IN-KY,KY-OH,KY-WV,VA-WV,KY-MO,KY-TN,TN-VA,NC-VA,FL-GA,AL-FL,NC-SC,GA-SC,DE-MD,DE-PA,NJ-PA,DE-NJ,NJ-NY,NY-PA,MA-NH,MA-VT,MA-NY,CT-MA,MA-RI,AL-GA,MS-TN,AL-TN,GA-TN,NC-TN,NY-VT,MD-WV,MD-VA,OH-WV,IN-OH,IL-IN,IA-MO,MI-WI,AZ-CA,CA-NV,CA-OR,ID-MT,MT-SD,SD-WY,NE-SD,IA-SD,MN-ND,MN-SD,IA-MN,ID-OR,OR-WA,KS-OK,AL-MS,AR-LA,MT-WY,ID-WY,UT-WY,CO-WY,CO-NE,KS-NE,ID-WA,KY-VA,ME-NH,NH-VT,CT-NY,AR-MO,IN-MI,MI-OH,GA-NC,MO-TN,AZ-NM,CO-UT,MD-PA,PA-WV,OH-PA,CT-RI,ID-UT,NV-UT,AZ-NV,NE-WY,LA-TX,AR-TX,OK-TX,ND-SD,MT-ND".Split(',');
    private static readonly string[] _outStates = "AL,AZ,CA,CT,DE,FL,GA,ID,LA,MA,MD,ME,MI,MN,MS,MT,NC,ND,NH,NJ,NM,NY,OH,OR,PA,RI,SC,TX,VA,VT,WA,WI".Split(',');
    private static readonly string[] _outlying = "AK,HI".Split(',');

    private Dictionary<string, Shape> _openBorders;
    private Dictionary<string, Shape> _outShapes;
    private Shape[][] _outOpen; // First index is same as in ‘outlying’; second is day of week

    private static readonly Dictionary<string, Shape> _seed1_outShapes = new Dictionary<string, Shape>
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

    private static readonly Dictionary<string, Shape> _seed1_openBorders = new Dictionary<string, Shape>
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

    private static readonly Shape[][] _seed1_outOpen = new Shape[][]
    {
        new Shape[] { Shape.Circle, Shape.Square, Shape.Trapezoid, Shape.Parallelogram, Shape.Diamond, Shape.Triangle, Shape.Heart },
        new Shape[] { Shape.Square, Shape.Circle, Shape.Triangle, Shape.Diamond, Shape.Parallelogram, Shape.Trapezoid, Shape.Star }
    };

    protected override void GenerateRules(MonoRandom rnd)
    {
        if (rnd.Seed == 1)
        {
            _outShapes = _seed1_outShapes;
            _openBorders = _seed1_openBorders;
            _outOpen = _seed1_outOpen;
            return;
        }

        _outShapes = new Dictionary<string, Shape>();
        _openBorders = new Dictionary<string, Shape>();

        tryAgain:
        var usedShapesPerState = new Dictionary<string, List<Shape>>();

        // STEP 1: Decide which states have an “out” (connection to Alaska/Hawaii) and assign them shapes
        var availableOutStates = rnd.ShuffleFisherYates(_outStates.ToArray());
        for (var i = 0; i < 8; i++)
        {
            _outShapes[availableOutStates[i]] = _shapes[i];
            usedShapesPerState.AddSafe(availableOutStates[i], _shapes[i]);
        }

        // STEP 2: Generate the main maze and assign a shape to each opened border
        _openBorders = new Dictionary<string, Shape>();
        var openBorder = new Func<string, bool>(borderId =>
        {
            var m = Regex.Match(borderId, "^(..)-(..)$");
            if (!m.Success)
            {
                Log("There is a bug in the rule seed generator. Please contact the developer about this.", _moduleID);
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
                usedShapesPerState.AddSafe(st, chosenShape);
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
                LogDebug(@"Rule seed generator: ran into the case with no extra borders. Retrying");
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

        LogDebug("Open borders: {1}", _moduleID, _openBorders.Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value)).Join("; "));
        LogDebug("Out-connections: {1}", _moduleID, _outShapes.Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value)).Join("; "));
        for (var outIx = 0; outIx < _outlying.Length; outIx++)
            LogDebug("Out times for {1}: {2}", _moduleID, _outlying[outIx], _outOpen[outIx].Join(", "));
    }

    private DayOfWeek getDow()
    {
        if (_scaffold.Info.QueryWidgets("day", null).Count == 1)
            return (DayOfWeek) Enum.Parse(typeof(DayOfWeek), _scaffold.Info.QueryWidgets("day", null).Select(x => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(x)).First()["day"]);
        return DateTime.Now.DayOfWeek;
    }

    protected override MoveResult TryMove(string fromState, Shape shape, object externalInfo = null)
    {
        Shape s;

        // Are we in an outlying territory? Then we need to fly back on an open flight
        var dow = (externalInfo as DayOfWeek?) ?? getDow();
        var outIx = Array.IndexOf(_outlying, fromState);
        if (outIx != -1)
        {
            var stateIx = _states.IndexOf(st => _outShapes.TryGetValue(st, out s) && s == shape);

            // This should never happen because the rule generator generates an out-connection for every shape
            if (stateIx == -1)
                return new MoveResult { StrikeMessage = string.Format("{0} - there is no state with an out-connection with that shape. Strike!", shape) };

            if (_outOpen[outIx][(int) dow] != shape)
                return new MoveResult { StrikeMessage = string.Format("{0} - this connection is not open on this day ({1}). Strike!", shape, dow) };

            return new MoveResult { NewState = _states[stateIx] };
        }

        // Does the current state have this shape as an out-connection?
        if (_outShapes.TryGetValue(fromState, out s) && s == shape)
        {
            // Is this out-connection open on the current day?
            for (var outlIx = 0; outlIx < _outlying.Length; outlIx++)
                if (_outOpen[outlIx][(int) dow] == shape)
                    return new MoveResult { NewState = _outlying[outlIx] };
            return new MoveResult { StrikeMessage = string.Format("{0} - tried to fly out but this connection is not open on this day ({1}). Strike!", shape, dow) };
        }

        // Does this shape connect the current state to another state?
        foreach (var otherState in _states)
            if ((_openBorders.TryGetValue(fromState + "-" + otherState, out s) && s == shape) ||
                (_openBorders.TryGetValue(otherState + "-" + fromState, out s) && s == shape))
                return new MoveResult { NewState = otherState };

        return new MoveResult { StrikeMessage = string.Format("{0} - the current state has no connection with that symbol. Strike!", shape) };
    }

    protected override List<string> GetAllStates() { return _states.Concat(_outlying).ToList(); }
    protected override object TwitchSolver_GetExternalInfo() { return getDow(); }
    protected override bool TwitchSolver_ExternalInfoStillValid(object externalInfo) { return externalInfo is DayOfWeek && (DayOfWeek) externalInfo == getDow(); }
    protected override string GetStateDisplayName(string code) { return code; }

    protected override string GetStateFullName(string code)
    {
        switch (code)
        {
            case "AK": return "Alaska";
            case "AL": return "Alabama";
            case "AR": return "Arkansas";
            case "AZ": return "Arizona";
            case "CA": return "California";
            case "CO": return "Colorado";
            case "CT": return "Connecticut";
            case "DE": return "Delaware";
            case "FL": return "Florida";
            case "GA": return "Georgia";
            case "HI": return "Hawaii";
            case "IA": return "Iowa";
            case "ID": return "Idaho";
            case "IL": return "Illinois";
            case "IN": return "Indiana";
            case "KS": return "Kansas";
            case "KY": return "Kentucky";
            case "LA": return "Louisiana";
            case "MA": return "Massachusetts";
            case "MD": return "Maryland";
            case "ME": return "Maine";
            case "MI": return "Michigan";
            case "MN": return "Minnesota";
            case "MO": return "Missouri";
            case "MS": return "Mississippi";
            case "MT": return "Montana";
            case "NC": return "North Carolina";
            case "ND": return "North Dakota";
            case "NE": return "Nebraska";
            case "NH": return "New Hampshire";
            case "NJ": return "New Jersey";
            case "NM": return "New Mexico";
            case "NV": return "Nevada";
            case "NY": return "New York";
            case "OH": return "Ohio";
            case "OK": return "Oklahoma";
            case "OR": return "Oregon";
            case "PA": return "Pennsylvania";
            case "RI": return "Rhode Island";
            case "SC": return "South Carolina";
            case "SD": return "South Dakota";
            case "TN": return "Tennessee";
            case "TX": return "Texas";
            case "UT": return "Utah";
            case "VA": return "Virginia";
            case "VT": return "Vermont";
            case "WA": return "Washington";
            case "WI": return "Wisconsin";
            case "WV": return "West Virginia";
            case "WY": return "Wyoming";
            default: return null;
        }
    }
}