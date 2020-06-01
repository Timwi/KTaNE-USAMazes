using UnityEngine;

sealed class WorldMazeScaffold : MonoBehaviour
{
#pragma warning disable 649    // Stop compiler warning: “Field is not assigned to” (they are assigned in Unity)
    public KMBombInfo Info;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] Shapes;
    public KMSelectable Reset;
    public TextMesh VisCurrent, VisMaze, VisDestination;
#pragma warning restore 649
}
