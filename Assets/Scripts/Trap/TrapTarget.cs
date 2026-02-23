using UnityEngine;

public interface TrapTarget
{
    void ApplyStun(float duration, float damage);

    void Capture(GameObject trapObject);
}
