using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IcebergMesh : MathMesh<SimpleVertex> {
	private struct Edge {
		public Vertex<SimpleVertex> V1;
		public Vertex<SimpleVertex> V2;
		
		public Vector3 Direction { get { return (V2.Position - V1.Position); } } 
		public Vector3 Midpoint { get { return (V1.Position + V2.Position) /2; } }
		
		public Edge( Vertex<SimpleVertex> v1, Vertex<SimpleVertex> v2 ) {
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
		
		public Vertex<SimpleVertex> NonSharedVertex( Edge other ) {
			// Note: this code assumes there's exactly 1 shared vertex
			if( V1 == other.V1 || V1 == other.V2 ) { return V2; }
			if( V2 == other.V1 || V2 == other.V2 ) { return V1; }
			return null;
		}
	}
	
	private List<Edge> m_outerEdges = new List<Edge>();
	// private List<Triangle<SuperleggeraVertex>> m_outerTris = new List<Triangle<SuperleggeraVertex>>();
	
	public bool ShouldDraw = false;
	
#region Implementation
	public IcebergMesh( MeshFilter meshFilter, MeshCollider collider, bool immediate )
		: this( meshFilter, 1, collider, immediate ) {}
	public IcebergMesh( MeshFilter meshFilter, int submeshes = 1, MeshCollider collider = null, bool immediate = false )
		: base( meshFilter, submeshes, collider, immediate ) {}
#endregion
	
	
#region Public
	public void SpawnTriangle( float genRadius, float stitchRadius ) {
		if( m_outerEdges.Count == 0 ) {
			_SpawnFirst( genRadius );
			return;
		}
		
		var edge = m_outerEdges.Pick();
		_SpawnFrom( edge, genRadius, stitchRadius );
		_Draw();
	}
	
	public void Coerce( float min, float max ) {
		foreach( var vertex in m_vertices ) {
			var height = Random.Range( min, max );
			vertex.Properties.Position = vertex.Position.WithY( height );
		}
		
		SplitTriangles();
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
		if( !ShouldDraw ) {
			return;
		}
		
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
		
		Vertex<SimpleVertex> foundVertex = null;
		Edge foundEdge = new Edge();
		
		var shouldFlip = _TryFindAngleStitch( edge, ref foundEdge, ref foundVertex );
		if( foundVertex == null ) {
			shouldFlip = _TryFindDistanceStitch( edge, searchRadius, ref foundEdge, ref foundVertex );
		}
		
		if( foundVertex != null ) {
			if( shouldFlip ) {
				// Extensions.TimeLogError( "Flipping burgers" );
				foundVertex = edge.NonSharedVertex( foundEdge );
				var savedEdge = edge;
				edge = foundEdge;
				foundEdge = savedEdge;
			}
			
			_Stitch( edge, foundEdge, foundVertex );
		}
		else {
			var cross2 = Vector3.Cross( direction, Vector3.up );
			cross2 = cross2.normalized *genRadius *Random.Range( 0.8f, 2f );
			cross2 += Random.insideUnitCircle.X0Y() *0.5f *genRadius;
			
			_Expand( edge, midpoint + cross2 );
			
			if( ShouldDraw ) {
				Draw.Ray( midpoint, cross2, Palette.violet, 0.5f, 2f );
			}
		}
	}
	
	private bool _TryFindAngleStitch( Edge edge, ref Edge foundEdge, ref Vertex<SimpleVertex> foundVertex ) {
		var direction = edge.Direction;
		var midpoint = edge.Midpoint;
		var shouldFlip = false;
		
		foreach( var outerEdge in m_outerEdges ) {
			if( !outerEdge.SharesVertex( edge ) ) {
				continue;
			}
			
			var vertex = outerEdge.NonSharedVertex( edge );
			var vertices = new Vertex<SimpleVertex>[] {
				edge.V1,
				edge.V2,
				vertex
			};
			
			var trianglePresent = false;
			foreach( var tris in m_triangles ) {
				if( tris.HasVertices( vertices ) ) {
					trianglePresent = true;
					break;
				}
			}
			
			if( trianglePresent ) {
				continue;
			}
			
			var angle = Vector3.Angle( edge.Direction, outerEdge.Direction );
			
			if( ShouldDraw ) {
				Draw.RayFromTo( edge.V1.Position, edge.V2.Position, Palette.yellow, 1f, 2f );
			}
			
			// Note: bad blood if the tris already exists between the two.
			if( angle > 130f ) {
				foundVertex = vertex;
				foundEdge = outerEdge;
				
				if( vertex == outerEdge.V2 ) {
					shouldFlip = true;
				}
				if( ShouldDraw ) {
					Draw.RayFromTo( edge.Midpoint, foundVertex.Position, Palette.red, 1f, 2f );
				}
				return shouldFlip;
			}
		}
		return shouldFlip;
	}
	
	private bool _TryFindDistanceStitch( Edge edge, float searchRadius, ref Edge foundEdge, ref Vertex<SimpleVertex> foundVertex ) {
		var direction = edge.Direction;
		var midpoint = edge.Midpoint;
		var distance = float.MaxValue;
		var shouldFlip = false;
		
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
				
				if( vertex == outerEdge.V2 ) {
					shouldFlip = true;
				}
			}
		}
		
		return shouldFlip;
	}
	
	private void _Stitch( Edge edge, Edge stitchTo, Vertex<SimpleVertex> byVertex ) {
		// Extensions.TimeLogError( "[Stitching]" );
		if( ShouldDraw ) {
			Draw.RayFromTo( edge.Midpoint, byVertex.Position, Palette.cyan, 0.5f, 2f );
		}
		
		var otherNonShared = edge.NonSharedVertex( stitchTo );
		var vertices = new Vertex<SimpleVertex>[] {
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
		// Extensions.TimeLogError( "[Expanding]" );
		var newVertex = AddVertex( position );
		var vertices = new Vertex<SimpleVertex>[] {
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
