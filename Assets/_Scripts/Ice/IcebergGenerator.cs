using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IcebergGenerator : MonoBehaviour {
	[SerializeField] private Material m_material;
	
	private Iceberg m_generated;
	
#region Implementation
	void Update() {
		if( m_generated == null ) {
			m_generated = Generate();
		}
		
		if( Input.GetKeyDown( KeyCode.G ) ) {
			_GenIteration( m_generated.Mesh );
		}
	}
#endregion
	
	
#region Public
	public Iceberg Generate() {
		var prep = _PrepareMesh();
		var iceberg = prep.Filter.gameObject.AddComponent<Iceberg>();
		iceberg.Init( prep );
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
		mesh.SpawwnTriangle();
		mesh.WriteToMesh();
	}
#endregion
	
	
#region Temporary
#endregion
}
