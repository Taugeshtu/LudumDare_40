using UnityEngine;
using System.Collections.Generic;

public abstract class AICreature : CreatureBase {
	private static Vector2 s_decisionInterval = new Vector2( 2, 4 );
	
	private float m_nextDecitionTime;
	
#region Implementation
	void Update() {
		_Idle();
		
		if( Time.time > m_nextDecitionTime ) {
			m_nextDecitionTime = Time.time + Random.Range( s_decisionInterval.x, s_decisionInterval.y );
			_Decide();
		}
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	protected override void _Ignite() {
		base._Ignite();
		m_nextDecitionTime = Time.time + Random.Range( s_decisionInterval.x, s_decisionInterval.y );
	}
	
	protected virtual void _Idle() {}
	protected virtual void _Decide() {}
#endregion
	
	
#region Temporary
#endregion
}
