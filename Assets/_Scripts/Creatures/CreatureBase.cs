using UnityEngine;
using System.Collections.Generic;

public abstract class CreatureBase : MonoBehaviour {
	[SerializeField] private ValueCurve m_pushoutScale;
	[SerializeField] private float m_castRadius = 0.2f;
	[SerializeField] private float m_castDepth = 1f;
	
	private bool m_shouldDraw = true;
	private Rigidbody m_rigid;
	
	private Rigidbody _rigidbody {
		get {
			if( m_rigid == null ) { m_rigid = GetComponent<Rigidbody>(); }
			return m_rigid;
		}
	}
	
#region Implementation
	void FixedUpdate() {
		_Pushout();
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
	private void _Pushout() {
		RaycastHit hit;
		var ray = new Ray( transform.position + transform.up *m_castDepth, -transform.up );
		if( Caster.SphereCast( ray, m_castRadius, out hit, m_castDepth, Physics.DefaultRaycastLayers, m_shouldDraw ) ) {
			var factor = Mathf.InverseLerp( m_castDepth, 0f, hit.distance );
			
			factor = m_pushoutScale.Evaluate( factor );
			factor *= Physics.gravity.magnitude;
			
			var pushout = transform.up *factor;
			_rigidbody.AddForce( -_rigidbody.velocity, ForceMode.VelocityChange );
			_rigidbody.AddForce( pushout, ForceMode.Acceleration );
		}
	}
#endregion
	
	
#region Temporary
#endregion
}
