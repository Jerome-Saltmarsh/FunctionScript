using UnityEngine;

public class AdventureDemoPlayer : MonoBehaviour
{
    private const KeyCode MoveLeftKey = KeyCode.A;
    private const KeyCode MoveRightKey = KeyCode.D;
    private const KeyCode MoveUpKey = KeyCode.W;
    private const KeyCode MoveDownKey = KeyCode.S;
    private const KeyCode SpinKey = KeyCode.Space;
    private const float CameraSmooth = 0.035f;

    public AdventureDemo_Square square;
    private Camera camera;

    private void Start()
    {
        camera = FindObjectOfType<Camera>();

        float breathExpansion = 0.1f;
        
        Script breathScript = transform.loop()
            .expand(transform, 1.5f, Ease.InOutQuad, x: breathExpansion, y: breathExpansion)
            .wait(0.25f)
            .shrink(transform, 1.5f, Ease.InOutQuad, x: breathExpansion, y: breathExpansion)
            .wait(0.25f);
    }

    void Update()
    {
        handleKeyboardInput();
        updateCamera();
    }

    private void handleKeyboardInput()
    {
        MoveLeftKey.onHeld(square.moveLeft);
        MoveRightKey.onHeld(square.moveRight);
        MoveUpKey.onHeld(square.moveUp);
        MoveDownKey.onHeld(square.moveDown);
        SpinKey.onHeld(square.spin);
    }

    private void updateCamera()
    {
        Vector3 cameraPosition = camera.ScreenToWorldPoint(transform.position);
        float difference = cameraPosition.x - transform.position.x;
        cameraPosition.x -= difference * CameraSmooth;
        camera.transform.position = cameraPosition;
    }
}
