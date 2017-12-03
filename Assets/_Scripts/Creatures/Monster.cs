using UnityEngine;
using System.Collections.Generic;

public class Monster : AICreature {
	public bool IsAlive = true;
	
	private Penguin m_target;
	
	protected override int _layerMask {
		get { return (1 << 8) + (1 << 4); }	// Note: because Monster can swim can actually walk on monsters
	}
	
#region Implementation
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	protected override void _Idle() {
		if( m_target != null ) {
			m_motionTarget = m_target.Position;
		}
	}
	
	protected override void _Decide() {
		if( m_target == null ) {
			_PickVictim();
		}
	}
	
	private void _PickVictim() {
		var minValue = float.MaxValue;
		foreach( var pengu in m_iceberg.Penguins ) {
			var penguValue = pengu.Value *Vector3.Distance( Position, pengu.Position );
			penguValue = 1f /penguValue;
			if( penguValue < minValue ) {
				m_target = pengu;
				minValue = penguValue;
			}
		}
	}
	
	protected override void _Move() {
		if( m_target != null ) {
			m_moveDirection = (m_target.Position - Position).normalized;
		}
		
		base._Move();
	}
#endregion
	
	
#region Temporary
#endregion
}
