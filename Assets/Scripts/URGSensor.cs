using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using SCIP_library;
using System.Threading.Tasks;
using UnityEngine;

public class URGSensor
{
	TcpClient _urg;
	NetworkStream _stream;
	int _start_step = 0;
	int _end_step = 2160;

	List<long> _read_data;
	Task _task;
	long[] _distance;
	long[] _env_distance;
	long[] _filtered_distance;
	bool _isRun;
	object _lockobj;
	int _ares;
	int _amax;
	int _skip = 1;
	int _distanceGap = 100;
	int _minSize = 20;
	int _maxSize = 150;
	bool _isDirty = false;

	Matrix4x4 _pose;
	public Matrix4x4 Pose { set { _pose = value; } }

	bool _isOpen = false;
	public bool IsOpen => _isOpen;

	private List<Vector4> _objs;
	public Vector4[] Objs
	{
		get
		{
			lock (_lockobj)
			{
				if (_objs.Count == 0) return null;
				var arry = new Vector4[_objs.Count];
				_objs.CopyTo(arry);
				return arry;
			}
		}
	}

	public bool LargestObj(out Vector4 v)
	{
		lock (_lockobj)
		{
			if (_objs.Count == 0)
			{
				v = Vector4.zero;
				return false;
			}

			var maxSize = 0.0f;
			var id = 0;
			for (int i = 0; i < _objs.Count; i++)
			{
				var p = _objs[i]; // w is size
				if (p.w > maxSize)
				{
					maxSize = p.w;
					id = i;
				}
			}

			v = _objs[id];

			return true;
		}
	}

	public List<Vector4> SortedObj()
	{
		lock (_lockobj)
		{
			var res = new List<Vector4>();
			res.AddRange(_objs);
			res.Sort((a, b) => Math.Sign(b.w - a.w));
			return res;
		}
	}

	public int Skip
	{
		get { return _skip; }
		set { _skip = value; }
	}
	public int Steps => (_amax + 1);
	public long[] Distances => _distance;
	public long[] FilteredDistances => _filtered_distance;
	public long[] EnvironmentDistances => _env_distance;

	private void readSpec(string spec)
	{
		foreach (var sp in spec.Split('\n'))
		{
			var tokens = sp.Split(new char[] { ':', ';' });
			if (tokens[0] == "ARES") { _ares = int.Parse(tokens[1]); }
			if (tokens[0] == "AMAX") { _amax = int.Parse(tokens[1]); }
		}
	}

	public void SetDetectParam(int gap, int minSize, int maxSize)
	{
		if (gap > 0) _distanceGap = gap;
		if (minSize > 0) _minSize = minSize;
		if (maxSize > _minSize) _maxSize = maxSize;
	}

	public void OpenStream(string ip_address, int start_step, int end_step)
	{
		const int port_number = 10940;

		_start_step = start_step;
		_end_step = end_step;

		_urg = new TcpClient();
		_urg.SendBufferSize = 0;
		_urg.ReceiveBufferSize = 0;
		try
		{
			_urg.Connect(ip_address, port_number);
		}
		catch (System.Net.Sockets.SocketException e)
		{
			_isOpen = false;
			Debug.Log(e.Message);
			return;
		}

		_isOpen = true;
		_stream = _urg.GetStream();

		write(_stream, SCIP_Writer.PP());
		readSpec(read_line(_stream));

		write(_stream, SCIP_Writer.SCIP2());
		read_line(_stream); // ignore echo back
		write(_stream, SCIP_Writer.MD(_start_step, _end_step));
		read_line(_stream);  // ignore echo back

		_lockobj = new object();
		_read_data = new List<long>();
		_distance = new long[_amax + 1];
		_env_distance = new long[_amax + 1]; // store calibration data
		_filtered_distance = new long[_amax + 1];

		_objs = new List<Vector4>();

		_isRun = true;
		_task = Task.Run(() =>
		{
			var time_stamp = 0L;
			while (_isRun)
			{
				var receive_data = read_line(_stream);
				SCIP_Reader.MD(receive_data, ref time_stamp, ref _read_data);
				if (_read_data.Count == 0) continue;

				lock (_lockobj)
				{
					_read_data.CopyTo(_distance);
					_isDirty = true;
				}
			}
		});
	}

