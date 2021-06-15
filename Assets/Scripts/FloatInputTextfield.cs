using UnityEngine;

// https://forum.unity3d.com/threads/gui-textfield-submission-via-return-key.69361/

public class FloatInputTextfield
{
    public FloatInputTextfield(float val = 0.0f)
    {
        Value = val;
    }

    public float Value
    {
        get { return validValue; }
        set
        {
            validValue = currentValue = value;
            text = "" + validValue;
            error = isEdited = false;
        }
    }

    static GUIStyle style = null;

    float validValue = 0.0f;
    float currentValue = 0.0f;
    bool isEdited = false;
    bool error = false;
    string text = "";

    public bool OnGUI() //Rect area)
    {
        if (style == null) // Has to be done with OnGUI call
        {
            style = new GUIStyle(GUI.skin.textField);
            style.alignment = TextAnchor.MiddleCenter;
        }

        bool changed = false;
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Return)
            {
                Value = currentValue;
                changed = true;
            }
            else
            if (e.keyCode == KeyCode.Escape)
            {
                Value = validValue;
            }
        }
        Color saved = GUI.color;
        GUI.color = error ? Color.red : (isEdited ? Color.yellow : saved);
        //text = GUI.TextField(area, text, style);
        text = GUILayout.TextField(text, style);
        error = !float.TryParse(text, out currentValue);
        GUI.color = saved;
        isEdited = error || validValue != currentValue;

        return changed;
    }
}