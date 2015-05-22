//
// The component that manages the ball.
//

using System.Collections;
using UnityEngine;

public class GGBallComponent: MonoBehaviour {
	/* Initializing. */
	
	public void Start() {
		this.rigidbody2D          = this.GetComponent<Rigidbody2D>();
		this.shotAudioSource      = this.transform.Find("Shot Audio").GetComponent<AudioSource>();
		this.collisionAudioSource = this.transform.Find("Collision Audio").GetComponent<AudioSource>();
	}
	
	/* Configuring the component. */
	
	public float mediumShotForce    = 10.0f;
	public float bigShotForce       = 19.0f;
	public float shotPitchVariation = 0.1f;
	
	public float collisionTriggerForce   = 2.0f;
	public float minCollisionVolumeForce = 2.0f;
	public float maxCollisionVolumeForce = 10.0f;
	public float minCollisionVolume      = 0.1f;
	public float maxCollisionVolume      = 1.0f;
	public float collisionPitchVariation = 0.2f;
	
	public float grassPitchVariation = 0.0f;
	public float rockPitchVariation  = 0.0f;
	public float dirtPitchVariation  = 0.0f;
	public float sandPitchVariation  = 0.0f;
	public float sheepPitchVariation = 0.0f;
	
	public AudioClip[] smallShotAudioClips;
	public AudioClip[] mediumShotAudioClips;
	public AudioClip[] bigShotAudioClips;
	
	public AudioClip[] grassAudioClips;
	public AudioClip[] rockAudioClips;
	public AudioClip[] dirtAudioClips;
	public AudioClip[] sandAudioClips;
	public AudioClip[] sheepAudioClips;
	
	/* Accessing components. */
	
	new private Rigidbody2D rigidbody2D;
	private AudioSource shotAudioSource;
	private AudioSource collisionAudioSource;
	
	/* Getting information about the ball. */
	
	// The hole object that this ball is contained in, if any.
	public GameObject containingHole { get; private set; }
	
	// The hole that the ball was most recently in, if any.
	public GameObject mostRecentHole { get; private set; }
	
	// Whether or not the ball is inside of a hole right now.
	public bool isInHole { get {
		return this.containingHole != null;
	} }
	
	/* Shooting the ball. */
	
	public void Shoot(Vector2 force) {
		var magnitude                         = force.magnitude;
		this.durationUnderForceSleepThreshold = 0.0f;
		this.rigidbody2D.isKinematic          = false;
		this.undoPosition                     = this.transform.position;
		this.shotAudioSource.pitch            = 1.0f + Random.Range(-this.shotPitchVariation, this.shotPitchVariation);
		
		if (magnitude >= this.bigShotForce) {
			this.shotAudioSource.clip = this.GetRandomAudioClip(this.bigShotAudioClips);
		}
		else if (magnitude >= this.mediumShotForce) {
			this.shotAudioSource.clip = this.GetRandomAudioClip(this.mediumShotAudioClips);
		}
		else {
			this.shotAudioSource.clip = this.GetRandomAudioClip(this.smallShotAudioClips);
		}
		
		this.shotAudioSource.Play();
		this.shouldCancelNextCollisionAudioEvent = true;
		this.rigidbody2D.AddForce(force, ForceMode2D.Impulse);
	}
	
	private void BounceFromHole() {
		this.timeToPlugHole                   = 0.4f;
		this.durationUnderForceSleepThreshold = 0.0f;
		this.rigidbody2D.isKinematic          = false;
		this.rigidbody2D.AddForce(new Vector2(0.0f, 5.0f), ForceMode2D.Impulse);
	}
	
	/* Undoing the ball's position. */
	
	// The world-space position that the ball should be reverted to if RestoreUndoPosition is
	// called. This should be set before hitting the ball in case it goes off screen.
	public Vector3 undoPosition;
	
	public bool canRestoreUndoPosition { get {
 		return this.undoPosition != new Vector3() && (this.transform.position - this.undoPosition).sqrMagnitude > 0.5f;
	} }
	
	public void RestoreUndoPosition() {
		var position                          = this.undoPosition;
		position.y                           += 0.275f;
		this.transform.position               = position;
		this.rigidbody2D.velocity             = new Vector2();
		this.rigidbody2D.isKinematic          = false;
		this.durationUnderForceSleepThreshold = 0.0f;
 	}
 	
 	public void RestoreUndoPositionIfPossible() {
 		if (this.canRestoreUndoPosition) {
 			this.RestoreUndoPosition();
 		}
 	}
 	
 	public void RestoreUndoPositionOrGameOver() {
 		if (GGGameSceneComponent.mode == GGGameMode.Hard) {
 			GGGameSceneComponent.instance.GameOverMan();
 		}
 		else {
 			this.RestoreUndoPosition();
 		}
 	}
	
	/* Persisting the ball's position. */
	
	// Saves the ball's current position in PlayerPrefs. Can later call LoadPersistedPosition to
	// restore the position.
	public void PersistPosition() {
		var yOffset = -GGGameSceneComponent.instance.mapComponent.yBottom;
		
		if (this.isInHole) {
			yOffset += 0.6f;
		}
		
		GGSaveData.SetBallX(this.transform.position.x);
		GGSaveData.SetBallY(this.transform.position.y + yOffset);
	}
	
