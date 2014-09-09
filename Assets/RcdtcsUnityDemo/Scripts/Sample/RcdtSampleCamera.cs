using UnityEngine;

//Basic sample camera controls
public class RcdtSampleCamera : MonoBehaviour {
	float m_YAngle = 0.0f;
	float m_XAngle = 0.0f;

	void Start () {
		m_YAngle = transform.rotation.eulerAngles.y;
		m_XAngle = - transform.rotation.eulerAngles.x;
	}

	void Update () {
		if (Input.GetButton("Camera Rotate")){
			float sensitivity = 8.0f;

			m_YAngle += Input.GetAxis("Mouse X") * sensitivity;
			m_XAngle += Input.GetAxis("Mouse Y") * sensitivity;

            m_XAngle = Mathf.Clamp(m_XAngle, -80.0f, 80.0f);

            Quaternion xQuaternion = Quaternion.AngleAxis(m_XAngle, Vector3.left);
            Quaternion yQuaternion = Quaternion.AngleAxis(m_YAngle, Vector3.up);

            transform.rotation = Quaternion.identity * yQuaternion * xQuaternion;
		}

		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");

		if (Mathf.Abs(v) > Mathf.Epsilon){
			transform.localPosition += transform.forward * Time.deltaTime * 8.0f * v;
		}
		if (Mathf.Abs(h) > Mathf.Epsilon){
			transform.localPosition += transform.right * Time.deltaTime * - 8.0f * - h;
		}
	}
}
