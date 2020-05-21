using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Clutter;
using Clutter.Mesh;

public class IceGenerator : MonoSingular<IceGenerator> {
	protected override BehaviourSettings Behaviour { get { return new BehaviourSettings( false, true, true ); } }
	
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
	public static Iceberg GenerateNew( int iterations ) {
		var pivotObject = new GameObject( "Iceberg" );
		pivotObject.layer = Game.c_layerIceberg;
		
		var mainFilter = _SpawnMesh( "Visuals", pivotObject.transform );
		var skirtFilter = _SpawnMesh( "skirt", mainFilter.transform );
		var mesh = new IcebergMesh( mainFilter, skirtFilter, Skirt );
		
		var iceberg = pivotObject.AddComponent<Iceberg>();
		iceberg.Ignite( mesh );
		
		var genDog = new WatchDog( "Ice generation" );
		_FillIcebergMesh( iceberg.Mesh, iterations );
		genDog.Stop();
		return iceberg;
	}
	
	public static Iceberg SpawnSplit( MorphMesh driftMesh, Vector3 pivotPosition ) {
		var pivotObject = new GameObject( "Iceberg" );
		pivotObject.layer = Game.c_layerIceberg;
		pivotObject.transform.position = pivotPosition;
		
		var mainFilter = _SpawnMesh( "Visuals", pivotObject.transform );
		var skirtFilter = _SpawnMesh( "skirt", mainFilter.transform );
		var mesh = new IcebergMesh( driftMesh, mainFilter, skirtFilter, Skirt );
		
		var iceberg = pivotObject.AddComponent<Iceberg>();
		iceberg.Ignite( mesh );
		
		s_Instance.StartCoroutine( s_Instance._KillerRoutine( iceberg ) );	// TODO: rework iceberg kill mechanism, considering we can jump!
		return iceberg;
	}
#endregion
	
	
#region Private
	private static MeshFilter _SpawnMesh( string name, Transform parent ) {
		var meshObject = new GameObject( name );
		meshObject.layer = Game.c_layerIceberg;
		meshObject.transform.SetParent( parent, true );
		
		var filter = meshObject.AddComponent<MeshFilter>();
		filter.sharedMesh = new Mesh();
		meshObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
		meshObject.AddComponent<MeshRenderer>().material = Material;
		
		return filter;
	}
	
	private static void _FillIcebergMesh( IcebergMesh mesh, int iceIterations ) {
		// Setting up first tris:
		var a = Vector3.forward *(Random.Range( 0.9f, 1.1f ) *GenRadius);
		var b = Quaternion.AngleAxis( 120f, Vector3.up ) *Vector3.forward *(Random.Range( 0.9f, 1.1f ) *GenRadius);
		var c = Quaternion.AngleAxis( 240f, Vector3.up ) *Vector3.forward *(Random.Range( 0.9f, 1.1f ) *GenRadius);
		var firstTris = mesh.EmitTriangle( a, b, c );
		
		var selection = new Selection( mesh, firstTris );
		for( var i = 0; i < iceIterations; i++ ) {
			var outline = _Sequentialize( selection.OutlineEdges );
			var edgeIndex = Random.Range( 0, outline.Count );
			var edge = outline[edgeIndex];
			var previous = outline.RoundRobin( edgeIndex - 1 );
			var next = outline.RoundRobin( edgeIndex + 1 );
			
			var tris = _GrowFromEdge( mesh, edge, previous, next );
			selection.Add( tris );
		}
		
		mesh.Coerce( Coercion.x, Coercion.y );
		mesh.Write();
	}
	
	// Note: this is gonna fail if we have holes in our mesh!
	private static List<Edge> _Sequentialize( IEnumerable<Edge> edges ) {
		var work = new Queue<Edge>( edges );
		var result = new List<Edge>( work.Count );
		
		var lastEdge = work.Dequeue();
		result.Add( lastEdge );
		while( work.Count > 0 ) {
			var edge = work.Dequeue();
			if( edge.A == lastEdge.B ) {
				result.Add( edge );
				lastEdge = edge;
			}
			else {
				work.Enqueue( edge );
			}
		}
		
		return result;
	}
	
	private static Triangle _GrowFromEdge( IcebergMesh mesh, Edge edge, Edge previous, Edge next ) {
		var tangent = (Quaternion.AngleAxis( -90f, Vector3.up ) *edge.AB).normalized;
		
		// handling "degenerate" (non-convex) cases first
		if( Vector3.Dot( edge.AB, previous.BA ) > 0 && Vector3.Dot( tangent, previous.BA ) > 0 ) {
			return mesh.EmitTriangle( previous.A, edge.B, edge.A );
		}
		
		if( Vector3.Dot( edge.AB, next.AB ) < 0 && Vector3.Dot( tangent, next.AB ) > 0 ) {
			return mesh.EmitTriangle( edge.A, next.B, edge.B );
		}
		
		var tangentScale = GenRadius *Random.Range( 0.8f, 2f );
		var lateral = edge.AB *Random.value;
		
		var position = edge.A.Position + lateral + tangent *tangentScale;
		var vertex = mesh.EmitVertex( position );
		return mesh.EmitTriangle( edge.A, vertex, edge.B );
	}
	
	private IEnumerator _KillerRoutine( Iceberg iceberg ) {
		yield return new WaitForSeconds( 30 );
		Destroy( iceberg );
	}
#endregion
	
	
#region Temporary
#endregion
}
