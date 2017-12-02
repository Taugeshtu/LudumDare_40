using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IcebergMesh : MathMesh<SuperleggeraVertex> {
	private struct Edge {
		public Vertex<SuperleggeraVertex> V1;
		public Vertex<SuperleggeraVertex> V2;
		public Edge( Vertex<SuperleggeraVertex> v1, Vertex<SuperleggeraVertex> v2 ) {
			V1 = v1;
			V2 = v2;
		}
	}
	
	private List<Edge> m_outerEdges = new List<Edge>();
	// private List<Triangle<SuperleggeraVertex>> m_outerTris = new List<Triangle<SuperleggeraVertex>>();
	
#region Implementation
	public IcebergMesh( MeshFilter meshFilter, MeshCollider collider, bool immediate )
		: this( meshFilter, 1, collider, immediate ) {}
	public IcebergMesh( MeshFilter meshFilter, int submeshes = 1, MeshCollider collider = null, bool immediate = false )
		: base( meshFilter, submeshes, collider, immediate ) {}
#endregion
	
	
#region Public
	public void SpawwnTriangle() {
		var genRadius = 3f;
		var stitchRadius = 4.2f;
		
		if( m_outerEdges.Count == 0 ) {
			_SpawnFirst( genRadius );
			return;
		}
		
		var edge = m_outerEdges.Pick();
		_SpawnFrom( edge, genRadius, stitchRadius );
	}
#endregion
	
	
#region Private
	private void _SpawnFirst( float size ) {
		var radius = Vector3.forward *size;
		var vertices = new Vector3[] {
			radius,
			Quaternion.AngleAxis( 120, Vector3.up ) *radius,
			Quaternion.AngleAxis( -120, Vector3.up ) *radius
		};
		
		var tris = AddTriangle( vertices );
		
		m_outerEdges.Add( new Edge( tris.Vertices[0], tris.Vertices[1] ) );
		m_outerEdges.Add( new Edge( tris.Vertices[1], tris.Vertices[2] ) );
		m_outerEdges.Add( new Edge( tris.Vertices[2], tris.Vertices[0] ) );
		
		// m_outerTris.Add( tris );
		return;
	}
	
	private void _SpawnFrom( Edge edge, float genRadius, float searchRadius ) {
		var direction = (edge.V2.Position - edge.V1.Position);
		var midpoint = (edge.V1.Position + edge.V2.Position) /2;
		
		Draw.Circle( edge.V1.Position, Vector3.up, searchRadius, Palette.yellow, 24, 2f );
		Draw.Circle( edge.V2.Position, Vector3.up, searchRadius, Palette.orange, 24, 2f );
		Draw.RayFromTo( edge.V1.Position, edge.V2.Position, Palette.green, 2f, 2f );
		
		var distance = float.MaxValue;
		Vertex<SuperleggeraVertex> found = null;
		
		foreach( var outerEdge in m_outerEdges ) {
			_JumpPairs( edge, outerEdge,
				(vert) => {
					var cross = Vector3.Cross( direction, vert.Position - midpoint );
					if( Vector3.Angle( Vector3.up, cross ) < 30f ) {
						return;
					}
					
					var currentDistance = Vector3.Distance( midpoint, vert.Position );
					if( (currentDistance < distance) && (currentDistance < searchRadius) ) {
						distance = currentDistance;
						found = vert;
					}
					
					Draw.RayFromTo( midpoint, vert.Position, Palette.cyan, 0.5f, 2f );
					Draw.Ray( Vector3.zero, cross.normalized *2, Palette.red, 0.5f, 2f );
				}
			);
		}
		
		if( found != null ) {
			_Stitch( edge, found );
		}
		else {
			var cross2 = Vector3.Cross( direction, Vector3.up );
			cross2 = cross2.normalized *genRadius *Random.Range( 0.8f, 2f );
			cross2 += Random.insideUnitCircle.X0Y() *0.5f *genRadius;
			
			_Expand( edge, midpoint + cross2 );
			Draw.Ray( midpoint, cross2, Palette.violet, 0.5f, 2f );
		}
	}
	
	private void _Stitch( Edge edge, Vertex<SuperleggeraVertex> vert ) {
		Extensions.TimeLogError( "Found!" );
	}
	
	private void _Expand( Edge edge, Vector3 position ) {
		var newVertex = AddVertex( position );
		var vertices = new Vertex<SuperleggeraVertex>[] {
			edge.V1,
			newVertex,
			edge.V2
		};
		
		var tris = AddTriangle( vertices );
		
		m_outerEdges.Add( new Edge( edge.V1, newVertex ) );
		m_outerEdges.Add( new Edge( newVertex, edge.V2 ) );
		m_outerEdges.Remove( edge );
		
		// m_outerTris.Add( tris );
	}
	
	private void _JumpPairs( Edge a, Edge b, System.Action<Vertex<SuperleggeraVertex>> callback ) {
		var v1Valid = true;
		var v2Valid = true;
		
		if( (b.V1 == a.V1) || (b.V1 == a.V2) ) { v1Valid = false; }
		if( (b.V2 == a.V1) || (b.V2 == a.V2) ) { v2Valid = false; }
		
		if( v1Valid ) { callback( b.V1 ); }
		if( v2Valid ) { callback( b.V2 ); }
	}
#endregion
	
	
#region Temporary
#endregion
}
