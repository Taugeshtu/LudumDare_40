using UnityEngine;
using System.Collections.Generic;

public class Waver : MonoBehaviour {
	[SerializeField] private float m_amp = 0.5f;
	[SerializeField] private float m_period = 5f;
	
	private Vector3 m_savedPosition;
	private float m_offset = 0f;
	
#region Implementation
	void Awake() {
		m_savedPosition = transform.position;
		m_offset = Random.Range( 0f, m_period );
	}
	
	void Update() {
		var phase = Time.time /m_period;
		phase *= Mathf.PI *2;
		
		var newPosition = m_savedPosition + Vector3.up *m_amp *Mathf.Sin( phase );
		transform.position = newPosition;
	}
#endregion
	
	
#region Public
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
