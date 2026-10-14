using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    class Objective
    {
        public string id;
        public string text;
        public bool done;
    }

    readonly List<Objective> list = new List<Objective>();

    void Awake()
    {
        Instance = this;
        Add("redkey", "Find the red key");
        Add("bluekey", "Find the blue key");
        Add("goldkey", "Steal the gold key");
        Add("lever", "Pull the lever in the spike field");
        Add("gems", "Collect the gems ({0}/{1})");
        Add("exitdoor", "Open the exit vault");
        Add("escape", "Step into the exit portal");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Add(string id, string text)
    {
        list.Add(new Objective { id = id, text = text });
    }

    public void Clear()
    {
        list.Clear();
    }

    public void Complete(string id)
    {
        foreach (var o in list)
        {
            if (o.id == id && !o.done)
            {
                o.done = true;
                Refresh();
                return;
            }
        }
    }

    public void Refresh()
    {
        if (UIManager.Instance == null) return;

        var gm = GameManager.Instance;
        string gems = gm != null ? gm.Gems.ToString() : "0";
        string req = gm != null ? gm.gemsRequired.ToString() : "5";

        var sb = new StringBuilder();
        bool currentMarked = false;
        foreach (var o in list)
        {
            string text = o.text.Replace("{0}", gems).Replace("{1}", req);
            if (o.done)
            {
                sb.Append("<color=#7CFC9A>[x] ").Append(text).Append("</color>\n");
            }
            else if (!currentMarked)
            {
                currentMarked = true;
                sb.Append("<color=#FFE066><b>> ").Append(text).Append("</b></color>\n");
            }
            else
            {
                sb.Append("<color=#9AA4B5>[ ] ").Append(text).Append("</color>\n");
            }
        }
        UIManager.Instance.SetObjectives(sb.ToString());
    }
}
