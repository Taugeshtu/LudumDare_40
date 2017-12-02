using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IcebergGenerator : MonoBehaviour {
	[SerializeField] private Material m_material;
	[SerializeField] private int m_startIterations = 40;
	[SerializeField] private Vector2 m_coercion = Vector2.up *0.5f;
	
	private Iceberg m_generated;
	
#region Implementation
	void Update() {
		if( m_generated == null ) {
			m_generated = Generate( m_startIterations );
		}
		
		if( Input.GetKeyDown( KeyCode.G ) ) {
			_GenIteration( m_generated.Mesh );
		}
	}
#endregion
	
	
#region Public
	public Iceberg Generate( int iterations ) {
		var prep = _PrepareMesh();
		
		var iceberg = prep.Filter.gameObject.AddComponent<Iceberg>();
		iceberg.Init( prep );
		
		for( var i = 0; i < iterations; i++ ) {
			iceberg.Mesh.SpawnTriangle();
		}
		
		iceberg.Mesh.Coerce( m_coercion.x, m_coercion.y );
		iceberg.Mesh.WriteToMesh();
		
		return iceberg;
	}
#endregion
	
	
#region Private
	private IcebergMesh _PrepareMesh() {
		var holder = new GameObject( "Iceberg" );
		var filter = holder.AddComponent<MeshFilter>();
		var collider = holder.AddComponent<MeshCollider>();
		var render = holder.AddComponent<MeshRenderer>();
		
		var mesh = new Mesh();
		filter.mesh = mesh;
		collider.sharedMesh = mesh;
		render.material = m_material;
		
		var mathMesh = new IcebergMesh( filter, collider, false );
		return mathMesh;
	}
	
	private void _GenIteration( IcebergMesh mesh ) {
		mesh.SpawnTriangle();
		mesh.WriteToMesh();
	}
#endregion
	
	
#region Temporary
#endregion
}
