using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WorldMazes;

class DACHMaze : WorldMazeBase
{
    protected override string MazeID { get { return "DACH"; } }

    // All the states have unique codes which are only used internally by the module.
    // Both the logging and the manual show the intended display name.
    // For example, internally, BE=Berlin and BN=Bern, but they both show as BE.
    private static readonly string[] _states = "AG,AI,AR,BB,BE,BL,BN,BS,BU,BW,BY,FL,FR,GE,GL,GR,HB,HE,HH,JU,KN,LU,MV,ND,NE,NI,NO,NT,NW,OO,OT,OW,RP,SA,SC,SG,SH,SL,SM,SN,SO,ST,SZ,TG,TH,TI,UR,VD,VO,VS,WI,ZG,ZH".Split(',');
    private static readonly string[] _stateBorders = "AG-BL,BL-BS,AG-BW,AG-LU,AG-SO,AG-ZG,AG-ZH,AI-AR,AI-SG,AR-SG,BB-BE,BB-MV,BB-NI,BB-SN,BB-ST,BN-VD,BL-JU,BL-SO,BN-FR,BN-JU,BN-LU,BN-NE,BN-OW,BN-SO,BN-UR,BN-VD,BN-VS,BS-BW,BU-NO,BU-SM,BW-BY,BW-HE,BW-RP,BW-TG,BW-ZH,BY-HE,BY-NT,BY-OO,BY-SA,BY-SN,BY-TH,BY-VO,FL-GR,FL-SG,FL-VO,FR-VD,GE-VD,GL-GR,GL-SG,GL-SZ,GL-UR,GR-NT,GR-SG,GR-TI,GR-UR,GR-VO,HB-NI,NI-SH,HE-NI,HE-NW,HE-RP,HE-TH,HH-NI,HH-SH,JU-SO,KN-OT,KN-SA,KN-SM,LU-ND,LU-OW,LU-SZ,MV-NI,MV-SH,ND-OW,ND-UR,NE-VD,NI-ST,NI-TH,NO-OO,NO-SM,NO-WI,NT-SA,NT-VO,NW-NI,NW-RP,OO-SA,OO-SM,OT-SA,OW-UR,RP-SL,SA-SM,SG-SZ,SG-TG,SG-VO,SG-ZH,SC-BW,SC-TG,SC-ZH,SN-ST,SN-TH,ST-TH,SZ-UR,SZ-ZG,SZ-ZH,TG-ZH,TI-UR,TI-VS,UR-VS,VD-VS,ZG-ZH".Split(',');
    private static readonly string[][] _clashes = "BE,BN;ND,NW;SC,SH;SM,ST".Split(';').Select(pair => pair.Split(',')).ToArray();

    private Dictionary<string, Shape> _openBorders;

