using UnityEngine;
using System.Collections.Generic;

public class Penguin : AICreature {
	private enum State {
		Idle,
		Move,
		Flock,
		Slide,
		Sprint
	}
	
	private static float s_herdRadius = 7f;
	private static float s_detectionRedius { get { return s_herdRadius *2; } }
	private static float s_monsterAvoidRedius { get { return s_herdRadius *0.5f; } }
	private static float s_penguinAvoidRedius = 1.5f;
	
	protected override int _layerMask {
		get { return (1 << Game.c_layerIceberg); }
	}
	
	protected override bool _canJump {
		get { return true; }
	}
	
	private State m_state;
	private Vector3 m_targetPosition;
	
	public float Value = 1f;
	
#region Implementation
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	protected override void _Idle() {
		_UpdateValue();
	}
	
	protected override void _Decide() {
		var moveChance = 0.05f;
		var idleChance = 0.3f;
		
		if( Dice.Roll( moveChance ) ) {
			m_state = State.Move;
			
			m_targetPosition = Iceberg.RandomOnIce();
			m_targetPosition = Iceberg.transform.InverseTransformPoint( m_targetPosition );
		}
		else if( Dice.Roll( idleChance ) ) {
			m_state = State.Idle;
		}
		else {
			m_state = State.Flock;
		}
	}
	
	private void _UpdateValue() {
		var newValue = 1f;
		foreach( var pengu in Iceberg.Penguins ) {
			if( pengu == this ) {
				continue;
			}
			
			if( Vector3.Distance( transform.position, pengu.transform.position ) < s_herdRadius ) {
				newValue += 1;
			}
		}
		
		if( Iceberg.Player != null ) {
			if( Vector3.Distance( transform.position, Iceberg.Player.Position ) < s_herdRadius *2 ) {
				newValue += 10;
			}
		}
		
		Value = newValue;
	}
	
	protected override void _Move() {
		var moveDirection = Vector3.zero;
		if( IsAlive ) {
			if( m_state == State.Move ) {
				moveDirection = (m_targetPosition - Position).normalized;
			}
			else if( m_state == State.Flock ) {
				moveDirection = _FlockAI();
			}
		}
		
		_SetMoveDirection( moveDirection );
		base._Move();
	}
	
	private Vector3 _FlockAI() {
		var monsters = _GetMonsters();
		var flock = _GetFlock();
		
		var repel = Vector3.zero;
		var repelsCount = 1f;
		foreach( var monster in monsters ) {
			var diff = Position - monster.Position;
			var scale = diff.magnitude;
			scale = Mathf.InverseLerp( -0.1f, s_monsterAvoidRedius, scale );
			diff = diff.normalized *scale;
			
			repel = Vector3.Lerp( repel, diff, 1f /repelsCount );
			repelsCount += 1;
		}
		
		var pull = Vector3.zero;
		var pullCount = 1f;
		var needsPull = true;
		
		var align = Vector3.zero;
		var alignCount = 1f;
		
		foreach( var pengu in flock ) {
			align = Vector3.Lerp( align, pengu.MoveDirection, 1f /alignCount );
			alignCount += 1;
			
			var diff = pengu.Position - Position;
			if( diff.magnitude > s_penguinAvoidRedius ) {
				pull = Vector3.Lerp( pull, diff, 1f /pullCount );
				pullCount += 1;
			}
			else {
				needsPull = false;
			}
		}
		if( !needsPull ) { pull = Vector3.zero; }
		align = Quaternion.AngleAxis( Random.Range( -15, 15 ), Vector3.up ) *align;
		
		var moveDirection = (repel + align + pull) /3f;
		return moveDirection;
	}
	
	private List<Monster> _GetMonsters() {
		var result = new List<Monster>();
		
		foreach( var monster in Iceberg.Monsters ) {
			if( monster.IsAlive ) {
				if( Vector3.Distance( Position, monster.Position ) < s_monsterAvoidRedius ) {
					result.Add( monster );
				}
			}
		}
		
		return result;
	}
	
	private List<CreatureBase> _GetFlock() {
		var result = new List<CreatureBase>();
		
		foreach( var pengu in Iceberg.Penguins ) {
			if( pengu.IsAlive && pengu != this ) {
				if( Vector3.Distance( Position, pengu.Position ) < s_detectionRedius ) {
					result.Add( pengu );
				}
			}
		}
		
		// Now the player counts as 3 penguins!
		if( Iceberg != null && Iceberg.Player != null ) {
			for( var i = 0; i < 3; i++ ) {
				result.Add( Iceberg.Player );
			}
		}
		
		return result;
	}
#endregion
	
	
#region Temporary
#endregion
}
