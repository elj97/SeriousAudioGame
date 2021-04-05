/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2021
 *	
 *	"Moveable_PickUp.cs"
 * 
 *	Attaching this script to a GameObject allows it to be
 *	picked up and manipulated freely by the player.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Attaching this component to a GameObject allows it to be picked up and manipulated freely by the player.
	 */
	[RequireComponent (typeof (Rigidbody))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable___pick_up.html")]
	public class Moveable_PickUp : DragBase, iActionListAssetReferencer
	{

		#region Variables

		/** If True, the object can be rotated */
		public bool allowRotation = false;
		/** The maximum force magnitude that can be applied by the player - if exceeded, control will be removed */
		public float breakForce = 300f;
		/** If True, the object can be thrown */
		public bool allowThrow = false;
		/** How long a "charge" takes, if the object cen be thrown */
		public float chargeTime = 0.5f;
		/** How far the object is pulled back while chargine, if the object can be thrown */
		public float pullbackDistance = 0.6f;
		/** How far the object can be thrown */
		public float throwForce = 400f;
		/** If True, then Rigidbody constraints will be set automatically based on the interaction state */
		public bool autoSetConstraints = true;

		/** Where to locate interactions */
		public ActionListSource actionListSource = ActionListSource.InScene;
		/** The Interaction to run whenever the object is picked up by the player */
		public Interaction interactionOnGrab;
		/** The Interaction to run whenever the object is let go by the player (and actionListSource = ActionListSource.InScene) */
		public Interaction interactionOnDrop = null;
		/** The ActionListAsset to run whenever the object is grabbed by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnGrab = null;
		/** The ActionListAsset to run whenever the object is let go by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnDrop = null;
		/** The parameter ID to set as this object in the interactionOnGrab / actionListAssetOnGrab ActionLists */
		public int moveParameterID = -1;
		/** The parameter ID to set as this object in the interactionOnDrop / actionListAssetOnDrop ActionLists */
		public int dropParameterID = -1;

		/** The lift to give objects picked up, so that they aren't touching the ground when initially held */
		public float initialLift = 0.05f;

		protected bool isChargingThrow = false;
		protected float throwCharge = 0f;
		protected float chargeStartTime;
		protected bool inRotationMode = false;
		protected FixedJoint fixedJoint;
		protected Rigidbody fixedJointRigidbody;
		protected float originalDistanceToCamera;

		protected Vector3 worldMousePosition;
		protected Vector3 deltaMovement;
		protected LerpUtils.Vector3Lerp fixedJointLerp = new LerpUtils.Vector3Lerp ();

		protected Vector3 fixedJointOffset;

		#endregion


		#region UnityStandards

		protected override void Awake()
		{
			base.Awake ();

			if (_rigidbody == null)
			{
				ACDebug.LogWarning ("A Rigidbody component is required for " + name, this);
			}
		}


		protected override void Start ()
		{
			LimitCollisions ();
			base.Start ();
		}


		protected new void Update ()
		{
			if (!isHeld) return;

			if (allowThrow)
			{
				if (KickStarter.playerInput.InputGetButton ("ThrowMoveable"))
				{
					ChargeThrow ();
				}
				else if (isChargingThrow)
				{
					ReleaseThrow ();
				}
			}

			if (allowRotation)
			{
				if (KickStarter.playerInput.InputGetButton ("RotateMoveable"))
				{
					SetRotationMode (true);
				}
				else if (KickStarter.playerInput.InputGetButtonUp ("RotateMoveable"))
				{
					SetRotationMode (false);
					return;
				}

				if (KickStarter.playerInput.InputGetButtonDown ("RotateMoveableToggle"))
				{
					SetRotationMode (!inRotationMode);
					if (!inRotationMode)
					{
						return;
					}
				}
			}

			if (allowZooming)
			{
				UpdateZoom ();
			}
		}


		protected void LateUpdate ()
		{
			if (!isHeld || inRotationMode) return;

			worldMousePosition = GetWorldMousePosition ();

			Vector3 deltaPositionRaw = (worldMousePosition - fixedJointOffset - FixedJointPosition) * 100f;
			deltaMovement = Vector3.Lerp (deltaMovement, deltaPositionRaw, Time.deltaTime * 6f);
		}


		protected void OnCollisionEnter (Collision collision)
		{
			BaseOnCollisionEnter (collision);
		}
		
		
		protected void OnDestroy ()
		{
			if (fixedJoint)
			{
				Destroy (fixedJoint.gameObject);
				fixedJoint = null;
			}
		}

		#endregion


		#region PublicFunctions

		public override void UpdateMovement ()
		{
			base.UpdateMovement ();

			if (_rigidbody && moveSound && moveSoundClip && !inRotationMode)
			{
				if (numCollisions > 0)
			    {
					PlayMoveSound (_rigidbody.velocity.magnitude);
				}
				else if (moveSound.IsPlaying ())
				{
					moveSound.Stop ();
				}
			}
		}


		public override void Grab (Vector3 grabPosition)
		{
			inRotationMode = false;
			isChargingThrow = false;
			throwCharge = 0f;

			if (fixedJoint == null)
			{
				CreateFixedJoint ();
			}
			FixedJointPosition = grabPosition;
			fixedJointOffset = Vector3.zero;
			deltaMovement = Vector3.zero;

			_rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
			originalDistanceToCamera = (grabPosition - KickStarter.CameraMainTransform.position).magnitude;

			if (autoSetConstraints)
			{
				_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			}

			base.Grab (grabPosition);

			RunInteraction (true);
		}


		public override void LetGo (bool ignoreInteractions = false)
		{
			if (inRotationMode)
			{
				SetRotationMode (false);
			}

			if (autoSetConstraints)
			{
				_rigidbody.constraints = RigidbodyConstraints.None;
			}

			if (fixedJoint && fixedJoint.connectedBody)
			{
				fixedJoint.connectedBody = null;
			}

			_rigidbody.drag = originalDrag;
			_rigidbody.angularDrag = originalAngularDrag;

			if (inRotationMode)
			{
				_rigidbody.velocity = Vector3.zero;
			}
			else if (!isChargingThrow && !ignoreInteractions)
			{
				_rigidbody.AddForce (deltaMovement * Time.deltaTime / Time.fixedDeltaTime * 7f);
			}

			_rigidbody.useGravity = true;

			base.LetGo (ignoreInteractions);

			RunInteraction (false);
		}


		protected void RunInteraction (bool onGrab)
		{
			int parameterID = (onGrab) ? moveParameterID : dropParameterID;

			switch (actionListSource)
			{
				case ActionListSource.InScene:
					Interaction interaction = (onGrab) ? interactionOnGrab : interactionOnDrop;
					if (interaction && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onGrab || !KickStarter.actionListManager.IsListRunning (interaction))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = interaction.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
								}
							}

							interaction.Interact ();
						}
					}
					break;

				case ActionListSource.AssetFile:
					ActionListAsset actionListAsset = (onGrab) ? actionListAssetOnGrab : actionListAssetOnDrop;
					if (actionListAsset && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onGrab || !KickStarter.actionListAssetManager.IsListRunning (actionListAsset))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = actionListAsset.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
									if (GetComponent <ConstantID>())
									{
										parameter.intValue = GetComponent <ConstantID>().constantID;
									}
									else
									{
										ACDebug.LogWarning ("Cannot set the value of parameter " + parameterID + " ('" + parameter.label + "') as " + gameObject.name + " has no Constant ID component.", gameObject);
									}
								}
							}

							actionListAsset.Interact ();
						}
					}
					break;
			}
		}


		public override bool CanToggleCursor ()
		{
			if (isChargingThrow || inRotationMode)
			{
				return false;
			}
			return true;
		}


		public override void ApplyDragForce (Vector3 force, Vector3 _screenMousePosition, float _distanceToCamera)
		{
			distanceToCamera = _distanceToCamera;

			if (inRotationMode)
			{
				// Scale force
				force *= speedFactor * _rigidbody.drag * distanceToCamera * Time.deltaTime;
				
				// Limit magnitude
				if (force.magnitude > maxSpeed)
				{
					force *= maxSpeed / force.magnitude;
				}

				Vector3 newRot = Vector3.Cross (force, KickStarter.CameraMainTransform.forward);
				newRot /= Mathf.Sqrt ((grabPoint.position - Transform.position).magnitude) * 2.4f * rotationFactor;
				_rigidbody.AddTorque (newRot);
			}
			else
			{
				UpdateFixedJoint ();
			}
		}


		/**
		 * Unsets the FixedJoint used to hold the object in place
		 */
		public void UnsetFixedJoint ()
		{
			fixedJoint = null;
			isHeld = false;
		}

		#endregion


		#region ProtectedFunctions

		protected void ChargeThrow ()
		{
			if (!isChargingThrow)
			{
				isChargingThrow = true;
				chargeStartTime = Time.time;
				throwCharge = 0f;
			}
			else if (throwCharge < 1f)
			{
				throwCharge = (Time.time - chargeStartTime) / chargeTime;
			}

			if (throwCharge > 1f)
			{
				throwCharge = 1f;
			}
		}


		protected void ReleaseThrow ()
		{
			LetGo ();

			_rigidbody.useGravity = true;
			_rigidbody.drag = originalDrag;
			_rigidbody.angularDrag = originalAngularDrag;

			Vector3 moveVector = (Transform.position - KickStarter.CameraMainTransform.position).normalized;
			_rigidbody.AddForce (throwForce * throwCharge * moveVector);
		}
		
		
		protected void CreateFixedJoint ()
		{
			GameObject go = new GameObject (this.name + " (Joint)");
			go.transform.parent = transform;
			go.transform.localPosition = Vector3.zero;
			fixedJointRigidbody = go.AddComponent <Rigidbody>();
			fixedJointRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			fixedJointRigidbody.useGravity = false;
			fixedJointRigidbody.isKinematic = true;
			fixedJoint = go.AddComponent <FixedJoint>();
			fixedJoint.breakForce = fixedJoint.breakTorque = breakForce;

			go.AddComponent <JointBreaker>();
		}


		protected void SetRotationMode (bool on)
		{
			_rigidbody.velocity = Vector3.zero;
			_rigidbody.useGravity = !on;

			if (inRotationMode != on)
			{
				if (on)
				{
					KickStarter.playerInput.forceGameplayCursor = ForceGameplayCursor.KeepUnlocked;

					fixedJoint.connectedBody = null;
					_rigidbody.constraints = RigidbodyConstraints.None;
				}
				else
				{
					KickStarter.playerInput.forceGameplayCursor = ForceGameplayCursor.None;
					_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

					if (!KickStarter.playerInput.GetInGameCursorState ())
					{
						fixedJointOffset = GetWorldMousePosition () - FixedJointPosition;
						deltaMovement = Vector3.zero;
					}
				}
			}

			inRotationMode = on;
		}


		protected void UpdateFixedJoint ()
		{
			if (fixedJoint)
			{
				FixedJointPosition = fixedJointLerp.Update (FixedJointPosition, worldMousePosition - fixedJointOffset, 10f);
				
				if (!inRotationMode && fixedJoint.connectedBody != _rigidbody)
				{
					fixedJoint.connectedBody = _rigidbody;
				}
			}
		}


		protected new void UpdateZoom ()
		{
			float zoom = Input.GetAxis ("ZoomMoveable");

			if ((originalDistanceToCamera <= minZoom && zoom < 0f) || (originalDistanceToCamera >= maxZoom && zoom > 0f))
			{}
			else
			{
				originalDistanceToCamera += (zoom * zoomSpeed / 10f * Time.deltaTime);
			}

			originalDistanceToCamera = Mathf.Clamp (originalDistanceToCamera, minZoom, maxZoom);
		}


		protected Vector3 GetWorldMousePosition ()
		{
			Vector3 screenMousePosition = KickStarter.playerInput.GetMousePosition ();
			float alignedDistance = GetAlignedDistance (screenMousePosition);

			screenMousePosition.z = alignedDistance - (throwCharge * pullbackDistance);

			Vector3 pos = KickStarter.CameraMain.ScreenToWorldPoint (screenMousePosition);
			pos += Vector3.up * initialLift;

			return pos;
		}


		protected float GetAlignedDistance (Vector3 screenMousePosition)
		{
			screenMousePosition.z = 1f;
			Vector3 tempWorldMousePosition = KickStarter.CameraMain.ScreenToWorldPoint (screenMousePosition);

			float angle = Vector3.Angle (KickStarter.CameraMainTransform.forward, tempWorldMousePosition - KickStarter.CameraMainTransform.position);

			return originalDistanceToCamera * Mathf.Cos (angle * Mathf.Deg2Rad);
		}

		#endregion


		#region GetSet

		protected Vector3 FixedJointPosition
		{
			get
			{
				return fixedJoint.transform.position;
			}
			set
			{
				Vector3 origin = _rigidbody.position;
				Vector3 direction = value - origin;
				RaycastHit hit;
				if (Physics.Raycast (origin, direction, out hit, direction.magnitude))
				{
					if (hit.collider.gameObject != gameObject)
					{
						value = hit.point;
					}
				}

				fixedJointRigidbody.MovePosition (value);
			}
		}

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListSource == ActionListSource.AssetFile)
			{
				if (actionListAssetOnGrab == actionListAsset) return true;
				if (actionListAssetOnDrop == actionListAsset) return true;
			}
			return false;
		}

		#endif

	}

}