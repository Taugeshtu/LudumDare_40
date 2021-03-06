﻿using UnityEngine;
using System.Collections.Generic;

public class Iceberg : MonoBehaviour {
	public IcebergMesh Mesh { get; private set; }
	
	private Vector3 m_drift;
	private float m_driftDelay = 0.7f;
	private float m_driftBuildupTime = 2f;
	private float m_driftStartTime = float.MaxValue;
	
	private Vector3 m_velocity;
	private float m_maxSpeed = 2f;
	private float m_turnSpeed = 0f;
	
	private List<IcebergEntity> m_entities = new List<IcebergEntity>();
	
	private List<Penguin> m_penguins = new List<Penguin>();
	private List<Monster> m_monsters = new List<Monster>();
	
	public IList<Penguin> Penguins { get { return m_penguins; } }
	public IList<Monster> Monsters { get { return m_monsters; } }
	public Player Player { get; private set; }
	
	public Vector3 Drift { get { return m_drift; } }
	
#region Implementation
	void Update() {
		if( Player == null ) {
			_CheckDestroy();
		}
	}
	
	void FixedUpdate() {
		if( Time.time > m_driftStartTime ) {
			_ProcessDrift();
		}
	}
	
	void OnDrawGizmos() {
		if( Mesh != null ) {
			// Mesh.Draw();
		}
	}
	
	void OnDestroy() {
		var copy = new List<IcebergEntity>( m_entities );
		foreach( var x in copy ) {
			if( x != Player ) {
				Destroy( x );
			}
		}
		m_entities.Clear();
		m_penguins.Clear();
		m_monsters.Clear();
		Player = null;
	}
#endregion
	
	
#region Public
	public void Ignite( IcebergMesh mesh ) {
		Mesh = mesh;
	}
	
	public void AddEntity( IcebergEntity entity ) {
		if( entity == null ) {
			Debug.LogWarning( "This shouldn't happen. Trying to add null entity to Iceberg!", gameObject );
			return;
		}
		
		m_entities.Add( entity );
		
		entity.Link( this );
		entity.transform.SetParent( transform, true );
		
		if( entity is Player ) { Player = entity as Player; }
		else if( entity is Penguin ) { m_penguins.Add( entity as Penguin ); }
		else if( entity is Monster ) { m_monsters.Add( entity as Monster ); }
	}
	
	public void RemoveEntity( IcebergEntity entity ) {
		if( entity == null ) {
			Debug.LogWarning( "This shouldn't happen. Trying to remove null entity from Iceberg!", gameObject );
			return;
		}
		
		m_entities.Remove( entity );
		
		if( entity is Player ) { Player = null; }
		else if( entity is Penguin ) { m_penguins.Remove( entity as Penguin ); }
		else if( entity is Monster ) { m_monsters.Remove( entity as Monster ); }
	}
	
	public void SpawnPenguins( int count ) {
		var sanityCheck = count *20;
		var spawnedCount = 0;
		while( spawnedCount < count ) {
			// TODO: proper position!
			var point = RandomOnIce();
			var newPengu = _SpawnPenguin( point );
			
			AddEntity( newPengu );
			spawnedCount += 1;
			
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
		var pivotPosition = position + direction.normalized *IceGenerator.GenRadius *0.2f;
		var drift = direction.normalized *m_maxSpeed;
		
		var driftMesh = Mesh.Split( position, direction );
		Mesh.Write();
		
		var newIceberg = IceGenerator.SpawnSplit( driftMesh, pivotPosition );
		newIceberg.Mesh.Write();
		newIceberg.SetAdrift( drift, Random.Range( -5f, 5f ) );
		Game.RegisterIceberg( newIceberg );
	}
	
	public void SetAdrift( Vector3 drift, float turnSpeed ) {
		m_drift = drift;
		m_driftStartTime = Time.time + m_driftDelay;
		m_turnSpeed = turnSpeed;
	}
	
	public void TransferDrift( Iceberg target ) {
		target.m_driftStartTime = m_driftStartTime;
		target.m_turnSpeed = -m_turnSpeed;
		target.m_drift = -m_drift;
		
		m_drift = Vector3.zero;
		m_driftStartTime = float.MaxValue;
		m_turnSpeed = 0;
	}
	
	public Vector3 RandomOnIce() {
		var result = Vector3.up *5;
		var gotIt = false;
		var sanity = 100;
		while( !gotIt ) {
			var point = Random.insideUnitCircle.X0Y() *Mesh.Target.mesh.bounds.extents.magnitude;
			var ray = new Ray( point + Vector3.up *20, Vector3.down );
			RaycastHit hit;
			var mask = (1 << Game.c_layerIceberg);
			
			if( Physics.Raycast( ray, out hit, 50f, mask ) ) {
				if( hit.collider == Mesh.Collider ) {
					result = hit.point;
				}
			}
			
			sanity -= 1;
			if( sanity <= 0 ) { gotIt = true; }
		}
		
		return result;
	}
	
	public float Distance( Iceberg other ) {
		var boundsA = Mesh.Target.GetComponent<MeshRenderer>().bounds;
		var boundsB = other.Mesh.Target.GetComponent<MeshRenderer>().bounds;
		return boundsA.Distance( boundsB );
	}
#endregion
	
	
#region Private
	private Penguin _SpawnPenguin( Vector3 position ) {
		var pengu = Instantiate( Game.PenguinPrefab );
		pengu.gameObject.SetActive( true );
		// pengu.transform.SetParent( transform );
		pengu.transform.rotation = Quaternion.AngleAxis( Random.value *360f, Vector3.up );
		pengu.transform.position = position + Vector3.up *Random.Range( 1f, 3f );
		return pengu;
	}
	
	private Vector3 _GetPositionForMonster() {
		var bounds = Mesh.Target.mesh.bounds;
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
		var speed = driftFactor *driftFactor *driftFactor;
		m_velocity = m_drift *speed;
		
		transform.position += m_velocity *Time.fixedDeltaTime;
		transform.rotation *= Quaternion.AngleAxis( m_turnSpeed *Time.fixedDeltaTime, Vector3.up );
	}
	
	private void _CheckDestroy() {
		if( Game.Player == null ) {
			return;
		}
		
		var distanceToPlayerIceberg = Game.Player.Iceberg.Distance( this );
		if( distanceToPlayerIceberg > 50f ) {
			Game.DestroyIceberg( this );
		}
	}
#endregion
	
	
#region Temporary
#endregion
}
