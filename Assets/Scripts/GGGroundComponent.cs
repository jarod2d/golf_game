//
// The component that manages the ground object.
//

using System.Collections;
using UnityEngine;

public class GGGroundComponent: MonoBehaviour {
	/* Initializing. */
	
	public void Start() {
		this.collider2D.sharedMaterial = GGGameSceneComponent.instance.physicsComponent.grassMaterial;
		this.renderer.material.color   = GGGroundComponent.color;
	}
	
	/* Getting information about the ground. */
	
	public float height { get {
		return this.collider2D.bounds.size.y;
	} }
	
	/* Getting configuration values. */
	
	public static Color color = new Color(0.06f, 0.69f, 0.42f);
}
