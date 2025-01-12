using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Telekinesis : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform camTrans;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private CharacterController controller;
    
    [Header("Raycast Info")]
    [SerializeField] private float rayCastWidth = 3;
    [SerializeField] private LayerMask telekinesisLayer;
    
    [Header("Grab Info")]
    [SerializeField] private float grabSpeed = 10;
    [SerializeField] private float maxDistanceFromPlayer = 20;
    [SerializeField] private float minDistanceFromPlayer = 5;
    [SerializeField] private float scrollChange = 1f;
    private float distanceFromPlayer;
    private bool grabbingObject = false;
    private Transform grabbedObject;
    private Rigidbody grabbedRb;

    [Header("Push Info")]
    [SerializeField] private float defaultForce = 100f;
    [SerializeField] private float maxForceMultiplier = 30;
    [SerializeField] private float forceChange = 1f;
    [SerializeField] private float tickTime = .03f;
    private float currentMultiplier = 1;
    private bool increaseMultiplier = false;
    private float lastTick;

    [Header("Rotate Info")] 
    [SerializeField] private float torqueForce = 10f;

    [Header("Raise Earth Info")]
    [SerializeField]
    private List<GameObject> earthObjects;

    [SerializeField]
    private LayerMask earthMask;

    [SerializeField]
    private float raiseTickTime = 0.05f;

    [SerializeField]
    private float raiseIncreaseAmount = 0.2f;

    private GameObject earthObjectToRaise;
    private float lastRaiseTick;
    private bool raisingEarth = false;
    private GameObject earthObj;
    private bool secondaryMode = false;

    [SerializeField]
    private Material previewMaterial;

    private GameObject previewInstance;

    public void OnPush(InputAction.CallbackContext context)
    {
        if (secondaryMode)
            return;
        
        if (context.started)
        {
            increaseMultiplier = true;
            currentMultiplier = 1;
            lastTick = Time.time;
            return;
        }

        if (!context.canceled)
            return;

        increaseMultiplier = false;

        Vector3 camPos = camTrans.position;
        Quaternion camRot = camTrans.rotation;
        Vector3 forward = camTrans.forward;

        if (grabbingObject)
        {
            PushGameObject(grabbedObject.gameObject, forward);
            return;
        }

        RaycastHit[] hits = new RaycastHit[10];
        int hitCount = Physics.BoxCastNonAlloc(camPos + (-forward * 2), new Vector3(rayCastWidth, rayCastWidth, rayCastWidth), forward, hits, camRot, maxDistanceFromPlayer, telekinesisLayer);

        for (int i = 0; i < hitCount; i++)
        {
            PushGameObject(hits[i].transform.gameObject, forward);
        }
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            grabbingObject = false;

            if (grabbedObject == null)
                return;

            Rigidbody rigid = grabbedObject.GetComponent<Rigidbody>();
            if (rigid == null)
                return;

            rigid.useGravity = true;
            return;
        }

        if (secondaryMode)
            return;

        if (!context.started)
            return;

        Vector3 camPos = camTrans.position;
        Quaternion camRot = camTrans.rotation;
        Vector3 forward = camTrans.forward;

        RaycastHit[] hits = new RaycastHit[10];
        int hitCount = Physics.BoxCastNonAlloc(camPos + (-forward * 2), new Vector3(rayCastWidth, rayCastWidth, rayCastWidth), forward, hits, camRot, maxDistanceFromPlayer, telekinesisLayer);

        if (hitCount <= 0)
            return;

        GameObject obj = hits[0].transform.gameObject;
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        grabbedObject = obj.transform;
        grabbedRb = rb;
        rb.useGravity = false;
        grabbingObject = true;
        distanceFromPlayer = Vector3.Distance(obj.transform.position, camTrans.position);
    }

    public void OnChangeDistance(InputAction.CallbackContext context)
    {
        if (secondaryMode)
            return;
        
        if (!context.started)
            return;

        Vector2 scrollInput = context.ReadValue<Vector2>();
        float amountChange = scrollChange;
        if (scrollInput.y < 0)
        {
            amountChange *= -1;
        }

        float newAmount = distanceFromPlayer + amountChange;
        if (newAmount > maxDistanceFromPlayer)
        {
            distanceFromPlayer = maxDistanceFromPlayer;
            return;
        }

        if (newAmount < minDistanceFromPlayer)
        {
            distanceFromPlayer = minDistanceFromPlayer;
            return;
        }

        distanceFromPlayer += amountChange;
    }

    public void OnRotateObject(InputAction.CallbackContext context)
    {
        if (!secondaryMode)
            return;
        
        if (!grabbingObject)
            return;

        if (!context.started)
            return;
        
        Vector2 scrollInput = context.ReadValue<Vector2>();
        Vector3 direction = Vector3.up;
        if (scrollInput.y < 0)
        {
            direction = Vector3.right;
        }
        
        grabbedRb.AddTorque(direction * torqueForce, ForceMode.Impulse);
    }

    public void OnRaiseEarth(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            raisingEarth = false;
            return;
        }

        if (!secondaryMode)
            return;

        if (!context.started)
            return;

        Vector3 camPos = camTrans.position;
        Vector3 forward = camTrans.forward;

        bool hit = Physics.Raycast(camPos, forward, out RaycastHit hitInfo, maxDistanceFromPlayer, earthMask);
        if (!hit)
            return;

        earthObj = CreateEarth(hitInfo, new Vector3(1, .1f, 1));
        raisingEarth = true;
        lastRaiseTick = Time.time;
    }

    public void OnRaiseEarthMode(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            secondaryMode = false;
            return;
        }

        if (context.started)
        {
            secondaryMode = true;
        }
    }

    public void OnChangeRaiseObject(InputAction.CallbackContext context)
    {
        if (!secondaryMode)
            return;
        
        if (!context.started)
            return;
        
        earthObjectToRaise = earthObjects[(earthObjects.IndexOf(earthObjectToRaise) + 1) % earthObjects.Count];
    }

    private void Start()
    {
        distanceFromPlayer = minDistanceFromPlayer;
        earthObjectToRaise = earthObjects[0];
    }

    private void Update()
    {
        UpdateGrabPosition();
        UpdatePushIntensity();
        UpdateRaiseEarth();
        UpdatePreview();
    }

    private void UpdateGrabPosition()
    {
        if (!grabbingObject)
            return;

        float speed = grabSpeed;
        if (controller.velocity.magnitude > 0)
        {
            speed += movement.GetCurrentSpeed();
        }

        float step = speed * Time.deltaTime;
        Vector3 targetPos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceFromPlayer));
        targetPos += Vector3.up * 0.9f;
        grabbedObject.position = Vector3.MoveTowards(grabbedObject.position, targetPos, step);
    }

    private void UpdatePushIntensity()
    {
        if (!increaseMultiplier)
            return;

        if (Time.time - lastTick < tickTime)
            return;

        lastTick = Time.time;

        float newAmount = currentMultiplier + forceChange;
        if (newAmount > maxForceMultiplier)
        {
            currentMultiplier = maxForceMultiplier;
            return;
        }

        currentMultiplier = newAmount;
    }

    private void UpdatePreview()
    {
        bool previewMode = secondaryMode;

        if (previewMode)
        {
            Vector3 camPos = camTrans.position;
            Vector3 forward = camTrans.forward;

            if (Physics.Raycast(camPos, forward, out RaycastHit hitInfo, maxDistanceFromPlayer, earthMask))
            {
                if (previewInstance == null)
                {
                    CreatePreview();
                }

                previewInstance.transform.position = hitInfo.point + Vector3.up * 0.003f;
                previewInstance.transform.localScale = earthObjectToRaise.transform.localScale;

                Renderer renderer = previewInstance.GetComponent<Renderer>();
                Material mat = renderer.material;

                if (earthObjectToRaise != null)
                {
                    SetPreviewShape(mat, earthObjectToRaise);
                }
                else
                {
                    mat.SetInt("_Shape", 1);
                    mat.SetVector("_Size", Vector2.one);
                }

                previewInstance.SetActive(true);
            }
            else
            {
                DestroyPreview();
            }
        }
        else
        {
            DestroyPreview();
        }
    }

    private void SetPreviewShape(Material mat, GameObject obj)
    {
        if (obj.GetComponent<SphereCollider>() != null || obj.GetComponent<CapsuleCollider>() != null)
        {
            float radius = obj.GetComponent<SphereCollider>() != null
                ? obj.GetComponent<SphereCollider>().radius
                : obj.GetComponent<CapsuleCollider>().radius;
            mat.SetInt("_Shape", 0); // Circle
            mat.SetFloat("_Radius", radius);
        }
        else if (obj.GetComponent<BoxCollider>() != null)
        {
            mat.SetInt("_Shape", 1); // Rectangle
            mat.SetVector("_Size", new Vector2(obj.transform.localScale.x, obj.transform.localScale.z));
        }
        else if (obj.GetComponent<MeshFilter>() != null)
        {
            mat.SetInt("_Shape", 1);
            mat.SetVector(
                "_Size",
                new Vector2(obj.GetComponent<MeshFilter>().mesh.bounds.size.x, obj.GetComponent<MeshFilter>().mesh.bounds.size.z));
        }
        else
        {
            mat.SetInt("_Shape", 1);
            mat.SetVector("_Size", Vector2.one);
        }
    }

    private void CreatePreview()
    {
        previewInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(previewInstance.GetComponent<MeshCollider>());
        previewInstance.GetComponent<Renderer>().material = previewMaterial;
        previewInstance.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    private void UpdateRaiseEarth()
    {
        if (!raisingEarth)
            return;

        if (Time.time - lastRaiseTick < raiseTickTime)
            return;

        lastRaiseTick = Time.time;
        Vector3 scale = earthObj.transform.localScale;
        earthObj.transform.localScale = new Vector3(scale.x, scale.y + raiseIncreaseAmount, scale.z);
    }

    private void PushGameObject(GameObject obj, Vector3 direction)
    {
        grabbingObject = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.useGravity = true;
        rb.AddForce(direction * (defaultForce * currentMultiplier), ForceMode.Force);
    }

    private GameObject CreateEarth(RaycastHit hitInfo, Vector3 scale)
    {
        GameObject cube = Instantiate(earthObjectToRaise);
        cube.transform.position = new Vector3(hitInfo.point.x, hitInfo.point.y + (scale.y / 2), hitInfo.point.z);
        cube.transform.localScale = scale;
        return cube;
    }
}