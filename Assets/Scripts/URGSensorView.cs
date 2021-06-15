using System.Collections;
using UnityEngine;

public class URGSensorView : MonoBehaviour
{
    [SerializeField] Camera _camera;
    [SerializeField] int _distanceGap = 100;
    [SerializeField] int _minSize = 20;
    [SerializeField] int _maxSize = 100;
    //[SerializeField] float _ActualBoardSize_InMM= 1000;
    [SerializeField] Vector2 _offsetXY_mm = Vector2.zero;
    [SerializeField] float _offsetRot_deg = 0f;
    [SerializeField] string _sensorIP = "192.168.0.10";
    [SerializeField] int _sensorStartID = 0;
    [SerializeField] int _sensorEndID = 2160;
    [Range(1, 20)]
    [SerializeField] int _skip;

    public float ProjectedSize
    {
        set { _camera.orthographicSize = (1000.0f / value); }
    }

    public Vector2 CenterPosition
    {
        set { _offsetXY_mm = value; }
    }

    public float Rotate
    {
        set { _offsetRot_deg = value; }
    }

    public void SetDetectParam(int gap, int minSize, int maxSize, int skip)
    {
        _distanceGap = gap;
        _minSize = minSize;
        _maxSize = maxSize;
        _skip = skip;
    }

    private URGSensor _urg;     // URG sensor

    void Start()
    {
        _urg = new URGSensor();
        _urg.OpenStream(_sensorIP, _sensorStartID, _sensorEndID);

        if (_urg.IsOpen)
            StartCoroutine(AutoStoreEnvironmentData());
    }

    IEnumerator AutoStoreEnvironmentData()
    {
        yield return new WaitForSeconds(10);
        _urg.StoreEnvironmentData();
    }

    void Update()
    {
        if (_urg.IsOpen == false)
            return;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _urg.StoreEnvironmentData();
        }

        _camera.transform.localPosition = new Vector3(0, _camera.orthographicSize, -10);
        //_camera.orthographicSize = (1000.0f / _ActualBoardSize_InMM);

        transform.localPosition = new Vector3(_offsetXY_mm.x, _offsetXY_mm.y, 0f) / 1000f;
        transform.localEulerAngles = new Vector3(0f, 0f, _offsetRot_deg);

        _urg.SetDetectParam(_distanceGap, _minSize, _maxSize);
        _urg.Skip = _skip;

        // get urg pose matrix
        _urg.Pose = transform.localToWorldMatrix;

        _urg.Update();

    }

    private void OnDestroy()
    {
        _urg.CloseStream();
    }

    static Material lineMaterial;
    static void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }


    public void OnRenderObject()
    {
        if (_urg.IsOpen == false)
            return;

        CreateLineMaterial();
        lineMaterial.SetPass(0);

        GL.PushMatrix();

        var skip = _urg.Skip;

        GL.Begin(GL.LINES);
        GL.Color(new Color(1, 1, 1));
        var distance = _urg.Distances;
        if (distance == null) return;

        for (int i = 0; i < distance.Length; i += skip)
        {
            var o = transform.position;
            GL.Vertex3(o.x, o.y, 0);
            var p = _urg.CalcPos(i);
            GL.Vertex3(p.x, p.y, 0);
        }

        var calb_distance = _urg.EnvironmentDistances;
        for (int i = 0; i < distance.Length; i += skip)
        {
            var cd = calb_distance[i];
            var dd = distance[i];
            if (Mathf.Abs(cd - dd) > _distanceGap)
                GL.Color(new Color(1, 0, 0));
            else
                GL.Color(new Color(0.5f, 0.5f, 0.5f));

            var p = _urg.CalcPos(i);
            GL.Vertex3(p.x, p.y, 0);
            var cp = _urg.CalcCalbPos(i);
            GL.Vertex3(cp.x, cp.y, 0);
        }
        GL.End();

        foreach (var obj in _urg.SortedObj())
        {
            var p = obj; // w is size
            GL.Begin(GL.QUADS);
            GL.Color(Color.yellow);
            GL.Vertex3(-0.1f + p.x, 0.1f + p.y, 0);
            GL.Vertex3(-0.1f + p.x, -0.1f + p.y, 0);
            GL.Vertex3(0.1f + p.x, -0.1f + p.y, 0);
            GL.Vertex3(0.1f + p.x, 0.1f + p.y, 0);
            GL.End();
        }
        GL.PopMatrix();
    }
}