	// Resets the ball's position to the previously-saved position, if any.
	public void LoadPersistedPosition() {
		if (GGSaveData.HasBallPosition()) {
			var position            = this.transform.position;
			position.x              = GGSaveData.GetBallX();
			position.y              = GGSaveData.GetBallY() + GGGameSceneComponent.instance.mapComponent.yBottom;
			this.transform.position = position;
		}
	}
	
	/* Responding to events. */
	
	private bool shouldCancelNextCollisionAudioEvent = false;
	
	public void OnCollisionEnter2D(Collision2D collision) {
		if (collision.relativeVelocity.magnitude > this.collisionTriggerForce) {
			if (this.shouldCancelNextCollisionAudioEvent) {
				this.shouldCancelNextCollisionAudioEvent = false;
			}
			else {
				var colliderName                 = collision.collider.gameObject.name;
				var magnitude                    = collision.relativeVelocity.magnitude;
				var forceRange                   = this.maxCollisionVolumeForce - this.minCollisionVolumeForce;
				var volumeRange                  = this.maxCollisionVolume - this.minCollisionVolume;
				var pitchVariation               = this.collisionPitchVariation;
				this.collisionAudioSource.volume = this.minCollisionVolume + Mathf.Clamp01((magnitude - this.minCollisionVolumeForce) / forceRange) * volumeRange;
				
				switch (colliderName) {
					case "Ground": this.collisionAudioSource.clip = this.GetRandomAudioClip(this.grassAudioClips); pitchVariation += this.grassPitchVariation; break;
					case "Wall":   this.collisionAudioSource.clip = this.GetRandomAudioClip(this.rockAudioClips);  pitchVariation += this.rockPitchVariation;  break;
					case "Dirt":   this.collisionAudioSource.clip = this.GetRandomAudioClip(this.dirtAudioClips);  pitchVariation += this.dirtPitchVariation;  break;
					case "Grass":  this.collisionAudioSource.clip = this.GetRandomAudioClip(this.grassAudioClips); pitchVariation += this.grassPitchVariation; break;
					case "Sand":   this.collisionAudioSource.clip = this.GetRandomAudioClip(this.sandAudioClips);  pitchVariation += this.sandPitchVariation;  break;
					case "Rock":   this.collisionAudioSource.clip = this.GetRandomAudioClip(this.rockAudioClips);  pitchVariation += this.rockPitchVariation;  break;
					case "Sheep":  this.collisionAudioSource.clip = this.GetRandomAudioClip(this.sheepAudioClips); pitchVariation += this.sheepPitchVariation; break;
					case "Plug":   this.collisionAudioSource.clip = this.GetRandomAudioClip(this.rockAudioClips);  pitchVariation += this.rockPitchVariation;  break;
					default:       Debug.LogError("Audio event encountered unhandled collider name " + colliderName + "."); break;
				}
				
				this.collisionAudioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
				this.collisionAudioSource.Play();
			}
		}
	}
	
	public void OnTriggerEnter2D(Collider2D collider) {
		this.containingHole = collider.gameObject;
		this.mostRecentHole = this.containingHole;
	}
	
	public void OnTriggerStay2D(Collider2D collider) {
		this.containingHole = collider.gameObject;
		this.mostRecentHole = this.containingHole;
	}
	
	public void DidFinishTransitioningToMap() {
		this.BounceFromHole();
	}
	
	/* Getting audio clips. */
	
	private System.Random random = new System.Random();
	
	private AudioClip GetRandomAudioClip(AudioClip[] clips) {
		return clips[random.Next(clips.Length)];
	}
	
	/* Updating. */
	
	private float durationUnderForceSleepThreshold = 0.0f;
	private float durationOffScreen                = 0.0f;
	private float timeToPlugHole                   = 0.0f;
	
	public void FixedUpdate() {
		var screenPosition = Camera.main.WorldToViewportPoint(this.transform.position);
		
		if (!this.rigidbody2D.isKinematic) {
			if (this.rigidbody2D.velocity.magnitude < 0.15f) {
				this.durationUnderForceSleepThreshold += Time.deltaTime;
				
				if (this.durationUnderForceSleepThreshold > 0.4f) {
					this.rigidbody2D.isKinematic = true;
					this.PersistPosition();
				}
			}
			else {
				this.durationUnderForceSleepThreshold = 0.0f;
			}
			
			if (screenPosition.y < 0.0f) {
				this.durationOffScreen += Time.deltaTime;
				
				if (durationOffScreen > 1.0f) {
					this.RestoreUndoPositionOrGameOver();
					this.durationOffScreen = 0.0f;
				}
			}
			else {
				this.durationOffScreen = 0.0f;
			}
		}
		else {
			var isOffScreenHorizontally = (
				screenPosition.x < -GGMapComponent.mapWidth / 2.0f ||
				screenPosition.x >  GGMapComponent.mapWidth / 2.0f
			);
			
			var isOffScreenVertically = (
				screenPosition.y < 0.0f ||
				screenPosition.y > GGMapComponent.screenHeight
			);
			
			if (isOffScreenHorizontally || isOffScreenVertically) {
				this.RestoreUndoPositionOrGameOver();
			}
		}
		
		this.containingHole = null;
		
		if (this.timeToPlugHole > 0.0f) {
			this.timeToPlugHole = Mathf.Max(0.0f, this.timeToPlugHole - Time.deltaTime);
			
			if (this.timeToPlugHole == 0.0f) {
				this.mostRecentHole.GetComponent<GGHoleComponent>().Plug();
			}
		}
	}
}
