using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace UnityEngine
{
	/// <summary>
	/// A controller interface to the FOVE SDK. One (and only one) should be attached to an object in each scene.
	/// 
	/// This class doubles as a static class to get the state of the headset (where the user is looking, head
	/// orientation and position, etc...), and also as a component controller for a game object.
	/// 
	/// As a GameObject component, this class constructs stereo eye cameras in the scene in its Awake method.
	/// In the absence of a template camera, it will construct suitable cameras on its own. However a template
	/// camera can be supplied if you want to be able to have control of what other behaviours may be on the created
	/// cameras. For instance, if you have a set of image filters, AA, bloom, depth of field and so on, you could
	/// create that camera as a prefab and then attach it as the reference camera. You may also use a camera that
	/// exists in the scene, in which case the camera will be duplicated and adjusted for each eye (and the original
	/// will be disabled).
	/// 
	/// You can completely override both cameras by creating them in the scene and setting this object's left and
	/// right camera properties directly, however this will override all automatic hierarchy management on thie
	/// object, so make sure that if you want those cameras to rotate with the headset that they are positioned as
	/// children of this component's GameObject in the scene hierarchy.
	/// 
	/// As a static interface, this class offers access to a variety of helper functions for getting Unity-compatible
	/// information such as the headset's orientation and position, as well as eye gaze and convenience functions for
	/// determining if the user is looking at a given collider.
	/// </summary>
	public class FOVEInterface : MonoBehaviour
	{
		/****************************************************************************************************\
		 * Interface and implementations of a FOVE state class to allow for the SDK to work even if no HMD	*
		 * is currently attached.
		\****************************************************************************************************/
		private interface IFOVEState
		{
			void DoUpdate();
			Quaternion GetRotation();
			Vector3 GetPosition();
			Vector3 GetLeftEyePoint();
			Vector3 GetRightEyePoint();
		}

		private class FOVEState_NoHMD : IFOVEState
		{
			Vector3 last_mouse;
			float rot_x, rot_y;
			Quaternion quat = Quaternion.identity;

			public void DoUpdate()
			{
				Vector3 mouse = Input.mousePosition;
				if (Input.GetMouseButtonDown(1))
				{
					last_mouse.x = mouse.x;
					last_mouse.y = mouse.y;
				}

				if (Input.GetMouseButton(1))
				{
					rot_x += mouse.x - last_mouse.x;
					rot_y += mouse.y - last_mouse.y;
					last_mouse = mouse;
				}
			}

			public Quaternion GetRotation()
			{
				quat = Quaternion.identity;
				quat *= Quaternion.Euler(0, rot_x, 0);
				quat *= Quaternion.Euler(-rot_y, 0, 0);

				return quat;
			}

			public Vector3 GetPosition()
			{
				return new Vector3(0.0f, 0.0f, 0.0f);
			}

			public Vector3 GetLeftEyePoint()
			{
				float half_screen_w = Screen.width / 2;

				Vector3 result = new Vector3();

				Vector3 mouse = Input.mousePosition;
				if (mouse.x < half_screen_w)
				{   // left eye
					result.x = mouse.x / half_screen_w;
				}
				else
				{   // right eye
					result.x = (mouse.x - half_screen_w) / half_screen_w;
				}
				result.y = mouse.y / Screen.height;

				return result;
			}

			public Vector3 GetRightEyePoint()
			{
				return GetLeftEyePoint();
			}
		}

		[Tooltip("Separation between pupils. Very small. Average for people is 0.065.")]
		[SerializeField]
		private float interpupilaryDistance = 0.06f;
		[Tooltip("Height (up-down) from base of head to eyes.")]
		[SerializeField]
		private float eyeHeight = 0.08f;
		[Tooltip("Depth (front-to-back) from base of head to eyes.")]
		[SerializeField]
		private float eyeForward = 0.04f;
		[Tooltip("ADVANCED: Use this prefab as a template for creating eye cameras. Useful if you want custom shaders, etc...")]
		[SerializeField]
		private FOVEEyeCamera eyePrototype = null;
		[Tooltip("EXPERT: Overrides the left eye. Will not automatically create a camera. You're on your own if you use this.")]
		[SerializeField]
		private Camera leftEyeOverride = null;
		[Tooltip("EXPERT: Overrides the right eye. Will not automatically create a camera. You're on your own if you use this.")]
		[SerializeField]
		private Camera rightEyeOverride = null;

		// Static members
		private static IFOVEState _f_state;
		private static bool _isStaticInitialized;
		private static bool _hasUpdatedData;
		private static FOVEInterface instance;

		// Locally stored data, prepped for sending to Unity
		private static Ray _eyeRayLeft, _eyeRayRight;
		private static Quaternion _headRotation;
		private static Vector3 _headPosition;
		private static GameObject _leftCameraObject, _rightCameraObject;
		private static Camera _leftCamera, _rightCamera;

		/****************************************************************************************************\
		 * Private utility functions																		*
		\****************************************************************************************************/
		private Camera SetupFoveViewCamera(float ipd, string identifier, out GameObject go)
		{
			GameObject temp = null;
			Camera cam = null;
			BarrelDistortion dist = null;

			if (eyePrototype == null)
			{
				temp = new GameObject();
				cam = temp.AddComponent<Camera>();
				dist = temp.AddComponent<BarrelDistortion>();

				cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
				cam.fieldOfView = 85.0f;
				cam.hdr = true;
			}
			else
			{
				FOVEEyeCamera ec = Instantiate(eyePrototype);
				dist = ec.GetComponent<BarrelDistortion>();
				temp = dist.gameObject;
				cam = dist.GetComponent<Camera>();
			}

			temp.name = string.Format("FOVE Eye ({0})", identifier);
			temp.transform.parent = transform;
			temp.transform.localPosition = new Vector3(ipd, eyeHeight, eyeForward);
			temp.transform.localRotation = UnityEngine.Quaternion.identity;

			//ShaderBlit sb = go.AddComponent<ShaderBlit> ();
			dist.right = identifier.ToLower().Contains("right");

			go = temp;
			return cam;
		}

		/****************************************************************************************************\
		 * GameObject lifecycle methods																		*
		\****************************************************************************************************/
		void Awake() // called before any Start methods
		{

			if (instance != null)
			{
				Debug.LogWarning("Another instance of FOVEInterface might be getting smashed by having multiple in one scene.");
			}
			instance = this;

			if (!_isStaticInitialized)
			{
				Debug.Log("No HMD detected, falling back to software emulation...");
				_f_state = new FOVEState_NoHMD();

				_isStaticInitialized = true;
			}

			if (leftEyeOverride != null)
			{
				_leftCamera = leftEyeOverride;
				_leftCameraObject = _leftCamera.gameObject;
			}
			else
			{
				_leftCamera = SetupFoveViewCamera(-interpupilaryDistance * 0.5f, "Left", out _leftCameraObject);
			}

			if (rightEyeOverride != null)
			{
				_rightCamera = rightEyeOverride;
				_rightCameraObject = _rightCamera.gameObject;
			}
			else
			{
				_rightCamera = SetupFoveViewCamera(interpupilaryDistance * 0.5f, "Right", out _rightCameraObject);
			}

			if (eyePrototype != null)
			{
				if (eyePrototype.gameObject.activeInHierarchy)
				{
					eyePrototype.gameObject.SetActive(false);
				}
			}
		}

		// Most functionality is implemnented in the CheckConcurrency method, which runs the first time
		// each frame that something tries to access FOVE data. This ensures that this object is always
		// updated before other objects which rely on its functioning.
		void Update()
		{
			CheckDataConcurrency();

			_f_state.DoUpdate();
		}

		void LateUpdate()
		{
			Debug.DrawRay(_eyeRayLeft.origin, _eyeRayLeft.direction, Color.yellow);

			Debug.Assert(_hasUpdatedData, "FOVEInstance data was not updated this frame.");
			// Flag CheckDataConcurrency to run again next frame
			_hasUpdatedData = false;
		}

		void OnApplicationQuit()
		{
			Debug.Log("Destroying FOVEInterface");
		}

		/****************************************************************************************************\
		 * Interface Structures
		\****************************************************************************************************/
		public struct EyeRays
		{
			public Ray left;
			public Ray right;

			public EyeRays(Ray l, Ray r)
			{
				left = l;
				right = r;
			}
		}

		/****************************************************************************************************\
		 * Static interface methods																			*
		\****************************************************************************************************/

		/// <summary>
		/// Ensure that the data we retrieve each frame has been updated in that frame. Sensor data changes	*
		/// so rapidly that data could change mid-frame, which could cause things to become inconsistent.	*
		/// We plan on returning predicted data for head rotation and position and eye position anyway, so	*
		/// updating mid-frame should't really offer any perceivable improvement.							*
		/// </summary>
		private static void CheckDataConcurrency()
		{
			// Skip this function if it's already been run this frame
			if (_hasUpdatedData)
			{
				return;
			}

			// TODO: position head is still being implemented
			Vector3 position = _f_state.GetPosition();
			_headPosition = position;
            instance.gameObject.transform.localPosition = _headPosition;

			// rotate head
			Quaternion quat = _f_state.GetRotation();
			_headRotation.x = (float)quat.x;
			_headRotation.y = (float)quat.y;
			_headRotation.z = (float)quat.z;
			_headRotation.w = (float)quat.w;
			instance.gameObject.transform.localRotation = _headRotation;

			// generate eye rays
			Vector3 eyePoint;
			eyePoint = _f_state.GetLeftEyePoint();
			_eyeRayLeft = _leftCamera.ViewportPointToRay(eyePoint);

			eyePoint = _f_state.GetRightEyePoint();
			_eyeRayRight = _rightCamera.ViewportPointToRay(eyePoint);

			_hasUpdatedData = true;
		}


		/// <summary>
		/// Get a set of Unity Ray objects which describe where in the scene each of the user's eyes are looking.
		/// 
		/// These rays are overwritten each frame, so you should not retain references to them across frames.
		/// </summary>
		/// <returns>The set of Unity Ray objects describing the user's eye gaze.</returns>
		public static EyeRays GetEyeRays()
		{
			CheckDataConcurrency();
			return new EyeRays(_eyeRayLeft, _eyeRayRight);
		}

		/// <summary>
		/// Get a reference to the camera used to render the left-eye view.
		/// This object remains consistent between frames unless deleted or the scene changes.
		/// </summary>
		/// <returns>The camera used to render the left-eye view.</returns>
		public static Camera GetLeftEyeCamera()
		{
			CheckDataConcurrency();
			return _leftCameraObject.GetComponent<Camera>();
		}

		/// <summary>
		/// Get a reference to the camera used to render the right-eye view.
		/// This object remains consistent between frames unless deleted or the scene changes.
		/// </summary>
		/// <returns>The camera used to render the right-eye view.</returns>
		public static Camera GetRightEyeCamera()
		{
			CheckDataConcurrency();
			return _rightCameraObject.GetComponent<Camera>();
		}

		/// <summary>
		/// Get the current HMD rotation as a Unity quaternion. This value is automatically applied to
		/// the interface's GameObject and is only exposed here for reference.
		/// </summary>
		/// <returns>The Unity quaterion used to orient the view cameras inside Unity.</returns>
		public static Quaternion GetHMDRotation()
		{
			CheckDataConcurrency();
			return _headRotation;
		}

		/// <summary>
		/// Get the current HMD position as a Unity Vector3. This value is automatically applied to
		/// the interface's GameObject and is only exposed here for reference.
		/// </summary>
		/// <returns>The Unity Vector3 used to position the view caperas inside Unity.</returns>
		public static Vector3 GetHMDPosition()
		{
			CheckDataConcurrency();
			return _headPosition;
		}

		/// <summary>
		/// Performs a collider raycast (this is likely more efficient than doing a dumb raycast
		/// and checking to see if any of the hit objects are the one you're looking for) to determine
		/// if the user is looking at this collider.
		/// </summary>
		/// <param name="col">The collider to check against the user's gaze.</param>
		/// <returns>Whether or not the referenced collider is being looked at.</returns>
		public static bool IsLookingAtCollider(Collider col)
		{
			CheckDataConcurrency();
			if (_eyeRayLeft.origin == _eyeRayLeft.direction)
			{
				return false;
			}
			RaycastHit hit;
			if (col.Raycast(_eyeRayLeft, out hit, 1000))
			{
				return true;
			}

			// Right eye disabled for now
			//		if (col.Raycast (_eyeRayRight, out hit, 1000))
			//		{
			//			return true;
			//		}

			return false;
		}


		public enum Eye
		{
			Left,
			Right
		}
		/// <summary>
		/// Returns the position of the supplied Vector3 in normalized viewport space for whichever
		/// eye is specified. This is a convenience function wrapping Unity's built-in
		/// Camera.WorldToViewportPoint without the need to acquire references to each camera by hand.
		/// 
		/// In most cases, it is sufficient to query only one eye at a time, however both are accessible
		/// for advanced use cases.
		/// </summary>
		/// <param name="pos">The position in 3D world space to project to viewport space.</param>
		/// <param name="eye">Which Fove.Eye (Fove.Eye.Left or Fove.Eye.Right) to project onto.</param>
		/// <returns></returns>
		public static Vector3 GetNormalizedViewportPosition(Vector3 pos, Eye eye)
		{
			if (eye == Eye.Left)
			{
				return _leftCamera.WorldToViewportPoint(pos);
			}
			if (eye == Eye.Right)
			{
				return _rightCamera.WorldToViewportPoint(pos);
			}
			return new Vector3(0, 0, 0);
		}
	}
}
 