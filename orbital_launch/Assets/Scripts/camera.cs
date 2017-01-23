using UnityEngine;
using System.Collections;

public class camera : MonoBehaviour {

    public Transform targetLook;
    public Transform targetTrack;
    public Texture blackpixel;

    // Update is called once per frame
    void LateUpdate () {
	
		Vector3 temp = new Vector3 (targetLook.position.x, targetLook.position.y, -10);

		transform.position = temp;

		Vector3 direction = new Vector3 (targetTrack.transform.position.x, targetTrack.transform.position.y, 0.0f).normalized;
		bool onLeftSide = (Vector3.Dot (Vector3.Cross (direction, Vector3.up).normalized, Vector3.forward) > 0.0f) ? true : false;
		float angle = Mathf.Rad2Deg * Mathf.Acos (Vector3.Dot (direction, Vector3.up));
		
		if(onLeftSide)
		{
			angle = 360-angle;
		}
		angle = (angle + 360) % 360;
		
		transform.eulerAngles = new Vector3(0,0,angle);
    }

	void OnGUI()
	{
        if (Camera.current != null)
        {
            GUI.DrawTexture(new Rect(Camera.current.pixelRect.x, 0, 1, Camera.current.pixelHeight), blackpixel);
        }
    }
}
