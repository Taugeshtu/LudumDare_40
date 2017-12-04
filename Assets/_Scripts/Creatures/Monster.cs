using UnityEngine;
using System.Collections.Generic;

public class Monster : AICreature {
	private enum State {
		Hungry,
		Fed
	}
	
	private Penguin m_target;
	private int m_cycles;
	private int m_hunger;
	private Vector3 m_targetPosition;
	private State m_state;
	
	protected override int _layerMask {
		get { return (1 << 8) + (1 << 4); }	// Note: because Monster can swim can actually walk on monsters
	}
	
#region Implementation
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	protected override void _Ignite() {
		base._Ignite();
		
		m_hunger = Random.Range( 1, 4 );
		m_hunger = 1;
		
		m_state = State.Hungry;
	}
	
	protected override void _Idle() {
		if( m_target != null ) {
			var diff = m_target.Position - Position;
			if( diff.magnitude < 2.5f && Vector3.Angle( diff, transform.forward ) < 70f ) {
				m_target.GetKilled();
				m_target = null;
				m_hunger -= 1;
				
				if( m_hunger == 0 ) {
					m_state = State.Fed;
					m_targetPosition = Random.onUnitSphere.XZ().X0Y() *m_iceberg.Mesh.ActualMesh.bounds.size.magnitude *2;
				}
			}
		}
	}
	
	protected override void _Decide() {
		m_cycles += 1;
		if( m_target == null ) {
			_PickVictim();
		}
		else {
			if( m_cycles > 5 ) {
				_PickVictim();
			}
		}
	}
	
	private void _PickVictim() {
		var minValue = float.MaxValue;
		foreach( var pengu in m_iceberg.Penguins ) {
			if( pengu.IsAlive ) {
				var penguValue = pengu.Value *Vector3.Distance( Position, pengu.Position );
				penguValue = 1f /penguValue;
				if( penguValue < minValue ) {
					m_target = pengu;
					minValue = penguValue;
				}
			}
		}
	}
	
	protected override void _Move() {
		var moveDirection = Vector3.zero;
		if( m_state == State.Hungry ) {
			if( m_target != null ) {
				moveDirection = (m_target.Position - Position).normalized;
			}
		}
		else if( m_state == State.Fed ) {
			moveDirection = (m_targetPosition - Position).normalized;
		}
		
		_SetMoveDirection( moveDirection );
		base._Move();
	}
#endregion
	
	
#region Temporary
#endregion
}
