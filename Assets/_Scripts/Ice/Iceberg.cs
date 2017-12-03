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
	private float m_turnSpeed = 0f;
	
	private List<IcebergEntity> m_entities = new List<IcebergEntity>();
	
	private List<Penguin> m_penguins = new List<Penguin>();
	private List<Monster> m_monsters = new List<Monster>();
	
	public IList<Penguin> Penguins { get { return m_penguins; } }
	public IList<Monster> Monsters { get { return m_monsters; } }
	public Player Player { get; private set; }
	
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
	
	public void AddEntity( IcebergEntity entity ) {
		if( entity == null ) {
			Extensions.TimeLogWarning( "This shouldn't happen. Trying to add null entity to Iceberg!", gameObject );
			return;
		}
		
		m_entities.Add( entity );
		
		entity.Link( this );
		entity.transform.SetParent( transform, true );
		
		if( entity is Player ) { Player = entity as Player; }
		else if( entity is Penguin ) { m_penguins.Add( entity as Penguin ); }
		else if( entity is Monster ) { m_monsters.Add( entity as Monster ); }
	}
	
	public void TransferEntity( IcebergEntity entity, Iceberg target ) {
		if( entity == null ) {
			Extensions.TimeLogWarning( "This shouldn't happen. Trying to add null entity to Iceberg!", gameObject );
			return;
		}
		
		m_entities.Remove( entity );
		
		if( entity is Player ) { Player = null; }
		else if( entity is Penguin ) { m_penguins.Remove( entity as Penguin ); }
		else if( entity is Monster ) { m_monsters.Remove( entity as Monster ); }
		
		target.AddEntity( entity );
	}
	
	public void SpawnPenguins( int count ) {
		// TODO: find centroids!
		
		var sanityCheck = count *20;
		while( m_penguins.Count < count ) {
			// TODO: proper position!
			var point = Random.insideUnitCircle.X0Y() *20;
			var newPengu = _SpawnPenguin( point );
			
			AddEntity( newPengu );
			
			sanityCheck -= 1;
			if( sanityCheck <= 0 ) {
				break;
			}
		}
	}
	
	public void SpawnMonster() {
		var position = _GetPositionForMonster();
		var monster = _SpawnMonster( position );
		AddEntity( monster );
	}
	
	public void Split( Vector3 position, Vector3 direction ) {
		Mesh.UnSkirt( IceGenerator.Skirt );
		var drifters = Mesh.Split( position, direction );
		Mesh.WeldVertices();
		Mesh.ReSkirt( IceGenerator.Skirt );
		Mesh.MakeVerticesUnique();
		
		Mesh.WriteToMesh();
		
		var pivotPosition = position + direction.normalized *IceGenerator.GenRadius *0.2f;
		var drift = direction.normalized *m_maxSpeed;
		
		var newIceberg = IceGenerator.Generate( pivotPosition, drifters );
		newIceberg.SetAdrift( drift, Random.Range( -5f, 5f ) );
		
		var shift = Vector3.Project( position, direction );
		var plane = new Plane( shift, -shift.magnitude );
		var inverted = false;
		if( Vector3.Angle( shift, direction ) > 90 ) {	// Meaning we're beyond zer0
			inverted = true;
		}
		
		var entitiesLeaving = new List<IcebergEntity>();
		foreach( var entity in m_entities ) {
			if( plane.GetSide( entity.transform.position, inverted ) ) {
				entitiesLeaving.Add( entity );
			}
		}
		
		foreach( var entity in entitiesLeaving ) {
			TransferEntity( entity, newIceberg );
		}
	}
	
	public void SetAdrift( Vector3 drift, float turnSpeed ) {
		m_drift = drift;
		m_driftStartTime = Time.time + m_driftDelay;
		m_turnSpeed = turnSpeed;
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
			// pengu.transform.SetParent( transform );
			pengu.transform.rotation = Quaternion.AngleAxis( Random.value *360f, Vector3.up );
			pengu.transform.position = hit.point + Vector3.up *Random.Range( 1f, 3f );
			return pengu;
		}
		else {
			return null;
		}
	}
	
	private Vector3 _GetPositionForMonster() {
		var bounds = Mesh.ActualMesh.bounds;
		var point = Random.insideUnitCircle.normalized.X0Y();
		point *= bounds.size.magnitude /2 + 0;
		point += Vector3.up *Random.Range( 1f, 3f );
		return point;
	}
	
	private Monster _SpawnMonster( Vector3 position ) {
		var monster = Instantiate( Game.MonsterPrefab );
		monster.gameObject.SetActive( true );
		
		var rotation = Quaternion.LookRotation( -position.XZ().X0Y() );
		rotation *= Quaternion.AngleAxis( Random.Range( -20, 20 ), Vector3.up );
		monster.transform.rotation = rotation;
		monster.transform.position = position;
		
		return monster;
	}
	
	private void _ProcessDrift() {
		var driftFactor = Mathf.InverseLerp( m_driftStartTime, m_driftStartTime + m_driftBuildupTime, Time.time );
		driftFactor = Mathf.Clamp01( driftFactor );
		// var speed = Mathf.Sin( driftFactor *Mathf.PI /2 ) *m_maxSpeed;
		var speed = driftFactor *driftFactor *driftFactor *m_maxSpeed;
		m_velocity = m_drift.normalized *speed;
		
		transform.position += m_velocity *Time.fixedDeltaTime;
		transform.rotation *= Quaternion.AngleAxis( m_turnSpeed *Time.fixedDeltaTime, Vector3.up );
	}
#endregion
	
	
#region Temporary
#endregion
}
