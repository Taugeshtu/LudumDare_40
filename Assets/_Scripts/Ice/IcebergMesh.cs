﻿using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IcebergMesh : MathMesh<SuperleggeraVertex> {
	private struct Edge {
		public Vertex<SuperleggeraVertex> V1;
		public Vertex<SuperleggeraVertex> V2;
		
		public Vector3 Direction { get { return (V2.Position - V1.Position); } } 
		public Vector3 Midpoint { get { return (V1.Position + V2.Position) /2; } }
		
		public Edge( Vertex<SuperleggeraVertex> v1, Vertex<SuperleggeraVertex> v2 ) {
			V1 = v1;
			V2 = v2;
		}
		
		public bool SharesVertex( Edge other ) {
			var sharesV1 = false;
			var sharesV2 = false;
			if( V1 == other.V1 || V1 == other.V2 ) { sharesV1 = true; }
			if( V2 == other.V1 || V2 == other.V2 ) { sharesV2 = true; }
			
			if( sharesV1 && sharesV2 ) { return false; }
			if( sharesV1 || sharesV2 ) { return true; }
			return false;
		}
		
		public Vertex<SuperleggeraVertex> NonSharedVertex( Edge other ) {
			// Note: this code assumes there's exactly 1 shared vertex
			if( V1 == other.V1 || V1 == other.V2 ) { return V2; }
			if( V2 == other.V1 || V2 == other.V2 ) { return V1; }
			return null;
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
		_Draw();
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
	
	private void _Draw() {
		foreach( var edge in m_outerEdges ) {
			Draw.RayFromTo( edge.V1.Position, edge.V2.Position, Palette.aquamarine, 1f, 2f );
		}
	}
	
	private void _SpawnFrom( Edge edge, float genRadius, float searchRadius ) {
		var direction = (edge.V2.Position - edge.V1.Position);
		var midpoint = edge.Midpoint;
		
		// Draw.Circle( edge.V1.Position, Vector3.up, searchRadius, Palette.yellow, 24, 2f );
		// Draw.Circle( edge.V2.Position, Vector3.up, searchRadius, Palette.orange, 24, 2f );
		// Draw.RayFromTo( edge.V1.Position, edge.V2.Position, Palette.green, 2f, 2f );
		
		Vertex<SuperleggeraVertex> foundVertex = null;
		Edge foundEdge = new Edge();
		
		_TryFindAngleStitch( ref edge, ref foundEdge, ref foundVertex );
		if( foundVertex == null ) {
			_TryFindDistanceStitch( edge, searchRadius, ref foundEdge, ref foundVertex );
		}
		
		if( foundVertex != null ) {
			_Stitch( edge, foundEdge, foundVertex );
		}
		else {
			var cross2 = Vector3.Cross( direction, Vector3.up );
			cross2 = cross2.normalized *genRadius *Random.Range( 0.8f, 2f );
			cross2 += Random.insideUnitCircle.X0Y() *0.5f *genRadius;
			
			_Expand( edge, midpoint + cross2 );
			Draw.Ray( midpoint, cross2, Palette.violet, 0.5f, 2f );
		}
	}
	
	private void _TryFindAngleStitch( ref Edge edge, ref Edge foundEdge, ref Vertex<SuperleggeraVertex> foundVertex ) {
		var direction = edge.Direction;
		var midpoint = edge.Midpoint;
		
		foreach( var outerEdge in m_outerEdges ) {
			if( !outerEdge.SharesVertex( edge ) ) {
				continue;
			}
			
			var angle = Vector3.Angle( edge.Direction, outerEdge.Direction );
			Draw.RayFromTo( edge.V1.Position, edge.V2.Position, Palette.yellow, 1f, 2f );
			
			if( angle > 130f ) {
				foundVertex = outerEdge.NonSharedVertex( edge );
				foundEdge = outerEdge;
				
				var vertex = outerEdge.NonSharedVertex( edge );
				var cross = Vector3.Cross( direction, vertex.Position - midpoint );
				if( Vector3.Angle( Vector3.up, cross ) < 30f ) {
					foundEdge = edge;
					edge = outerEdge;
				}
				Draw.RayFromTo( edge.Midpoint, foundVertex.Position, Palette.red, 1f, 2f );
				return;
			}
		}
	}
	
	private void _TryFindDistanceStitch( Edge edge, float searchRadius, ref Edge foundEdge, ref Vertex<SuperleggeraVertex> foundVertex ) {
		var direction = edge.Direction;
		var midpoint = edge.Midpoint;
		var distance = float.MaxValue;
		
		foreach( var outerEdge in m_outerEdges ) {
			if( !outerEdge.SharesVertex( edge ) ) {
				continue;
			}
			
			var vertex = outerEdge.NonSharedVertex( edge );
			var cross = Vector3.Cross( direction, vertex.Position - midpoint );
			if( Vector3.Angle( Vector3.up, cross ) < 30f ) {
				continue;
			}
			
			var currentDistance = Vector3.Distance( midpoint, vertex.Position );
			if( (currentDistance < distance) && (currentDistance < searchRadius) ) {
				distance = currentDistance;
				foundVertex = vertex;
				foundEdge = outerEdge;
			}
		}
	}
	
	private void _Stitch( Edge edge, Edge stitchTo, Vertex<SuperleggeraVertex> byVertex ) {
		Extensions.TimeLogError( "[Stitching]" );
		Draw.RayFromTo( edge.Midpoint, byVertex.Position, Palette.cyan, 0.5f, 2f );
		
		var otherNonShared = edge.NonSharedVertex( stitchTo );
		var vertices = new Vertex<SuperleggeraVertex>[] {
			edge.V1,
			byVertex,
			edge.V2
		};
		
		var tris = AddTriangle( vertices );
		
		m_outerEdges.Add( new Edge( byVertex, otherNonShared ) );
		m_outerEdges.Remove( edge );
		m_outerEdges.Remove( stitchTo );
	}
	
	private void _Expand( Edge edge, Vector3 position ) {
		Extensions.TimeLogError( "[Expanding]" );
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
#endregion
	
	
#region Temporary
#endregion
}
