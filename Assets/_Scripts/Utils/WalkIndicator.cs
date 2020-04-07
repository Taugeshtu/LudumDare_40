using UnityEngine;
using System.Collections.Generic;

public class WalkIndicator : MonoBehaviour {
	[SerializeField] private Transform m_visual;
	[SerializeField] private float m_radius = 0.1f;
	[SerializeField] private float m_rollSpeed = 0.1f;
	
	private Vector3 m_previousPosition;
	
	private float _perimeter {
		get { return 2 *Mathf.PI *m_radius; }
	}
	
#region Implementation
	void LateUpdate() {
		transform.rotation = Quaternion.identity;
		
		var movement = transform.position - m_previousPosition;
		if( movement.sqrMagnitude > Mathf.Epsilon ) {
			var normal = Vector3.up;
			var tangent = Vector3.zero;
			Vector3.OrthoNormalize( ref normal, ref movement, ref tangent );
			
			var turns = movement.magnitude /_perimeter;
			var roll = Quaternion.AngleAxis( 360f *turns *m_rollSpeed, tangent );
			
			m_visual.rotation = roll *m_visual.rotation;
			m_visual.localPosition = Vector3.up *m_radius;
			
			Draw.Ray( transform.position, tangent, Palette.darkLime, 0.5f );
		}
		
		m_previousPosition = transform.position;
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
