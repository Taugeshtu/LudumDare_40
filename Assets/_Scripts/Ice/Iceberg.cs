using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class Iceberg : MonoBehaviour {
	public IcebergMesh Mesh { get; private set; }
	
	private List<Penguin> m_penguins = new List<Penguin>();
	private Player m_player;
	
#region Implementation
#endregion
	
	
#region Public
	public void Init( IcebergMesh mesh ) {
		Mesh = mesh;
	}
	
	public void LinkPlayer( Player player ) {
		m_player = player;
		m_player.Link( this );
	}
	
	public void SpawnPenguins( int count ) {
		Extensions.TimeLogError( "IMPLEMENT ME" );
	}
	
	public void Split( Vector3 position, Vector3 direction ) {
		Mesh.Split( position, direction );
		
		// TODO: should also re-assign creatures to new iceberg
	}
#endregion
	
	
#region Private
#endregion
	
	
#region Temporary
#endregion
}
