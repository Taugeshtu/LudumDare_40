using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathMeshes;

public class IceGenerator : MonoSingular<IceGenerator> {
	[SerializeField] private Material m_material;
	[SerializeField] private float m_genRadius = 3f;
	[SerializeField] private float m_stitchRadius = 5f;
	[SerializeField] private Vector2 m_coercion = Vector2.up *0.5f;
	[SerializeField] private Vector3 m_skirt = Vector3.up *10;
	
	public static Material Material {
		get { return s_Instance.m_material; }
		set { s_Instance.m_material = value; }
	}
	public static float GenRadius {
		get { return s_Instance.m_genRadius; }
		set { s_Instance.m_genRadius = value; }
	}
	public static float StitchRadius {
		get { return s_Instance.m_stitchRadius; }
		set { s_Instance.m_stitchRadius = value; }
	}
	public static Vector2 Coercion {
		get { return s_Instance.m_coercion; }
		set { s_Instance.m_coercion = value; }
	}
	public static Vector3 Skirt {
		get { return s_Instance.m_skirt; }
		set { s_Instance.m_skirt = value; }
	}
	
#region Implementation
#endregion
	
	
#region Public
	public static Iceberg GenerateSplit( Vector3 pivotPosition, List<Triangle<SimpleVertex>> triangles ) {
		var iceberg = _PrepareIceberg( pivotPosition );
		
		// Note: dirty hacking here!
		iceberg.Mesh.RegisterTriangles( triangles );
		iceberg.Mesh.UnSkirt( Skirt );
		
		iceberg.Mesh.WeldVertices();
		iceberg.Mesh.ReSkirt( Skirt );
		iceberg.Mesh.MakeVerticesUnique();
		
		iceberg.Mesh.WriteToMesh();
		
		s_Instance.StartCoroutine( s_Instance._KillerRoutine( iceberg ) );
		return iceberg;
	}
	
	public static Iceberg Generate( int iterations ) {
		var iceberg = _PrepareIceberg( Vector3.zero );
		
		for( var i = 0; i < iterations; i++ ) {
			iceberg.Mesh.SpawnTriangle( GenRadius, StitchRadius );
		}
		
		iceberg.Mesh.Coerce( Coercion.x, Coercion.y );
		iceberg.Mesh.ReSkirt( Skirt );
		iceberg.Mesh.MakeVerticesUnique();
		iceberg.Mesh.WriteToMesh();
		
		return iceberg;
	}
#endregion
	
	
#region Private
	private static Iceberg _PrepareIceberg( Vector3 pivot ) {
		var pivotObject = new GameObject( "Iceberg" );
		pivotObject.layer = 8;
		pivotObject.transform.position = pivot;
		
		var meshObject = new GameObject( "Visuals" );
		meshObject.layer = 8;
		meshObject.transform.SetParent( pivotObject.transform, true );
		
		var filter = meshObject.AddComponent<MeshFilter>();
		var collider = meshObject.AddComponent<MeshCollider>();
		var render = meshObject.AddComponent<MeshRenderer>();
		
		var mesh = new Mesh();
		filter.mesh = mesh;
		collider.sharedMesh = mesh;
		render.material = Material;
		
		var mathMesh = new IcebergMesh( filter, collider, false );
		
		var iceberg = pivotObject.AddComponent<Iceberg>();
		iceberg.Ignite( mathMesh );
		return iceberg;
	}
	
	private IEnumerator _KillerRoutine( Iceberg iceberg ) {
		yield return new WaitForSeconds( 30 );
		Destroy( iceberg );
	}
#endregion
	
	
#region Temporary
#endregion
}
