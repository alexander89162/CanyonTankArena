using UnityEngine;
using UnityEngine.InputSystem;

/*This component automatically animates tank treads for any unit it's 
attached to by updating scrollProgress in the shader. The target object
must have a material that uses the custom shader*/
public class TreadsAnimator : MonoBehaviour
{
    [Header("References")]
    public Renderer targetRenderer;

    [Header("Scroll Settings")]
    public float scrollSpeed = 0.025f;
    public Vector2 scrollDirection = new Vector2(1, 0);
    public float scrollProgress = 0; // value from 0-1 wrapping the tread animation
    public float scrollThreshold = 0.65f; // lower limit for y-coordinate of UVs that will be scrolled
    public InputAction moveAction;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 lastPosition;

    // Cache property IDs
    private static readonly int ScrollProgressID = Shader.PropertyToID("_ScrollProgress");
    private static readonly int ScrollDirectionID = Shader.PropertyToID("_ScrollDirection");
    private static readonly int ScrollThresholdID = Shader.PropertyToID("_ScrollThreshold");

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        
        propertyBlock = new MaterialPropertyBlock();
        lastPosition = transform.position;
    }

    void OnEnable()
    {
        if (moveAction != null)
            moveAction.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    void Update()
    {
        // TODO: possibly change scroll direction on each side to animate tank turning
        
        if (Time.deltaTime > 0)
        {
            Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
            float forward = Vector3.Dot(velocity, transform.forward) * scrollSpeed * Time.deltaTime;
            scrollProgress += forward;
            scrollProgress %= 1;
        }

        lastPosition = transform.position;
        ApplyScroll();
    }

    public void ApplyScroll()
    {
        targetRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(ScrollProgressID, scrollProgress);
        propertyBlock.SetVector(ScrollDirectionID, scrollDirection);
        propertyBlock.SetFloat(ScrollThresholdID, scrollThreshold);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetScroll(float scrollProgress)
    {
        this.scrollProgress = scrollProgress % 1;
    }
}