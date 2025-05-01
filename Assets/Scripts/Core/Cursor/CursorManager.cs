using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D standardCursor;
    [SerializeField] private Texture2D zoomInCursor;
    [SerializeField] private Texture2D zoomOutCursor;

    void Start()
    {
        //standard cursor
        Cursor.SetCursor(standardCursor, Vector2.zero, CursorMode.Auto);
    }

    public void zoom(bool zoomIn)
    {
        if (zoomIn)
        {
            Cursor.SetCursor(zoomInCursor, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(zoomOutCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    public void resetCursor()
    {
        Cursor.SetCursor(standardCursor, Vector2.zero, CursorMode.Auto);
    }
}
