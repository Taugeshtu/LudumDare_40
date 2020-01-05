using UnityEngine;
using System.Collections.Generic;

using Clutter;

public class CameraShake : MonoSingular<CameraShake> {
	protected override BehaviourSettings Behaviour { get { return new BehaviourSettings( false, true, true ); } }
	
	[SerializeField] private Vector2Int m_majorJumps;
	[SerializeField] private Vector2Int m_minorJumps;
	[SerializeField] private float m_majorAmp = 1.5f;
	[SerializeField] private float m_minorAmp = 0.5f;
	
	[SerializeField] private float m_jumpTime = 0.1f;
	
	private Queue<Vector3> m_jumps = new Queue<Vector3>();
	private Vector3 m_target;
	private Vector3 m_velocity;
	private bool m_isAnimating = false;
	private bool m_isMajor = false;
	
#region Implementation
	void Update() {
		if( !m_isAnimating ) {
			if( m_jumps.Count > 0 ) {
				m_target = m_jumps.Dequeue();
				m_isAnimating = true;
			}
		}
		
		var newPosition = transform.localPosition;
		var time = m_isMajor ? m_majorAmp : m_minorAmp;
		time *= m_jumpTime;
		
		newPosition = Vector3.SmoothDamp( newPosition, m_target, ref m_velocity, time );
		transform.localPosition = newPosition;
		
		if( (newPosition - m_target).magnitude < 0.05f ) {
			m_isAnimating = false;
		}
	}
#endregion
	
	
#region Public
	public static void MakeAShake( bool isMajor ) {
		s_Instance._MakeAShake( isMajor );
	}
#endregion
	
	
#region Private
	private void _MakeAShake( bool isMajor ) {
		m_isMajor = isMajor;
		
		var jumps = _GetJumps( isMajor );
		foreach( var jump in jumps ) {
			m_jumps.Enqueue( jump );
		}
	}
	
	private List<Vector3> _GetJumps( bool isMajor ) {
		var range = isMajor ? m_majorJumps : m_minorJumps;
		var amplitude = isMajor ? m_majorAmp : m_minorAmp;
		
		var total = Random.Range( range.x, range.y + 1 );
		var jumps = new List<Vector3>();
		for( var i = 0; i < total; i++ ) {
			var point = Random.onUnitSphere *amplitude;
			jumps.Add( point );
		}
		
		return jumps;
	}
#endregion
	
	
#region Temporary
#endregion
}
