#region Usings

using System.Collections.Generic;
using UnityEngine;

#endregion

[ExecuteInEditMode]
public class TouchProcessor : MonoBehaviour {
    #region Delegates

    public delegate void EventTouch( TouchPhase t, Vector2 pos );
    private event EventTouch _eventTouch;

    #endregion

    #region Properties

    private const string _triggerName = "TouchProcessor";
    private readonly List<KeyValuePair<int, ITouchable>> _listeners = new List<KeyValuePair<int, ITouchable>>();

    private static TouchProcessor _instance;
    private ITouchable _touchObject;

    private const int MAX_COUNT_MULTI_TOUCH = 10;
    private ITouchable[] _touchableObjects = new ITouchable[ MAX_COUNT_MULTI_TOUCH ];
    private readonly Vector2[] _prevTouchPositions = new Vector2[ MAX_COUNT_MULTI_TOUCH ];

    private Vector2 _prevTouch = Vector2.zero;
    [SerializeField] private bool _multitouch;
    public bool Multitouch {
        get { return _multitouch; }
        set { _multitouch = value; }
    }
    private static int _fingerId = -1;
    private bool _mouseMoved;

    public static TouchProcessor Instance {
        get {
            if ( _instance == null ) {
                _instance = (TouchProcessor) FindObjectOfType( typeof (TouchProcessor) );
                if ( _instance == null ) {
                    GameObject go = new GameObject( _triggerName );
                    _instance = go.AddComponent<TouchProcessor>();
                }
                //GameObject.DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }
    public ITouchable CurrentITouchable {
        get { return _touchObject; }
    }
    public Vector2 LastTouchPosition {
        get { return _prevTouch; }
    }
    public EventTouch EventOneTouch {
        get { return _eventTouch; }
        set { _eventTouch = value; }
    }
    public List<KeyValuePair<int, ITouchable>> Listeners {
        get { return _listeners; }
    }

    #endregion

    #region Methods

    private static int Sort( KeyValuePair<int, ITouchable> a, KeyValuePair<int, ITouchable> b ) {
        return a.Key < b.Key ? 1 : a.Key == b.Key ? 0 : -1;
    }


    private void Sort() {
        Listeners.Sort( Sort );
    }

    private void OnApplicationQuit() {
        Destroy( gameObject );
    }

    private void Update() {
        UpdateTouch();
    }

    public void UpdateTouch() {
#if UNITY_EDITOR
        Vector2 touchPoint = GetTouchPoint( Input.mousePosition );
        TouchPhase phase = TouchPhase.Stationary;
        if ( Input.GetMouseButtonDown( 0 ) ) {
            _mouseMoved = true;
            phase = TouchPhase.Began;
            NotifyListeners( touchPoint, TouchPhase.Began, 0 );
            _prevTouch = touchPoint;
        } else if ( Input.GetMouseButton( 0 ) ) {
            float shift = Vector2.Distance( touchPoint, _prevTouch );
            if ( _mouseMoved && shift > Vector2.kEpsilon ) {
                phase = TouchPhase.Moved;
                NotifyListeners( touchPoint, TouchPhase.Moved, 0 );
                _prevTouch = touchPoint;
            }
        }
        if ( Input.GetMouseButtonUp( 0 ) ) {
            _mouseMoved = false;
            phase = TouchPhase.Ended;
            NotifyListeners( touchPoint, TouchPhase.Ended, 0 );
        }
        if ( _eventTouch != null &&
             phase != TouchPhase.Stationary ) {
            _eventTouch( phase, touchPoint );
        }
#else
			ReadTouch(Input.touches);
			#endif
    }

    public bool AddListener( ITouchable listener, int priority ) {
        if ( !Listeners.Exists( t => ( listener == t.Value ) ) ) {
            Listeners.Add( new KeyValuePair<int, ITouchable>( priority, listener ) );
            Listeners.Sort( Sort );
            return true;
        }
        return false;
    }

    public void ClearListeners() {
        _listeners.Clear();
    }

    public bool RemoveListener( ITouchable listener ) {
        KeyValuePair<int, ITouchable> kp = Listeners.Find( t => ( listener == t.Value ) );
        if ( Listeners.Remove( kp ) ) {
            Sort();
            return true;
        }
        return false;
    }

    private Vector2 GetTouchPoint( Vector2 screenPoint ) {
        return Camera.main.ScreenPointToRay( screenPoint ).origin;
    }

    private void NotifyListeners( Vector2 touchPoint, TouchPhase touchPhase, int fingerId ) {
        switch ( touchPhase ) {
            case TouchPhase.Began:
                if ( _touchObject != null ) {
                    return;
                }
                foreach ( var listener in Listeners ) {
                    if ( listener.Value.TouchBegan( touchPoint ) ) {
                        _touchObject = listener.Value;
                        return;
                    }
                }
                break;
            case TouchPhase.Moved:
                if ( _touchObject != null ) {
                    _touchObject.TouchMove( touchPoint );
                }
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if ( _touchObject != null ) {
                    _touchObject.TouchEnd( touchPoint );
                }
                _touchObject = null;
                break;
        }
    }

    private void NotifyListenersMult( Vector2 touchPoint, TouchPhase touchPhase, int fingerId ) {
        switch ( touchPhase ) {
            case TouchPhase.Began:
                if ( _touchableObjects[ fingerId ] != null ) {
                    return;
                }
                foreach ( var listener in Listeners ) {
                    bool isTouched = false;
                    for ( int i = 0; i < MAX_COUNT_MULTI_TOUCH; ++i ) {
                        if ( _touchableObjects[ fingerId ] == listener.Value ) {
                            isTouched = true;
                            break;
                        }
                    }
                    if ( isTouched ) {
                        continue;
                    }
                    if ( listener.Value.TouchBegan( touchPoint ) ) {
                        _touchableObjects[ fingerId ] = listener.Value;
                        return;
                    }
                }
                break;
            case TouchPhase.Moved:
                if ( _touchableObjects[ fingerId ] != null ) {
                    _touchableObjects[ fingerId ].TouchMove( touchPoint );
                }
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if ( _touchableObjects[ fingerId ] != null ) {
                    _touchableObjects[ fingerId ].TouchEnd( touchPoint );
                }
                _touchableObjects[ fingerId ] = null;
                break;
        }
    }

    private void ReadTouch( IEnumerable<Touch> touches ) {
        if ( !Multitouch ) {
            foreach ( Touch touch in touches ) {
                Vector2 touchPoint = GetTouchPoint( touch.position );
                NotifyListeners( touchPoint, touch.phase, _fingerId );
                if ( _eventTouch != null &&
                     touch.phase != TouchPhase.Stationary ) {
                    _eventTouch( touch.phase, touch.position );
                }
                ;
                _prevTouch = touchPoint;
                break;
            }
        } else {
            // multiTouch
            foreach ( Touch touch in touches ) {
                Vector2 touchPoint = GetTouchPoint( touch.position );
                NotifyListenersMult( touchPoint, touch.phase, touch.fingerId );
                if ( _eventTouch != null &&
                     touch.phase != TouchPhase.Stationary ) {
                    _eventTouch( touch.phase, touch.position );
                }
                ;
                _prevTouch = touchPoint;
                _prevTouchPositions[ touch.fingerId ] = touchPoint;
            }
        }
    }

    #endregion
}