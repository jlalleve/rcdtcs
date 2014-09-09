using UnityEngine;
using UnityEngine.UI;

//Automatic function for a slider to control text
public class TextSetFloat : MonoBehaviour {

	private Text m_Text;
	public bool m_AsInt;

	void Start () {
		m_Text = GetComponent<Text>();
        Slider slider = GetComponentInParent<Slider>();
        if (slider != null) {
            Set(slider.value);
        }
	}

	public void Set(float val){
		if (m_Text != null){
			if (m_AsInt){
				m_Text.text = ((int) val).ToString();
			}else{
				m_Text.text = val.ToString("0.00");
			}

		}
	}

}
