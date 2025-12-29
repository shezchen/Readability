using UnityEngine;

namespace GamePlay.CharacterController.Events
{
    public record MoveInputEvent(Vector2 Direction, bool IsSprinting);
    public record SprintStateChangedEvent(bool IsSprinting);
    public record InteractEvent;
    public record BackpackToggleEvent;
}
