using UnityEngine;
using System.Collections.Generic;

using DrawUtils = Draw;

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
	
	private float _duration {
		get { return _distance /m_speed; }
	}
	
	private float _pauseDuration {
		get {
			if( _distance < c_unpausedJumpDistance ) { return 0f; }
			
			var pauseDuration = _distance *c_pausePerMeter;
			var pauseFraction = Mathf.Atan( pauseDuration ) /Mathf.PI;
			return _duration *pauseFraction;
		}
	}
	
	private Vector3 _verticalDiff {
		get {
			return (m_target - m_origin).ProjectedOn( Vector3.up );
		}
	}
	
	private Vector3 _horizontalDiff {
		get {
			return (m_target - m_origin).Flat( Vector3.up );
		}
	}
	
#region Implementation
	public Jump( Vector3 origin, Vector3 target, float speed, bool draw = false ) {
		m_origin = origin;
		m_target = target;
		
		m_speed = speed;
		m_startTime = Time.time;
		
		if( draw ) {
			Draw();
		}
	}
#endregion
	
	
#region Public
	public Vector3? CalculatePosition() {
		var timeDiff = Time.time - m_startTime;
		
		if( timeDiff < _pauseDuration ) {
			return m_origin;
		}
		
		var endTime = m_startTime + _duration;
		if( Time.time > endTime ) {
			return null;
		}
		
		var factor = Mathf.InverseLerp( m_startTime + _pauseDuration, endTime, Time.time );
		return _CalculateForFactor( factor );
	}
	
	public void Draw() {
		for( var i = 1; i <= 10; i++ ) {
			var previous = _CalculateForFactor( (i - 1) /10f );
			var current = _CalculateForFactor( i /10f );
			
			DrawUtils.RayFromTo( previous, current, Palette.orange, 0.1f, 3 );
		}
	}
#endregion
	
	
#region Private
	private Vector3 _CalculateForFactor( float factor ) {
		// given that y = high - (x - distanceToHigh)^2, and it passes through
		// (0, 0) == origin; (hDiff, vDiff) == target
		// find arc constants:
		var vDiff = Vector3.up.Dot( _verticalDiff );
		var hDiff = _horizontalDiff.magnitude;
		
		var distanceToHigh = (vDiff + hDiff *hDiff) / (2 *hDiff);
		var high = distanceToHigh *distanceToHigh;
		
		// find position on an arc:
		var x = hDiff *factor;
		var y = high - ((x - distanceToHigh) *(x - distanceToHigh));
		
		// morph x to emulate drag:
		x = hDiff *Sin.Lerp( factor, SinShape.CRising );
		
		return m_origin + Vector3.up *y + _horizontalDiff.normalized *x;
	}
#endregion
	
	
#region Temporary
#endregion
}
