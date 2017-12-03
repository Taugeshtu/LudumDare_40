using UnityEngine;
using System.Collections.Generic;

public class Penguin : AICreature {
	private static float s_herdRadius = 7f;
	
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
		
	}
	
	private void _UpdateValue() {
		var newValue = 1f;
		foreach( var pengu in m_iceberg.Penguins ) {
			if( pengu == this ) {
				continue;
			}
			
			if( Vector3.Distance( transform.position, pengu.transform.position ) < s_herdRadius ) {
				newValue += 1;
			}
		}
		
		if( m_iceberg.Player != null ) {
			if( Vector3.Distance( transform.position, m_iceberg.Player.Position ) < s_herdRadius *2 ) {
				newValue += 10;
			}
		}
		
		Value = newValue;
	}
	
	protected override void _Move() {
		
		base._Move();
	}
#endregion
	
	
#region Temporary
#endregion
}
