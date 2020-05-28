using UnityEngine;
using System.Collections.Generic;

public struct Jump {
	private const float c_unpausedJumpDistance = 1f;
	private const float c_pausePerMeter = 0.1f;
	
	private Vector3 m_origin;
	private Vector3 m_target;
	
	private float m_speed;
	private float m_startTime;
	
	private float _distance {
		get { return Vector3.Distance( m_origin, m_target ); }
	}
	
	private float _pauseDuration {
		get {
			if( _distance < c_unpausedJumpDistance ) { return 0f; }
			return _distance *c_pausePerMeter;
		}
	}
	
	private float _jumpDuration {
		get { return _distance /m_speed; }
	}
	
#region Implementation
	public Jump( Vector3 origin, Vector3 target, float speed ) {
		m_origin = origin;
		m_target = target;
		
		m_speed = speed;
		m_startTime = Time.time;
	}
#endregion
	
	
#region Public
	public Vector3? CalculatePosition() {
		var timeDiff = Time.time - m_startTime;
		
		if( timeDiff < _pauseDuration ) {
			return m_origin;
		}
		
		var endTime = m_startTime + _pauseDuration + _jumpDuration;
		if( Time.time > endTime ) {
			return null;
		}
		
		var progress = Mathf.InverseLerp( m_startTime + _pauseDuration, endTime, Time.time );
		// TODO: proper arc here!
		
		return Vector3.Lerp( m_origin, m_target, progress );
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
