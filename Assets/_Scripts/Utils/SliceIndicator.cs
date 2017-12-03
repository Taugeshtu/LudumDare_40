using UnityEngine;
using System.Collections.Generic;

public class SliceIndicator : MonoBehaviour {
	[SerializeField] private Transform m_prefab;
	[SerializeField] private float m_totalWidth = 3f;
	[SerializeField] private int m_count = 5;
	[SerializeField] private float m_lineWidth = 0.3f;
	
	private List<Transform> m_segments = new List<Transform>();
	
	private float _segmentWidth {
		get { return m_totalWidth /m_count; }
	}
	
#region Implementation
	void Awake() {
		_Spawn();
	}
	
	void FixedUpdate() {
		_Update();
	}
#endregion
	
	
#region Public
	public void Position( Vector3 point, Vector3 direction ) {
		transform.position = point;
		transform.rotation = Quaternion.LookRotation( direction );
	}
#endregion
	
	
#region Private
	private void _Spawn() {
		for( var i = 0; i < m_count; i++ ) {
			var copy = Instantiate( m_prefab );
			copy.SetParent( m_prefab.parent );
			m_segments.Add( copy );
		}
		
		m_prefab.gameObject.SetActive( false );
	}
	
	private void _Update() {
		var corner = transform.position + Vector3.up *10;
		corner -= transform.right *m_totalWidth /2;
		var chunk = transform.right *_segmentWidth;
		
		var hits = new Vector3[m_count + 1];
		var gotHits = new bool[m_count + 1];
		for( var i = 0; i < hits.Length; i++ ) {
			hits[i] = corner + chunk *i - Vector3.up *10;
			gotHits[i] = false;
			
			var ray = new Ray( corner + chunk *i, Vector3.down );
			RaycastHit hit;
			var mask = (1 << 8);
			if( Physics.Raycast( ray, out hit, 20f, mask ) ) {
				hits[i] = hit.point;
				gotHits[i] = true;
			}
		}
		
		for( var i = 0; i < m_count; i++ ) {
			var a = hits[i];
			var b = hits[i + 1];
			var mid = (a + b) /2;
			var diff = (b - a);
			var rotation = Quaternion.LookRotation( diff );
			var scale = (Vector3.one *m_lineWidth).WithZ( diff.magnitude );
			
			m_segments[i].position = mid;
			m_segments[i].rotation = rotation;
			m_segments[i].localScale = scale;
			
			m_segments[i].gameObject.SetActive( gotHits[i] || gotHits[i + 1] );
		}
	}
#endregion
	
	
#region Temporary
#endregion
}
