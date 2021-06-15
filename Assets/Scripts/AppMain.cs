using UnityEngine;
using Nett; // toml

public class Sensor {
    public float Height { get; set; }
    public float ShiftX { get; set; }
    public float Rotate { get; set; }
    public float ProjectedSize { get; set; } // in mm
    public int Gap { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public int Skip { get; set; }
}

public class Goal
{
    public float[] Region { get; set; }
}

public class Param
{
    public Sensor Sensor { get; set; }
    public Goal Goal { get; set; }
}

public class AppMain : MonoBehaviour {

    [SerializeField]
    private GUISkin _skin;
    FloatInputTextfield _projectedSize;
    FloatInputTextfield _sensorHeight;
    FloatInputTextfield _sensorRotate;
    FloatInputTextfield _sensorShiftX;
    FloatInputTextfield _sensorGap;
    FloatInputTextfield _sensorMinSize;
    FloatInputTextfield _sensorMaxSize;
    FloatInputTextfield _sensorSkip;

    [SerializeField] URGSensorView _urgView;

    [SerializeField] Param param;

    // Use this for initialization
    private void Awake()
    {
        var toml_file = string.Format("{0}/../config.toml", Application.dataPath);
        param = Toml.ReadFile<Param>(toml_file);

        _urgView.ProjectedSize = param.Sensor.ProjectedSize;
        _urgView.CenterPosition = new Vector2(param.Sensor.ShiftX, param.Sensor.Height);
        _urgView.Rotate = param.Sensor.Rotate;
        _urgView.SetDetectParam( param.Sensor.Gap, param.Sensor.MinSize, param.Sensor.MaxSize, param.Sensor.Skip);

        _projectedSize = new FloatInputTextfield(param.Sensor.ProjectedSize);
        _sensorHeight = new FloatInputTextfield(param.Sensor.Height);
        _sensorRotate = new FloatInputTextfield(param.Sensor.Rotate);
        _sensorShiftX = new FloatInputTextfield(param.Sensor.ShiftX);
        _sensorGap = new FloatInputTextfield(param.Sensor.Gap);
        _sensorMinSize = new FloatInputTextfield(param.Sensor.MinSize);
        _sensorMaxSize = new FloatInputTextfield(param.Sensor.MaxSize);
        _sensorSkip = new FloatInputTextfield(param.Sensor.Skip);
    }

    void Start() { }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnGUI()
    {
        GUI.skin = _skin;
        var w = 500;
        var h = 400;
        var x = w / 2;
        var y = 0;
        GUILayout.Window(0, new Rect(x - w / 2, y, w, h), ConfigurationWindow, "Configuration");

        if (_projectedSize.Value > 0)
        {
            _urgView.ProjectedSize = _projectedSize.Value;
        }
        else
        {
            _projectedSize.Value = 1;
        }

        _urgView.CenterPosition = new Vector2(_sensorShiftX.Value, _sensorHeight.Value);
        _urgView.Rotate = _sensorRotate.Value;
        _urgView.SetDetectParam(
            Mathf.RoundToInt(_sensorGap.Value),
            Mathf.RoundToInt(_sensorMinSize.Value),
            Mathf.RoundToInt(_sensorMaxSize.Value),
            Mathf.RoundToInt(_sensorSkip.Value));
    }

    private void EditValues(string title, FloatInputTextfield field)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(title, GUILayout.Width(300));
        field.OnGUI();
        GUILayout.EndHorizontal();
    }

    void ConfigurationWindow(int wid)
    {
        GUILayout.BeginVertical();
        EditValues("Actual Board Size(mm)", _projectedSize);
        EditValues("Sensor Height (mm)", _sensorHeight);
        EditValues("Shift X(mm)", _sensorShiftX);
        EditValues("Sensor rotate(deg)", _sensorRotate);
        EditValues("Sensor Gap", _sensorGap);
        EditValues("Sensor Min Size", _sensorMinSize);
        EditValues("Sensor Max Size", _sensorMaxSize);
        EditValues("Sensor Skip", _sensorSkip);

        if (GUILayout.Button("SAVE"))
        {
            param.Sensor.Height = _sensorHeight.Value;
            param.Sensor.ShiftX = _sensorShiftX.Value;
            param.Sensor.Rotate = _sensorRotate.Value;
            param.Sensor.ProjectedSize = _projectedSize.Value;
            param.Sensor.MinSize = Mathf.RoundToInt(_sensorMinSize.Value);
            param.Sensor.MaxSize = Mathf.RoundToInt(_sensorMaxSize.Value);
            param.Sensor.Skip = Mathf.RoundToInt(_sensorSkip.Value);
            param.Sensor.Gap = Mathf.RoundToInt(_sensorGap.Value);

            Toml.WriteFile<Param>(param, "config.toml");
        }
        GUILayout.EndVertical();
    }
}
