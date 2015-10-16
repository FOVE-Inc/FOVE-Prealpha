using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

//based on UnityStandardAssets.ImageEffects

[ExecuteInEditMode]
[RequireComponent (typeof(Camera))]
public class BarrelDistortion : MonoBehaviour
{
	public bool editable = false;
	public bool useRenderTexture = true;

	[Header("Color/Lens Correction")]
	public float blueOffset = 8.0f;	// these numbers are large to be sane; the shader multiplies by 0.001
	public float redOffset = -6.6f;
	public float factor = 1.4f;
	public float gammaMod = 1.0f;

	[Tooltip("Does this camera draw the right eye's view?")]
	public bool right = false;

	[Header("Screen Center")]
	public float xCenter = 0.5f;
	public float yCenter = 0.5f;

	[Header("Barrel Shader")]
	public Shader barrelShader = null;
	public RenderTexture _rTex;

	private bool _isSupported = true;

	private Material _barrelMat = null;
	private Camera _camera;
	
	private Rect _leftCameraRect = new Rect(0.0f, 0.0f, 0.5f, 1.0f);
	private Rect _rightCameraRect = new Rect(0.5f, 0.0f, 0.5f, 1.0f);
	private Rect _fullCameraRect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

	void Start ()
	{
		InitRenderTexture ();
		_camera = GetComponent<Camera> ();
	}

	void InitRenderTexture() {
		_rTex = new RenderTexture (1792, 2016, 0);
		_rTex.useMipMap = false;
		_rTex.filterMode = FilterMode.Trilinear;
		_rTex.anisoLevel = 0;
		_rTex.format = RenderTextureFormat.ARGBHalf;
	}

	void Update ()
	{
		if (!editable) {
			return;
		}
		if (Input.GetKeyDown (KeyCode.A)) {
			if (Input.GetKey (KeyCode.LeftShift)) {
				blueOffset += 0.0001f;
			} else {
				blueOffset += 0.001f;
			}
		}
		if (Input.GetKeyDown (KeyCode.Z)) {
			if (Input.GetKey (KeyCode.LeftShift)) {
				blueOffset -= 0.0001f;
			} else {
				blueOffset -= 0.001f;
			}
		}
		if (Input.GetKeyDown (KeyCode.S)) {
			if (Input.GetKey (KeyCode.LeftShift)) {
				redOffset += 0.0001f;
			} else {
				redOffset += 0.001f;
			}
		}
		if (Input.GetKeyDown (KeyCode.X)) {
			if (Input.GetKey (KeyCode.LeftShift)) {
				redOffset -= 0.0001f;
			} else {
				redOffset -= 0.001f;
			}
		}
		if (Input.GetKeyDown (KeyCode.D)) {
			if (Input.GetKey (KeyCode.LeftShift)) {
				factor += 0.001f;
			} else {
				factor += 0.01f;
			}
		}
		if (Input.GetKeyDown (KeyCode.C)) {
			if (Input.GetKey (KeyCode.LeftShift)) {
				factor -= 0.001f;
			} else {
				factor -= 0.01f;
			}
		}
		if (Input.GetKeyDown (KeyCode.Return)) {
			Debug.Log ("Current shader values: \n" + 
			           "--- BLUE OFFSET: " + blueOffset + "\n" +
			           "--- RED  OFFSET: " + redOffset + "\n" +
			           "--- FACTOR: " + factor);
		}
	}

	void ReportNotSupported() {
		Debug.LogError("The image effect " + this.ToString() + " on "+this.name+" is not supported on this platform!");
		enabled = false;
	}

	private bool CheckSupport(bool needDepth)
	{
		if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures) 
		{
			ReportNotSupported();
			enabled = false;
			return false;
		}               
		
		if(needDepth && !SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth))
		{
			ReportNotSupported();
			enabled = false;
			return false;
		}
		
		return true;
	}

	void OnEnable() {
		_isSupported = true;
	}

	Material CheckShaderAndCreateMaterial (Shader s, Material m2Create) {
		if (!s) { 
			Debug.Log("Missing shader in " + this.ToString ());
			enabled = false;
			return null;
		}
		
		if (s.isSupported && m2Create && m2Create.shader == s) 
			return m2Create;
		
		if (!s.isSupported) {
			NotSupported ();
			Debug.Log("The shader " + s.ToString() + " on effect "+this.ToString()+" is not supported on this platform!");
			return null;
		}
		else {
			m2Create = new Material (s);    
			m2Create.hideFlags = HideFlags.DontSave;                
			if (m2Create) 
				return m2Create;
			else return null;
		}
	}

	void NotSupported () {
		enabled = false;
		_isSupported = false;
	}

	public bool CheckResources ()
	{
		CheckSupport (false);
		_barrelMat = CheckShaderAndCreateMaterial(barrelShader,_barrelMat);
		
		if (!_isSupported)
		{
			Debug.LogWarning ("The image effect " + this.ToString() + " has been disabled as it's not supported on the current platform.");
		}
		return _isSupported;
	}

	// If you put this on pre-render, it tends to crash when swapping
	// between render texture and the effects pipeline.
	void OnPostRender() {
		if (useRenderTexture) {
			if (_rTex == null) {
				InitRenderTexture ();
			}
			_camera.targetTexture = _rTex;
			_camera.rect = _fullCameraRect;
		} else {
			_camera.targetTexture = null;
			if (right) {
				_camera.rect = _rightCameraRect;
			} else {
				_camera.rect = _leftCameraRect;
			}
		}
	}

	// Called by the camera to apply the image effect
	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		if (CheckResources()==false)
		{
			// TODO: This would break and not render anything to screen currently
			Graphics.Blit (source, destination);
			return;
		}

		_barrelMat.SetFloat ("_blueOffset", blueOffset);
		_barrelMat.SetFloat ("_redOffset", redOffset);
		_barrelMat.SetFloat ("_Factor", factor);
		_barrelMat.SetFloat ("_xCenter", xCenter);
		_barrelMat.SetFloat ("_yCenter", yCenter);
		_barrelMat.SetFloat ("_gammaMod", gammaMod);

		if (useRenderTexture) {
			Graphics.Blit (source, _rTex, _barrelMat);
		} else {
			Graphics.Blit (source, destination, _barrelMat);
		}
	}

	void OnGUI ()
	{
		if (useRenderTexture) {
			// FYI: When rendering to texture, this effect needs to be last in the camera's hierarchy.
			if (!right) {
				GUI.DrawTexture (new Rect (0, 0, Screen.width / 2, Screen.height), _rTex, ScaleMode.ScaleAndCrop);
			} else {
				GUI.DrawTexture (new Rect (Screen.width / 2, 0, Screen.width / 2, Screen.height), _rTex, ScaleMode.ScaleAndCrop);
			}
		}
	}

}