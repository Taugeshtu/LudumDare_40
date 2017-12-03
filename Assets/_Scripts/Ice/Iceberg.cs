using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class Iceberg : MonoBehaviour {
	public IcebergMesh Mesh { get; private set; }
	
	private Vector3 m_drift;
	private float m_driftDelay = 0.7f;
	private float m_driftBuildupTime = 2f;
	private float m_driftStartTime = 1f;
	
	private Vector3 m_velocity;
	private float m_maxSpeed = 2f;
	
	private List<Penguin> m_penguins = new List<Penguin>();
	private Player m_player;
	
#region Implementation
	void FixedUpdate() {
		if( Time.time > m_driftStartTime ) {
			_ProcessDrift();
		}
	}
#endregion
	
	
#region Public
	public void Ignite( IcebergMesh mesh ) {
		Mesh = mesh;
	}
	
	public void LinkPlayer( Player player ) {
		m_player = player;
		m_player.Link( this );
		m_player.transform.SetParent( transform, true );
	}
	
	public void SpawnPenguins( int count ) {
		// TODO: find centroids!
		
		var sanityCheck = count *20;
		while( m_penguins.Count < count ) {
			// TODO: proper position!
			var point = Random.insideUnitCircle.X0Y() *20;
			var newPengu = _SpawnPenguin( point );
			
			if( newPengu != null ) {
				m_penguins.Add( newPengu );
			}
			
			sanityCheck -= 1;
			if( sanityCheck <= 0 ) {
				break;
			}
		}
	}
	
	public void Split( Vector3 position, Vector3 direction ) {
		var drifters = Mesh.Split( position, direction );
		var newIceberg = IceGenerator.Generate( drifters );
		var drift = direction.normalized *m_maxSpeed;
		newIceberg.m_drift = drift;
		newIceberg.m_driftStartTime = Time.time + m_driftDelay;
		
		var shift = Vector3.Project( position, direction );
		var plane = new Plane( shift, -shift.magnitude );
		var inverted = false;
		if( Vector3.Angle( shift, direction ) > 90 ) {	// Meaning we're beyond zer0
			inverted = true;
		}
		
		var sadTimes = new List<Penguin>();
		foreach( var pengu in m_penguins ) {
			if( plane.GetSide( pengu.transform.position, inverted ) ) {
				sadTimes.Add( pengu );
			}
		}
		
		foreach( var sadPengu in sadTimes ) {
			m_penguins.Remove( sadPengu );
			sadPengu.transform.SetParent( newIceberg.transform, true );
		}
	}
#endregion
	
	
#region Private
	private Penguin _SpawnPenguin( Vector3 position ) {
		var ray = new Ray( position + Vector3.up *20, Vector3.down );
		RaycastHit hit;
		var mask = (1 << 8);
		
		if( Physics.Raycast( ray, out hit, 50f, mask ) ) {
			if( hit.collider != Mesh.Collider ) {
				return null;
			}
			
			var pengu = Instantiate( Game.PenguinPrefab );
			pengu.gameObject.SetActive( true );
			pengu.transform.SetParent( transform );
			pengu.transform.rotation = Quaternion.AngleAxis( Random.value *360f, Vector3.up );
			pengu.transform.position = hit.point + Vector3.up *Random.Range( 1f, 3f );
			return pengu;
		}
		else {
			return null;
		}
	}
	
	private void _ProcessDrift() {
		var driftFactor = Mathf.InverseLerp( m_driftStartTime, m_driftStartTime + m_driftBuildupTime, Time.time );
		driftFactor = Mathf.Clamp01( driftFactor );
		// var speed = Mathf.Sin( driftFactor *Mathf.PI /2 ) *m_maxSpeed;
		var speed = driftFactor *driftFactor *driftFactor *m_maxSpeed;
		m_velocity = m_drift.normalized *speed;
		
		transform.position += m_velocity *Time.fixedDeltaTime;
	}
#endregion
	
	
#region Temporary
#endregion
}
