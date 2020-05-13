using UnityEngine;

public class AdventureDemo_Square : MonoBehaviour
{

    private Script currentAction;
    
    private const float MovementDuration = 0.15f;
    private const Ease MovementEase = Ease.InQuart;
    private const Ease SpinEase = Ease.InExpo;
    private const float FloatSpinDuration = 1f;
    public const float MovementDistance = 1f;

    public void moveLeft()
    {
        move(-1, 0);
    }

    public void moveRight()
    {
        move(1, 0);
    }

    public void moveUp()
    {
        move(0, 1);
    }

    public void moveDown()
    {
        move(0, -1);
    }

    public void spin()
    {
        if(busy) return;

        float angle = Random.value > 0.5f ? 360 : -360; // randomly get the direction to spin in
        
        currentAction = this.script()
            .rotate(transform, angle, FloatSpinDuration, SpinEase)
            .perform(() => currentAction = null);
    }

    private void move(int x, int y)
    {
        if(busy) return;
        
        currentAction = this.script()
            .translate(transform, MovementDuration, MovementEase, x: x * MovementDistance, y:y * MovementDistance)
            .perform(() => currentAction = null);
    }

    public void moveTowards(Vector3 position)
    {
        if (busy || arrivedAt(position)) return;
        
        Vector3 difference = position - transform.position;
        if (difference.x.abs() > difference.y.abs())
        {
            difference.x.isNegative().then(moveLeft, moveRight);
        }
        else
        {
            difference.y.isNegative().then(moveDown, moveUp);
        }
    }

    public bool arrivedAt(Vector3 position)
    {
        return transform.distanceFrom(position) < MovementDistance;
    }
    
    private bool busy => currentAction != null;
}