	public void StoreEnvironmentData()
	{
		_distance.CopyTo(_env_distance, 0);
	}

	private Vector3 distToPos(int step, long dist)
	{
		var th = (Mathf.PI * 2f / _ares) * step - (Mathf.PI * 0.25f);
		var x = Mathf.Cos(th) * dist;
		var y = Mathf.Sin(th) * dist;
		return new Vector3(x, y, 0) * 0.001f; // mm -> m
	}

	/// <summary>
	/// get raw position  
	/// </summary>
	/// <param name="step"></param>
	/// <returns></returns>
	public Vector3 CalcRawPos(int step)
	{
		return distToPos(step, _distance[step]);
	}

	public Vector3 CalcCalibPos(int step)
	{
		return distToPos(step, Math.Abs(_distance[step] - _env_distance[step]));
	}

	public Vector3 CalcCalibDataPos(int step)
	{
		return distToPos(step, _env_distance[step]);
	}

	/// <summary>
	/// get position with pose 
	/// </summary>
	/// <param name="step"></param>
	/// <returns></returns>
	public Vector3 CalcPos(int step)
	{
		return _pose.MultiplyPoint(CalcRawPos(step));
	}

	public Vector3 CalcCalbPos(int step)
	{
		var d = distToPos(step, _env_distance[step]);
		return _pose.MultiplyPoint(d);
	}

	public void Update()
	{
		if (_isDirty == false) return;
		var count = 0;
		var sp = Vector3.zero;
		var cp = Vector3.zero;

		_objs.Clear();
		for (int i = 0; i < this.Steps - _skip; i += _skip)
		{
			var cd = _env_distance[i];
			var dd = _distance[i];

			if (Math.Abs(cd - dd) > _distanceGap)
			{
				if (count == 0)
				{
					cp = CalcPos(i);
				}
				else if (count > 0)
				{
					cp += CalcPos(i);
				}
				++count;
			}
			else
			{
				if (count > _minSize && count < _maxSize)
				{
					Vector4 rp = cp / (float)count;     // center of object
					rp.w = count;
					_objs.Add(rp);
				}
				count = 0;
			}
		}

		_isDirty = true;
	}

	public void CloseStream()
	{
		_isRun = false;
		if (_isOpen)
		{
			_task.Wait();

			write(_stream, SCIP_Writer.QT());    // stop measurement mode
			read_line(_stream); // ignore echo back
			_stream.Close();
			_urg.Close();
		}
	}

	/// <summary>
	/// Read to "\n\n" from NetworkStream
	/// </summary>
	/// <returns>receive data</returns>
	private static string read_line(NetworkStream stream)
	{
		if (stream.CanRead)
		{
			StringBuilder sb = new StringBuilder();
			bool is_NL2 = false;
			bool is_NL = false;
			do
			{
				char buf = (char)stream.ReadByte();
				if (buf == '\n')
				{
					if (is_NL)
					{
						is_NL2 = true;
					}
					else
					{
						is_NL = true;
					}
				}
				else
				{
					is_NL = false;
				}
				sb.Append(buf);
			} while (!is_NL2);

			return sb.ToString();
		}
		else
		{
			return null;
		}
	}

	/// <summary>
	/// write data
	/// </summary>
	private static bool write(NetworkStream stream, string data)
	{
		if (stream.CanWrite)
		{
			byte[] buffer = Encoding.ASCII.GetBytes(data);
			stream.Write(buffer, 0, buffer.Length);
			return true;
		}
		else
		{
			return false;
		}
	}
}