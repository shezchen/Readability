using UnityEngine;

namespace GamePlay.Events
{
    public record MoveInputEvent(Vector2 Direction, bool IsSprinting);

    public record SprintStateChangedEvent(bool IsSprinting);

    public record InteractEvent;

    public record LeftPageEvent;

    public record RightPageEvent;
}
