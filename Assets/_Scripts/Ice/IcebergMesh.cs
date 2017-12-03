using UnityEngine;
using System.Collections.Generic;
using MathMeshes;

public class IcebergMesh : BigMesh<SimpleVertex> {
	private List<Edge<SimpleVertex>> m_outerEdges = new List<Edge<SimpleVertex>>();
	
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
			var heightChange = Random.Range( min, max );
			var newPosition = vertex.Position + Vector3.up *heightChange;
			vertex.Properties.Position = newPosition;
		}
	}
	
	public void RegisterTriangles( List<Triangle<SimpleVertex>> triangles ) {
		foreach( var tris in triangles ) {
			AddTriangle( tris );
		}
	}
	
	public List<Triangle<SimpleVertex>> Split( Vector3 position, Vector3 direction ) {
		var replacements = new List<Triangle<SimpleVertex>>();
		var inverted = false;
		var plane = _GetSplitPlane( position, direction, out inverted );
		
		WeldVertices();
		
		var trisCopy = new List<Triangle<SimpleVertex>>( m_triangles );
		var drifters = new List<Triangle<SimpleVertex>>();
		
		var tangent = Vector3.right;
		Vector3.OrthoNormalize( ref direction, ref tangent );
		
		Triangle<SimpleVertex> startTris = null;
		var minDistance = float.MaxValue;
		var ideal = Vector3.Project( position, tangent );
		foreach( var tris in trisCopy ) {
			if( tris._GetSide( ref plane, inverted ) == 0 ) {
				var projection = Vector3.Project( tris.Center, tangent );
				var distance = (projection - ideal).magnitude;
				if( distance < minDistance ) {
					minDistance = distance;
					startTris = tris;
				}
			}
		}
		
		if( startTris == null ) {
			return drifters;
		}
		
		var bendLeft = Dice.Roll( 0.25f ) ? 30f : 30f;
		var bendRight = Dice.Roll( 0.25f ) ? -30f : -30f;
		var directionLeft = direction;
		var directionRight = direction;
		
		var section = new Vector3[2];
		drifters.AddRange( startTris.TrySplit( ref plane, inverted, replacements, out section ) );
		
		trisCopy.Remove( startTris );
		foreach( var startTrisReplacement in replacements ) { trisCopy.Remove( startTrisReplacement ); }
		trisCopy = _FilterCrack( position, direction, startTris, trisCopy );
		
		_CrackSide( startTris, section[0], directionLeft, trisCopy, drifters, replacements );
		_CrackSide( startTris, section[1], directionRight, trisCopy, drifters, replacements );
		
		foreach( var leftover in trisCopy ) {
			DestroyTriangle( leftover );
			Draw.Cross( leftover.Center, Palette.green, 1, 2 );
		}
		drifters.AddRange( trisCopy );
		
		replacements = new List<Triangle<SimpleVertex>>( Optimize( replacements ) );
		foreach( var newTriangle in replacements ) {
			AddTriangle( newTriangle );
		}
		
		SyncVerticesToTriangles();
		
		return drifters;
	}
	
	public void UnSkirt( Vector3 forNormal ) {
		var toRemove = new List<Triangle<SimpleVertex>>();
		foreach( var tris in m_triangles ) {
			if( Vector3.Angle( tris.Normal, forNormal ) > 60 ) {
				toRemove.Add( tris );
			}
		}
		
		foreach( var tris in toRemove ) {
			DestroyTriangle( tris );
		}
	}
	
	public void ReSkirt( Vector3 forNormal ) {
		var outline = GetOutline( m_triangles );
		foreach( var edge in outline ) {
			// edge.DrawMe( Palette.yellow, 1, 2 );
			
			var a = new Vertex<SimpleVertex>( edge.V1, null );
			var b = new Vertex<SimpleVertex>( edge.V2, null );
			var c = new Vertex<SimpleVertex>( a, null );
			var d = new Vertex<SimpleVertex>( b, null );
			c.Properties.Position = c.Position - forNormal;
			d.Properties.Position = d.Position - forNormal;
			
			var vertices1 = new Vertex<SimpleVertex>[] { a, b, d };
			var vertices2 = new Vertex<SimpleVertex>[] { a, d, c };
			
			// HACK- adding inverse triangles
			var vertices3 = new Vertex<SimpleVertex>[] { a, d, b };
			var vertices4 = new Vertex<SimpleVertex>[] { a, c, d };
			
			var t1 = _EmitTriangle( vertices1 );
			var t2 = _EmitTriangle( vertices2 );
			
			AddTriangle( t1 );
			AddTriangle( t2 );
		}
	}
	
	private List<Triangle<SimpleVertex>> _FilterCrack( Vector3 point, Vector3 originalDirection,
					Triangle<SimpleVertex> parent,
					List<Triangle<SimpleVertex>> triangles ) {
		var inverted = false;
		var plane = _GetSplitPlane( point, originalDirection, out inverted );
		
		for( var i = triangles.Count - 1; i >= 0; i-- ) {
			var tris = triangles[i];
			var siding = tris._GetSide( ref plane, inverted );
			if( siding == -1 ) {
				// Draw.Cross( tris.Center, Palette.red, 3, 2 );
				triangles.RemoveAt( i );
			}
		}
		
		var joinedTriangles = new List<Triangle<SimpleVertex>>();
		joinedTriangles.Add( parent );
		
		var foundJoin = true;
		while( foundJoin ) {
			for( var a = 0; a < joinedTriangles.Count; a++ ) {
				var joined = joinedTriangles[a];
				foundJoin = false;
				
				for( var b = triangles.Count - 1; b >= 0; b-- ) {
					var tris = triangles[b];
					if( tris.SharesSide( joined ) ) {
						triangles.RemoveAt( b );
						joinedTriangles.Add( tris );
						foundJoin = true;
						
						// tris.DrawMe( Palette.violet );
						break;
					}
				}
				if( foundJoin ) { break; }
			}
		}
		
		joinedTriangles.Remove( parent );
		return joinedTriangles;
	}
	
	private void _CrackSide( Triangle<SimpleVertex> parent, Vector3 point, Vector3 originalDirection,
					List<Triangle<SimpleVertex>> triangles,
					List<Triangle<SimpleVertex>> drifters,
					List<Triangle<SimpleVertex>> replacements ) {
		var inverted = false;
		var plane = _GetSplitPlane( point, originalDirection, out inverted );
		
		for( var i = triangles.Count - 1; i >= 0; i-- ) {
			var tris = triangles[i];
			var siding = tris._GetSide( ref plane, inverted );
			
			if( siding == 0 ) {
				if( tris.SharesSide( parent ) ) {
					triangles.RemoveAt( i );
					
					var section = new Vector3[2];
					drifters.AddRange( tris.TrySplit( ref plane, inverted, replacements, out section ) );
					
					var newPoint = section[0].EpsilonEquals( point ) ? section[1] : section[0];
					_CrackSide( tris, newPoint, originalDirection, triangles, drifters, replacements );
					break;
				}
			}
		}
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
		
		m_outerEdges.Add( new Edge<SimpleVertex>( tris.Vertices[0], tris.Vertices[1] ) );
		m_outerEdges.Add( new Edge<SimpleVertex>( tris.Vertices[1], tris.Vertices[2] ) );
		m_outerEdges.Add( new Edge<SimpleVertex>( tris.Vertices[2], tris.Vertices[0] ) );
		
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
	
	private void _SpawnFrom( Edge<SimpleVertex> edge, float genRadius, float searchRadius ) {
		var direction = (edge.V2.Position - edge.V1.Position);
		var midpoint = edge.Midpoint;
		
		Vertex<SimpleVertex> foundVertex = null;
		Edge<SimpleVertex> foundEdge = null;
		
		var shouldFlip = _TryFindAngleStitch( edge, ref foundEdge, ref foundVertex );
		if( foundVertex == null ) {
			shouldFlip = _TryFindDistanceStitch( edge, searchRadius, ref foundEdge, ref foundVertex );
		}
		
		if( foundVertex != null ) {
			if( shouldFlip ) {
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
	
	private bool _TryFindAngleStitch( Edge<SimpleVertex> edge, ref Edge<SimpleVertex> foundEdge, ref Vertex<SimpleVertex> foundVertex ) {
		var direction = edge.Direction;
		var midpoint = edge.Midpoint;
		var shouldFlip = false;
		
		foreach( var outerEdge in m_outerEdges ) {
			if( outerEdge.SharedVertexCount( edge ) != 1 ) {
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
	
	private bool _TryFindDistanceStitch( Edge<SimpleVertex> edge, float searchRadius, ref Edge<SimpleVertex> foundEdge, ref Vertex<SimpleVertex> foundVertex ) {
		var direction = edge.Direction;
		var midpoint = edge.Midpoint;
		var distance = float.MaxValue;
		var shouldFlip = false;
		
		foreach( var outerEdge in m_outerEdges ) {
			if( outerEdge.SharedVertexCount( edge ) != 1 ) {
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
	
	private void _Stitch( Edge<SimpleVertex> edge, Edge<SimpleVertex> stitchTo, Vertex<SimpleVertex> byVertex ) {
		if( ShouldDraw ) {
			Draw.RayFromTo( edge.Midpoint, byVertex.Position, Palette.cyan, 0.5f, 2f );
		}
		
		var otherNonShared = edge.NonSharedVertex( stitchTo );
		var vertices = new Vertex<SimpleVertex>[] {
			edge.V1,
			byVertex,
			edge.V2
		};
		
		var tris = _EmitTriangle( vertices );
		AbortMeshWriting();
		m_triangles.Add( tris );
		
		m_outerEdges.Add( new Edge<SimpleVertex>( byVertex, otherNonShared ) );
		m_outerEdges.Remove( edge );
		m_outerEdges.Remove( stitchTo );
	}
	
	private void _Expand( Edge<SimpleVertex> edge, Vector3 position ) {
		var newVertex = _EmitVertex( position );
		var vertices = new Vertex<SimpleVertex>[] {
			edge.V1,
			newVertex,
			edge.V2
		};
		
		var tris = _EmitTriangle( vertices );
		m_vertices.Add( newVertex );
		AbortMeshWriting();
		m_triangles.Add( tris );
		
		m_outerEdges.Add( new Edge<SimpleVertex>( edge.V1, newVertex ) );
		m_outerEdges.Add( new Edge<SimpleVertex>( newVertex, edge.V2 ) );
		m_outerEdges.Remove( edge );
	}
	
	private Plane _GetSplitPlane( Vector3 position, Vector3 direction, out bool isInverted ) {
		isInverted = false;
		var shift = Vector3.Project( position, direction );
		var plane = new Plane( shift, -shift.magnitude );
		if( Vector3.Angle( shift, direction ) > 90 ) {	// Meaning we're beyond zer0
			isInverted = true;
		}
		return plane;
	}
#endregion
	
	
#region Temporary
#endregion
}