    protected override void GenerateRules(MonoRandom rnd)
    {
        tryAgain:
        var usedShapesPerState = new Dictionary<string, List<Shape>>();

        // STEP 1: Generate the main maze and assign a shape to each opened border
        _openBorders = new Dictionary<string, Shape>();
        var openBorder = new Func<string, bool>(borderId =>
        {
            var m = Regex.Match(borderId, "^(..)-(..)$");
            if (!m.Success)
            {
                Log("There is a bug in the rule seed generator. Please contact Timwi about this.", _moduleID);
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

            // If either state has a counterpart with a clashing abbreviation, and has no shape in common with it yet, try to accommodate that
            var clashIx = _clashes.IndexOf(cl => cl.Contains(state) || cl.Contains(otherState));
            if (clashIx != -1)
            {
                var st = _clashes[clashIx].Contains(state) ? state : otherState;
                var counterpart = _clashes[clashIx].First(cl => cl != st);
                if (usedShapesPerState.ContainsKey(counterpart) && (!usedShapesPerState.ContainsKey(st) || !usedShapesPerState[st].Any(sh => usedShapesPerState[counterpart].Contains(sh))))
                {
                    var preferential = availableShapes.Where(sh => usedShapesPerState[counterpart].Contains(sh)).ToList();
                    if (preferential.Count > 0)
                        availableShapes = preferential;
                }
            }

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

        // STEP 2: Open three more random borders
        var allBorders = rnd.ShuffleFisherYates(_stateBorders.ToList());
        var ix = 0;
        var extraBorders = 0;
        while (extraBorders < 3)
        {
            if (ix >= allBorders.Count)
            {
                LogDebug("Rule seed generator: ran into the case with no extra borders. Retrying");
                goto tryAgain;
            }
            if (!_openBorders.ContainsKey(allBorders[ix]) && openBorder(allBorders[ix]))
                extraBorders++;
            ix++;
        }

        foreach (var border in _openBorders)
            if (border.Key.Contains("BL"))
                LogDebug("{0} = {1}", border.Key, border.Value);
    }

    protected override MoveResult TryMove(string fromState, Shape shape, object externalInfo = null)
    {
        Shape s;

        foreach (var otherState in _states)
            if ((_openBorders.TryGetValue(fromState + "-" + otherState, out s) && s == shape) ||
                (_openBorders.TryGetValue(otherState + "-" + fromState, out s) && s == shape))
                return new MoveResult { NewState = otherState };

        return new MoveResult { StrikeMessage = string.Format("{0} - the current state has no connection with that symbol. Strike!", shape) };
    }

    protected override List<string> GetAllStates() { return _states.ToList(); }
    protected override string GetStateDisplayName(string code)
    {
        switch (code)
        {
            case "SM": return "ST";
            case "BN": return "BE";
            case "ND": return "NW";
            case "SC": return "SH";
        }
        return code;
    }

    protected override string GetStateFullName(string code)
    {
        switch (code)
        {
            case "BU": return "Burgenland, A";
            case "KN": return "Carinthia, A";
            case "NO": return "Lower Austria, A";
            case "NT": return "North Tyrol, A";
            case "OO": return "Upper Austria, A";
            case "OT": return "East Tyrol, A";
            case "SA": return "Salzburg, A";
            case "SM": return "Styria, A";
            case "VO": return "Vorarlberg, A";
            case "WI": return "Vienna, A";
            case "AG": return "Aargau, CH";
            case "AI": return "Appenzell Inner Rhodes, CH";
            case "AR": return "Appenzell Outer Rhodes, CH";
            case "BL": return "Basel Country, CH";
            case "BN": return "Bern, CH";
            case "BS": return "Basel City, CH";
            case "FR": return "Fribourg, CH";
            case "GE": return "Geneva, CH";
            case "GL": return "Glarus, CH";
            case "GR": return "Grisons, CH";
            case "JU": return "Jura, CH";
            case "LU": return "Luzern, CH";
            case "ND": return "Nidwalden, CH";
            case "NE": return "Neuchâtel, CH";
            case "OW": return "Obwalden, CH";
            case "SC": return "Schaffhausen, CH";
            case "SG": return "St. Gallen, CH";
            case "SO": return "Solothurn, CH";
            case "SZ": return "Schwyz, CH";
            case "TG": return "Thurgau, CH";
            case "TI": return "Ticino, CH";
            case "UR": return "Uri, CH";
            case "VD": return "Vaud, CH";
            case "VS": return "Valais, CH";
            case "ZG": return "Zug, CH";
            case "ZH": return "Zürich, CH";
            case "BB": return "Brandenburg, D";
            case "BE": return "Berlin, D";
            case "BW": return "Baden-Württemberg, D";
            case "BY": return "Bavaria, D";
            case "HB": return "Bremen, D";
            case "HE": return "Hesse, D";
            case "HH": return "Hamburg, D";
            case "MV": return "Mecklenburg-Vorpommern, D";
            case "NI": return "Lower Saxony, D";
            case "NW": return "North Rhine-Westphalia, D";
            case "RP": return "Rhineland-Palatinate, D";
            case "SH": return "Schleswig-Holstein, D";
            case "SL": return "Saarland, D";
            case "SN": return "Saxony, D";
            case "ST": return "Saxony-Anhalt, D";
            case "TH": return "Thuringia, D";
            case "FL": return "Liechtenstein";
            default: return null;
        }
    }
}