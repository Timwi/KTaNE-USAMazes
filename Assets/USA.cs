using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class USA : MonoBehaviour
{
    private static int _moduleIDCounter = 1;
    private int _moduleID;
    public KMBombInfo Info;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable[] Shapes;
    public TextMesh visCurrent, visOrigin, visDestination;
    bool isActive = false;
    private int origin, destination, current;
    private readonly int[] circle = new [] { 4, 36, 25, 49, 37, 33, 44, 26, 42, 31, 40, 11, 2, 17, 1, 8 },
        square = new[] { 46, 12, 31, 35, 25, 40, 13, 23, 2, 24, 8, 9, 37, 19, 45, 29 },
        diamond = new[] { 36, 12, 5, 15, 39, 9, 27, 40, 13, 14, 41, 24, 19, 7, 29, 20 },
        trap = new[] { 36, 32, 40, 28, 35, 15, 14, 21, 1, 24, 16, 44, 37, 30, 18, 33, 2, 41 },
        parallel = new[] { 3, 32, 22, 11, 41, 23, 21, 34, 35, 5 },
        triangle = new[] { 12, 49, 22, 47, 42, 2, 16, 34, 45, 18 },
        heart = new[] { 12, 43, 47, 13, 23, 35, 34, 48, 18, 6 },
        star = new[] { 43, 49, 28, 23, 11, 13, 47, 21, 48, 37, 18, 38, 44, 41 };
    public string[] states;

    void Start()
    {
        _moduleID = _moduleIDCounter++;
        origin = UnityEngine.Random.Range(0, 50);
        destination = origin;
        //while (destination == origin) destination = UnityEngine.Random.Range(0, 50);
        while (destination.Equals(origin) || (destination.Equals(0) && !origin.Equals(10)) || (destination.Equals(10) && !origin.Equals(0)))
        {
            destination = UnityEngine.Random.Range(0, 50);
        }
        current = origin;
        Module.OnActivate += delegate () { Activate(); };
    }

    void Activate()
    {
        visCurrent.text = states[current];
        visOrigin.text = states[origin];
        visDestination.text = states[destination];
        for (int i = 0; i < Shapes.Count(); i++)
        {
            int j = i;
            Shapes[i].OnInteract = ButtonHandler(j);
        }
        isActive = true;
    }

    KMSelectable.OnInteractHandler ButtonHandler(int j)
    {
        return delegate
        {
            Shapes[j].AddInteractionPunch(.3f);
            if (!isActive) return false;
            switch (j)
            {
                case 0:
                    if (circle.Contains(current))
                    {
                        var i = Array.IndexOf(circle, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = circle[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 1:
                    if (square.Contains(current))
                    {
                        var i = Array.IndexOf(square, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = square[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 2:
                    if (diamond.Contains(current))
                    {
                        var i = Array.IndexOf(diamond, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = diamond[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 3:
                    if (trap.Contains(current))
                    {
                        var i = Array.IndexOf(trap, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = trap[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 4:
                    if (parallel.Contains(current))
                    {
                        var i = Array.IndexOf(parallel, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = parallel[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 5:
                    if (triangle.Contains(current))
                    {
                        var i = Array.IndexOf(triangle, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = triangle[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 6:
                    if (heart.Contains(current))
                    {
                        var i = Array.IndexOf(heart, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = heart[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                case 7:
                    if (star.Contains(current))
                    {
                        var i = Array.IndexOf(star, current);
                        var ni = (i % 2).Equals(0) ? i + 1 : i - 1;
                        current = star[ni];
                        visCurrent.text = states[current];
                    }
                    else goto default;
                    break;
                default:
                    if ((current.Equals(0) || current.Equals(10)) && !(destination.Equals(0) || destination.Equals(10)))
                    {
                        var stay = current;
                        while (stay.Equals(current)) current = UnityEngine.Random.Range(0, 50);
                        visCurrent.text = states[current];
                    }
                    else if ((current.Equals(0) && destination.Equals(10)) || (current.Equals(10) && destination.Equals(0)))
                    {
                        visCurrent.text = states[destination];
                        Module.HandlePass();
                        isActive = false;
                    }
                    else Module.HandleStrike();
                    break;
            }
            if (current == destination)
            {
                Module.HandlePass();
                isActive = false;
            }
            return false;
        };
    }

    private string TwitchHelpMessage = "Submit path using !{0} press 01234567 or !{0} press c q d z p t h r [Circle, sQuare, Diamond, trapiZoid, Parallelogram, Triangle, Heart, staR]";

    private KMSelectable[] ProcessTwitchCommand(string command)
    {
        int num = 0;
        command = command.ToLowerInvariant();
        List<KMSelectable> s = new List<KMSelectable>();
        if (!command.StartsWith("press ") || command.Contains("8") || command.Contains("9")) return null;
        command = command.Replace("press ", "");
        for (int i = 0; i < command.Length; i++)
        {
            if (int.TryParse(command, out num)) s.Add(Shapes[command[i] - '0']);
            else
            {
                switch (command[i])
                {
                    case 'c':
                        s.Add(Shapes[0]);
                        break;
                    case 'q':
                        s.Add(Shapes[1]);
                        break;
                    case 'd':
                        s.Add(Shapes[2]);
                        break;
                    case 'z':
                        s.Add(Shapes[3]);
                        break;
                    case 'p':
                        s.Add(Shapes[4]);
                        break;
                    case 't':
                        s.Add(Shapes[5]);
                        break;
                    case 'h':
                        s.Add(Shapes[6]);
                        break;
                    case 'r':
                        s.Add(Shapes[7]);
                        break;
                    case ' ':
                        break;
                    default:
                        return null;
                }
            }
        }
        return s.ToArray();
    }
}