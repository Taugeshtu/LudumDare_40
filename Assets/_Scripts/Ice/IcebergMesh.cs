using UnityEngine;
using System.Collections.Generic;

using Clutter.Mesh;

public class IcebergMesh : MorphMesh {
	private MorphMesh m_skirtMesh;
	private Vector3 m_skirtNormal;
	
	public bool ShouldDraw = false;
	
	public Collider Collider { get; private set; }
	
#region Implementation
	public IcebergMesh( Component mainTarget, Component skirtTarget, Vector3 skirtNormal ) : base( mainTarget ) {
		Collider = mainTarget.GetComponent<MeshCollider>();
		
		m_skirtNormal = skirtNormal;
		m_skirtMesh = new MorphMesh( skirtTarget );
	}
#endregion
	
	
#region Public
	public new void Write( GameObject target ) {
		Write( target.transform );
	}
	public new void Write( Component target = null ) {
		base.Write( target );
		
		_RegenerateSkirt();
		m_skirtMesh.Write( target );
	}
	
	public void Coerce( float min, float max ) {
		for( var index = 0; index < m_topVertexIndex; index++ ) {
			var heightChange = Random.Range( min, max );
			var newPosition = m_positions[index] + Vector3.up *heightChange;
			m_positions[index] = newPosition;
		}
	}
	
	public Selection Split( Vector3 position, Vector3 direction ) {
		var drifters = base.Slice( position, direction );
		// TODO: integrate logic with complex cracks here!
		
		return drifters;
		/*
		var replacements = new List<Triangle>();
		var inverted = false;
		var plane = _GetSplitPlane( position, direction, out inverted );
		
		WeldVertices();
		
		var trisCopy = new List<Triangle>( m_triangles );
		var drifters = new List<Triangle>();
		
		var tangent = Vector3.right;
		Vector3.OrthoNormalize( ref direction, ref tangent );
		
		Triangle startTris = null;
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
		
		replacements = new List<Triangle>( Optimize( replacements ) );
		foreach( var newTriangle in replacements ) {
			AddTriangle( newTriangle );
		}
		
		SyncVerticesToTriangles();
		
		return drifters;
		*/
	}
	
	private void _RegenerateSkirt( bool bothSides = false ) {
		
	}
	
	/*
	private List<Triangle> _FilterCrack( Vector3 point, Vector3 originalDirection,
					Triangle parent,
					List<Triangle> triangles ) {
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
		
		var joinedTriangles = new List<Triangle>();
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
	
	private void _CrackSide( Triangle parent, Vector3 point, Vector3 originalDirection,
					List<Triangle> triangles,
					List<Triangle> drifters,
					List<Triangle> replacements ) {
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
	*/
#endregion
	
	
#region Private
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
