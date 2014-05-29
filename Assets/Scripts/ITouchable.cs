#region Usings

using UnityEngine;

#endregion

public interface ITouchable {
    #region Properties

    bool IsTouchable { set; get; }

    #endregion

    #region Methods

    Rect GetTouchableBound();
    bool IsPointInBound( Vector2 point ); //point in touchable rect

    bool TouchBegan( Vector2 touchPoint );
    bool TouchMove( Vector2 touchPoint );
    void TouchEnd( Vector2 touchPoint );
    void TouchCancel( Vector2 touchPoint );

    #endregion
}